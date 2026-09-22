using Automatou.Simulation;

enum TestBehavior { Advance, Hold, Skirmish }
sealed class TestUnit : Unit {
    private UnitStatistics statistics = new() { TurnEnergy = 6 };
    public override UnitStatistics Statistics => this.statistics;
    public TestBehavior Behavior { init => this.AutomatonInstance = value == TestBehavior.Hold ? new HoldAutomaton() : new StandardAutomaton { Settings = new() { Aggression = value == TestBehavior.Skirmish ? .1 : .8, Caution = value == TestBehavior.Skirmish ? 1 : .1, KeepDistance = value == TestBehavior.Skirmish } }; }
    public Func<IAutomatonSystemCalls, ActionPlan> Actions { init => this.AutomatonInstance = new CallbackAutomaton(value); }
    public override Unit CreateFresh() {
        return new TestUnit { statistics = this.statistics };
    }

    public new string Name { get => this.statistics.Name; init => this.statistics = this.statistics with { Name = value }; }
    public new int Size { get => this.statistics.Size; init => this.statistics = this.statistics with { Size = value }; }
    public new int Health { get => this.statistics.Health; init => this.statistics = this.statistics with { Health = value }; }
    public new int Armor { get => this.statistics.Armor; init => this.statistics = this.statistics with { Armor = value }; }
    public new int Damage { get => this.statistics.Damage; init => this.statistics = this.statistics with { Damage = value }; }
    public new int MeleeDamage { get => this.statistics.MeleeDamage; init => this.statistics = this.statistics with { MeleeDamage = value }; }
    public new int Range { get => this.statistics.Range; init => this.statistics = this.statistics with { Range = value }; }
    public new int TurnEnergy { get => this.statistics.TurnEnergy; init => this.statistics = this.statistics with { TurnEnergy = value }; }
    public new int Evasion { get => this.statistics.Evasion; init => this.statistics = this.statistics with { Evasion = value }; }
    public new int BlastRadius { get => this.statistics.BlastRadius; init => this.statistics = this.statistics with { BlastRadius = value }; }
    public new int SightRange { get => this.statistics.SightRange; init => this.statistics = this.statistics with { SightRange = value }; }
    public new int HeatPerShot { get => this.statistics.HeatPerShot; init => this.statistics = this.statistics with { HeatPerShot = value }; }
    public new int CoolingPerTurn { get => this.statistics.CoolingPerTurn; init => this.statistics = this.statistics with { CoolingPerTurn = value }; }
    public new Mobility Mobility { get => this.statistics.Mobility; init => this.statistics = this.statistics with { Mobility = value }; }
}
