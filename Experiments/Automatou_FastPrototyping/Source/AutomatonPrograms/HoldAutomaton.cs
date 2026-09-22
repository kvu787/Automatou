namespace Automatou.Simulation;

public sealed class HoldAutomaton : StandardAutomaton {
    public override string ProgramName => "Hold";
    protected override UnitAutomaton CreateInstance() {
        return new HoldAutomaton();
    }

    protected override IReadOnlyList<UnitAction> Think(VisionObservation vision, TerrainObservation terrain, int energy) {
        this.Explain(vision.World.Turn, "Hold", "An inert target for controlled experiments.");
        return [];
    }
}
