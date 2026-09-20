namespace Automatou.Simulation;

// A deliberately small tuning surface; unit classes supply their own defaults and choices.
public sealed record BehaviorSettings
{
    public double Aggression { get; set; } = .65;
    public double Caution { get; set; } = .5;
    public double Commitment { get; set; } = .15;
    public int HeatReserve { get; set; } = 70;
    public bool RememberContacts { get; set; } = true;
}

public sealed record ContactMemory
{
    public int Id { get; set; }
    public Faction Faction { get; set; }
    public Hex Position { get; set; }
    public int LastSeenTurn { get; set; }
    public int Health { get; set; }
    public int MaximumHealth { get; set; }
    public int SearchStep { get; set; }
}

public sealed record DecisionConsideration(string Name, double Score, string Reason);
public sealed record DecisionTrace(int Turn, string Intention, string Reason, Hex? Destination);

public sealed class AutomatonMemory
{
    public string Intention { get; set; } = "Observe";
    public string Reason { get; set; } = "Waiting for the first turn.";
    public int IntentionSince { get; set; }
    public int LastUpdatedTurn { get; set; } = -1;
    public Hex? Destination { get; set; }
    public List<ContactMemory> Contacts { get; set; } = [];
    public List<DecisionConsideration> Considerations { get; set; } = [];
    public List<DecisionTrace> History { get; set; } = [];
    public int ShotsFired { get; set; }
    public int IntentionChanges { get; set; }
    public int PatrolIndex { get; set; }
}

public abstract partial class UnitAutomaton
{
    public int TurnsObserved { get; set; }
    public int? TargetId { get; set; }
    public BehaviorSettings Settings { get; set; } = new();
    public AutomatonMemory State { get; set; } = new();

    public void ValidateMemory()
    {
        if (Settings is null || State is null ||
            new[] { Settings.Aggression, Settings.Caution, Settings.Commitment }.Any(value => !double.IsFinite(value) || value is < 0 or > 1) ||
            Settings.HeatReserve is < 40 or > 100 || TurnsObserved < 0 || TargetId is < 1 ||
            State.Contacts is null || State.Contacts.Count > 8 || State.Contacts.Any(contact => contact is null || contact.Id < 1 ||
                !Enum.IsDefined(contact.Faction) || contact.LastSeenTurn < 0 || contact.MaximumHealth < 1 || contact.Health < 1 ||
                contact.Health > contact.MaximumHealth || contact.SearchStep is < 0 or > 3) ||
            State.Contacts.Select(contact => contact.Id).Distinct().Count() != State.Contacts.Count ||
            State.History is null || State.History.Count > 12 || State.History.Any(trace => trace is null || trace.Turn < 0 ||
                string.IsNullOrWhiteSpace(trace.Intention) || trace.Reason is null) ||
            State.Considerations is null || State.Considerations.Count > 10 || State.Considerations.Any(consideration => consideration is null ||
                !double.IsFinite(consideration.Score) || consideration.Score is < 0 or > 1 || string.IsNullOrWhiteSpace(consideration.Name) || consideration.Reason is null) ||
            string.IsNullOrWhiteSpace(State.Intention) || State.Reason is null || State.IntentionSince < 0 || State.LastUpdatedTurn < -1 ||
            State.ShotsFired < 0 || State.IntentionChanges < 0 || State.PatrolIndex < 0)
            throw new InvalidDataException("Invalid automaton settings or memory.");
    }
}
