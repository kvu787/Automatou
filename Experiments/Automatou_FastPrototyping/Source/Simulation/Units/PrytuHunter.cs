using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class PrytuHunter : Unit
{
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Prytu hunter", Health = 75, Armor = 3, Damage = 28, MeleeDamage = 28, Range = 1, ActionPoints = 6 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => Memory;
    public override Unit CreateFresh() => new PrytuHunter();

    public sealed class Automaton : UnitAutomaton
    {
        public Automaton() => Settings = new() { Aggression = .95, Caution = .15, Commitment = .1 };

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
            var intention = BehaviorPlanning.Choose(this, observation, choices);
            foreach (var action in BehaviorPlanning.Execute(this, senses, intention, keepDistance: false))
                yield return action;
        }
    }
}
