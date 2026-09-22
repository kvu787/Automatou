namespace Automatou.Simulation;

public sealed class CloneInfantry : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Clone infantry", Health = 38, Armor = 1, Damage = 12, Range = 2, TurnEnergy = 7 };
    public CloneInfantry() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .85, Caution = .25, Commitment = .1, PreferWounded = true } }; }
    public override Unit CreateFresh() {
        return new CloneInfantry();
    }
}
