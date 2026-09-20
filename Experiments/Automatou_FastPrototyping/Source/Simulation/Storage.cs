using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public static class Storage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public sealed record CellData(Hex Position, Terrain Terrain);
    public sealed class WorldData
    {
        public List<CellData> Cells { get; set; } = [];
        public List<Entity> Entities { get; set; } = [];
        public int Turn { get; set; }
        public int NextId { get; set; }
        public uint RandomState { get; set; }
        public int Casualties { get; set; }
    }
    public static string Encode(World world) => JsonSerializer.Serialize(new WorldData
    {
        Cells = world.Terrain.Select(c => new CellData(c.Key, c.Value)).ToList(), Entities = world.Entities,
        Turn = world.Turn, NextId = world.NextId, RandomState = world.RandomState, Casualties = world.Casualties
    }, Options);
    public static World Decode(string json)
    {
        var data = JsonSerializer.Deserialize<WorldData>(json, Options) ?? throw new InvalidDataException("Empty world file.");
        if (data.Cells.Count is < 1 or > 20000 || data.Turn < 0 || data.RandomState == 0) throw new InvalidDataException("Invalid world metadata.");
        var world = new World { Turn = data.Turn, NextId = data.NextId, RandomState = data.RandomState, Casualties = data.Casualties };
        foreach (var cell in data.Cells)
            if (!Enum.IsDefined(cell.Terrain) || !world.Terrain.TryAdd(cell.Position, cell.Terrain)) throw new InvalidDataException("Invalid world cell.");
        var identifiers = new HashSet<int>();
        foreach (var entity in data.Entities)
        {
            if ((entity.Unit is null) == (entity.Building is null) || entity.Facing is < 0 or > 5 || !Enum.IsDefined(entity.Faction) || entity.Id < 1 || !identifiers.Add(entity.Id)) throw new InvalidDataException("Invalid entity.");
            entity.Unit?.Validate(); entity.Building?.Validate();
            if (entity.Health <= 0 || entity.Health > entity.MaximumHealth || !world.CanOccupy(entity, entity.Position, entity.Facing, out _)) throw new InvalidDataException("Invalid entity placement or health.");
            world.Entities.Add(entity); world.RebuildOccupancy();
        }
        world.NextId = Math.Max(world.NextId, identifiers.DefaultIfEmpty(0).Max() + 1);
        world.Note("World restored. Automata paused."); return world;
    }
    public static void SaveWorld(string path, World world) => Write(path, Encode(world));
    public static World LoadWorld(string path) => Decode(File.ReadAllText(path));
    public static void SaveDesign<T>(string path, T design) => Write(path, JsonSerializer.Serialize(design, Options));
    public static T LoadDesign<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("Empty design file.");
    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, text);
        File.Move(temporary, path, true);
    }
    public static string FileName(string name)
    {
        string result = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')).Trim();
        if (result.Length == 0) throw new ArgumentException("Enter a name containing letters or numbers.");
        return result;
    }
}
