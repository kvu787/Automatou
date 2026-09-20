using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class CloneInfantry : Unit
{
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Clone infantry", Health = 38, Armor = 1, Damage = 12, Range = 2, ActionPoints = 5 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => Memory;
    public override Unit CreateFresh() => new CloneInfantry();

    public sealed class Automaton : UnitAutomaton
    {
        public Automaton() => Settings = new() { Aggression = .85, Caution = .25, Commitment = .1 };

        public override IEnumerable<UnitAction> Act(UnitSenses senses)
        {
            var observation = BehaviorPlanning.Begin(this, senses);
            var target = BehaviorPlanning.Enemy(this, observation, preferWounded: true, considerBlast: false);
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
