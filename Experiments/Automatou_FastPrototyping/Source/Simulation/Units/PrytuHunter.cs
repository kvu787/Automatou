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
        public int TurnsObserved { get; set; }
        public int? TargetId { get; set; }

        public override IEnumerable<UnitAction> Act(UnitSenses senses)
        {
            TurnsObserved++;
            while (senses.RemainingPoints > 0)
            {
                var observation = senses.Observe();
                var target = TacticalPlanning.SelectTarget(observation, TargetId, preferWounded: true);
                TargetId = target?.Id;
                if (target is null) yield break;
                var action = TacticalPlanning.Engage(senses, observation, target, keepDistance: false);
                if (action is null) yield break;
                yield return action;
            }
        }
    }
}
