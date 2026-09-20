using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class SiegeWalker : Unit
{
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "Siege walker", Size = 2, Health = 210, Armor = 12, Damage = 35, Range = 3, ActionPoints = 4, HeatPerShot = 45, CoolingPerTurn = 12 };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => Memory;
    public override Unit CreateFresh() => new SiegeWalker();

    public sealed class Automaton : UnitAutomaton
    {
        public Automaton() => Settings = new() { Aggression = .65, Caution = .45, Commitment = .15, HeatReserve = 70 };

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
            choices.Add(BehaviorPlanning.Recover(this, senses, observation));
            choices.Add(BehaviorPlanning.Escort(this, senses, observation));
            var intention = BehaviorPlanning.Choose(this, observation, choices);
            foreach (var action in BehaviorPlanning.Execute(this, senses, intention, keepDistance: false))
                yield return action;
        }
    }
}
