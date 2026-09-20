using System.Text.Json.Serialization;

namespace Automatou.Simulation;

public sealed class Home : Unit
{
    [JsonIgnore]
    public override UnitStatistics Statistics { get; } = new() { Name = "H.O.M.E.", Size = 3, Health = 380, Armor = 8, Damage = 15, Range = 2, ActionPoints = 5, Evasion = 15, Mobility = Mobility.Amphibious };
    public Automaton Memory { get; set; } = new();
    [JsonIgnore]
    public override UnitAutomaton Brain => Memory;
    public override Unit CreateFresh() => new Home();

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
