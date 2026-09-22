namespace Automatou.Simulation;

// This assembly is the complete public vocabulary available to an automaton.
// Programs reference it, never the world implementation or the Godot interface.
public interface IAutomatonSystemCalls {
    int RemainingEnergy { get; }
    VisionObservation? ScanVision(int extraRange = 0);
    TerrainObservation? SurveyTerrain();
}

public sealed record EntityObservation(int Id, Faction Faction, Hex Position, int Facing,
    int Health, int MaximumHealth, bool Stationary, UnitStatistics Unit, IReadOnlyList<Hex> Cells,
    int Heat = 0, bool WeaponLocked = false, int? BondedUnitId = null);
public sealed record WorldObservation(int Turn, EntityObservation Self, IReadOnlyList<EntityObservation> Entities);
public sealed record VisionObservation(WorldObservation World, SimulationSettings Settings,
    int Range, IReadOnlyList<ActionOutcome> PreviousOutcomes);
public sealed record TerrainObservation(IReadOnlyDictionary<Hex, Terrain> Cells);

public abstract record UnitAction;
public sealed record TurnAction(int Direction) : UnitAction;
public sealed record MoveForwardAction : UnitAction;
public sealed record AttackAction(int TargetId) : UnitAction;
public sealed record ActionOutcome(int Turn, int Index, UnitAction Action, bool Succeeded, int EnergySpent, string Reason);
public sealed record SensingReceipt(string Call, int EnergySpent, int RemainingEnergy, bool Succeeded);
public sealed record ActionPlan(IReadOnlyList<UnitAction> Actions) {
    public static ActionPlan Empty { get; } = new(Array.AsReadOnly(Array.Empty<UnitAction>()));
}

public static class AutomatonCosts {
    public const int Vision = 1;
    public const int TerrainSurvey = 1;
    public const int MaximumExtraRange = 12;
    public const int MaximumActions = 32;
    public static int VisionCost(int extraRange) {
        return Vision + extraRange;
    }
}

public abstract partial class UnitAutomaton {
    public abstract string ProgramName { get; }
    // A program owns its body/configuration constraints. The host invokes this without
    // knowing program-specific policies; new policies never require a resolver switch.
    public virtual void Validate(UnitStatistics body) {
        this.ValidateMemory();
    }
    protected abstract UnitAutomaton CreateInstance();
    public UnitAutomaton Copy() {
        UnitAutomaton copy = this.CreateInstance();
        this.CopyTo(copy);
        return copy;
    }

    // Sense and Remember happen before Think. Act returns a detached list of requests;
    // none of those requests has happened yet, and no world reference crosses this API.
    public virtual ActionPlan RunTurn(IAutomatonSystemCalls system) {
        VisionObservation? vision = this.Sense(system);
        if (vision is null) { this.State.Reason = "Not enough energy to sense."; return ActionPlan.Empty; }
        this.Remember(vision);
        TerrainObservation? terrain = system.SurveyTerrain();
        if (terrain is null) { this.State.Reason = "Not enough energy to survey terrain."; return ActionPlan.Empty; }
        return this.Act(this.Think(vision, terrain, system.RemainingEnergy));
    }
    protected virtual VisionObservation? Sense(IAutomatonSystemCalls system) {
        return system.ScanVision(this.Settings.ExtraSightRange);
    }

    protected virtual void Remember(VisionObservation vision) {
        this.TurnsObserved++;
        WorldObservation observation = vision.World;
        this.State.LastUpdatedTurn = observation.Turn;
        this.State.PreviousOutcomes = [.. vision.PreviousOutcomes];
        this.State.ShotsFired += vision.PreviousOutcomes.Count(outcome => outcome.Succeeded && outcome.Action is AttackAction);
        if (!this.Settings.RememberContacts) { this.State.Contacts.Clear(); return; }
        foreach (EntityObservation enemy in observation.Entities.Where(entity => entity.Faction != observation.Self.Faction)) {
            _ = this.State.Contacts.RemoveAll(contact => contact.Id == enemy.Id);
            this.State.Contacts.Add(new() {
                Id = enemy.Id, Faction = enemy.Faction, Position = enemy.Position,
                LastSeenTurn = observation.Turn, Health = enemy.Health, MaximumHealth = enemy.MaximumHealth
            });
        }
        this.State.Contacts = [.. this.State.Contacts.Where(contact => observation.Turn - contact.LastSeenTurn <= 8 && contact.SearchStep < 3)
            .OrderByDescending(contact => contact.LastSeenTurn).ThenBy(contact => contact.Id).Take(8)];
    }
    protected abstract IReadOnlyList<UnitAction> Think(VisionObservation vision, TerrainObservation terrain, int energy);
    protected virtual ActionPlan Act(IReadOnlyList<UnitAction> actions) {
        return new(Array.AsReadOnly(actions.Take(AutomatonCosts.MaximumActions).ToArray()));
    }
}
