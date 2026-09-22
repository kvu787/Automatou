namespace Automatou.Simulation;

public sealed record UnitStatistics {
    public string Name { get; init; } = "Unit";
    public int Size { get; init; } = 1;
    public int Health { get; init; } = 80;
    public int Armor { get; init; } = 3;
    public int Damage { get; init; } = 18;
    public int MeleeDamage { get; init; } = 18;
    public int Range { get; init; } = 3;
    public int ActionPoints { get; init; } = 4;
    public int Evasion { get; init; }
    public int BlastRadius { get; init; }
    public int SightRange { get; init; } = 10;
    public int HeatPerShot { get; init; }
    public int CoolingPerTurn { get; init; } = 15;
    public Mobility Mobility { get; init; } = Mobility.Ground;
}

public abstract class Unit {
    public Unit Copy() {
        Unit copy = this.CreateFresh();
        this.AutomatonInstance.CopyTo(copy.AutomatonInstance);
        return copy;
    }

    public abstract UnitStatistics Statistics { get; }
    public abstract UnitAutomaton AutomatonInstance { get; }
    public abstract Unit CreateFresh();
    public string Name => this.Statistics.Name;
    public int Size => this.Statistics.Size;
    public int Health => this.Statistics.Health;
    public int Armor => this.Statistics.Armor;
    public int Damage => this.Statistics.Damage;
    public int MeleeDamage => this.Statistics.MeleeDamage;
    public int Range => this.Statistics.Range;
    public int ActionPoints => this.Statistics.ActionPoints;
    public int Evasion => this.Statistics.Evasion;
    public int BlastRadius => this.Statistics.BlastRadius;
    public int SightRange => this.Statistics.SightRange;
    public int HeatPerShot => this.Statistics.HeatPerShot;
    public int CoolingPerTurn => this.Statistics.CoolingPerTurn;
    public Mobility Mobility => this.Statistics.Mobility;
    public void Validate() {
        if (this.AutomatonInstance is null || string.IsNullOrWhiteSpace(this.Name) || this.Size is < 1 or > 12 || this.Health is < 1 or > 10000 ||
            this.Armor is < 0 or > 1000 || this.Damage is < 1 or > 1000 || this.MeleeDamage is < 1 or > 1000 || this.Range is < 1 or > 30 ||
            this.ActionPoints is < 1 or > 20 || this.Evasion is < 0 or > 90 || this.BlastRadius is < 0 or > 3 || this.SightRange is < 1 or > 40 ||
            this.HeatPerShot is < 0 or > 100 || this.CoolingPerTurn is < 0 or > 100 || !Enum.IsDefined(this.Mobility)) {
            throw new InvalidDataException("Invalid source-defined unit or missing automaton memory.");
        }

        this.AutomatonInstance.ValidateMemory();
    }
}
