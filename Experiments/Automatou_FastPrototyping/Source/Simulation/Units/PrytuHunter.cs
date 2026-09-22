namespace Automatou.Simulation;

public sealed class PrytuHunter : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Prytu hunter", Health = 75, Armor = 3, Damage = 28, MeleeDamage = 28, Range = 1, ActionPoints = 6 };
    public Automaton Memory { get; set; } = new();
    public override UnitAutomaton AutomatonInstance => this.Memory;
    public override Unit CreateFresh() {
        return new PrytuHunter();
    }

    public sealed class Automaton : UnitAutomaton {
        // Aggressive pursuit with a wounded-target bonus. This automaton offers no Escort
        // candidate, so assigning a bond does not change its decisions.
        public Automaton() {
            this.Settings = new() { Aggression = .95, Caution = .15, Commitment = .1 };
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
