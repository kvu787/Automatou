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
                var action = TacticalPlanning.Engage(senses, observation, target, keepDistance: false);
                if (action is null) yield break;
                yield return action;
            }
        }
    }
}
