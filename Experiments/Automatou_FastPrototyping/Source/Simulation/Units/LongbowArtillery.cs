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
        public int TurnsObserved { get; set; }
        public int? TargetId { get; set; }

        public override IEnumerable<UnitAction> Act(UnitSenses senses)
        {
            TurnsObserved++;
            while (senses.RemainingPoints > 0)
            {
                var observation = senses.Observe();
                var target = TacticalPlanning.SelectTarget(observation, TargetId, preferWounded: false);
                TargetId = target?.Id;
                if (target is null) yield break;
                var action = TacticalPlanning.Engage(senses, observation, target, keepDistance: true);
                if (action is null) yield break;
                yield return action;
            }
        }
    }
}
