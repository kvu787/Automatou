namespace Automatou.Simulation;

public sealed class TravelerOutrider : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Traveler outrider", Health = 65, Armor = 2, Damage = 22, Range = 2, TurnEnergy = 9, Evasion = 30 };
    public TravelerOutrider() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .6, Caution = .85, Commitment = .15, KeepDistance = true } }; }
    public override Unit CreateFresh() {
        return new TravelerOutrider();
    }
}
