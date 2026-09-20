using Automatou.Simulation;

// Test-only units can request arbitrary actions without adding a production behavior switch.
enum TestBehavior { Advance, Hold, Skirmish }
sealed class TestUnit : Unit {
    private UnitStatistics statistics = new();
    public override UnitStatistics Statistics => this.statistics;
    public TestBehavior Behavior { get; init; }
    public Func<UnitSenses, IEnumerable<UnitAction>>? Actions { get; init; }
    private UnitAutomaton? brain;
    public override UnitAutomaton Brain => this.brain ??= new TestAutomaton(this);
    public override Unit CreateFresh() {
        return new TestUnit();
    }

    public new string Name { get => this.statistics.Name; init => this.statistics = this.statistics with { Name = value }; }
    public new int Size { get => this.statistics.Size; init => this.statistics = this.statistics with { Size = value }; }
    public new int Health { get => this.statistics.Health; init => this.statistics = this.statistics with { Health = value }; }
    public new int Armor { get => this.statistics.Armor; init => this.statistics = this.statistics with { Armor = value }; }
    public new int Damage { get => this.statistics.Damage; init => this.statistics = this.statistics with { Damage = value }; }
    public new int MeleeDamage { get => this.statistics.MeleeDamage; init => this.statistics = this.statistics with { MeleeDamage = value }; }
    public new int Range { get => this.statistics.Range; init => this.statistics = this.statistics with { Range = value }; }
    public new int ActionPoints { get => this.statistics.ActionPoints; init => this.statistics = this.statistics with { ActionPoints = value }; }
    public new int Evasion { get => this.statistics.Evasion; init => this.statistics = this.statistics with { Evasion = value }; }
    public new int BlastRadius { get => this.statistics.BlastRadius; init => this.statistics = this.statistics with { BlastRadius = value }; }
    public new int SightRange { get => this.statistics.SightRange; init => this.statistics = this.statistics with { SightRange = value }; }
    public new int HeatPerShot { get => this.statistics.HeatPerShot; init => this.statistics = this.statistics with { HeatPerShot = value }; }
    public new int CoolingPerTurn { get => this.statistics.CoolingPerTurn; init => this.statistics = this.statistics with { CoolingPerTurn = value }; }
    public new Mobility Mobility { get => this.statistics.Mobility; init => this.statistics = this.statistics with { Mobility = value }; }
    private sealed class TestAutomaton(TestUnit unit) : UnitAutomaton {
        public override IEnumerable<UnitAction> Act(UnitSenses senses) {
            if (unit.Actions is not null) {
                foreach (UnitAction action in unit.Actions(senses)) {
                    yield return action;
                }

                yield break;
            }
            while (senses.RemainingPoints > 0) {
                WorldObservation observation = senses.Observe();
                EntityObservation? target = TacticalPlanning.SelectTarget(observation, null, false);
                if (target is null) {
                    yield break;
                }

                if (unit.Behavior == TestBehavior.Hold && observation.Self.Cells.Min(c => target.Cells.Min(c.Distance)) > unit.Range) {
                    yield break;
                }

                UnitAction? action = TacticalPlanning.Engage(senses, observation, target, unit.Behavior == TestBehavior.Skirmish);
                if (action is null) {
                    yield break;
                }

                yield return action;
            }
        }
    }
}
