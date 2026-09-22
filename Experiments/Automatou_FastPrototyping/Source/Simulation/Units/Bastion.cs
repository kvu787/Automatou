namespace Automatou.Simulation;

public sealed class Bastion : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Bastion", Size = 2, Health = 500, Armor = 22, Damage = 52, MeleeDamage = 75, Range = 4, TurnEnergy = 6, SightRange = 14 };
    public Bastion() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .85, Caution = .3, Commitment = .25 } }; }
    public override Unit CreateFresh() {
        return new Bastion();
    }
}
