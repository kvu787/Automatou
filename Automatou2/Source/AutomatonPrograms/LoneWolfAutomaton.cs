namespace Automatou.Simulation;

public sealed class LoneWolfAutomaton : StandardAutomaton {
    public override string ProgramName => "Lone wolf";
    protected override UnitAutomaton CreateInstance() {
        return new LoneWolfAutomaton();
    }

    protected override IReadOnlyList<UnitAction> Think(VisionObservation vision, TerrainObservation terrain, int energy) {
        ActionPlanning plan = new(vision, terrain, energy);
        EntityObservation[] allies = [.. plan.Entities.Where(entity => entity.Id != plan.Self.Id && entity.Faction == plan.Self.Faction)];
        if (!plan.MaintainSpacing(allies, this.Settings.Spacing)) {
            this.Explain(vision.World.Turn, "Separate", $"First priority: keep {this.Settings.Spacing} hex steps from every observed ally.", destination: plan.Self.Position);
        } else { this.PlanStandard(vision, terrain, plan); }
        return plan.Actions;
    }
}
