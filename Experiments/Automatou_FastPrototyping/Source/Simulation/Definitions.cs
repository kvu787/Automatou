namespace Automatou.Simulation;

public enum Terrain { Water, Air, Space, Forest, Plains, Mountain, Wetlands, Paved, Desert, Tundra, ExclusionZone }
public enum Faction { Bastions, Travelers, MechAndTank, InfantryAndArtillery, Prytu }
public enum Mobility { Ground, Amphibious, Flight, Spaceflight }

public sealed record SimulationSettings
{
    public bool LimitedPerception { get; set; } = true;
    public bool HeatEnabled { get; set; } = true;
    public bool BondsEnabled { get; set; } = true;
}

public sealed class Entity
{
    public int Id { get; set; }
    public Faction Faction { get; set; }
    public Hex Position { get; set; }
    public int Facing { get; set; }
    public int Health { get; set; }
    public required Unit Unit { get; set; }
    public bool Stationary { get; set; }
    public int Heat { get; set; }
    public bool WeaponLocked { get; set; }
    public int? BondedUnitId { get; set; }
    public string Name => Unit.Name;
    public int MaximumHealth => Unit.Health;
    public IEnumerable<Hex> OccupiedCells() => Hex.Disk(Unit.Size).Select(cell => Position + cell);
}

public static class Catalog
{
    public static readonly string[] FactionNames = ["Bastions", "Travelers", "Mech & tank", "Infantry & artillery", "Prytu"];
    public static readonly string[] FactionColors = ["f3c66b", "66d9df", "84b4fb", "f39379", "bd95e9"];
    public static readonly string[] TerrainNames = ["Water", "Air", "Space", "Forest", "Plains", "Mountain", "Wetlands", "Paved", "Desert", "Tundra", "Exclusion zone"];
    public static readonly string[] TerrainColors = ["193e55", "354754", "191f36", "244a3c", "354638", "4a4b50", "345454", "555760", "67563c", "526468", "442c3d"];
    public static List<Unit> Units() =>
        [new Bastion(), new TravelerOutrider(), new Home(), new SiegeWalker(), new CloneInfantry(), new LongbowArtillery(), new PrytuHunter(), new PrytuManifestation(), new TrainingTarget()];
}
