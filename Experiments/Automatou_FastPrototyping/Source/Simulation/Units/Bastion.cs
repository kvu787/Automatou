using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class Bastion : Unit
{
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Bastion", Size = 2, Health = 500, Armor = 22, Damage = 52, MeleeDamage = 75, Range = 4, ActionPoints = 3 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => Memory;
    public override Unit CreateFresh() => new Bastion();

    public sealed class Automaton : UnitAutomaton
    {
        public Automaton() => Settings = new() { Aggression = .85, Caution = .3, Commitment = .25 };

        public override IEnumerable<UnitAction> Act(UnitSenses senses)
        {
            var observation = BehaviorPlanning.Begin(this, senses);
            var target = BehaviorPlanning.Enemy(this, observation, preferWounded: false, considerBlast: false);
            var choices = new List<BehaviorOption?>();
            if (target is not null)
            {
                choices.Add(BehaviorPlanning.Engage(this, observation, target, considerBlast: false));
                choices.Add(BehaviorPlanning.Withdraw(this, observation, target, skirmish: false));
            }
            else choices.Add(BehaviorPlanning.Explore(this, senses, observation));
            choices.Add(BehaviorPlanning.Escort(this, senses, observation));
            var intention = BehaviorPlanning.Choose(this, observation, choices);
            foreach (var action in BehaviorPlanning.Execute(this, senses, intention, keepDistance: false))
                yield return action;
        }
    }
}
