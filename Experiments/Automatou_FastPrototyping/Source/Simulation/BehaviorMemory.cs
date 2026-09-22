namespace Automatou.Simulation;

// A deliberately small tuning surface; unit classes supply their own defaults and choices.
public sealed record BehaviorSettings {
    // Aggression raises engagement scores; caution raises withdrawal and blast concerns.
    // Commitment biases target selection and resists small intention-score changes.
    public double Aggression { get; set; } = .65;
    public double Caution { get; set; } = .5;
    public double Commitment { get; set; } = .15;
    public int HeatReserve { get; set; } = 70;
    public bool RememberContacts { get; set; } = true;
}

public sealed record ContactMemory {
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

public sealed class AutomatonMemory {
    // Checkpoints need independent mutable contacts and lists. Immutable trace/score
    // records can be shared; the execution iterator itself is never saved.
    public AutomatonMemory Copy() {
        return new() {
            Intention = this.Intention, Reason = this.Reason, IntentionSince = this.IntentionSince,
            LastUpdatedTurn = this.LastUpdatedTurn, Destination = this.Destination,
            Contacts = this.Contacts.Select(contact => contact with { }).ToList(),
            Considerations = [.. this.Considerations], History = [.. this.History],
            ShotsFired = this.ShotsFired, IntentionChanges = this.IntentionChanges, PatrolIndex = this.PatrolIndex
        };
    }

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

public abstract partial class UnitAutomaton {
    // Extend this copy path when adding persistent automaton data: World.Copy reaches it
    // through Entity.Copy and Unit.Copy, so rewinds must preserve the same decisions.
    public void CopyTo(UnitAutomaton copy) {
        copy.TurnsObserved = this.TurnsObserved;
        copy.TargetId = this.TargetId;
        copy.Settings = this.Settings with { };
        copy.State = this.State.Copy();
    }

    public int TurnsObserved { get; set; }
    public int? TargetId { get; set; }
    public BehaviorSettings Settings { get; set; } = new();
    public AutomatonMemory State { get; set; } = new();

    public void ValidateMemory() {
        if (this.Settings is null || this.State is null ||
            new[] { this.Settings.Aggression, this.Settings.Caution, this.Settings.Commitment }.Any(value => !double.IsFinite(value) || value is < 0 or > 1) ||
            this.Settings.HeatReserve is < 40 or > 100 || this.TurnsObserved < 0 || this.TargetId is < 1 ||
            this.State.Contacts is null || this.State.Contacts.Count > 8 || this.State.Contacts.Any(contact => contact is null || contact.Id < 1 ||
                !Enum.IsDefined(contact.Faction) || contact.LastSeenTurn < 0 || contact.MaximumHealth < 1 || contact.Health < 1 ||
                contact.Health > contact.MaximumHealth || contact.SearchStep is < 0 or > 3) ||
            this.State.Contacts.Select(contact => contact.Id).Distinct().Count() != this.State.Contacts.Count ||
            this.State.History is null || this.State.History.Count > 12 || this.State.History.Any(trace => trace is null || trace.Turn < 0 ||
                string.IsNullOrWhiteSpace(trace.Intention) || trace.Reason is null) ||
            this.State.Considerations is null || this.State.Considerations.Count > 10 || this.State.Considerations.Any(consideration => consideration is null ||
                !double.IsFinite(consideration.Score) || consideration.Score is < 0 or > 1 || string.IsNullOrWhiteSpace(consideration.Name) || consideration.Reason is null) ||
            string.IsNullOrWhiteSpace(this.State.Intention) || this.State.Reason is null || this.State.IntentionSince < 0 || this.State.LastUpdatedTurn < -1 ||
            this.State.ShotsFired < 0 || this.State.IntentionChanges < 0 || this.State.PatrolIndex < 0) {
            throw new InvalidDataException("Invalid automaton settings or memory.");
        }
    }
}
