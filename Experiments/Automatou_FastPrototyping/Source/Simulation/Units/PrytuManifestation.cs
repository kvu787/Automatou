using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class PrytuManifestation : Unit {
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Prytu manifestation", Size = 3, Health = 600, Armor = 10, Damage = 65, MeleeDamage = 65, Range = 2, ActionPoints = 4 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => this.Memory;
    public override Unit CreateFresh() {
        return new PrytuManifestation();
    }

    public sealed class Automaton : UnitAutomaton {
        public Automaton() {
            this.Settings = new() { Aggression = .9, Caution = .15, Commitment = .25 };
        }

        public override IEnumerable<UnitAction> Act(UnitSenses senses) {
            WorldObservation observation = BehaviorPlanning.Begin(this, senses);
            EntityObservation? target = BehaviorPlanning.Enemy(this, observation, preferWounded: true, considerBlast: false);
            List<BehaviorOption?> choices = [];
            if (target is not null) {
                choices.Add(BehaviorPlanning.Engage(this, observation, target, considerBlast: false));
                choices.Add(BehaviorPlanning.Withdraw(this, observation, target, skirmish: false));
            } else {
                choices.Add(BehaviorPlanning.Explore(this, senses, observation));
            }

            BehaviorOption intention = BehaviorPlanning.Choose(this, observation, choices);
            foreach (UnitAction action in BehaviorPlanning.Execute(this, senses, intention, keepDistance: false)) {
                yield return action;
            }
        }
    }
}
