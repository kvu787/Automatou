namespace Automatou.Simulation;

public sealed class Bastion : Unit {
    public override UnitStatistics Statistics { get; } = new() { Name = "Bastion", Size = 2, Health = 500, Armor = 22, Damage = 52, MeleeDamage = 75, Range = 4, ActionPoints = 3 };
    public Automaton Memory { get; set; } = new();
    public override UnitAutomaton Brain => this.Memory;
    public override Unit CreateFresh() {
        return new Bastion();
    }

    public sealed class Automaton : UnitAutomaton {
        public Automaton() {
            this.Settings = new() { Aggression = .85, Caution = .3, Commitment = .25 };
        }

        public override IEnumerable<UnitAction> Act(UnitSenses senses) {
            WorldObservation observation = BehaviorPlanning.Begin(this, senses);
            EntityObservation? target = BehaviorPlanning.Enemy(this, observation, preferWounded: false, considerBlast: false);
            List<BehaviorOption?> choices = [];
            if (target is not null) {
                choices.Add(BehaviorPlanning.Engage(this, observation, target, considerBlast: false));
                choices.Add(BehaviorPlanning.Withdraw(this, observation, target, skirmish: false));
            } else {
                choices.Add(BehaviorPlanning.Explore(this, senses, observation));
            }

            choices.Add(BehaviorPlanning.Escort(this, senses, observation));
            BehaviorOption intention = BehaviorPlanning.Choose(this, observation, choices);
            foreach (UnitAction action in BehaviorPlanning.Execute(this, senses, intention, keepDistance: false)) {
                yield return action;
            }
        }
    }
}
