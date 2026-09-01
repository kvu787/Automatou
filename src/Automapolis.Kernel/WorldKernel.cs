namespace Automapolis.Kernel;

/// <summary>
/// The complete game engine. It owns simulation state and rules, but has no dependency on
/// a renderer, input device, file format, network transport, or game framework.
/// </summary>
public sealed class WorldKernel
{
    private const int ChronicleLimit = 14;
    private readonly WorldConfig _config;
    private readonly TileState[,] _tiles;
    private readonly List<BeingState> _beings = [];
    private readonly List<string> _chronicle = [];
    private int _nextBeingId = 1;

    public WorldKernel(WorldConfig config)
    {
        _config = config.Validate();
        _tiles = new TileState[_config.Width, _config.Height];
        GenerateWorld();
    }

    public int Turn { get; private set; }

    public CommandResult Execute(WorldCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_config.Mode is PlayerMode.Observer && command is not AdvanceTurn)
        {
            return new(false, "Observer mode accepts only AdvanceTurn; the world authors itself.", Snapshot());
        }

        var message = command switch
        {
            AdvanceTurn => SimulateTurn(),
            InfuseAether action => Infuse(action),
            TransmuteTerrain action => Transmute(action),
            CreateLife action => Create(action),
            FoundSettlement action => Found(action),
            InvokeCataclysm action => Cataclysm(action),
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
                    tile.Aether,
                    tile.Vitality,
                    tile.Stability,
                    TerrainGlyph(tile.Terrain),
                    $"{TerrainName(tile.Terrain)} · aether {tile.Aether} · vitality {tile.Vitality} · stability {tile.Stability}"));
            }
        }

        var beings = _beings
            .OrderBy(static being => being.Id)
            .Select(static being => new BeingSnapshot(
                being.Id,
                being.Position,
                being.Kind,
                being.Name,
                BeingGlyph(being.Kind),
                being.Energy,
                being.Population,
                being.Age,
                being.Intent))
            .ToArray();

        var population = _beings.Sum(static being => being.Kind is BeingKind.Settlement ? being.Population : 1);
        var metrics = new WorldMetrics(
            tiles.Sum(static tile => tile.Aether),
            tiles.Sum(static tile => tile.Vitality),
            population,
            _beings.Count,
            _beings.Count(static being => being.Kind is BeingKind.Settlement),
            (int)Math.Round(tiles.Average(static tile => tile.Stability)));

        return new(
            _config.Name,
            _config.Seed,
            _config.Mode,
            _config.Width,
            _config.Height,
            Turn,
            tiles,
            beings,
            metrics,
            _chronicle.ToArray());
    }

    private void GenerateWorld()
    {
        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                var terrainRoll = DeterministicNoise.Range(_config.Seed, 0, x / 2, y / 2, 1, 100);
                var terrain = terrainRoll switch
                {
                    < 18 => TerrainKind.AetherSea,
                    < 34 => TerrainKind.AshDunes,
                    < 51 => TerrainKind.IronSteppe,
                    < 68 => TerrainKind.StarGlass,
                    < 84 => TerrainKind.DreamMarsh,
                    _ => TerrainKind.CrystalForest
                };

                _tiles[x, y] = new TileState
                {
                    Position = new(x, y),
                    Terrain = terrain,
                    Aether = BaseAether(terrain) + DeterministicNoise.Range(_config.Seed, 0, x, y, 2, 20),
                    Vitality = BaseVitality(terrain) + DeterministicNoise.Range(_config.Seed, 0, x, y, 3, 18),
                    Stability = 58 + DeterministicNoise.Range(_config.Seed, 0, x, y, 4, 40)
                };
            }
        }

        var initialBeings = Math.Clamp((_config.Width * _config.Height) / 35, 4, 18);
        for (var index = 0; index < initialBeings; index++)
        {
            var x = DeterministicNoise.Range(_config.Seed, 0, index, 0, 10, _config.Width);
            var y = DeterministicNoise.Range(_config.Seed, 0, index, 0, 11, _config.Height);
            var kind = index % 6 is 0 ? BeingKind.Oracle : index % 3 is 0 ? BeingKind.SynthBeast : BeingKind.Wanderer;
            AddBeing(new(x, y), kind, GeneratedName(kind, index), 55 + index % 20);
        }

        var capitalPosition = FindMostVitalTile();
        AddSettlement(capitalPosition, "First Lantern", 24);
        Record($"TURN 000 · {_config.Name} wakes beneath a fractured violet sun.");
    }

    private string SimulateTurn()
    {
        Turn++;
        EvolveTerrain();
        ActBeings();
        ResolveRifts();
        SeedSpontaneousLife();

        var summary = BuildTurnSummary();
        Record(summary);
        return $"Turn {Turn} resolved autonomously.";
    }

    private void EvolveTerrain()
    {
        var nextAether = new int[_config.Width, _config.Height];
        var nextVitality = new int[_config.Width, _config.Height];

        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                var tile = _tiles[x, y];
                var neighbors = OrthogonalNeighbors(tile.Position).Select(GetTile).ToArray();
                var neighborAether = (int)neighbors.Average(static value => value.Aether);
                var pulse = DeterministicNoise.Range(_config.Seed, Turn, x, y, 20, 7) - 3;
                var terrainGrowth = tile.Terrain switch
                {
                    TerrainKind.CrystalForest => 3,
                    TerrainKind.DreamMarsh => 2,
                    TerrainKind.AshDunes => -2,
                    _ => 0
                };

                nextAether[x, y] = Math.Clamp(tile.Aether + (neighborAether - tile.Aether) / 6 + pulse, 0, 100);
                nextVitality[x, y] = Math.Clamp(tile.Vitality + terrainGrowth + tile.Aether / 35 - 1, 0, 100);
            }
        }

        for (var y = 0; y < _config.Height; y++)
        {
            for (var x = 0; x < _config.Width; x++)
            {
                var tile = _tiles[x, y];
                tile.Aether = nextAether[x, y];
                tile.Vitality = nextVitality[x, y];
                tile.Stability = Math.Clamp(tile.Stability + (tile.Aether is > 80 ? -2 : 1), 0, 100);

                if (tile.Vitality > 82 && tile.Terrain is TerrainKind.AshDunes or TerrainKind.IronSteppe)
                {
                    tile.Terrain = TerrainKind.CrystalForest;
                }
                else if (tile.Stability < 18 && tile.Terrain is not TerrainKind.AetherSea)
                {
                    tile.Terrain = TerrainKind.StarGlass;
                }
            }
        }
    }

    private void ActBeings()
    {
        var newborns = new List<(GridPoint Position, BeingKind Kind, string Name, int Energy)>();

        foreach (var being in _beings.OrderBy(static value => value.Id).ToArray())
        {
            being.Age++;
            var tile = GetTile(being.Position);

            switch (being.Kind)
            {
                case BeingKind.Settlement:
                    var growth = tile.Vitality / 24 + tile.Aether / 40 - 2;
                    being.Population = Math.Max(1, being.Population + growth);
                    being.Energy = Math.Clamp(being.Energy + tile.Aether / 18 - 2, 0, 100);
                    being.Intent = growth >= 0 ? "Cultivating a luminous district" : "Enduring a lean cycle";
                    tile.Vitality = Math.Max(0, tile.Vitality - Math.Max(1, being.Population / 35));
                    if (being.Population > 55 && being.Age % 9 is 0 && _beings.Count + newborns.Count < 80)
                    {
                        var destination = BestNeighbor(being, preferVitality: true);
                        newborns.Add((destination, BeingKind.Wanderer, $"Pilgrim {being.Id}-{being.Age}", 48));
                        being.Population -= 8;
                    }

                    break;

                case BeingKind.Rift:
                    being.Energy = Math.Max(0, being.Energy - 3);
                    being.Intent = "Distorting adjacent reality";
                    foreach (var point in OrthogonalNeighbors(being.Position).Append(being.Position))
                    {
                        var affected = GetTile(point);
                        affected.Stability = Math.Max(0, affected.Stability - 4);
                        affected.Aether = Math.Min(100, affected.Aether + 3);
                    }

                    break;

                default:
                    being.Position = BestNeighbor(being, preferVitality: being.Kind is not BeingKind.Oracle);
                    tile = GetTile(being.Position);
                    var harvest = Math.Min(tile.Aether, being.Kind is BeingKind.Oracle ? 3 : 6);
                    tile.Aether -= harvest;
                    being.Energy = Math.Clamp(being.Energy + harvest - 4, 0, 100);
                    being.Intent = being.Kind switch
                    {
                        BeingKind.Oracle => "Reading tomorrow's ruins",
                        BeingKind.SynthBeast => "Grazing on ferrous spores",
                        _ => "Following aether currents"
                    };

                    if (being.Energy > 82 && being.Age % 7 is 0 && _beings.Count + newborns.Count < 80)
                    {
                        newborns.Add((being.Position, being.Kind, GeneratedName(being.Kind, being.Id + Turn), 42));
                        being.Energy -= 24;
                    }

                    break;
            }
        }

        _beings.RemoveAll(static being => being.Energy <= 0 && being.Kind is not BeingKind.Settlement);
        foreach (var newborn in newborns)
        {
            AddBeing(newborn.Position, newborn.Kind, newborn.Name, newborn.Energy);
        }
    }

    private void ResolveRifts()
    {
        var collapsed = _beings
            .Where(static being => being.Kind is BeingKind.Rift && being.Energy <= 0)
            .ToArray();

        foreach (var rift in collapsed)
        {
            GetTile(rift.Position).Stability = Math.Min(100, GetTile(rift.Position).Stability + 35);
            _beings.Remove(rift);
        }

        if (Turn % 11 is 0)
        {
            var unstable = EnumerateTiles().OrderBy(static tile => tile.Stability).First();
            if (unstable.Stability < 28 && !_beings.Any(being => being.Kind is BeingKind.Rift && being.Position == unstable.Position))
            {
                AddBeing(unstable.Position, BeingKind.Rift, $"Rift {Turn}", 28);
                Record($"TURN {Turn:000} · A singing rift opens at {unstable.Position}.");
            }
        }
    }

    private void SeedSpontaneousLife()
    {
        if (Turn % 5 is not 0 || _beings.Count >= 80)
        {
            return;
        }

        var fertile = EnumerateTiles()
            .Where(static tile => tile.Vitality >= 75)
            .OrderByDescending(static tile => tile.Vitality + tile.Aether)
            .ThenBy(static tile => tile.Position.Y)
            .ThenBy(static tile => tile.Position.X)
            .FirstOrDefault();

        if (fertile is not null)
        {
            AddBeing(fertile.Position, BeingKind.SynthBeast, GeneratedName(BeingKind.SynthBeast, Turn), 52);
        }
    }

    private string Infuse(InfuseAether action)
    {
        var tile = RequireTile(action.Position);
        var amount = Math.Clamp(action.Amount, 1, 100);
        tile.Aether = Math.Clamp(tile.Aether + amount, 0, 100);
        tile.Stability = Math.Max(0, tile.Stability - amount / 8);
        var message = $"The Creator infused {action.Position} with {amount} aether.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Transmute(TransmuteTerrain action)
    {
        var tile = RequireTile(action.Position);
        var former = tile.Terrain;
        tile.Terrain = action.Terrain;
        tile.Vitality = Math.Clamp((tile.Vitality + BaseVitality(action.Terrain)) / 2, 0, 100);
        var message = $"The Creator transmuted {TerrainName(former)} at {action.Position} into {TerrainName(action.Terrain)}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Create(CreateLife action)
    {
        RequireTile(action.Position);
        if (action.Kind is BeingKind.Settlement or BeingKind.Rift)
        {
            throw new InvalidOperationException("Use the dedicated settlement or cataclysm command for that being kind.");
        }

        var being = AddBeing(action.Position, action.Kind, GeneratedName(action.Kind, _nextBeingId + Turn), 65);
        var message = $"The Creator shaped {being.Name} at {action.Position}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Found(FoundSettlement action)
    {
        RequireTile(action.Position);
        if (string.IsNullOrWhiteSpace(action.Name) || action.Name.Length > 32)
        {
            throw new ArgumentException("Settlement name must contain 1 to 32 characters.", nameof(action));
        }

        AddSettlement(action.Position, action.Name.Trim(), 18);
        var message = $"The Creator founded {action.Name.Trim()} at {action.Position}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private string Cataclysm(InvokeCataclysm action)
    {
        RequireTile(action.Position);
        var radius = Math.Clamp(action.Radius, 0, 4);
        foreach (var tile in EnumerateTiles().Where(tile => Manhattan(tile.Position, action.Position) <= radius))
        {
            tile.Aether = Math.Min(100, tile.Aether + 25);
            tile.Vitality = Math.Max(0, tile.Vitality - 35);
            tile.Stability = Math.Max(0, tile.Stability - 45);
            tile.Terrain = TerrainKind.StarGlass;
        }

        _beings.RemoveAll(being => Manhattan(being.Position, action.Position) <= radius && being.Kind is not BeingKind.Rift);
        AddBeing(action.Position, BeingKind.Rift, $"Creator Rift {Turn}", 40);
        var message = $"A radius-{radius} cataclysm remade the world around {action.Position}.";
        Record($"TURN {Turn:000} · {message}");
        return message;
    }

    private GridPoint BestNeighbor(BeingState being, bool preferVitality)
    {
        return OrthogonalNeighbors(being.Position)
            .Append(being.Position)
            .Select(point => new
            {
                Point = point,
                Score = GetTile(point).Aether + (preferVitality ? GetTile(point).Vitality : GetTile(point).Stability) +
                        DeterministicNoise.Range(_config.Seed, Turn, point.X, point.Y, being.Id, 9)
            })
            .OrderByDescending(static candidate => candidate.Score)
            .ThenBy(static candidate => candidate.Point.Y)
            .ThenBy(static candidate => candidate.Point.X)
            .First()
            .Point;
    }

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
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the world grid.");
        }

        return GetTile(position);
    }

    private TileState GetTile(GridPoint position) => _tiles[position.X, position.Y];

    private BeingState AddBeing(GridPoint position, BeingKind kind, string name, int energy)
    {
        var being = new BeingState
        {
            Id = _nextBeingId++,
            Position = position,
            Kind = kind,
            Name = name,
            Energy = energy,
            Population = 1
        };
        _beings.Add(being);
        return being;
    }

    private BeingState AddSettlement(GridPoint position, string name, int population)
    {
        var settlement = AddBeing(position, BeingKind.Settlement, name, 65);
        settlement.Population = population;
        settlement.Intent = "Mapping the newborn world";
        return settlement;
    }

    private GridPoint FindMostVitalTile() => EnumerateTiles()
        .OrderByDescending(static tile => tile.Vitality + tile.Stability)
        .ThenBy(static tile => tile.Position.Y)
        .ThenBy(static tile => tile.Position.X)
        .First()
        .Position;

    private string BuildTurnSummary()
    {
        var settlements = _beings.Count(static being => being.Kind is BeingKind.Settlement);
        var living = _beings.Count(static being => being.Kind is not BeingKind.Rift);
        var weakest = EnumerateTiles().Min(static tile => tile.Stability);
        return $"TURN {Turn:000} · {living} beings wander; {settlements} lantern-cities endure; lowest stability is {weakest}.";
    }

    private void Record(string message)
    {
        _chronicle.Insert(0, message);
        if (_chronicle.Count > ChronicleLimit)
        {
            _chronicle.RemoveRange(ChronicleLimit, _chronicle.Count - ChronicleLimit);
        }
    }

    private static int Manhattan(GridPoint left, GridPoint right) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private static int BaseAether(TerrainKind terrain) => terrain switch
    {
        TerrainKind.AetherSea => 68,
        TerrainKind.CrystalForest => 45,
        TerrainKind.DreamMarsh => 38,
        TerrainKind.StarGlass => 52,
        TerrainKind.IronSteppe => 24,
        _ => 17
    };

    private static int BaseVitality(TerrainKind terrain) => terrain switch
    {
        TerrainKind.CrystalForest => 65,
        TerrainKind.DreamMarsh => 53,
        TerrainKind.IronSteppe => 35,
        TerrainKind.AetherSea => 28,
        TerrainKind.StarGlass => 20,
        _ => 12
    };

    private static string TerrainGlyph(TerrainKind terrain) => terrain switch
    {
        TerrainKind.StarGlass => "◇",
        TerrainKind.AshDunes => "∴",
        TerrainKind.AetherSea => "≈",
        TerrainKind.CrystalForest => "♢",
        TerrainKind.IronSteppe => "≡",
        TerrainKind.DreamMarsh => "~",
        _ => "?"
    };

    private static string BeingGlyph(BeingKind kind) => kind switch
    {
        BeingKind.Wanderer => "w",
        BeingKind.SynthBeast => "b",
        BeingKind.Oracle => "o",
        BeingKind.Settlement => "A",
        BeingKind.Rift => "×",
        _ => "?"
    };

    private static string TerrainName(TerrainKind terrain) => terrain switch
    {
        TerrainKind.StarGlass => "star-glass",
        TerrainKind.AshDunes => "ash dunes",
        TerrainKind.AetherSea => "aether sea",
        TerrainKind.CrystalForest => "crystal forest",
        TerrainKind.IronSteppe => "iron steppe",
        TerrainKind.DreamMarsh => "dream marsh",
        _ => terrain.ToString()
    };

    private static string GeneratedName(BeingKind kind, int number) => kind switch
    {
        BeingKind.Wanderer => $"Vagrant-{number:X}",
        BeingKind.SynthBeast => $"Ferric Moth {number:X}",
        BeingKind.Oracle => $"Oracle {number:X}",
        BeingKind.Rift => $"Rift {number:X}",
        _ => $"Being {number:X}"
    };
}
