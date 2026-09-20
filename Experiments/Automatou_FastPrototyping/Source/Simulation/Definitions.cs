namespace Automatou.Simulation;

public enum Terrain { Water, Air, Space, Forest, Plains, Mountain, Wetlands, Paved, Desert, Tundra, ExclusionZone }
public enum Faction { Bastions, Travelers, MechAndTank, InfantryAndArtillery, Prytu }
public enum Mobility { Ground, Amphibious, Flight, Spaceflight }

public sealed class BuildingDesign
{
    public string Name { get; set; } = "New building";
    public int Health { get; set; } = 400;
    public List<Hex> Cells { get; set; } = [new(0, 0)];
    public Hex EditorOrigin { get; set; } = Hex.FromOffset(10, 5);
    public int PatchWidth { get; set; } = 20;
    public int PatchHeight { get; set; } = 10;
    public BuildingDesign Copy() => new() { Name = Name, Health = Health, Cells = [.. Cells], EditorOrigin = EditorOrigin, PatchWidth = PatchWidth, PatchHeight = PatchHeight };
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 60 || Health is < 1 or > 10000 || Cells.Count is < 1 or > 2000 || Cells.Distinct().Count() != Cells.Count || PatchWidth is < 2 or > 100 || PatchHeight is < 2 or > 100)
            throw new InvalidDataException("Building needs a name, health, and 1–2,000 distinct connected cells.");
        var remaining = Cells.ToHashSet();
        var frontier = new Queue<Hex>();
        frontier.Enqueue(Cells[0]); remaining.Remove(Cells[0]);
        while (frontier.TryDequeue(out var cell))
            foreach (var direction in Hex.Directions)
                if (remaining.Remove(cell + direction)) frontier.Enqueue(cell + direction);
        if (remaining.Count != 0) throw new InvalidDataException("A building must form one connected island of cells.");
    }
}

public sealed class Entity
{
    public int Id { get; set; }
    public Faction Faction { get; set; }
    public Hex Position { get; set; }
    public int Facing { get; set; }
    public int Health { get; set; }
    public Unit? Unit { get; set; }
    public BuildingDesign? Building { get; set; }
    public bool Stationary { get; set; }
    public string Name => Unit?.Name ?? Building?.Name ?? "Unknown";
    public int MaximumHealth => Unit?.Health ?? Building?.Health ?? 1;
    public IEnumerable<Hex> OccupiedCells() => Unit is not null
        ? Hex.Disk(Unit.Size).Select(cell => Position + cell)
        : Building!.Cells.Select(cell => Position + cell.Rotate(Facing));
}

public static class Catalog
{
    public static readonly string[] FactionNames = ["Bastions", "Travelers", "Mech & tank", "Infantry & artillery", "Prytu"];
    public static readonly string[] FactionColors = ["f3c66b", "66d9df", "84b4fb", "f39379", "bd95e9"];
    public static readonly string[] TerrainNames = ["Water", "Air", "Space", "Forest", "Plains", "Mountain", "Wetlands", "Paved", "Desert", "Tundra", "Exclusion zone"];
    public static readonly string[] TerrainColors = ["193e55", "354754", "191f36", "244a3c", "354638", "4a4b50", "345454", "555760", "67563c", "526468", "442c3d"];
    public static List<Unit> Units() =>
        [new Bastion(), new TravelerOutrider(), new Home(), new SiegeWalker(), new CloneInfantry(), new LongbowArtillery(), new PrytuHunter(), new PrytuManifestation()];
    // Explicit authored footprint, never procedurally generated.
    public static BuildingDesign Outpost() => new() { Name = "Watch station", Cells = [new(0, 0), new(1, 0), new(0, 1), new(-1, 1), new(-1, 0)], Health = 350 };
}
