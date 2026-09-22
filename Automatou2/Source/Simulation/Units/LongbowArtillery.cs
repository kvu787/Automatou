namespace Automatou.Simulation;

public sealed class LongbowArtillery : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Longbow artillery", Health = 90, Armor = 3, Damage = 60, Range = 9, TurnEnergy = 7, BlastRadius = 1 };
    public LongbowArtillery() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .75, Caution = .4, Commitment = .2, KeepDistance = true, ConsiderBlast = true } }; }
    public override Unit CreateFresh() {
        return new LongbowArtillery();
    }
}
