namespace Automatou.Kernel;

/// <summary>
/// The complete game engine. It owns simulation state and rules, but has no dependency on
/// a renderer, input device, file format, network transport, or game framework.
/// </summary>
public sealed class WorldKernel {
    private const int ChronicleLimit = 14;
    private const int ForceLimit = 96;
    private readonly WorldConfig _config;
    private readonly TileState[,] _tiles;
    private readonly List<ForceState> _forces = [];
    private readonly List<string> _chronicle = [];
    private int _nextForceId = 1;

    public WorldKernel(WorldConfig config) {
        this._config = config.Validate();
        this._tiles = new TileState[this._config.Width, this._config.Height];
        this.GenerateTheater();
    }

    public int Turn { get; private set; }

    public CommandResult Execute(WorldCommand command) {
        ArgumentNullException.ThrowIfNull(command);

        string message = command switch {
            AdvanceTurn => this.SimulateTurn(),
            ChannelResonance action => this.Channel(action),
            FortifyTerrain action => this.Fortify(action),
            DeployForce action => this.Deploy(action),
            EstablishEnclave action => this.Establish(action),
            InvokePurge action => this.Purge(action),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown world command.")
        };

        return new(true, message, this.Snapshot());
    }

    public WorldSnapshot Snapshot() {
        List<TileSnapshot> tiles = new(this._config.Width * this._config.Height);
        for (int y = 0; y < this._config.Height; y++) {
            for (int x = 0; x < this._config.Width; x++) {
                TileState tile = this._tiles[x, y];
                tiles.Add(new(
                    tile.Position,
                    tile.Terrain,
                    tile.Resonance,
                    tile.Biomass,
                    tile.Integrity,
                    TerrainGlyph(tile.Terrain),
                    $"{TerrainName(tile.Terrain)} · resonance {tile.Resonance} · biomass {tile.Biomass} · integrity {tile.Integrity}"));
            }
        }

        ForceSnapshot[] forces = this._forces
            .OrderBy(static force => force.Id)
            .Select(static force => new ForceSnapshot(
                force.Id,
                force.Position,
                force.Kind,
                force.Name,
                ForceGlyph(force.Kind),
                force.Strength,
                force.Population,
                force.ServiceTurns,
                force.Intent))
            .ToArray();

        int humanForces = this._forces.Count(IsHuman);
        int alienForces = this._forces.Count(IsAlien);
        int population = this._forces
            .Where(static force => force.Kind is ForceKind.Enclave)
            .Sum(static force => force.Population);
        WorldMetrics metrics = new(
            tiles.Sum(static tile => tile.Resonance),
            tiles.Sum(static tile => tile.Biomass),
            population,
            humanForces,
            alienForces,
            this._forces.Count(static force => force.Kind is ForceKind.Bastion),
            this._forces.Count(static force => force.Kind is ForceKind.Enclave),
            (int)Math.Round(tiles.Average(static tile => tile.Integrity)));

        return new(
            this._config.Name,
            this._config.Seed,
            this._config.Width,
            this._config.Height,
            this.Turn,
            tiles,
            forces,
            metrics,
            [.. this._chronicle]);
    }

    private void GenerateTheater() {
        for (int y = 0; y < this._config.Height; y++) {
            for (int x = 0; x < this._config.Width; x++) {
                int terrainRoll = DeterministicNoise.Range(this._config.Seed, 0, x / 2, y / 2, 1, 100);
                TerrainKind terrain = terrainRoll switch {
                    < 18 => TerrainKind.LeyChannel,
                    < 34 => TerrainKind.AshWaste,
                    < 51 => TerrainKind.FortifiedReach,
                    < 68 => TerrainKind.ShatteredPlain,
                    < 84 => TerrainKind.BroodMire,
                    _ => TerrainKind.Xenoforest
                };

                this._tiles[x, y] = new TileState {
                    Position = new(x, y),
                    Terrain = terrain,
                    Resonance = BaseResonance(terrain) + DeterministicNoise.Range(this._config.Seed, 0, x, y, 2, 20),
                    Biomass = BaseBiomass(terrain) + DeterministicNoise.Range(this._config.Seed, 0, x, y, 3, 18),
                    Integrity = BaseIntegrity(terrain) + DeterministicNoise.Range(this._config.Seed, 0, x, y, 4, 24)
                };
            }
        }

        int alienSeeds = Math.Clamp(this._config.Width * this._config.Height / 30, 5, 22);
        for (int index = 0; index < alienSeeds; index++) {
            GridPoint position = new(
                DeterministicNoise.Range(this._config.Seed, 0, index, 0, 10, this._config.Width),
                DeterministicNoise.Range(this._config.Seed, 0, index, 0, 11, this._config.Height));
            ForceKind kind = index % 4 is 0 ? ForceKind.BroodNode : ForceKind.Ravener;
            _ = this.AddForce(position, kind, GeneratedName(kind, index), 52 + (index % 18));
        }

        GridPoint enclavePosition = this.FindHumanLanding();
        _ = this.AddEnclave(enclavePosition, "Vigil Enclave", 42);
        foreach (GridPoint position in this.Neighbors(enclavePosition).Take(2)) {
            _ = this.AddForce(position, ForceKind.Soldier, GeneratedName(ForceKind.Soldier, this._nextForceId), 62);
        }

        ForceState bastion = this.AddForce(enclavePosition, ForceKind.Bastion, "Bastion Zero", 100);
        bastion.Intent = "Standing between the enclave and extinction";
        this.Record($"TURN 000 · CONTACT: {this._config.Name} is overrun. One Bastion answers humanity's distress call.");
    }

    private string SimulateTurn() {
        this.Turn++;
        this.EvolveFront();
        this.ActEnclaves();
        this.MoveHumanForces();
        this.MoveAlienForces();
        this.ResolveEngagements();
        this.SpawnBroodForces();
        this.RemoveDestroyedForces();

        string summary = this.BuildTurnSummary();
        this.Record(summary);
        return $"Turn {this.Turn} resolved: the front moved.";
    }

    private void EvolveFront() {
        int[,] nextResonance = new int[this._config.Width, this._config.Height];
        int[,] nextBiomass = new int[this._config.Width, this._config.Height];

        for (int y = 0; y < this._config.Height; y++) {
            for (int x = 0; x < this._config.Width; x++) {
                TileState tile = this._tiles[x, y];
                TileState[] neighbors = this.Neighbors(tile.Position).Select(this.GetTile).ToArray();
                int neighborResonance = (int)neighbors.Average(static value => value.Resonance);
                int neighborBiomass = (int)neighbors.Average(static value => value.Biomass);
                int pulse = DeterministicNoise.Range(this._config.Seed, this.Turn, x, y, 20, 7) - 3;
                int biomassGrowth = tile.Terrain switch {
                    TerrainKind.BroodMire => 4,
                    TerrainKind.Xenoforest => 3,
                    TerrainKind.AshWaste => -2,
                    TerrainKind.FortifiedReach => -1,
                    TerrainKind.ShatteredPlain => 0,
                    TerrainKind.LeyChannel => 0,
                    _ => 0
                };

                nextResonance[x, y] = Math.Clamp(
                    tile.Resonance + ((neighborResonance - tile.Resonance) / 7) + pulse +
                    (tile.Terrain is TerrainKind.LeyChannel ? 2 : 0), 0, 100);
                nextBiomass[x, y] = Math.Clamp(
                    tile.Biomass + ((neighborBiomass - tile.Biomass) / 8) + biomassGrowth, 0, 100);
            }
        }

        for (int y = 0; y < this._config.Height; y++) {
            for (int x = 0; x < this._config.Width; x++) {
                TileState tile = this._tiles[x, y];
                tile.Resonance = nextResonance[x, y];
                tile.Biomass = nextBiomass[x, y];
                bool humanPresence = this._forces.Any(force => force.Position == tile.Position && IsHuman(force));
                bool alienPresence = this._forces.Any(force => force.Position == tile.Position && IsAlien(force));
                tile.Integrity = Math.Clamp(
                    tile.Integrity + (humanPresence ? 2 : 0) - (alienPresence ? 3 : 0) - (tile.Biomass / 45),
                    0, 100);

                if (tile.Biomass > 82) {
                    tile.Terrain = TerrainKind.BroodMire;
                } else if (tile.Biomass > 66 && tile.Terrain is TerrainKind.ShatteredPlain or TerrainKind.FortifiedReach) {
                    tile.Terrain = TerrainKind.Xenoforest;
                } else if (tile.Biomass < 18 && tile.Terrain is TerrainKind.Xenoforest or TerrainKind.BroodMire) {
                    tile.Terrain = TerrainKind.ShatteredPlain;
                }
            }
        }
    }

    private void ActEnclaves() {
        List<GridPoint> reinforcements = [];
        foreach (ForceState? enclave in this._forces.Where(static force => force.Kind is ForceKind.Enclave).OrderBy(static force => force.Id)) {
            enclave.ServiceTurns++;
            TileState tile = this.GetTile(enclave.Position);
            bool threatened = this._forces.Any(force => IsAlien(force) && HexGrid.Distance(force.Position, enclave.Position) <= 2);
            int growth = (tile.Integrity / 30) + (tile.Resonance / 35) - (tile.Biomass / 24) - (threatened ? 2 : 0);
            enclave.Population = Math.Max(0, enclave.Population + growth);
            enclave.Strength = Math.Clamp(enclave.Strength + (tile.Resonance / 22) - (threatened ? 3 : 1), 0, 100);
            enclave.Intent = threatened ? "Holding shelters against the swarm" : "Forging magitech arms for the front";
            tile.Resonance = Math.Max(0, tile.Resonance - 2);
            tile.Biomass = Math.Max(0, tile.Biomass - 1);

            if (enclave.Population >= 58 && enclave.ServiceTurns % 7 is 0 && this._forces.Count + reinforcements.Count < ForceLimit) {
                reinforcements.Add(this.BestAdjacent(enclave.Position, static tile => tile.Integrity + tile.Resonance - tile.Biomass));
                enclave.Population -= 8;
            }
        }

        foreach (GridPoint position in reinforcements) {
            _ = this.AddForce(position, ForceKind.Soldier, GeneratedName(ForceKind.Soldier, this._nextForceId + this.Turn), 58);
        }
    }

    private void MoveHumanForces() {
        foreach (ForceState? force in this._forces.Where(force => force.Kind is ForceKind.Bastion or ForceKind.Soldier).OrderBy(static force => force.Id)) {
            force.ServiceTurns++;
            ForceState? target = this.NearestEnemy(force.Position, IsAlien);
            if (target is not null) {
                force.Position = this.StepToward(force, target.Position);
                force.Intent = force.Kind is ForceKind.Bastion
                    ? $"Hunting {target.Name}"
                    : $"Advancing in Bastion Zero's wake";
            } else {
                force.Intent = "Sweeping for alien spoor";
            }

            TileState tile = this.GetTile(force.Position);
            int recovery = force.Kind is ForceKind.Bastion ? tile.Resonance / 12 : tile.Resonance / 25;
            force.Strength = Math.Clamp(force.Strength + recovery - 1, 0, force.Kind is ForceKind.Bastion ? 100 : 75);
            tile.Biomass = Math.Max(0, tile.Biomass - (force.Kind is ForceKind.Bastion ? 5 : 2));
        }
    }

    private void MoveAlienForces() {
        foreach (ForceState? force in this._forces.Where(static force => force.Kind is ForceKind.Ravener or ForceKind.BroodNode).OrderBy(static force => force.Id)) {
            force.ServiceTurns++;
            TileState tile = this.GetTile(force.Position);
            if (force.Kind is ForceKind.BroodNode) {
                force.Intent = "Seeding a planetary nervous system";
                force.Strength = Math.Clamp(force.Strength + (tile.Biomass / 18) - 2, 0, 90);
                tile.Biomass = Math.Min(100, tile.Biomass + 5);
                tile.Integrity = Math.Max(0, tile.Integrity - 4);
                continue;
            }

            ForceState? target = this.NearestEnemy(force.Position, IsHuman);
            if (target is not null) {
                force.Position = this.StepToward(force, target.Position);
                force.Intent = $"Closing on {target.Name}";
            } else {
                force.Intent = "Following human heat through the ruins";
            }

            tile = this.GetTile(force.Position);
            int feeding = Math.Min(tile.Biomass, 6);
            tile.Biomass -= feeding;
            force.Strength = Math.Clamp(force.Strength + feeding - 3, 0, 80);
        }
    }

    private void ResolveEngagements() {
        IGrouping<GridPoint, ForceState>[] contested = this._forces
            .GroupBy(static force => force.Position)
            .Where(group => group.Any(IsHuman) && group.Any(IsAlien))
            .OrderBy(static group => group.Key.Y)
            .ThenBy(static group => group.Key.X)
            .ToArray();

        foreach (IGrouping<GridPoint, ForceState>? engagement in contested) {
            ForceState[] humans = engagement.Where(IsHuman).OrderBy(HumanCasualtyPriority).ThenBy(static force => force.Id).ToArray();
            ForceState[] aliens = engagement.Where(IsAlien).OrderBy(static force => force.Kind is ForceKind.BroodNode ? 1 : 0).ThenBy(static force => force.Id).ToArray();
            int humanAttack = humans.Sum(static force => force.Kind switch {
                ForceKind.Bastion => 48,
                ForceKind.Soldier => 17,
                ForceKind.Enclave => 7,
                ForceKind.Ravener => 0,
                ForceKind.BroodNode => 0,
                _ => 0
            });
            int alienAttack = aliens.Sum(static force => force.Kind is ForceKind.Ravener ? 15 : 9);

            DealDamage(aliens, humanAttack);
            DealDamage(humans, alienAttack);

            foreach (ForceState? enclave in humans.Where(static force => force.Kind is ForceKind.Enclave && force.Strength > 0)) {
                enclave.Population = Math.Max(0, enclave.Population - Math.Max(1, alienAttack / 9));
            }

            TileState tile = this.GetTile(engagement.Key);
            tile.Biomass = Math.Max(0, tile.Biomass - (humanAttack / 6));
            tile.Integrity = Math.Max(0, tile.Integrity - (alienAttack / 5));
            bool bastionPresent = humans.Any(static force => force.Kind is ForceKind.Bastion && force.Strength > 0);
            this.Record($"TURN {this.Turn:000} · ENGAGEMENT {engagement.Key}: {(bastionPresent ? "the Bastion breaks the swarm" : "human lines meet the swarm")}; {aliens.Count(force => force.Strength <= 0)} alien and {humans.Count(force => force.Strength <= 0)} human formations lost.");
        }
    }

    private void SpawnBroodForces() {
        List<GridPoint> hatchlings = [];
        foreach (ForceState? node in this._forces.Where(static force => force.Kind is ForceKind.BroodNode && force.Strength > 0)) {
            if (node.ServiceTurns % 6 is 0 && this.GetTile(node.Position).Biomass >= 55 && this._forces.Count + hatchlings.Count < ForceLimit) {
                hatchlings.Add(this.BestAdjacent(node.Position, static tile => tile.Biomass - tile.Integrity));
            }
        }

        foreach (GridPoint position in hatchlings) {
            _ = this.AddForce(position, ForceKind.Ravener, GeneratedName(ForceKind.Ravener, this._nextForceId + this.Turn), 48);
        }

        if (this.Turn % 9 is 0 && this._forces.Count + hatchlings.Count < ForceLimit) {
            TileState infested = this.EnumerateTiles()
                .Where(tile => !this._forces.Any(force => force.Kind is ForceKind.BroodNode && force.Position == tile.Position))
                .OrderByDescending(static tile => tile.Biomass - tile.Integrity)
                .ThenBy(static tile => tile.Position.Y)
                .ThenBy(static tile => tile.Position.X)
                .First();
            _ = this.AddForce(infested.Position, ForceKind.BroodNode, GeneratedName(ForceKind.BroodNode, this.Turn), 55);
            this.Record($"TURN {this.Turn:000} · INCURSION: a brood node roots itself at {infested.Position}.");
        }
    }

    private string Channel(ChannelResonance action) {
        TileState tile = this.RequireTile(action.Position);
        int amount = Math.Clamp(action.Amount, 1, 100);
        tile.Resonance = Math.Clamp(tile.Resonance + amount, 0, 100);
        tile.Integrity = Math.Clamp(tile.Integrity + (amount / 5), 0, 100);
        tile.Biomass = Math.Max(0, tile.Biomass - (amount / 8));
        string message = $"Command channeled {amount} resonance into {action.Position}.";
        this.Record($"TURN {this.Turn:000} · {message}");
        return message;
    }

    private string Fortify(FortifyTerrain action) {
        TileState tile = this.RequireTile(action.Position);
        TerrainKind former = tile.Terrain;
        tile.Terrain = action.Terrain;
        tile.Integrity = Math.Clamp((tile.Integrity + BaseIntegrity(action.Terrain)) / 2, 0, 100);
        tile.Biomass = Math.Clamp((tile.Biomass + BaseBiomass(action.Terrain)) / 2, 0, 100);
        string message = $"Command converted {TerrainName(former)} at {action.Position} into {TerrainName(action.Terrain)}.";
        this.Record($"TURN {this.Turn:000} · {message}");
        return message;
    }

    private string Deploy(DeployForce action) {
        _ = this.RequireTile(action.Position);
        if (action.Kind is not (ForceKind.Bastion or ForceKind.Soldier)) {
            throw new InvalidOperationException("Command may deploy only human field forces.");
        }

        if (action.Kind is ForceKind.Bastion && this._forces.Any(static force => force.Kind is ForceKind.Bastion && force.Strength > 0)) {
            throw new InvalidOperationException("A living Bastion is already committed to this front.");
        }

        ForceState force = this.AddForce(action.Position, action.Kind, GeneratedName(action.Kind, this._nextForceId + this.Turn), action.Kind is ForceKind.Bastion ? 100 : 65);
        string message = action.Kind is ForceKind.Bastion
            ? $"A rare Bastion answered the front at {action.Position}."
            : $"{force.Name} deployed at {action.Position}.";
        this.Record($"TURN {this.Turn:000} · {message}");
        return message;
    }

    private string Establish(EstablishEnclave action) {
        _ = this.RequireTile(action.Position);
        if (string.IsNullOrWhiteSpace(action.Name) || action.Name.Length > 32) {
            throw new ArgumentException("Enclave name must contain 1 to 32 characters.", nameof(action));
        }

        _ = this.AddEnclave(action.Position, action.Name.Trim(), 24);
        string message = $"Command established {action.Name.Trim()} at {action.Position}.";
        this.Record($"TURN {this.Turn:000} · {message}");
        return message;
    }

    private string Purge(InvokePurge action) {
        _ = this.RequireTile(action.Position);
        int radius = Math.Clamp(action.Radius, 0, 4);
        foreach (TileState? tile in this.EnumerateTiles().Where(tile => HexGrid.Distance(tile.Position, action.Position) <= radius)) {
            tile.Resonance = Math.Max(0, tile.Resonance - 18);
            tile.Biomass = Math.Max(0, tile.Biomass - 70);
            tile.Integrity = Math.Max(0, tile.Integrity - 38);
            tile.Terrain = TerrainKind.AshWaste;
        }

        foreach (ForceState? force in this._forces.Where(force => HexGrid.Distance(force.Position, action.Position) <= radius)) {
            force.Strength -= force.Kind is ForceKind.Bastion ? 25 : 100;
            if (force.Kind is ForceKind.Enclave) {
                force.Population = Math.Max(0, force.Population - 20);
            }
        }

        this.RemoveDestroyedForces();
        string message = $"A radius-{radius} magitech purge burned the grid around {action.Position}.";
        this.Record($"TURN {this.Turn:000} · {message}");
        return message;
    }

    private ForceState? NearestEnemy(GridPoint origin, Func<ForceState, bool> predicate) {
        return this._forces
        .Where(force => force.Strength > 0 && predicate(force))
        .OrderBy(force => HexGrid.Distance(origin, force.Position))
        .ThenBy(static force => force.Id)
        .FirstOrDefault();
    }

    private GridPoint StepToward(ForceState force, GridPoint target) {
        return this.Neighbors(force.Position)
        .Append(force.Position)
        .Select(point => new {
            Point = point,
            Distance = HexGrid.Distance(point, target),
            TerrainScore = IsHuman(force)
                ? this.GetTile(point).Integrity + this.GetTile(point).Resonance - this.GetTile(point).Biomass
                : this.GetTile(point).Biomass - this.GetTile(point).Integrity,
            Noise = DeterministicNoise.Range(this._config.Seed, this.Turn, point.X, point.Y, force.Id, 9)
        })
        .OrderBy(static candidate => candidate.Distance)
        .ThenByDescending(static candidate => candidate.TerrainScore + candidate.Noise)
        .ThenBy(static candidate => candidate.Point.Y)
        .ThenBy(static candidate => candidate.Point.X)
        .First()
        .Point;
    }

    private GridPoint BestAdjacent(GridPoint origin, Func<TileState, int> score) {
        return this.Neighbors(origin)
        .Append(origin)
        .Select(this.GetTile)
        .OrderByDescending(score)
        .ThenBy(static tile => tile.Position.Y)
        .ThenBy(static tile => tile.Position.X)
        .First()
        .Position;
    }

    private IEnumerable<GridPoint> Neighbors(GridPoint point) {
        return HexGrid.Neighbors(point, this._config.Width, this._config.Height);
    }

    private IEnumerable<TileState> EnumerateTiles() {
        for (int y = 0; y < this._config.Height; y++) {
            for (int x = 0; x < this._config.Width; x++) {
                yield return this._tiles[x, y];
            }
        }
    }

    private TileState RequireTile(GridPoint position) {
        return position.X < 0 || position.X >= this._config.Width || position.Y < 0 || position.Y >= this._config.Height
            ? throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the theater grid.")
            : this.GetTile(position);
    }

    private TileState GetTile(GridPoint position) {
        return this._tiles[position.X, position.Y];
    }

    private ForceState AddForce(GridPoint position, ForceKind kind, string name, int strength) {
        ForceState force = new() {
            Id = this._nextForceId++,
            Position = position,
            Kind = kind,
            Name = name,
            Strength = strength,
            Population = 1,
            Intent = kind switch {
                ForceKind.Bastion => "Awaiting the impossible mission",
                ForceKind.Soldier => "Holding formation",
                ForceKind.Ravener => "Scenting human heat",
                ForceKind.BroodNode => "Rooting into the theater",
                ForceKind.Enclave => "Maintaining the ward line",
                _ => "Awaiting contact"
            }
        };
        this._forces.Add(force);
        return force;
    }

    private ForceState AddEnclave(GridPoint position, string name, int population) {
        ForceState enclave = this.AddForce(position, ForceKind.Enclave, name, 75);
        enclave.Population = population;
        enclave.Intent = "Sheltering humanity behind resonance wards";
        return enclave;
    }

    private void RemoveDestroyedForces() {
        _ = this._forces.RemoveAll(static force => force.Strength <= 0 || force.Kind is ForceKind.Enclave && force.Population <= 0);
    }

    private static void DealDamage(IEnumerable<ForceState> targets, int damage) {
        int remaining = damage;
        foreach (ForceState target in targets) {
            if (remaining <= 0) {
                break;
            }

            int absorbed = Math.Min(Math.Max(0, target.Strength), remaining);
            target.Strength -= remaining;
            remaining -= absorbed;
        }
    }

    private GridPoint FindHumanLanding() {
        return this.EnumerateTiles()
        .OrderByDescending(static tile => tile.Integrity + tile.Resonance - (tile.Biomass * 2))
        .ThenBy(static tile => tile.Position.Y)
        .ThenBy(static tile => tile.Position.X)
        .First()
        .Position;
    }

    private string BuildTurnSummary() {
        ForceState? bastion = this._forces.SingleOrDefault(static force => force.Kind is ForceKind.Bastion);
        string bastionState = bastion is null ? "BASTION LOST" : $"Bastion strength {bastion.Strength}";
        int human = this._forces.Count(IsHuman);
        int alien = this._forces.Count(IsAlien);
        int enclaves = this._forces.Count(static force => force.Kind is ForceKind.Enclave);
        return $"TURN {this.Turn:000} · {bastionState}; {human} human formations hold {enclaves} enclaves against {alien} alien organisms.";
    }

    private void Record(string message) {
        this._chronicle.Insert(0, message);
        if (this._chronicle.Count > ChronicleLimit) {
            this._chronicle.RemoveRange(ChronicleLimit, this._chronicle.Count - ChronicleLimit);
        }
    }

    private static bool IsHuman(ForceState force) {
        return force.Kind is ForceKind.Bastion or ForceKind.Soldier or ForceKind.Enclave;
    }

    private static bool IsAlien(ForceState force) {
        return force.Kind is ForceKind.Ravener or ForceKind.BroodNode;
    }

    private static int HumanCasualtyPriority(ForceState force) {
        return force.Kind switch {
            ForceKind.Soldier => 0,
            ForceKind.Enclave => 1,
            ForceKind.Bastion => 2,
            ForceKind.Ravener => 3,
            ForceKind.BroodNode => 3,
            _ => 3
        };
    }

    private static int BaseResonance(TerrainKind terrain) {
        return terrain switch {
            TerrainKind.LeyChannel => 68,
            TerrainKind.FortifiedReach => 44,
            TerrainKind.ShatteredPlain => 32,
            TerrainKind.AshWaste => 22,
            TerrainKind.Xenoforest => 18,
            TerrainKind.BroodMire => 12,
            _ => 12
        };
    }

    private static int BaseBiomass(TerrainKind terrain) {
        return terrain switch {
            TerrainKind.BroodMire => 70,
            TerrainKind.Xenoforest => 58,
            TerrainKind.ShatteredPlain => 25,
            TerrainKind.LeyChannel => 18,
            TerrainKind.FortifiedReach => 14,
            TerrainKind.AshWaste => 8,
            _ => 8
        };
    }

    private static int BaseIntegrity(TerrainKind terrain) {
        return terrain switch {
            TerrainKind.FortifiedReach => 72,
            TerrainKind.LeyChannel => 58,
            TerrainKind.ShatteredPlain => 48,
            TerrainKind.AshWaste => 34,
            TerrainKind.Xenoforest => 24,
            TerrainKind.BroodMire => 12,
            _ => 12
        };
    }

    private static string TerrainGlyph(TerrainKind terrain) {
        return terrain switch {
            TerrainKind.ShatteredPlain => "·",
            TerrainKind.AshWaste => ":",
            TerrainKind.LeyChannel => "≈",
            TerrainKind.Xenoforest => "^",
            TerrainKind.FortifiedReach => "#",
            TerrainKind.BroodMire => "~",
            _ => "?"
        };
    }

    private static string ForceGlyph(ForceKind kind) {
        return kind switch {
            ForceKind.Bastion => "B",
            ForceKind.Soldier => "S",
            ForceKind.Ravener => "r",
            ForceKind.BroodNode => "N",
            ForceKind.Enclave => "E",
            _ => "?"
        };
    }

    private static string TerrainName(TerrainKind terrain) {
        return terrain switch {
            TerrainKind.ShatteredPlain => "shattered plain",
            TerrainKind.AshWaste => "ash waste",
            TerrainKind.LeyChannel => "ley channel",
            TerrainKind.Xenoforest => "xenoforest",
            TerrainKind.FortifiedReach => "fortified reach",
            TerrainKind.BroodMire => "brood mire",
            _ => terrain.ToString()
        };
    }

    private static string GeneratedName(ForceKind kind, int index) {
        return kind switch {
            ForceKind.Bastion => $"Bastion {index:00}",
            ForceKind.Soldier => $"Soldier {index:00}",
            ForceKind.Ravener => $"Ravener Strain {index:00}",
            ForceKind.BroodNode => $"Brood Node {index:00}",
            ForceKind.Enclave => $"Enclave {index:00}",
            _ => $"Unknown {index:00}"
        };
    }
}
