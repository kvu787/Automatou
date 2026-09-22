namespace Automatou.Simulation;

public sealed class TrainingTarget : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Training target", Health = 5000, Armor = 0, Damage = 1, MeleeDamage = 1, Range = 1, TurnEnergy = 3 };
    public TrainingTarget() { this.AutomatonInstance = new HoldAutomaton { Settings = new() { Aggression = 0 } }; }
    public override Unit CreateFresh() {
        return new TrainingTarget();
    }
}
