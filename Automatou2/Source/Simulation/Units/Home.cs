namespace Automatou.Simulation;

public sealed class Home : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "H.O.M.E.", Size = 3, Health = 380, Armor = 8, Damage = 15, Range = 2, TurnEnergy = 7, Evasion = 15, Mobility = Mobility.Amphibious };
    public Home() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .35, Caution = .95, Commitment = .2, KeepDistance = true } }; }
    public override Unit CreateFresh() {
        return new Home();
    }
}
