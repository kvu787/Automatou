using Automatou.Simulation;

// Test-only units can request arbitrary actions without adding a production behavior switch.
enum TestBehavior { Advance, Hold, Skirmish }
sealed class TestUnit : Unit
{
    private UnitStatistics statistics = new();
    public override UnitStatistics Statistics => statistics;
    public TestBehavior Behavior { get; init; }
    public Func<UnitSenses, IEnumerable<UnitAction>>? Actions { get; init; }
    private UnitAutomaton? brain;
    public override UnitAutomaton Brain => brain ??= new TestAutomaton(this);
    public override Unit CreateFresh() => new TestUnit();
    public new string Name { get => statistics.Name; init => statistics = statistics with { Name = value }; }
    public new int Size { get => statistics.Size; init => statistics = statistics with { Size = value }; }
    public new int Health { get => statistics.Health; init => statistics = statistics with { Health = value }; }
    public new int Armor { get => statistics.Armor; init => statistics = statistics with { Armor = value }; }
    public new int Damage { get => statistics.Damage; init => statistics = statistics with { Damage = value }; }
    public new int MeleeDamage { get => statistics.MeleeDamage; init => statistics = statistics with { MeleeDamage = value }; }
    public new int Range { get => statistics.Range; init => statistics = statistics with { Range = value }; }
    public new int ActionPoints { get => statistics.ActionPoints; init => statistics = statistics with { ActionPoints = value }; }
    public new int Evasion { get => statistics.Evasion; init => statistics = statistics with { Evasion = value }; }
    public new int BlastRadius { get => statistics.BlastRadius; init => statistics = statistics with { BlastRadius = value }; }
    public new int SightRange { get => statistics.SightRange; init => statistics = statistics with { SightRange = value }; }
    public new int HeatPerShot { get => statistics.HeatPerShot; init => statistics = statistics with { HeatPerShot = value }; }
    public new int CoolingPerTurn { get => statistics.CoolingPerTurn; init => statistics = statistics with { CoolingPerTurn = value }; }
    public new Mobility Mobility { get => statistics.Mobility; init => statistics = statistics with { Mobility = value }; }
    private sealed class TestAutomaton(TestUnit unit) : UnitAutomaton
    {
        public override IEnumerable<UnitAction> Act(UnitSenses senses)
        {
            if (unit.Actions is not null)
            {
                foreach (var action in unit.Actions(senses)) yield return action;
                yield break;
            }
            while (senses.RemainingPoints > 0)
            {
                var observation = senses.Observe();
                var target = TacticalPlanning.SelectTarget(observation, null, false);
                if (target is null) yield break;
                if (unit.Behavior == TestBehavior.Hold && observation.Self.Cells.Min(c => target.Cells.Min(c.Distance)) > unit.Range) yield break;
                var action = TacticalPlanning.Engage(senses, observation, target, unit.Behavior == TestBehavior.Skirmish);
                if (action is null) yield break;
                yield return action;
            }
        }
    }
}
