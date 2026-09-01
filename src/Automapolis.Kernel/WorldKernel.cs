namespace Automapolis.Kernel;

/// <summary>
/// The complete game engine. It owns simulation state and rules, but has no dependency on
/// a renderer, input device, file format, network transport, or game framework.
/// </summary>
public sealed class WorldKernel
{
    private const int ChronicleLimit = 14;
    private const int ForceLimit = 96;
    private readonly WorldConfig _config;
    private readonly TileState[,] _tiles;
    private readonly List<ForceState> _forces = [];
    private readonly List<string> _chronicle = [];
    private int _nextForceId = 1;

    public WorldKernel(WorldConfig config)
    {
        _config = config.Validate();
        _tiles = new TileState[_config.Width, _config.Height];
        GenerateTheater();
    }

    public int Turn { get; private set; }

    public CommandResult Execute(WorldCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_config.Mode is PlayerMode.Witness && command is not AdvanceTurn)
        {
            return new(false, "Witness mode accepts only AdvanceTurn; field command is sealed.", Snapshot());
        }

        var message = command switch
        {
            AdvanceTurn => SimulateTurn(),
            ChannelResonance action => Channel(action),
            FortifyTerrain action => Fortify(action),
            DeployForce action => Deploy(action),
            EstablishEnclave action => Establish(action),
            InvokePurge action => Purge(action),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown world command.")
        };

        return new(true, message, Snapshot());
    }

    public WorldSnapshot Snapshot()
    {
        var tiles = new List<TileSnapshot>(_config.Width * _config.Height);
        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                var tile = _tiles[x, y];
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

        var forces = _forces
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

        var humanForces = _forces.Count(IsHuman);
        var alienForces = _forces.Count(IsAlien);
        var population = _forces
            .Where(static force => force.Kind is ForceKind.Enclave)
            .Sum(static force => force.Population);
        var metrics = new WorldMetrics(
            tiles.Sum(static tile => tile.Resonance),
            tiles.Sum(static tile => tile.Biomass),
            population,
            humanForces,
            alienForces,
            _forces.Count(static force => force.Kind is ForceKind.Bastion),
            _forces.Count(static force => force.Kind is ForceKind.Enclave),
            (int)Math.Round(tiles.Average(static tile => tile.Integrity)));

        return new(
            _config.Name,
            _config.Seed,
            _config.Mode,
            _config.Width,
            _config.Height,
            Turn,
            tiles,
            forces,
            metrics,
            _chronicle.ToArray());
    }

    private void GenerateTheater()
    {
        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                var terrainRoll = DeterministicNoise.Range(_config.Seed, 0, x / 2, y / 2, 1, 100);
                var terrain = terrainRoll switch
                {
                    < 18 => TerrainKind.LeyChannel,
                    < 34 => TerrainKind.AshWaste,
                    < 51 => TerrainKind.FortifiedReach,
                    < 68 => TerrainKind.ShatteredPlain,
                    < 84 => TerrainKind.BroodMire,
                    _ => TerrainKind.Xenoforest
                };

                _tiles[x, y] = new TileState
                {
                    Position = new(x, y),
                    Terrain = terrain,
                    Resonance = BaseResonance(terrain) + DeterministicNoise.Range(_config.Seed, 0, x, y, 2, 20),
                    Biomass = BaseBiomass(terrain) + DeterministicNoise.Range(_config.Seed, 0, x, y, 3, 18),
                    Integrity = BaseIntegrity(terrain) + DeterministicNoise.Range(_config.Seed, 0, x, y, 4, 24)
                };
            }
        }

        var alienSeeds = Math.Clamp((_config.Width * _config.Height) / 30, 5, 22);
        for (var index = 0; index < alienSeeds; index++)
        {
            var position = new GridPoint(
                DeterministicNoise.Range(_config.Seed, 0, index, 0, 10, _config.Width),
                DeterministicNoise.Range(_config.Seed, 0, index, 0, 11, _config.Height));
            var kind = index % 4 is 0 ? ForceKind.BroodNode : ForceKind.Ravener;
            AddForce(position, kind, GeneratedName(kind, index), 52 + index % 18);
        }

        var enclavePosition = FindHumanLanding();
        AddEnclave(enclavePosition, "Vigil Enclave", 42);
        foreach (var position in OrthogonalNeighbors(enclavePosition).Take(2))
        {
            AddForce(position, ForceKind.Soldier, GeneratedName(ForceKind.Soldier, _nextForceId), 62);
        }

        var bastion = AddForce(enclavePosition, ForceKind.Bastion, "Bastion Zero", 100);
        bastion.Intent = "Standing between the enclave and extinction";
        Record($"TURN 000 · CONTACT: {_config.Name} is overrun. One Bastion answers humanity's distress call.");
    }

    private string SimulateTurn()
    {
        Turn++;
        EvolveFront();
        ActEnclaves();
        MoveHumanForces();
        MoveAlienForces();
        ResolveEngagements();
        SpawnBroodForces();
        RemoveDestroyedForces();

        var summary = BuildTurnSummary();
        Record(summary);
        return $"Turn {Turn} resolved: the front moved.";
    }

    private void EvolveFront()
    {
        var nextResonance = new int[_config.Width, _config.Height];
        var nextBiomass = new int[_config.Width, _config.Height];

        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                var tile = _tiles[x, y];
                var neighbors = OrthogonalNeighbors(tile.Position).Select(GetTile).ToArray();
                var neighborResonance = (int)neighbors.Average(static value => value.Resonance);
                var neighborBiomass = (int)neighbors.Average(static value => value.Biomass);
                var pulse = DeterministicNoise.Range(_config.Seed, Turn, x, y, 20, 7) - 3;
                var biomassGrowth = tile.Terrain switch
                {
                    TerrainKind.BroodMire => 4,
                    TerrainKind.Xenoforest => 3,
                    TerrainKind.AshWaste => -2,
                    TerrainKind.FortifiedReach => -1,
                    _ => 0
                };

                nextResonance[x, y] = Math.Clamp(
                    tile.Resonance + (neighborResonance - tile.Resonance) / 7 + pulse +
                    (tile.Terrain is TerrainKind.LeyChannel ? 2 : 0), 0, 100);
                nextBiomass[x, y] = Math.Clamp(
                    tile.Biomass + (neighborBiomass - tile.Biomass) / 8 + biomassGrowth, 0, 100);
            }
        }

        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                var tile = _tiles[x, y];
                tile.Resonance = nextResonance[x, y];
                tile.Biomass = nextBiomass[x, y];
                var humanPresence = _forces.Any(force => force.Position == tile.Position && IsHuman(force));
                var alienPresence = _forces.Any(force => force.Position == tile.Position && IsAlien(force));
                tile.Integrity = Math.Clamp(
                    tile.Integrity + (humanPresence ? 2 : 0) - (alienPresence ? 3 : 0) - tile.Biomass / 45,
                    0, 100);

                if (tile.Biomass > 82)
                {
                    tile.Terrain = TerrainKind.BroodMire;
                }
                else if (tile.Biomass > 66 && tile.Terrain is TerrainKind.ShatteredPlain or TerrainKind.FortifiedReach)
                {
                    tile.Terrain = TerrainKind.Xenoforest;
                }
                else if (tile.Biomass < 18 && tile.Terrain is TerrainKind.Xenoforest or TerrainKind.BroodMire)
                {
                    tile.Terrain = TerrainKind.ShatteredPlain;
                }
            }
        }
    }

    private void ActEnclaves()
    {
        var reinforcements = new List<GridPoint>();
        foreach (var enclave in _forces.Where(static force => force.Kind is ForceKind.Enclave).OrderBy(static force => force.Id))
        {
            enclave.ServiceTurns++;
            var tile = GetTile(enclave.Position);
            var threatened = _forces.Any(force => IsAlien(force) && Manhattan(force.Position, enclave.Position) <= 2);
            var growth = tile.Integrity / 30 + tile.Resonance / 35 - tile.Biomass / 24 - (threatened ? 2 : 0);
            enclave.Population = Math.Max(0, enclave.Population + growth);
            enclave.Strength = Math.Clamp(enclave.Strength + tile.Resonance / 22 - (threatened ? 3 : 1), 0, 100);
            enclave.Intent = threatened ? "Holding shelters against the swarm" : "Forging magitech arms for the front";
            tile.Resonance = Math.Max(0, tile.Resonance - 2);
            tile.Biomass = Math.Max(0, tile.Biomass - 1);

            if (enclave.Population >= 58 && enclave.ServiceTurns % 7 is 0 && _forces.Count + reinforcements.Count < ForceLimit)
            {
                reinforcements.Add(BestAdjacent(enclave.Position, static tile => tile.Integrity + tile.Resonance - tile.Biomass));
                enclave.Population -= 8;
            }
        }

        foreach (var position in reinforcements)
        {
            AddForce(position, ForceKind.Soldier, GeneratedName(ForceKind.Soldier, _nextForceId + Turn), 58);
        }
    }

    private void MoveHumanForces()
    {
        foreach (var force in _forces.Where(force => force.Kind is ForceKind.Bastion or ForceKind.Soldier).OrderBy(static force => force.Id))
        {
            force.ServiceTurns++;
            var target = NearestEnemy(force.Position, IsAlien);
            if (target is not null)
            {
                force.Position = StepToward(force, target.Position);
                force.Intent = force.Kind is ForceKind.Bastion
                    ? $"Hunting {target.Name}"
                    : $"Advancing in Bastion Zero's wake";
            }
            else
            {
                force.Intent = "Sweeping for alien spoor";
            }

            var tile = GetTile(force.Position);
            var recovery = force.Kind is ForceKind.Bastion ? tile.Resonance / 12 : tile.Resonance / 25;
            force.Strength = Math.Clamp(force.Strength + recovery - 1, 0, force.Kind is ForceKind.Bastion ? 100 : 75);
            tile.Biomass = Math.Max(0, tile.Biomass - (force.Kind is ForceKind.Bastion ? 5 : 2));
        }
    }

    private void MoveAlienForces()
    {
        foreach (var force in _forces.Where(static force => force.Kind is ForceKind.Ravener or ForceKind.BroodNode).OrderBy(static force => force.Id))
        {
            force.ServiceTurns++;
            var tile = GetTile(force.Position);
            if (force.Kind is ForceKind.BroodNode)
            {
                force.Intent = "Seeding a planetary nervous system";
                force.Strength = Math.Clamp(force.Strength + tile.Biomass / 18 - 2, 0, 90);
                tile.Biomass = Math.Min(100, tile.Biomass + 5);
                tile.Integrity = Math.Max(0, tile.Integrity - 4);
                continue;
            }

            var target = NearestEnemy(force.Position, IsHuman);
            if (target is not null)
            {
                force.Position = StepToward(force, target.Position);
                force.Intent = $"Closing on {target.Name}";
            }
            else
            {
                force.Intent = "Following human heat through the ruins";
            }

            tile = GetTile(force.Position);
            var feeding = Math.Min(tile.Biomass, 6);
            tile.Biomass -= feeding;
            force.Strength = Math.Clamp(force.Strength + feeding - 3, 0, 80);
        }
    }

    private void ResolveEngagements()
    {
        var contested = _forces
            .GroupBy(static force => force.Position)
            .Where(group => group.Any(IsHuman) && group.Any(IsAlien))
            .OrderBy(static group => group.Key.Y)
            .ThenBy(static group => group.Key.X)
            .ToArray();

        foreach (var engagement in contested)
        {
            var humans = engagement.Where(IsHuman).OrderBy(HumanCasualtyPriority).ThenBy(static force => force.Id).ToArray();
            var aliens = engagement.Where(IsAlien).OrderBy(static force => force.Kind is ForceKind.BroodNode ? 1 : 0).ThenBy(static force => force.Id).ToArray();
            var humanAttack = humans.Sum(static force => force.Kind switch
            {
                ForceKind.Bastion => 48,
                ForceKind.Soldier => 17,
                ForceKind.Enclave => 7,
                _ => 0
            });
            var alienAttack = aliens.Sum(static force => force.Kind is ForceKind.Ravener ? 15 : 9);

            DealDamage(aliens, humanAttack);
            DealDamage(humans, alienAttack);

            foreach (var enclave in humans.Where(static force => force.Kind is ForceKind.Enclave && force.Strength > 0))
            {
                enclave.Population = Math.Max(0, enclave.Population - Math.Max(1, alienAttack / 9));
            }

            var tile = GetTile(engagement.Key);
            tile.Biomass = Math.Max(0, tile.Biomass - humanAttack / 6);
            tile.Integrity = Math.Max(0, tile.Integrity - alienAttack / 5);
            var bastionPresent = humans.Any(static force => force.Kind is ForceKind.Bastion && force.Strength > 0);
            Record($"TURN {Turn:000} · ENGAGEMENT {engagement.Key}: {(bastionPresent ? "the Bastion breaks the swarm" : "human lines meet the swarm")}; {aliens.Count(force => force.Strength <= 0)} alien and {humans.Count(force => force.Strength <= 0)} human formations lost.");
        }
    }

    private void SpawnBroodForces()
    {
        var hatchlings = new List<GridPoint>();
        foreach (var node in _forces.Where(static force => force.Kind is ForceKind.BroodNode && force.Strength > 0))
        {
            if (node.ServiceTurns % 6 is 0 && GetTile(node.Position).Biomass >= 55 && _forces.Count + hatchlings.Count < ForceLimit)
            {
                hatchlings.Add(BestAdjacent(node.Position, static tile => tile.Biomass - tile.Integrity));
            }
        }

        foreach (var position in hatchlings)
        {
            AddForce(position, ForceKind.Ravener, GeneratedName(ForceKind.Ravener, _nextForceId + Turn), 48);
        }

        if (Turn % 9 is 0 && _forces.Count + hatchlings.Count < ForceLimit)
        {
            var infested = EnumerateTiles()
                .Where(tile => !_forces.Any(force => force.Kind is ForceKind.BroodNode && force.Position == tile.Position))
                .OrderByDescending(static tile => tile.Biomass - tile.Integrity)
                .ThenBy(static tile => tile.Position.Y)
                .ThenBy(static tile => tile.Position.X)
                .First();
            AddForce(infested.Position, ForceKind.BroodNode, GeneratedName(ForceKind.BroodNode, Turn), 55);
            Record($"TURN {Turn:000} · INCURSION: a brood node roots itself at {infested.Position}.");
        }
    }

    private string Channel(ChannelResonance action)
    {
        var tile = RequireTile(action.Position);
        var amount = Math.Clamp(action.Amount, 1, 100);
        tile.Resonance = Math.Clamp(tile.Resonance + amount, 0, 100);
        tile.Integrity = Math.Clamp(tile.Integrity + amount / 5, 0, 100);
        tile.Biomass = Math.Max(0, tile.Biomass - amount / 8);
        var message = $"Command channeled {amount} resonance into {action.Position}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Fortify(FortifyTerrain action)
    {
        var tile = RequireTile(action.Position);
        var former = tile.Terrain;
        tile.Terrain = action.Terrain;
        tile.Integrity = Math.Clamp((tile.Integrity + BaseIntegrity(action.Terrain)) / 2, 0, 100);
        tile.Biomass = Math.Clamp((tile.Biomass + BaseBiomass(action.Terrain)) / 2, 0, 100);
        var message = $"Command converted {TerrainName(former)} at {action.Position} into {TerrainName(action.Terrain)}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Deploy(DeployForce action)
    {
        RequireTile(action.Position);
        if (action.Kind is not (ForceKind.Bastion or ForceKind.Soldier))
        {
            throw new InvalidOperationException("Command may deploy only human field forces.");
        }

        if (action.Kind is ForceKind.Bastion && _forces.Any(static force => force.Kind is ForceKind.Bastion && force.Strength > 0))
        {
            throw new InvalidOperationException("A living Bastion is already committed to this front.");
        }

        var force = AddForce(action.Position, action.Kind, GeneratedName(action.Kind, _nextForceId + Turn), action.Kind is ForceKind.Bastion ? 100 : 65);
        var message = action.Kind is ForceKind.Bastion
            ? $"A rare Bastion answered the front at {action.Position}."
            : $"{force.Name} deployed at {action.Position}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Establish(EstablishEnclave action)
    {
        RequireTile(action.Position);
        if (string.IsNullOrWhiteSpace(action.Name) || action.Name.Length > 32)
        {
            throw new ArgumentException("Enclave name must contain 1 to 32 characters.", nameof(action));
        }

        AddEnclave(action.Position, action.Name.Trim(), 24);
        var message = $"Command established {action.Name.Trim()} at {action.Position}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Purge(InvokePurge action)
    {
        RequireTile(action.Position);
        var radius = Math.Clamp(action.Radius, 0, 4);
        foreach (var tile in EnumerateTiles().Where(tile => Manhattan(tile.Position, action.Position) <= radius))
        {
            tile.Resonance = Math.Max(0, tile.Resonance - 18);
            tile.Biomass = Math.Max(0, tile.Biomass - 70);
            tile.Integrity = Math.Max(0, tile.Integrity - 38);
            tile.Terrain = TerrainKind.AshWaste;
        }

        foreach (var force in _forces.Where(force => Manhattan(force.Position, action.Position) <= radius))
        {
            force.Strength -= force.Kind is ForceKind.Bastion ? 25 : 100;
            if (force.Kind is ForceKind.Enclave)
            {
                force.Population = Math.Max(0, force.Population - 20);
            }
        }

        RemoveDestroyedForces();
        var message = $"A radius-{radius} magitech purge burned the grid around {action.Position}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private ForceState? NearestEnemy(GridPoint origin, Func<ForceState, bool> predicate) => _forces
        .Where(force => force.Strength > 0 && predicate(force))
        .OrderBy(force => Manhattan(origin, force.Position))
        .ThenBy(static force => force.Id)
        .FirstOrDefault();

    private GridPoint StepToward(ForceState force, GridPoint target) => OrthogonalNeighbors(force.Position)
        .Append(force.Position)
        .Select(point => new
        {
            Point = point,
            Distance = Manhattan(point, target),
            TerrainScore = IsHuman(force)
                ? GetTile(point).Integrity + GetTile(point).Resonance - GetTile(point).Biomass
                : GetTile(point).Biomass - GetTile(point).Integrity,
            Noise = DeterministicNoise.Range(_config.Seed, Turn, point.X, point.Y, force.Id, 9)
        })
        .OrderBy(static candidate => candidate.Distance)
        .ThenByDescending(static candidate => candidate.TerrainScore + candidate.Noise)
        .ThenBy(static candidate => candidate.Point.Y)
        .ThenBy(static candidate => candidate.Point.X)
        .First()
        .Point;

    private GridPoint BestAdjacent(GridPoint origin, Func<TileState, int> score) => OrthogonalNeighbors(origin)
        .Append(origin)
        .Select(GetTile)
        .OrderByDescending(score)
        .ThenBy(static tile => tile.Position.Y)
        .ThenBy(static tile => tile.Position.X)
        .First()
        .Position;

    private IEnumerable<GridPoint> OrthogonalNeighbors(GridPoint point)
    {
        if (point.X > 0) yield return point with { X = point.X - 1 };
        if (point.X + 1 < _config.Width) yield return point with { X = point.X + 1 };
        if (point.Y > 0) yield return point with { Y = point.Y - 1 };
        if (point.Y + 1 < _config.Height) yield return point with { Y = point.Y + 1 };
    }

    private IEnumerable<TileState> EnumerateTiles()
    {
        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                yield return _tiles[x, y];
            }
        }
    }

    private TileState RequireTile(GridPoint position)
    {
        if (position.X < 0 || position.X >= _config.Width || position.Y < 0 || position.Y >= _config.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the theater grid.");
        }

        return GetTile(position);
    }

    private TileState GetTile(GridPoint position) => _tiles[position.X, position.Y];

    private ForceState AddForce(GridPoint position, ForceKind kind, string name, int strength)
    {
        var force = new ForceState
        {
            Id = _nextForceId++,
            Position = position,
            Kind = kind,
            Name = name,
            Strength = strength,
            Population = 1,
            Intent = kind switch
            {
                ForceKind.Bastion => "Awaiting the impossible mission",
                ForceKind.Soldier => "Holding formation",
                ForceKind.Ravener => "Scenting human heat",
                ForceKind.BroodNode => "Rooting into the theater",
                ForceKind.Enclave => "Maintaining the ward line",
                _ => "Awaiting contact"
            }
        };
        _forces.Add(force);
        return force;
    }

    private ForceState AddEnclave(GridPoint position, string name, int population)
    {
        var enclave = AddForce(position, ForceKind.Enclave, name, 75);
        enclave.Population = population;
        enclave.Intent = "Sheltering humanity behind resonance wards";
        return enclave;
    }

    private void RemoveDestroyedForces()
    {
        _forces.RemoveAll(static force => force.Strength <= 0 || force.Kind is ForceKind.Enclave && force.Population <= 0);
    }

    private static void DealDamage(IEnumerable<ForceState> targets, int damage)
    {
        var remaining = damage;
        foreach (var target in targets)
        {
            if (remaining <= 0)
            {
                break;
            }

            var absorbed = Math.Min(Math.Max(0, target.Strength), remaining);
            target.Strength -= remaining;
            remaining -= absorbed;
        }
    }

    private GridPoint FindHumanLanding() => EnumerateTiles()
        .OrderByDescending(static tile => tile.Integrity + tile.Resonance - tile.Biomass * 2)
        .ThenBy(static tile => tile.Position.Y)
        .ThenBy(static tile => tile.Position.X)
        .First()
        .Position;

    private string BuildTurnSummary()
    {
        var bastion = _forces.SingleOrDefault(static force => force.Kind is ForceKind.Bastion);
        var bastionState = bastion is null ? "BASTION LOST" : $"Bastion strength {bastion.Strength}";
        var human = _forces.Count(IsHuman);
        var alien = _forces.Count(IsAlien);
        var enclaves = _forces.Count(static force => force.Kind is ForceKind.Enclave);
        return $"TURN {Turn:000} · {bastionState}; {human} human formations hold {enclaves} enclaves against {alien} alien organisms.";
    }

    private void Record(string message)
    {
        _chronicle.Insert(0, message);
        if (_chronicle.Count > ChronicleLimit)
        {
            _chronicle.RemoveRange(ChronicleLimit, _chronicle.Count - ChronicleLimit);
        }
    }

    private static bool IsHuman(ForceState force) => force.Kind is ForceKind.Bastion or ForceKind.Soldier or ForceKind.Enclave;

    private static bool IsAlien(ForceState force) => force.Kind is ForceKind.Ravener or ForceKind.BroodNode;

    private static int HumanCasualtyPriority(ForceState force) => force.Kind switch
    {
        ForceKind.Soldier => 0,
        ForceKind.Enclave => 1,
        ForceKind.Bastion => 2,
        _ => 3
    };

    private static int Manhattan(GridPoint left, GridPoint right) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private static int BaseResonance(TerrainKind terrain) => terrain switch
    {
        TerrainKind.LeyChannel => 68,
        TerrainKind.FortifiedReach => 44,
        TerrainKind.ShatteredPlain => 32,
        TerrainKind.AshWaste => 22,
        TerrainKind.Xenoforest => 18,
        _ => 12
    };

    private static int BaseBiomass(TerrainKind terrain) => terrain switch
    {
        TerrainKind.BroodMire => 70,
        TerrainKind.Xenoforest => 58,
        TerrainKind.ShatteredPlain => 25,
        TerrainKind.LeyChannel => 18,
        TerrainKind.FortifiedReach => 14,
        _ => 8
    };

    private static int BaseIntegrity(TerrainKind terrain) => terrain switch
    {
        TerrainKind.FortifiedReach => 72,
        TerrainKind.LeyChannel => 58,
        TerrainKind.ShatteredPlain => 48,
        TerrainKind.AshWaste => 34,
        TerrainKind.Xenoforest => 24,
        _ => 12
    };

    private static string TerrainGlyph(TerrainKind terrain) => terrain switch
    {
        TerrainKind.ShatteredPlain => "·",
        TerrainKind.AshWaste => "░",
        TerrainKind.LeyChannel => "≈",
        TerrainKind.Xenoforest => "♣",
        TerrainKind.FortifiedReach => "▦",
        TerrainKind.BroodMire => "~",
        _ => "?"
    };

    private static string ForceGlyph(ForceKind kind) => kind switch
    {
        ForceKind.Bastion => "B",
        ForceKind.Soldier => "S",
        ForceKind.Ravener => "r",
        ForceKind.BroodNode => "N",
        ForceKind.Enclave => "E",
        _ => "?"
    };

    private static string TerrainName(TerrainKind terrain) => terrain switch
    {
        TerrainKind.ShatteredPlain => "shattered plain",
        TerrainKind.AshWaste => "ash waste",
        TerrainKind.LeyChannel => "ley channel",
        TerrainKind.Xenoforest => "xenoforest",
        TerrainKind.FortifiedReach => "fortified reach",
        TerrainKind.BroodMire => "brood mire",
        _ => terrain.ToString()
    };

    private static string GeneratedName(ForceKind kind, int index) => kind switch
    {
        ForceKind.Bastion => $"Bastion {index:00}",
        ForceKind.Soldier => $"Soldier {index:00}",
        ForceKind.Ravener => $"Ravener Strain {index:00}",
        ForceKind.BroodNode => $"Brood Node {index:00}",
        ForceKind.Enclave => $"Enclave {index:00}",
        _ => $"Unknown {index:00}"
    };
}
