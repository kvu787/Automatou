namespace Automatou.Simulation;

// One invocation per turn. Each yielded request is resolved before the iterator resumes.
// Store lasting goals/state in serializable properties of the concrete automaton.
public abstract class UnitAutomaton
{
    public abstract IEnumerable<UnitAction> Act(UnitSenses senses);
}

public abstract record UnitAction;
public sealed record TurnAction(int Direction) : UnitAction;
public sealed record MoveForwardAction : UnitAction;
public sealed record AttackAction(int TargetId) : UnitAction;

public sealed record EntityObservation(int Id, Faction Faction, Hex Position, int Facing,
    int Health, int MaximumHealth, bool Stationary, UnitStatistics Unit, IReadOnlyList<Hex> Cells);
public sealed record WorldObservation(int Turn, EntityObservation Self, IReadOnlyList<EntityObservation> Entities);

// No mutable world/entity/brain references escape this read-only sensing interface.
// Observe returns detached values; call it again after actuation for fresh information.
// Visibility is currently global; future sensing restrictions belong here.
public sealed class UnitSenses
{
    private readonly World world;
    private readonly Entity actor;
    internal UnitSenses(World world, Entity actor) { this.world = world; this.actor = actor; RemainingPoints = actor.Unit.ActionPoints; }
    public int RemainingPoints { get; internal set; }
    public bool HasAttacked { get; internal set; }
    private static EntityObservation Copy(Entity entity) => new(entity.Id, entity.Faction, entity.Position, entity.Facing,
        entity.Health, entity.MaximumHealth, entity.Stationary, entity.Unit.Statistics, Array.AsReadOnly(entity.OccupiedCells().ToArray()));
    public WorldObservation Observe() => new(world.Turn, Copy(actor), Array.AsReadOnly(world.Entities.Select(Copy).ToArray()));
    public Terrain? TerrainAt(Hex cell) => world.Terrain.TryGetValue(cell, out var terrain) ? terrain : null;
    public bool CanOccupy(Hex position, int facing) => world.CanOccupy(actor, position, facing, out _);
    public int FindDirection(int targetId) => world.Entities.FirstOrDefault(e => e.Id == targetId) is { } target ? world.FindDirection(actor, target) : -1;
}

// Reusable tactics, not a behavior registry. Each unit's nested automaton chooses
// which helpers to use and may replace this logic with its own goals/actions.
public static class TacticalPlanning
{
    public static EntityObservation? SelectTarget(WorldObservation observation, int? previousTarget, bool preferWounded) =>
        observation.Entities.Where(e => e.Faction != observation.Self.Faction)
            .OrderBy(e => e.Cells.Min(observation.Self.Position.Distance) - (preferWounded ? (1 - (double)e.Health / e.MaximumHealth) * 5 : 0))
            .ThenBy(e => e.Id == previousTarget ? 0 : 1).ThenBy(e => e.Id).FirstOrDefault();

    public static UnitAction? Engage(UnitSenses senses, WorldObservation observation, EntityObservation target, bool keepDistance)
    {
        var actor = observation.Self;
        var unit = actor.Unit;
        int distance = actor.Cells.Min(c => target.Cells.Min(c.Distance));
        Hex aim = target.Cells.MinBy(c => c.Distance(actor.Position));
        bool inArc = Hex.TurnDistance(actor.Facing, actor.Position.DirectionTo(aim)) <= 1;
        if (!senses.HasAttacked && distance <= unit.Range && inArc && senses.RemainingPoints >= 2) return new AttackAction(target.Id);
        if (actor.Stationary) return null;
        bool retreat = keepDistance && distance < Math.Max(2, unit.Range - 1);
        int desired;
        if (retreat)
        {
            var escape = Enumerable.Range(0, 6).Where(d => senses.CanOccupy(actor.Position + Hex.Directions[d], d))
                .OrderByDescending(d => (actor.Position + Hex.Directions[d]).Distance(target.Position))
                .ThenBy(d => Hex.TurnDistance(actor.Facing, d)).ToArray();
            if (escape.Length == 0) return null;
            desired = escape[0];
        }
        else if (distance <= unit.Range)
        {
            if (senses.HasAttacked || inArc) return null;
            desired = actor.Position.DirectionTo(aim);
        }
        else
        {
            desired = senses.FindDirection(target.Id);
            if (desired < 0) return null;
        }
        if (actor.Facing != desired) return new TurnAction((desired - actor.Facing + 6) % 6 <= 3 ? 1 : -1);
        if (!retreat && distance <= unit.Range) return null;
        return new MoveForwardAction();
    }
}
