namespace Automatou.Simulation;

public sealed class KeepYourDistanceAutomaton : StandardAutomaton {
    public override string ProgramName => "Keep your distance";
    protected override UnitAutomaton CreateInstance() {
        return new KeepYourDistanceAutomaton();
    }

    public override void Validate(UnitStatistics body) {
        base.Validate(body);
        if (body.Range <= 1 || this.Settings.Spacing >= body.Range) {
            throw new InvalidDataException("Keep your distance requires a ranged unit and 1 <= spacing < attack range.");
        }
    }

    protected override IReadOnlyList<UnitAction> Think(VisionObservation vision, TerrainObservation terrain, int energy) {
        ActionPlanning plan = new(vision, terrain, energy);
        if (!plan.MaintainSpacing(plan.Enemies, this.Settings.Spacing)) {
            this.Explain(vision.World.Turn, "Keep distance", $"First priority: keep {this.Settings.Spacing} hex steps from every observed enemy.", destination: plan.Self.Position);
        } else { this.PlanStandard(vision, terrain, plan); }
        return plan.Actions;
    }
}
