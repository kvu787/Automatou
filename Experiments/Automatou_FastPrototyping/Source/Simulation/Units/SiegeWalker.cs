namespace Automatou.Simulation;

public sealed class SiegeWalker : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Siege walker", Size = 2, Health = 210, Armor = 12, Damage = 35, Range = 3, TurnEnergy = 6, HeatPerShot = 45, CoolingPerTurn = 12 };
    public SiegeWalker() { this.AutomatonInstance = new StandardAutomaton { Settings = new() { Aggression = .65, Caution = .45, Commitment = .15, HeatReserve = 70 } }; }
    public override Unit CreateFresh() {
        return new SiegeWalker();
    }
}
