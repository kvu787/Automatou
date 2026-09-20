using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class LongbowArtillery : Unit
{
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Longbow artillery", Health = 90, Armor = 3, Damage = 60, Range = 9, ActionPoints = 3, BlastRadius = 1 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => Memory;
    public override Unit CreateFresh() => new LongbowArtillery();

    public sealed class Automaton : UnitAutomaton
    {
        public Automaton() => Settings = new() { Aggression = .75, Caution = .4, Commitment = .2 };

        public override IEnumerable<UnitAction> Act(UnitSenses senses)
        {
            var observation = BehaviorPlanning.Begin(this, senses);
            var target = BehaviorPlanning.Enemy(this, observation, preferWounded: false, considerBlast: true);
            var choices = new List<BehaviorOption?>();
            if (target is not null)
            {
                choices.Add(BehaviorPlanning.Engage(this, observation, target, considerBlast: true));
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
