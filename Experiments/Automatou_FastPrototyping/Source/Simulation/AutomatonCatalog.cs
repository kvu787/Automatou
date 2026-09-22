namespace Automatou.Simulation;

// Registration belongs to the host, never to the individual programs.
public static class AutomatonCatalog {
    public static readonly string[] Names = ["Standard", "Lone wolf", "Keep your distance", "Hunt the weakest"];
    public static UnitAutomaton Create(string name) {
        return name switch {
            "Standard" => new StandardAutomaton(),
            "Lone wolf" => new LoneWolfAutomaton(),
            "Keep your distance" => new KeepYourDistanceAutomaton(),
            "Hunt the weakest" => new HuntTheWeakestAutomaton(),
            _ => throw new ArgumentException("Unknown automaton program.", nameof(name))
        };
    }

    public static void Assign(Unit unit, string name) {
        if (unit is TrainingTarget) { throw new InvalidOperationException("Training targets use Hold."); }
        UnitAutomaton next = Create(name);
        next.Settings = unit.AutomatonInstance.Settings with { };
        if (name == "Keep your distance") { next.Settings.Spacing = Math.Min(next.Settings.Spacing, unit.Range - 1); }
        next.Validate(unit.Statistics);
        unit.AutomatonInstance = next;
    }
}
