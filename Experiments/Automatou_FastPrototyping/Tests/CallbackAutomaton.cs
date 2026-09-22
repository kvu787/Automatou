using Automatou.Simulation;

sealed class CallbackAutomaton(Func<IAutomatonSystemCalls, ActionPlan> callback) : UnitAutomaton {
    public override string ProgramName => "Test callback";
    protected override UnitAutomaton CreateInstance() {
        return new CallbackAutomaton(callback);
    }

    public override ActionPlan RunTurn(IAutomatonSystemCalls system) {
        return callback(system);
    }

    protected override IReadOnlyList<UnitAction> Think(VisionObservation vision, TerrainObservation terrain, int energy) {
        throw new NotSupportedException();
    }
}
