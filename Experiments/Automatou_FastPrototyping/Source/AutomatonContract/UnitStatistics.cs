namespace Automatou.Simulation;

public sealed record UnitStatistics {
    public string Name { get; init; } = "Unit";
    public int Size { get; init; } = 1;
    public int Health { get; init; } = 80;
    public int Armor { get; init; } = 3;
    public int Damage { get; init; } = 18;
    public int MeleeDamage { get; init; } = 18;
    public int Range { get; init; } = 3;
    public int TurnEnergy { get; init; } = 4;
    public int Evasion { get; init; }
    public int BlastRadius { get; init; }
    public int SightRange { get; init; } = 10;
    public int HeatPerShot { get; init; }
    public int CoolingPerTurn { get; init; } = 15;
    public Mobility Mobility { get; init; } = Mobility.Ground;
}
