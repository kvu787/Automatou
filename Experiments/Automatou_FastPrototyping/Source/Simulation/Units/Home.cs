using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class Home : Unit {
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "H.O.M.E.", Size = 3, Health = 380, Armor = 8, Damage = 15, Range = 2, ActionPoints = 5, Evasion = 15, Mobility = Mobility.Amphibious };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => this.Memory;
    public override Unit CreateFresh() {
        return new Home();
    }

    public sealed class Automaton : UnitAutomaton {
        public Automaton() {
            this.Settings = new() { Aggression = .35, Caution = .95, Commitment = .2 };
        }

        public override IEnumerable<UnitAction> Act(UnitSenses senses) {
            WorldObservation observation = BehaviorPlanning.Begin(this, senses);
            EntityObservation? target = BehaviorPlanning.Enemy(this, observation, preferWounded: false, considerBlast: false);
            List<BehaviorOption?> choices = [];
            if (target is not null) {
                choices.Add(BehaviorPlanning.Engage(this, observation, target, considerBlast: false));
                choices.Add(BehaviorPlanning.Withdraw(this, observation, target, skirmish: true));
            } else {
                choices.Add(BehaviorPlanning.Explore(this, senses, observation));
            }

            choices.Add(BehaviorPlanning.Escort(this, senses, observation));
            BehaviorOption intention = BehaviorPlanning.Choose(this, observation, choices);
            foreach (UnitAction action in BehaviorPlanning.Execute(this, senses, intention, keepDistance: true)) {
                yield return action;
            }
        }
    }
}
