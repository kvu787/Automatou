namespace Automatou.Simulation;

public sealed class PrytuManifestation : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Prytu manifestation", Size = 3, Health = 600, Armor = 10, Damage = 65, MeleeDamage = 65, Range = 2, TurnEnergy = 6 };
    public PrytuManifestation() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .9, Caution = .15, Commitment = .25, PreferWounded = true, UseBonds = false } }; }
    public override Unit CreateFresh() {
        return new PrytuManifestation();
    }
}
