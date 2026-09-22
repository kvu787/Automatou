namespace Automatou.Simulation;

public sealed class PrytuHunter : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Prytu hunter", Health = 75, Armor = 3, Damage = 28, MeleeDamage = 28, Range = 1, TurnEnergy = 8 };
    public PrytuHunter() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .95, Caution = .15, Commitment = .1, PreferWounded = true, UseBonds = false } }; }
    public override Unit CreateFresh() {
        return new PrytuHunter();
    }
}
