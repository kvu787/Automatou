namespace Automatou.Simulation;

// One invocation per turn. Each yielded request is resolved before the iterator resumes.
// Store lasting goals/state in the memory types copied by UnitAutomaton.CopyTo.
public abstract partial class UnitAutomaton {
    public abstract IEnumerable<UnitAction> Act(UnitSenses senses);
}

public abstract record UnitAction;
public sealed record TurnAction(int Direction) : UnitAction;
public sealed record MoveForwardAction : UnitAction;
public sealed record AttackAction(int TargetId) : UnitAction;

public sealed record EntityObservation(int Id, Faction Faction, Hex Position, int Facing,
    int Health, int MaximumHealth, bool Stationary, UnitStatistics Unit, IReadOnlyList<Hex> Cells,
    int Heat = 0, bool WeaponLocked = false, int? BondedUnitId = null);
public sealed record WorldObservation(int Turn, EntityObservation Self, IReadOnlyList<EntityObservation> Entities);

// No mutable world/entity/brain references escape this read-only sensing interface.
// Observe returns detached values; call it again after actuation for fresh information.
// Terrain is known globally. Every entity query obeys the same current visibility.
public sealed class UnitSenses {
    private readonly World world;
    private readonly Entity actor;
    internal UnitSenses(World world, Entity actor) { this.world = world; this.actor = actor; this.RemainingPoints = actor.Unit.ActionPoints; }
    public int RemainingPoints { get; internal set; }
    public bool HasAttacked { get; internal set; }
    public SimulationSettings Settings => this.world.Settings with { };
    public IReadOnlyList<Hex> KnownCells => Array.AsReadOnly(this.world.Terrain.Keys.ToArray());
    private static EntityObservation Copy(Entity entity) {
        return new(entity.Id, entity.Faction, entity.Position, entity.Facing,
        entity.Health, entity.MaximumHealth, entity.Stationary, entity.Unit.Statistics, Array.AsReadOnly(entity.OccupiedCells().ToArray()),
        entity.Heat, entity.WeaponLocked, entity.BondedUnitId);
    }

    public WorldObservation Observe() {
        return new(this.world.Turn, Copy(this.actor), Array.AsReadOnly(this.world.Entities.Where(entity => this.world.CanObserve(this.actor, entity)).Select(Copy).ToArray()));
    }

    public Terrain? TerrainAt(Hex cell) {
        return this.world.Terrain.TryGetValue(cell, out Terrain terrain) ? terrain : null;
    }

    public bool CanOccupy(Hex position) {
        return this.world.CanOccupyKnown(this.actor, position);
    }

    public bool CanAttack(int targetId) {
        return !this.HasAttacked && this.RemainingPoints >= 2 &&
            this.world.Entities.FirstOrDefault(entity => entity.Id == targetId) is { } target && this.world.CanAttack(this.actor, target);
    }

    public int MovementCost(Hex destination) {
        return this.world.MovementCost(this.actor, destination);
    }

    public int FindDirection(int targetId) {
        return this.world.Entities.FirstOrDefault(e => e.Id == targetId && this.world.CanObserve(this.actor, e)) is { } target ? this.world.FindDirection(this.actor, target) : -1;
    }

    public int FindDirection(Hex destination, int stoppingDistance = 0) {
        return this.world.FindDirection(this.actor, destination, stoppingDistance);
    }
}

// Reusable tactics, not a behavior registry. Each unit's nested automaton chooses
// which helpers to use and may replace this logic with its own goals/actions.
public static class TacticalPlanning {
    public static EntityObservation? SelectTarget(WorldObservation observation, int? previousTarget, bool preferWounded) {
        return observation.Entities.Where(e => e.Faction != observation.Self.Faction)
            .OrderBy(e => e.Cells.Min(observation.Self.Position.Distance) - (preferWounded ? (1 - ((double)e.Health / e.MaximumHealth)) * 5 : 0))
            .ThenBy(e => e.Id == previousTarget ? 0 : 1).ThenBy(e => e.Id).FirstOrDefault();
    }

    public static UnitAction? Engage(UnitSenses senses, WorldObservation observation, EntityObservation target, bool keepDistance) {
        EntityObservation actor = observation.Self;
        UnitStatistics unit = actor.Unit;
        int distance = actor.Cells.Min(c => target.Cells.Min(c.Distance));
        Hex aim = target.Cells.MinBy(c => c.Distance(actor.Position));
        bool inArc = Hex.TurnDistance(actor.Facing, actor.Position.DirectionTo(aim)) <= 1;
        if (senses.CanAttack(target.Id)) {
            return new AttackAction(target.Id);
        }

        if (actor.Stationary) {
            return null;
        }

        bool retreat = keepDistance && distance < Math.Max(2, unit.Range - 1);
        int desired;
        if (retreat) {
            int[] escape = Enumerable.Range(0, 6).Where(d => senses.CanOccupy(actor.Position + Hex.Directions[d]))
                .OrderByDescending(d => (actor.Position + Hex.Directions[d]).Distance(target.Position))
                .ThenBy(d => Hex.TurnDistance(actor.Facing, d)).ToArray();
            if (escape.Length == 0) {
                return null;
            }

            desired = escape[0];
        } else if (distance <= unit.Range) {
            if (senses.HasAttacked || inArc) {
                return null;
            }

            desired = actor.Position.DirectionTo(aim);
        } else {
            desired = senses.FindDirection(target.Id);
            if (desired < 0) {
                return null;
            }
        }
        if (actor.Facing != desired) {
            return new TurnAction((desired - actor.Facing + 6) % 6 <= 3 ? 1 : -1);
        }

        return !retreat && distance <= unit.Range
            ? null
            : (UnitAction?)(senses.RemainingPoints >= senses.MovementCost(actor.Position + Hex.Directions[actor.Facing]) ? new MoveForwardAction() : null);
    }
}
