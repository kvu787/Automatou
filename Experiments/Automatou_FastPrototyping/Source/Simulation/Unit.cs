using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed record UnitStatistics
{
    public string Name { get; init; } = "Unit";
    public int Size { get; init; } = 1;
    public int Health { get; init; } = 80;
    public int Armor { get; init; } = 3;
    public int Damage { get; init; } = 18;
    public int MeleeDamage { get; init; } = 18;
    public int Range { get; init; } = 3;
    public int ActionPoints { get; init; } = 4;
    public int Evasion { get; init; } = 0;
    public int BlastRadius { get; init; } = 0;
    public int SightRange { get; init; } = 10;
    public int HeatPerShot { get; init; } = 0;
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
public abstract class Unit
{
    [JsonIgnore] public abstract UnitStatistics Statistics { get; }
    [JsonIgnore] public abstract UnitAutomaton Brain { get; }
    public abstract Unit CreateFresh();
    [JsonIgnore] public string Name => Statistics.Name;
    [JsonIgnore] public int Size => Statistics.Size;
    [JsonIgnore] public int Health => Statistics.Health;
    [JsonIgnore] public int Armor => Statistics.Armor;
    [JsonIgnore] public int Damage => Statistics.Damage;
    [JsonIgnore] public int MeleeDamage => Statistics.MeleeDamage;
    [JsonIgnore] public int Range => Statistics.Range;
    [JsonIgnore] public int ActionPoints => Statistics.ActionPoints;
    [JsonIgnore] public int Evasion => Statistics.Evasion;
    [JsonIgnore] public int BlastRadius => Statistics.BlastRadius;
    [JsonIgnore] public int SightRange => Statistics.SightRange;
    [JsonIgnore] public int HeatPerShot => Statistics.HeatPerShot;
    [JsonIgnore] public int CoolingPerTurn => Statistics.CoolingPerTurn;
    [JsonIgnore] public Mobility Mobility => Statistics.Mobility;
    public void Validate()
    {
        if (Brain is null || string.IsNullOrWhiteSpace(Name) || Size is < 1 or > 12 || Health is < 1 or > 10000 ||
            Armor is < 0 or > 1000 || Damage is < 1 or > 1000 || MeleeDamage is < 1 or > 1000 || Range is < 1 or > 30 ||
            ActionPoints is < 1 or > 20 || Evasion is < 0 or > 90 || BlastRadius is < 0 or > 3 || SightRange is < 1 or > 40 ||
            HeatPerShot is < 0 or > 100 || CoolingPerTurn is < 0 or > 100 || !Enum.IsDefined(Mobility))
            throw new InvalidDataException("Invalid source-defined unit or missing automaton memory.");
        Brain.ValidateMemory();
    }
}
