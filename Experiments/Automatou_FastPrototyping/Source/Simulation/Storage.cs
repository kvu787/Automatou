using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class Storage {
    private readonly Dictionary<string, string> worlds = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> WorldNames => this.worlds.Keys.Order(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public sealed record CellData(Hex Position, Terrain Terrain);
    public sealed class WorldData {
        public List<CellData> Cells { get; set; } = [];
        public List<Entity> Entities { get; set; } = [];
        public int Turn { get; set; }
        public int NextId { get; set; }
        public uint RandomState { get; set; }
        public int Casualties { get; set; }
        public SimulationSettings Settings { get; set; } = new();
    }
    public static string Encode(World world) {
        return JsonSerializer.Serialize(new WorldData {
            Cells = [.. world.Terrain.Select(c => new CellData(c.Key, c.Value))], Entities = world.Entities,
            Turn = world.Turn, NextId = world.NextId, RandomState = world.RandomState, Casualties = world.Casualties, Settings = world.Settings
        }, Options);
    }

    public static World Decode(string json) {
        WorldData data = JsonSerializer.Deserialize<WorldData>(json, Options) ?? throw new InvalidDataException("Empty world file.");
        if (data.Cells is null || data.Entities is null || data.Settings is null || data.Cells.Count is < 1 or > 20000 || data.Turn < 0 || data.RandomState == 0) {
            throw new InvalidDataException("Invalid world metadata.");
        }

        World world = new() { Turn = data.Turn, NextId = data.NextId, RandomState = data.RandomState, Casualties = data.Casualties, Settings = data.Settings };
        foreach (CellData cell in data.Cells) {
            if (!Enum.IsDefined(cell.Terrain) || !world.Terrain.TryAdd(cell.Position, cell.Terrain)) {
                throw new InvalidDataException("Invalid world cell.");
            }
        }

        HashSet<int> identifiers = [];
        foreach (Entity entity in data.Entities) {
            if (entity.Unit is null || entity.Facing is < 0 or > 5 || entity.Heat is < 0 or > 200 || entity.BondedUnitId is < 1 || !Enum.IsDefined(entity.Faction) || entity.Id < 1 || !identifiers.Add(entity.Id)) {
                throw new InvalidDataException("Invalid entity.");
            }

            entity.Unit.Validate();
            if (entity.Health <= 0 || entity.Health > entity.MaximumHealth || !world.CanOccupy(entity, entity.Position, out _)) {
                throw new InvalidDataException("Invalid entity placement or health.");
            }

            world.Entities.Add(entity); world.RebuildOccupancy();
        }
        world.NextId = Math.Max(world.NextId, identifiers.DefaultIfEmpty(0).Max() + 1);
        world.Note("World restored. Simulation paused."); return world;
    }
    public void SaveWorld(string name, World world) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.worlds[name.Trim()] = Encode(world);
    }

    public World LoadWorld(string name) {
        return Decode(this.worlds[name.Trim()]);
    }
}
