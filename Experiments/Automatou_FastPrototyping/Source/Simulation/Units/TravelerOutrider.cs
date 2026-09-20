using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class TravelerOutrider : Unit
{
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Traveler outrider", Health = 65, Armor = 2, Damage = 22, Range = 2, ActionPoints = 7, Evasion = 30 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => Memory;
    public override Unit CreateFresh() => new TravelerOutrider();

    public sealed class Automaton : UnitAutomaton
    {
        public Automaton() => Settings = new() { Aggression = .6, Caution = .85, Commitment = .15 };

        public override IEnumerable<UnitAction> Act(UnitSenses senses)
        {
            var observation = BehaviorPlanning.Begin(this, senses);
            var target = BehaviorPlanning.Enemy(this, observation, preferWounded: false, considerBlast: false);
            var choices = new List<BehaviorOption?>();
            if (target is not null)
            {
                choices.Add(BehaviorPlanning.Engage(this, observation, target, considerBlast: false));
                choices.Add(BehaviorPlanning.Withdraw(this, observation, target, skirmish: true));
            }
            else choices.Add(BehaviorPlanning.Explore(this, senses, observation));
            choices.Add(BehaviorPlanning.Escort(this, senses, observation));
            var intention = BehaviorPlanning.Choose(this, observation, choices);
            foreach (var action in BehaviorPlanning.Execute(this, senses, intention, keepDistance: true))
                yield return action;
        }
    }
}
