namespace Automatou.Simulation;

public sealed class LongbowArtillery : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Longbow artillery", Health = 90, Armor = 3, Damage = 60, Range = 9, ActionPoints = 3, BlastRadius = 1 };
    public Automaton Memory { get; set; } = new();
    public override UnitAutomaton Brain => this.Memory;
    public override Unit CreateFresh() {
        return new LongbowArtillery();
    }

    public sealed class Automaton : UnitAutomaton {
        // Allied blast exposure penalizes both target rank and Engage score.
        // It is a preference, not a prohibition: an accepted shot can hurt allies.
        public Automaton() {
            this.Settings = new() { Aggression = .75, Caution = .4, Commitment = .2 };
        }

        public override IEnumerable<UnitAction> Act(UnitSenses senses) {
            WorldObservation observation = BehaviorPlanning.Begin(this, senses);
            EntityObservation? target = BehaviorPlanning.Enemy(this, observation, preferWounded: false, considerBlast: true);
            List<BehaviorOption?> choices = [];
            if (target is not null) {
                choices.Add(BehaviorPlanning.Engage(this, observation, target, considerBlast: true));
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
