namespace Automatou.Simulation;

public sealed record BattleEffect(Hex From, Hex To, bool Hit, int Damage);

public sealed class World
{
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
    public Entity? At(Hex cell) => occupancy.GetValueOrDefault(cell);
    public void RebuildOccupancy()
    {
        occupancy.Clear();
        foreach (var entity in Entities)
            foreach (var cell in entity.OccupiedCells())
                if (!occupancy.TryAdd(cell, entity)) throw new InvalidDataException("Overlapping entities in world.");
    }
    public void Note(string message)
    {
        Events.Add($"{Turn:0000}  {message}");
        EventRecorded?.Invoke(Events[^1]);
        if (Events.Count > 100) Events.RemoveAt(0);
    }
    public static World Create(bool hexagonal, int width, int height, bool generated, int seed = 72491)
    {
        if (width < 1 || height < 1 || (hexagonal ? 1L + 3L * width * (width - 1) : (long)width * height) > 20000)
            throw new ArgumentException("Use positive dimensions with at most 20,000 cells.");
        var world = new World { RandomState = (uint)Math.Max(seed, 1) };
        IEnumerable<Hex> cells = hexagonal ? Hex.Disk(width) : Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x => Hex.FromOffset(x, y)));
        foreach (var cell in cells)
        {
            var terrain = Simulation.Terrain.Plains;
            if (generated)
            {
                double ridge = Math.Sin(cell.X * .38 + seed) + Math.Cos(cell.Y * .47 + seed * .17);
                terrain = ridge > 1.5 ? Simulation.Terrain.Mountain : ridge < -1.45 ? Simulation.Terrain.Water : ridge > .55 ? Simulation.Terrain.Forest : ridge < -.7 ? Simulation.Terrain.Wetlands : Simulation.Terrain.Plains;
                if (Math.Abs(cell.Y - height / 2) <= 1) terrain = Simulation.Terrain.Paved;
            }
            world.Terrain.Add(cell, terrain);
        }
        return world;
    }
    public bool CanOccupy(Entity entity, Hex position, int facing, out string reason)
    {
        var cells = entity.Unit is not null ? Hex.Disk(entity.Unit.Size).Select(c => position + c) : entity.Building!.Cells.Select(c => position + c.Rotate(facing));
        foreach (var cell in cells)
        {
            if (!Terrain.TryGetValue(cell, out var terrain)) { reason = "The footprint extends beyond the world."; return false; }
            if (At(cell) is { } occupant && occupant != entity) { reason = "The footprint overlaps another entity."; return false; }
            if (entity.Unit is not null && !Traversable(entity.Unit, terrain)) { reason = $"This unit cannot occupy {Catalog.TerrainNames[(int)terrain].ToLowerInvariant()}."; return false; }
        }
        reason = ""; return true;
    }
    public static bool Traversable(UnitDesign unit, Terrain terrain) => terrain != Simulation.Terrain.ExclusionZone && unit.Mobility switch
    {
        Mobility.Spaceflight => true,
        Mobility.Flight => terrain != Simulation.Terrain.Space,
        Mobility.Amphibious => terrain is not (Simulation.Terrain.Air or Simulation.Terrain.Space or Simulation.Terrain.Mountain),
        _ => terrain is not (Simulation.Terrain.Water or Simulation.Terrain.Air or Simulation.Terrain.Space or Simulation.Terrain.Mountain)
    };
    public bool Add(Entity entity, out string reason)
    {
        entity.Unit?.Validate(); entity.Building?.Validate();
        if ((entity.Unit is null) == (entity.Building is null)) throw new ArgumentException("Choose one entity type.");
        if (!CanOccupy(entity, entity.Position, entity.Facing, out reason)) return false;
        entity.Id = NextId++; entity.Health = entity.MaximumHealth;
        Entities.Add(entity); RebuildOccupancy(); return true;
    }
    public void Remove(Entity entity)
    {
        Entities.Remove(entity); RebuildOccupancy();
    }
    public bool Paint(Hex cell, Terrain terrain)
    {
        if (!Terrain.ContainsKey(cell)) return false;
        if (At(cell)?.Unit is { } unit && !Traversable(unit, terrain)) return false;
        Terrain[cell] = terrain; return true;
    }
    private int RandomPercent()
    {
        uint state = RandomState;
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        RandomState = state == 0 ? 72491u : state;
        return (int)(RandomState % 100);
    }
    public int Separation(Entity a, Entity b) => a.OccupiedCells().Min(c => b.OccupiedCells().Min(c.Distance));
    public static Hex AimCell(Hex from, Entity target) => target.OccupiedCells().MinBy(c => c.Distance(from));
    public bool InAttackArc(Entity a, Entity b) => Hex.TurnDistance(a.Facing, a.Position.DirectionTo(AimCell(a.Position, b))) <= 1;
    public bool CanAttack(Entity a, Entity b) => a.Unit is not null && a.Faction != b.Faction && Separation(a, b) <= a.Unit.Range && InAttackArc(a, b);
    public int ArmorAgainst(Entity defender, Hex attacker)
    {
        if (defender.Unit is null) return 4;
        int difference = Hex.TurnDistance(defender.Facing, defender.Position.DirectionTo(attacker));
        return difference == 0 ? defender.Unit.Armor : difference == 1 ? defender.Unit.Armor * 2 / 3 : defender.Unit.Armor / 4;
    }
    public void Attack(Entity attacker, Entity defender)
    {
        if (!CanAttack(attacker, defender)) return;
        bool hit = RandomPercent() >= (defender.Stationary ? 0 : defender.Unit?.Evasion ?? 0);
        bool melee = Separation(attacker, defender) <= 1;
        int power = melee ? attacker.Unit!.MeleeDamage : attacker.Unit!.Damage;
        int damage = Math.Max(1, power - ArmorAgainst(defender, attacker.Position));
        Hex impact = AimCell(attacker.Position, defender);
        Effects.Add(new(attacker.Position, impact, hit, hit ? damage : 0));
        if (!hit) { Note($"{defender.Name} evaded {attacker.Name}."); return; }
        var victims = !melee && attacker.Unit.BlastRadius > 0
            ? Entities.Where(e => e.OccupiedCells().Any(c => c.Distance(impact) <= attacker.Unit.BlastRadius)).ToArray()
            : [defender];
        foreach (var victim in victims)
        {
            int inflicted = Math.Max(1, power - ArmorAgainst(victim, attacker.Position));
            victim.Health -= inflicted;
            Note($"{attacker.Name} → {victim.Name}  −{inflicted}");
            if (victim.Health <= 0) { Note($"{victim.Name} destroyed."); Casualties++; Entities.Remove(victim); }
        }
        RebuildOccupancy();
    }
    public void Step()
    {
        Turn++; Effects.Clear();
        var order = Entities.Where(e => e.Unit is not null).OrderBy(e => e.Id).ToArray();
        if (order.Length == 0) return;
        // Rotate first actor each turn, avoiding a permanent first-faction advantage.
        order = order.Skip(Turn % order.Length).Concat(order.Take(Turn % order.Length)).ToArray();
        foreach (var actor in order)
        {
            if (!Entities.Contains(actor)) continue;
            int points = actor.Unit!.ActionPoints;
            bool fired = false;
            while (points > 0 && Entities.Contains(actor))
            {
                var target = Entities.Where(e => e.Faction != actor.Faction)
                    .OrderBy(e => actor.Position.Distance(AimCell(actor.Position, e)) - (actor.Unit.Automaton == Automaton.Swarm ? (1 - (double)e.Health / e.MaximumHealth) * 5 : 0)).ThenBy(e => e.Id).FirstOrDefault();
                if (target is null) break;
                int distance = Separation(actor, target);
                if (!fired && CanAttack(actor, target) && points >= 2)
                {
                    Attack(actor, target); points -= 2; fired = true; continue;
                }
                if (actor.Stationary) break;
                if (actor.Unit.Automaton == Automaton.Hold && distance > actor.Unit.Range) break;
                bool retreat = actor.Unit.Automaton is Automaton.Skirmish or Automaton.Artillery && distance < Math.Max(2, actor.Unit.Range - 1);
                int desired;
                if (retreat)
                {
                    var escape = Enumerable.Range(0, 6).Where(d => CanOccupy(actor, actor.Position + Hex.Directions[d], d, out _))
                        .OrderByDescending(d => (actor.Position + Hex.Directions[d]).Distance(target.Position)).ThenBy(d => Hex.TurnDistance(actor.Facing, d)).ToArray();
                    if (escape.Length == 0) break;
                    desired = escape[0];
                }
                else if (distance <= actor.Unit.Range)
                {
                    if (fired || InAttackArc(actor, target)) break;
                    desired = actor.Position.DirectionTo(AimCell(actor.Position, target));
                }
                else
                {
                    desired = FindDirection(actor, target);
                    if (desired < 0) break;
                }
                if (actor.Facing != desired)
                {
                    actor.Facing = (actor.Facing + ((desired - actor.Facing + 6) % 6 <= 3 ? 1 : 5)) % 6;
                    points--; continue;
                }
                if (!retreat && distance <= actor.Unit.Range) break;
                Hex next = actor.Position + Hex.Directions[actor.Facing];
                if (!CanOccupy(actor, next, actor.Facing, out _)) break;
                var terrain = Terrain[next];
                bool rough = terrain is Simulation.Terrain.Forest or Simulation.Terrain.Wetlands or Simulation.Terrain.Tundra;
                int cost = rough && actor.Unit.Mobility == Mobility.Ground && actor.Faction != Faction.InfantryAndArtillery ? 2 : 1;
                if (points < cost) break;
                actor.Position = next; points -= cost;
                if (rough && actor.Faction == Faction.InfantryAndArtillery) actor.Health -= 2;
                if (actor.Health <= 0) { Entities.Remove(actor); Casualties++; Note($"{actor.Name} lost crossing rough terrain."); }
                RebuildOccupancy();
            }
        }
    }
    private int FindDirection(Entity actor, Entity target)
    {
        var frontier = new PriorityQueue<(Hex Cell, int First), int>();
        var costs = new Dictionary<Hex, int> { [actor.Position] = 0 };
        var targetCells = target.OccupiedCells().ToArray();
        frontier.Enqueue((actor.Position, -1), 0);
        int expanded = 0;
        while (frontier.TryDequeue(out var current, out _) && expanded++ < 3000)
        {
            if (current.First >= 0 && targetCells.Min(current.Cell.Distance) <= actor.Unit!.Range + actor.Unit.Size - 1) return current.First;
            foreach (int direction in Enumerable.Range(0, 6).OrderBy(d => Hex.TurnDistance(actor.Facing, d)))
            {
                var next = current.Cell + Hex.Directions[direction];
                if (!CanOccupy(actor, next, direction, out _)) continue;
                int cost = costs[current.Cell] + 1;
                if (costs.TryGetValue(next, out var old) && old <= cost) continue;
                costs[next] = cost;
                frontier.Enqueue((next, current.First < 0 ? direction : current.First), cost + targetCells.Min(next.Distance));
            }
        }
        return -1;
    }
    public static World Demonstration()
    {
        var world = Create(false, 34, 24, true);
        var designs = Catalog.Units();
        void Place(int index, Faction faction, int x, int y, int facing)
        {
            var entity = new Entity { Unit = designs[index].Copy(), Faction = faction, Position = Hex.FromOffset(x, y), Facing = facing };
            foreach (var cell in entity.OccupiedCells()) world.Terrain[cell] = Simulation.Terrain.Plains;
            world.Add(entity, out _);
        }
        Place(0, Faction.Bastions, 5, 17, 5); Place(0, Faction.Bastions, 7, 20, 5);
        Place(2, Faction.Travelers, 7, 5, 1);
        for (int i = 0; i < 5; i++) Place(1, Faction.Travelers, 4 + i * 2, 9, 1);
        Place(3, Faction.MechAndTank, 26, 6, 2); Place(3, Faction.MechAndTank, 29, 9, 2);
        for (int i = 0; i < 8; i++) Place(4, Faction.InfantryAndArtillery, 16 + i % 4 * 2, 3 + i / 4 * 2, 1);
        Place(5, Faction.InfantryAndArtillery, 16, 1, 1); Place(5, Faction.InfantryAndArtillery, 22, 1, 1);
        Place(7, Faction.Prytu, 25, 19, 4);
        for (int i = 0; i < 7; i++) Place(6, Faction.Prytu, 19 + i * 2, 15 + i % 2, 3);
        world.Add(new() { Building = Catalog.Outpost(), Faction = Faction.Bastions, Position = Hex.FromOffset(4, 21) }, out _);
        world.Note("Five factions. One shared world. Start the automata or make it your own.");
        return world;
    }
}
