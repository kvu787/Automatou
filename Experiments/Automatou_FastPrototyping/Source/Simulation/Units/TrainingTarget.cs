using System.Text.Json.Serialization;

namespace Automatou.Simulation;

// An inert source-defined unit for reproducible experiments, also available in the creator.
public sealed class TrainingTarget : Unit {
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Training target", Health = 5000, Armor = 0, Damage = 1, MeleeDamage = 1, Range = 1, ActionPoints = 1 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore] public override UnitAutomaton Brain => this.Memory;
    public override Unit CreateFresh() {
        return new TrainingTarget();
    }

    public sealed class Automaton : UnitAutomaton {
        public override IEnumerable<UnitAction> Act(UnitSenses senses) {
            WorldObservation observation = BehaviorPlanning.Begin(this, senses);
            _ = BehaviorPlanning.Choose(this, observation, [new BehaviorOption("Hold", 1, "An inert target for controlled experiments.")]);
            yield break;
        }
    }
}
