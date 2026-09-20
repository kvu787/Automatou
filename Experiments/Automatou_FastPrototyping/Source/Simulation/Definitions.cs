namespace Automatou.Simulation;

public enum Terrain { Forest, Plains, Mountain, Water, ExclusionZone }
public enum Faction { Bastions, Travelers, MechAndTank, InfantryAndArtillery, Prytu }
public enum Mobility { Ground, Amphibious, Flight, Spaceflight }

public sealed record SimulationSettings {
    public bool LimitedPerception { get; set; } = true;
    public bool HeatEnabled { get; set; } = true;
    public bool BondsEnabled { get; set; } = true;
}

public sealed class Entity {
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
    public string Name => this.Unit.Name;
    public int MaximumHealth => this.Unit.Health;
    public IEnumerable<Hex> OccupiedCells() {
        return Hex.Disk(this.Unit.Size).Select(cell => this.Position + cell);
    }
}

public static class Catalog {
    public static readonly string[] FactionNames = ["Bastions", "Travelers", "Mech & tank", "Infantry & artillery", "Prytu"];
    public static readonly string[] FactionColors = ["f3c66b", "66d9df", "84b4fb", "f39379", "bd95e9"];
    public static readonly string[] TerrainNames = ["Forest", "Plains", "Mountains", "Water", "Exclusion zone"];
    public static readonly string[] TerrainColors = ["28543c", "807345", "616773", "245c86", "713b61"];
    public static List<Unit> Units() {
        return [new Bastion(), new TravelerOutrider(), new Home(), new SiegeWalker(), new CloneInfantry(), new LongbowArtillery(), new PrytuHunter(), new PrytuManifestation(), new TrainingTarget()];
    }
}
