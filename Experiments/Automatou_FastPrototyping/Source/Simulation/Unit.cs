using System.Text.Json.Serialization;

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

[JsonPolymorphic(TypeDiscriminatorPropertyName = "UnitType")]
[JsonDerivedType(typeof(Bastion), "Bastion")]
[JsonDerivedType(typeof(TravelerOutrider), "TravelerOutrider")]
[JsonDerivedType(typeof(Home), "Home")]
[JsonDerivedType(typeof(SiegeWalker), "SiegeWalker")]
[JsonDerivedType(typeof(CloneInfantry), "CloneInfantry")]
[JsonDerivedType(typeof(LongbowArtillery), "LongbowArtillery")]
[JsonDerivedType(typeof(PrytuHunter), "PrytuHunter")]
[JsonDerivedType(typeof(PrytuManifestation), "PrytuManifestation")]
[JsonDerivedType(typeof(TrainingTarget), "TrainingTarget")]
public abstract class Unit {
    [JsonIgnore] public abstract UnitStatistics Statistics { get; }
    [JsonIgnore] public abstract UnitAutomaton Brain { get; }
    public abstract Unit CreateFresh();
    [JsonIgnore] public string Name => this.Statistics.Name;
    [JsonIgnore] public int Size => this.Statistics.Size;
    [JsonIgnore] public int Health => this.Statistics.Health;
    [JsonIgnore] public int Armor => this.Statistics.Armor;
    [JsonIgnore] public int Damage => this.Statistics.Damage;
    [JsonIgnore] public int MeleeDamage => this.Statistics.MeleeDamage;
    [JsonIgnore] public int Range => this.Statistics.Range;
    [JsonIgnore] public int ActionPoints => this.Statistics.ActionPoints;
    [JsonIgnore] public int Evasion => this.Statistics.Evasion;
    [JsonIgnore] public int BlastRadius => this.Statistics.BlastRadius;
    [JsonIgnore] public int SightRange => this.Statistics.SightRange;
    [JsonIgnore] public int HeatPerShot => this.Statistics.HeatPerShot;
    [JsonIgnore] public int CoolingPerTurn => this.Statistics.CoolingPerTurn;
    [JsonIgnore] public Mobility Mobility => this.Statistics.Mobility;
    public void Validate() {
        if (this.Brain is null || string.IsNullOrWhiteSpace(this.Name) || this.Size is < 1 or > 12 || this.Health is < 1 or > 10000 ||
            this.Armor is < 0 or > 1000 || this.Damage is < 1 or > 1000 || this.MeleeDamage is < 1 or > 1000 || this.Range is < 1 or > 30 ||
            this.ActionPoints is < 1 or > 20 || this.Evasion is < 0 or > 90 || this.BlastRadius is < 0 or > 3 || this.SightRange is < 1 or > 40 ||
            this.HeatPerShot is < 0 or > 100 || this.CoolingPerTurn is < 0 or > 100 || !Enum.IsDefined(this.Mobility)) {
            throw new InvalidDataException("Invalid source-defined unit or missing automaton memory.");
        }

        this.Brain.ValidateMemory();
    }
}
