namespace Automatou.Simulation;

public sealed record BattleEffect(Hex From, Hex To, bool Hit, int Damage);

public sealed partial class World {
    public World Copy() {
        World copy = new() {
            Settings = this.Settings with { }, Turn = this.Turn, NextId = this.NextId,
            RandomState = this.RandomState, Casualties = this.Casualties
        };
        foreach (KeyValuePair<Hex, Terrain> cell in this.Terrain) { copy.Terrain.Add(cell.Key, cell.Value); }
        copy.Entities.AddRange(this.Entities.Select(entity => entity.Copy()));
        copy.Events.AddRange(this.Events);
        copy.Effects.AddRange(this.Effects);
        copy.RebuildOccupancy();
        // Event subscribers belong to the live workspace, never to a saved copy.
        return copy;
    }

    public SimulationSettings Settings { get; set; } = new();
    public Dictionary<Hex, Terrain> Terrain { get; } = [];
    public List<Entity> Entities { get; } = [];
    public List<string> Events { get; } = [];
    public List<BattleEffect> Effects { get; } = [];
    public int Turn { get; set; }
    public int NextId { get; set; } = 1;
    public uint RandomState { get; set; } = 72491;
    public int Casualties { get; set; }
    public Action<string>? EventRecorded { get; set; }
    private readonly Dictionary<Hex, Entity> occupancy = [];
    public Entity? At(Hex cell) {
        return this.occupancy.GetValueOrDefault(cell);
    }

    public void RebuildOccupancy() {
        this.occupancy.Clear();
        foreach (Entity entity in this.Entities) {
            foreach (Hex cell in entity.OccupiedCells()) {
                if (!this.occupancy.TryAdd(cell, entity)) {
                    throw new InvalidDataException("Overlapping entities in world.");
                }
            }
        }
    }
    public void Note(string message) {
        this.Events.Add($"{this.Turn:0000}  {message}");
        this.EventRecorded?.Invoke(this.Events[^1]);
        if (this.Events.Count > 100) {
            this.Events.RemoveAt(0);
        }
    }
    public static World Create(bool hexagonal, int width, int height) {
        // Bound width before the quadratic hexagon calculation can overflow long.
        if (width < 1 || height < 1 || width > 20000 || (hexagonal ? 1L + (3L * width * (width - 1)) : (long)width * height) > 20000) {
            throw new ArgumentException("Use positive dimensions with at most 20,000 cells.");
        }

        World world = new();
        IEnumerable<Hex> cells = hexagonal ? Hex.Disk(width) : Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x => Hex.FromOffset(x, y)));
        foreach (Hex cell in cells) {
            world.Terrain.Add(cell, Simulation.Terrain.Plains);
        }
        return world;
    }
    public bool CanOccupy(Entity entity, Hex position, out string reason) {
        IEnumerable<Hex> cells = Hex.Disk(entity.Unit.Size).Select(c => position + c);
        foreach (Hex cell in cells) {
            if (!this.Terrain.TryGetValue(cell, out Terrain terrain)) { reason = "The footprint extends beyond the world."; return false; }
            if (this.At(cell) is { } occupant && occupant != entity) { reason = "The footprint overlaps another entity."; return false; }
            if (!Traversable(entity.Unit, terrain)) { reason = $"This unit cannot occupy {Catalog.TerrainNames[(int)terrain].ToLowerInvariant()}."; return false; }
        }
        reason = ""; return true;
    }
    public static bool Traversable(Unit unit, Terrain terrain) {
        return terrain != Simulation.Terrain.ExclusionZone && unit.Mobility switch {
            Mobility.Spaceflight => true,
            Mobility.Flight => true,
            Mobility.Amphibious => terrain != Simulation.Terrain.Mountain,
            Mobility.Ground => terrain is not (Simulation.Terrain.Water or Simulation.Terrain.Mountain),
            _ => terrain is not (Simulation.Terrain.Water or Simulation.Terrain.Mountain)
        };
    }

    public bool Add(Entity entity, out string reason) {
        if (this.Entities.Contains(entity)) {
            reason = "This entity is already in the world.";
            return false;
        }
        ArgumentNullException.ThrowIfNull(entity.Unit);
        entity.Unit.Validate();
        if (!this.CanOccupy(entity, entity.Position, out reason)) {
            return false;
        }

        entity.Id = this.NextId++; entity.Health = entity.MaximumHealth;
        this.Entities.Add(entity); this.RebuildOccupancy(); return true;
    }
    public void Remove(Entity entity) {
        _ = this.Entities.Remove(entity); this.RebuildOccupancy();
    }
    public bool Paint(Hex cell, Terrain terrain) {
        if (!this.Terrain.ContainsKey(cell)) {
            return false;
        }

        if (this.At(cell)?.Unit is { } unit && !Traversable(unit, terrain)) {
            return false;
        }

        this.Terrain[cell] = terrain; return true;
    }
    private int RandomPercent() {
        uint state = this.RandomState;
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        this.RandomState = state == 0 ? 72491u : state;
        return (int)(this.RandomState % 100);
    }
    public static int Separation(Entity a, Entity b) {
        return a.OccupiedCells().Min(c => b.OccupiedCells().Min(c.Distance));
    }

    public static Hex AimCell(Hex from, Entity target) {
        return target.OccupiedCells().MinBy(c => c.Distance(from));
    }

    public static bool InAttackArc(Entity a, Entity b) {
        return Hex.TurnDistance(a.Facing, a.Position.DirectionTo(AimCell(a.Position, b))) <= 1;
    }

    public bool CanAttack(Entity a, Entity b) {
        return a.Faction != b.Faction && (!this.Settings.HeatEnabled || !a.WeaponLocked) &&
            this.CanObserve(a, b) && Separation(a, b) <= a.Unit.Range && InAttackArc(a, b);
    }

    public static int ArmorAgainst(Entity defender, Hex attacker) {
        int difference = Hex.TurnDistance(defender.Facing, defender.Position.DirectionTo(attacker));
        return difference == 0 ? defender.Unit.Armor : difference == 1 ? defender.Unit.Armor * 2 / 3 : defender.Unit.Armor / 4;
    }
    public void Attack(Entity attacker, Entity defender) {
        if (!this.CanAttack(attacker, defender)) {
            return;
        }

        attacker.Unit.AutomatonInstance.State.ShotsFired++;
        if (this.Settings.HeatEnabled && attacker.Unit.HeatPerShot > 0) {
            attacker.Heat = Math.Min(200, attacker.Heat + attacker.Unit.HeatPerShot);
            if (attacker.Heat >= 100 && !attacker.WeaponLocked) {
                attacker.WeaponLocked = true;
                this.Note($"{attacker.Name} overheated; weapon locked until heat falls to 40.");
            }
        }
        bool hit = this.RandomPercent() >= (defender.Stationary ? 0 : defender.Unit.Evasion);
        bool melee = Separation(attacker, defender) <= 1;
        int power = melee ? attacker.Unit.MeleeDamage : attacker.Unit.Damage;
        int damage = Math.Max(1, power - ArmorAgainst(defender, attacker.Position));
        Hex impact = AimCell(attacker.Position, defender);
        this.Effects.Add(new(attacker.Position, impact, hit, hit ? damage : 0));
        if (!hit) { this.Note($"{defender.Name} evaded {attacker.Name}."); return; }
        Entity[] victims = !melee && attacker.Unit.BlastRadius > 0
            ? this.Entities.Where(e => e.OccupiedCells().Any(c => c.Distance(impact) <= attacker.Unit.BlastRadius)).ToArray()
            : [defender];
        foreach (Entity? victim in victims) {
            int inflicted = Math.Max(1, power - ArmorAgainst(victim, attacker.Position));
            victim.Health -= inflicted;
            this.Note($"{attacker.Name} → {victim.Name}  −{inflicted}");
            if (victim.Health <= 0) { this.Note($"{victim.Name} destroyed."); this.Casualties++; _ = this.Entities.Remove(victim); }
        }
        this.RebuildOccupancy();
    }
    public void Step() {
        // One world turn: cool everyone, then let each surviving actor spend its whole
        // budget in sequence. Later actors see changes made by earlier actors this turn.
        this.Turn++; this.Effects.Clear();
        if (this.Settings.HeatEnabled) {
            foreach (Entity entity in this.Entities) {
                int cooling = entity.Unit.CoolingPerTurn;
                entity.Heat = Math.Max(0, entity.Heat - cooling);
                if (entity.WeaponLocked && entity.Heat <= 40) {
                    entity.WeaponLocked = false;
                    this.Note($"{entity.Name} cooled; weapon ready.");
                }
            }
        }

        Entity[] order = this.Entities.OrderBy(e => e.Id).ToArray();
        if (order.Length == 0) {
            return;
        }
        // Rotate first actor each turn, avoiding a permanent first-faction advantage.
        order = [.. order.Skip(this.Turn % order.Length), .. order.Take(this.Turn % order.Length)];
        foreach (Entity? actor in order) {
            if (!this.Entities.Contains(actor)) {
                continue;
            }

            UnitSenses senses = new(this, actor);
            // A rejected request ends this turn. Successful actions always consume points,
            // so even an automaton yielding endlessly cannot exceed its action budget.
            using IEnumerator<UnitAction> actions = actor.Unit.AutomatonInstance.Act(senses).GetEnumerator();
            while (senses.RemainingPoints > 0 && this.Entities.Contains(actor) && actions.MoveNext()) {
                if (!this.ApplyAction(actor, senses, actions.Current)) {
                    break;
                }
            }
        }
    }
    private bool ApplyAction(Entity actor, UnitSenses senses, UnitAction action) {
        // The world enforces legality independently of the automaton: at most one attack
        // (2 points), each turn step (1 point), or each forward move (terrain cost).
        // false stops this actor's turn; unused points are not carried into the next turn.
        switch (action) {
        case AttackAction attack:
            Entity? target = this.Entities.FirstOrDefault(e => e.Id == attack.TargetId);
            if (senses.HasAttacked || senses.RemainingPoints < 2 || target is null || !this.CanAttack(actor, target)) {
                return false;
            }

            this.Attack(actor, target);
            senses.RemainingPoints -= 2; senses.HasAttacked = true;
            return true;
        case TurnAction turn:
            if (actor.Stationary || turn.Direction is not (-1 or 1)) {
                return false;
            }

            actor.Facing = (actor.Facing + turn.Direction + 6) % 6;
            senses.RemainingPoints--;
            return true;
        case MoveForwardAction:
            if (actor.Stationary) {
                return false;
            }

            Hex next = actor.Position + Hex.Directions[actor.Facing];
            if (!this.CanOccupy(actor, next, out _)) {
                return false;
            }

            bool rough = this.Terrain[next] == Simulation.Terrain.Forest;
            int cost = this.MovementCost(actor, next);
            if (senses.RemainingPoints < cost) {
                return false;
            }

            actor.Position = next; senses.RemainingPoints -= cost;
            if (rough && actor.Faction == Faction.InfantryAndArtillery) {
                actor.Health -= 2;
            }

            if (actor.Health <= 0) { _ = this.Entities.Remove(actor); this.Casualties++; this.Note($"{actor.Name} lost crossing rough terrain."); }
            this.RebuildOccupancy();
            return true;
        default:
            return false;
        }
    }
    public static World Demonstration() {
        // Fixed encounter terrain, including the original starting units' clearings.
        // Rows run from bottom (Y = 0) to top; columns run left to right.
        string[] rows = [
            "PPPPPPPWWPPPPPPPPPPPPPPWWWPPPPPPPP",
            "PPPPPWWWWWPPPPPPPPPPPPPWWWWPPPPPPP",
            "PPPPPWWWWWWPPPPPPPPPPPWWWWWPPPPPPP",
            "PPPPPWPPPWWPPPPPPPPPPPPWWWWPPPPPPP",
            "PPPPPPPPPPPPPPPPPPPPPPPWWWPPPPPPPP",
            "FPPPPPPPPPPPPPFFPFPPPPPPPPPPPPPFFF",
            "FFFPPPPPPPPPPFFFFFFFPPPPPPPPPFFFFF",
            "MFFFPPPPPPPPFFFMMMFFFPPPPPPPFFFMMM",
            "MMFFFPPPPPPFFFMMMMMFFPPPPPPPFPPMMM",
            "MMFFPPPPPPPFPMMMMMMFFFPPPPPFPPPMMM",
            "MMFFFPPPPPPFFFMMMMFFFPPPPPPPFPPMMM",
            "PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP",
            "PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP",
            "PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP",
            "PPPPPPWWWWPPPPPPPPPPPPWWWWWPPPPPPP",
            "PPPPPWWWWWWPPPPPPPPPPPWPWWWPPPPPPP",
            "PPPPPPPWWWWPPPPPPPPPPPWWWPWPPPPPPP",
            "PPPPPPPWWWPPPPPPPPPPPPWWPPPPPPPPPP",
            "FPPPPPPPPPPPPPPFFPPPPPPPPPPPPPPPFF",
            "FFPPPPPPPPPPPFFFFFFPPPPPPPPPPPFFFF",
            "FFFFPPPPPPPPFFFFMFFFPPPPPPPPPFFFMF",
            "MMFFFPPPPPPFFFMMMMFFFPPPPPPPFFMMMM",
            "MMFFFPPPPPPFFMMMMMMFFFPPPPPFFFMMMM",
            "MMFFFPPPPPPFFFMMMMMFFPPPPPPPFFMMMM"
        ];
        World world = new();
        for (int y = 0; y < rows.Length; y++) {
            for (int x = 0; x < rows[y].Length; x++) {
                world.Terrain.Add(Hex.FromOffset(x, y), rows[y][x] switch {
                    'P' => Simulation.Terrain.Plains,
                    'F' => Simulation.Terrain.Forest,
                    'M' => Simulation.Terrain.Mountain,
                    'W' => Simulation.Terrain.Water,
                    _ => throw new InvalidOperationException("Unknown encounter terrain.")
                });
            }
        }
        List<Unit> designs = Catalog.Units();
        void Place(int index, Faction faction, int x, int y, int facing) {
            Entity entity = new() { Unit = designs[index].CreateFresh(), Faction = faction, Position = Hex.FromOffset(x, y), Facing = facing };
            _ = world.Add(entity, out _);
        }
        Place(0, Faction.Bastions, 5, 17, 5); Place(0, Faction.Bastions, 7, 20, 5);
        Place(2, Faction.Travelers, 7, 5, 1);
        for (int i = 0; i < 5; i++) {
            Place(1, Faction.Travelers, 4 + (i * 2), 9, 1);
        }

        Place(3, Faction.MechAndTank, 26, 6, 2); Place(3, Faction.MechAndTank, 29, 9, 2);
        for (int i = 0; i < 8; i++) {
            Place(4, Faction.InfantryAndArtillery, 16 + (i % 4 * 2), 3 + (i / 4 * 2), 1);
        }

        Place(5, Faction.InfantryAndArtillery, 16, 1, 1); Place(5, Faction.InfantryAndArtillery, 22, 1, 1);
        Place(7, Faction.Prytu, 25, 19, 4);
        for (int i = 0; i < 7; i++) {
            Place(6, Faction.Prytu, 19 + (i * 2), 15 + (i % 2), 3);
        }

        return world;
    }
}
