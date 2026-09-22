namespace Automatou.Simulation;

public sealed class HuntTheWeakestAutomaton : StandardAutomaton {
    public override string ProgramName => "Hunt the weakest";
    protected override UnitAutomaton CreateInstance() {
        return new HuntTheWeakestAutomaton();
    }

    protected override IReadOnlyList<UnitAction> Think(VisionObservation vision, TerrainObservation terrain, int energy) {
        ActionPlanning plan = new(vision, terrain, energy);
        // Health percentage is absolute policy here: distance and commitment cannot
        // promote a healthier enemy. Ties use stable identity, never collection order.
        EntityObservation? target = plan.Enemies.OrderBy(enemy => (double)enemy.Health / enemy.MaximumHealth).ThenBy(enemy => enemy.Id).FirstOrDefault();
        if (target is null) {
            this.Explain(vision.World.Turn, "Wait for contact", "No visible enemy; do not pursue stale or hidden targets.");
        } else {
            this.Explain(vision.World.Turn, "Hunt weakest", $"Only #{target.Id}: {100.0 * target.Health / target.MaximumHealth:0.#}% health.", target.Id, target.Position);
            if (!vision.Settings.HeatEnabled || !this.NeedsCooling(plan.Self)) { plan.Engage(target); }
        }
        return plan.Actions;
    }
}
