namespace Automatou.Simulation;

public enum Terrain { Forest, Plains, Mountain, Water, ExclusionZone }
public enum Faction { Bastions, Travelers, MechAndTank, InfantryAndArtillery, Prytu }
public enum Mobility { Ground, Amphibious, Flight, Spaceflight }

public sealed record SimulationSettings {
    public bool HeatEnabled { get; set; } = true;
    public bool BondsEnabled { get; set; } = true;
}
