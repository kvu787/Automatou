namespace Automatou.Simulation;

public sealed partial class World {
    // Sensing snapshots carry physical values only. Copying another program's private
    // memory is neither necessary nor safe when that program has just failed.
    private sealed class SnapshotUnit(UnitStatistics statistics) : Unit {
        public override UnitStatistics Statistics => statistics;
        public override Unit CreateFresh() {
            return new SnapshotUnit(statistics);
        }
    }

    private World SnapshotForSensing() {
        World snapshot = new() { Turn = this.Turn, Settings = this.Settings with { } };
        foreach (KeyValuePair<Hex, Terrain> cell in this.Terrain) { snapshot.Terrain.Add(cell.Key, cell.Value); }
        snapshot.Entities.AddRange(this.Entities.Select(entity => new Entity {
            Id = entity.Id, Unit = new SnapshotUnit(entity.Unit.Statistics), Faction = entity.Faction,
            Position = entity.Position, Facing = entity.Facing, Health = entity.Health, Stationary = entity.Stationary,
            Heat = entity.Heat, WeaponLocked = entity.WeaponLocked, BondedUnitId = entity.BondedUnitId,
            LastTurn = entity.LastTurn.Copy()
        }));
        snapshot.RebuildOccupancy();
        return snapshot;
    }
    private sealed class Submission(Entity actor, AutomatonKernel kernel, UnitAction[] actions) {
        public Entity Actor { get; } = actor;
        public AutomatonKernel Kernel { get; } = kernel;
        public UnitAction[] Actions { get; } = actions;
        public bool Stopped { get; set; }
        public bool Attacked { get; set; }
    }

    public void Step() {
        this.Turn++; this.Effects.Clear();
        if (this.Settings.HeatEnabled) {
            foreach (Entity entity in this.Entities) {
                entity.Heat = Math.Max(0, entity.Heat - entity.Unit.CoolingPerTurn);
                if (entity.WeaponLocked && entity.Heat <= 40) { entity.WeaponLocked = false; this.Note($"{entity.Name} cooled; weapon ready."); }
            }
        }
        // Freeze once, before invoking ANY program. Even later programs see precisely
        // the same physical turn state; the only live changes here are private memories.
        World snapshot = this.SnapshotForSensing();
        List<Submission> submissions = [];
        foreach (Entity actor in this.Entities.OrderBy(entity => entity.Id)) {
            AutomatonKernel kernel = new(snapshot, snapshot.Entities.Single(entity => entity.Id == actor.Id));
            UnitAction[] actions = [];
            string error = "";
            try {
                actor.Unit.Validate();
                ActionPlan plan = actor.Unit.AutomatonInstance.RunTurn(kernel);
                if (plan is null || plan.Actions is null || plan.Actions.Count > AutomatonCosts.MaximumActions || plan.Actions.Any(action => action is null)) {
                    throw new InvalidOperationException("Invalid or oversized action list.");
                }
                actions = plan.Actions.ToArray();
            } catch (Exception exception) when (exception is not OutOfMemoryException) {
                error = exception.Message;
                this.Note($"#{actor.Id} automaton rejected: {error}");
            } finally { kernel.Close(); }
            actor.LastTurn = new() {
                Turn = this.Turn, InitialEnergy = actor.Unit.TurnEnergy,
                RemainingEnergy = kernel.RemainingEnergy, Error = error, Sensing = [.. kernel.Receipts], Submitted = [.. actions]
            };
            submissions.Add(new(actor, kernel, actions));
        }
        // Execute action slot 0 for all units, then slot 1, etc. Move conflicts use
        // the complete set of requests in that slot. Attacks use rotating ID priority.
        for (int index = 0; index < AutomatonCosts.MaximumActions; index++) {
            Submission[] active = submissions.Where(value => !value.Stopped && index < value.Actions.Length).ToArray();
            if (active.Length == 0) { break; }
            Dictionary<Submission, Hex[]> moves = [];
            foreach (Submission value in active.Where(value => value.Actions[index] is MoveForwardAction && this.Entities.Contains(value.Actor))) {
                Hex next = value.Actor.Position + Hex.Directions[value.Actor.Facing];
                if (!value.Actor.Stationary && value.Actor.LastTurn.RemainingEnergy >= this.MovementCost(value.Actor, next) && this.CanOccupy(value.Actor, next, out _)) {
                    moves[value] = Hex.Disk(value.Actor.Unit.Size).Select(offset => next + offset).ToArray();
                }
            }
            HashSet<Submission> conflicts = [];
            foreach (KeyValuePair<Submission, Hex[]> first in moves) {
                if (moves.Any(second => second.Key != first.Key && first.Value.Intersect(second.Value).Any())) { _ = conflicts.Add(first.Key); }
            }
            // Occupied cells at slot start remain reserved even if their occupants
            // request departure: swaps and follow-through wait until a later slot/turn.
            HashSet<Submission> permittedMoves = [.. moves.Keys.Where(value => !conflicts.Contains(value))];
            int offset = this.Turn % active.Length;
            foreach (Submission value in active.Skip(offset).Concat(active.Take(offset))) {
                bool success = this.Resolve(value, index, permittedMoves, conflicts);
                if (!success) {
                    value.Stopped = true;
                    for (int later = index + 1; later < value.Actions.Length; later++) {
                        value.Actor.LastTurn.Outcomes.Add(new(this.Turn, later, value.Actions[later], false, 0, "Skipped after an earlier rejected request."));
                    }
                }
            }
        }
    }

    private bool Resolve(Submission submission, int index, HashSet<Submission> permittedMoves, HashSet<Submission> conflicts) {
        Entity actor = submission.Actor;
        UnitAction action = submission.Actions[index];
        int spent = 0;
        string reason;
        bool accepted = false;
        if (!this.Entities.Contains(actor)) { reason = "Unit no longer exists."; } else if (action is MoveForwardAction) {
            Hex next = actor.Position + Hex.Directions[actor.Facing];
            if (conflicts.Contains(submission)) { reason = "Conflicting destination requests; all contenders stay."; } else if (!permittedMoves.Contains(submission)) { reason = "Move blocked, deployed, or unaffordable."; } else {
                spent = this.MovementCost(actor, next); actor.Position = next; accepted = true; reason = "Moved.";
                if (this.Terrain[next] == Simulation.Terrain.Forest && actor.Faction == Faction.InfantryAndArtillery) { actor.Health -= 2; }
                if (actor.Health <= 0) { _ = this.Entities.Remove(actor); this.Casualties++; this.Note($"{actor.Name} lost crossing rough terrain."); }
                this.RebuildOccupancy();
            }
        } else if (action is TurnAction turn) {
            if (actor.Stationary || turn.Direction is not (-1 or 1) || actor.LastTurn.RemainingEnergy < 1) { reason = "Turn invalid, deployed, or unaffordable."; } else { actor.Facing = (actor.Facing + turn.Direction + 6) % 6; spent = 1; accepted = true; reason = "Turned."; }
        } else if (action is AttackAction attack) {
            Entity? target = this.Entities.FirstOrDefault(entity => entity.Id == attack.TargetId);
            if (submission.Attacked || actor.LastTurn.RemainingEnergy < 2 || !submission.Kernel.ObservedIds.Contains(attack.TargetId) ||
                target is null || !this.CanAttack(actor, target, submission.Kernel.VisionRange)) { reason = "Attack unavailable: energy, observation, range, facing, weapon, or target changed."; } else {
                this.Attack(actor, target, submission.Kernel.VisionRange); spent = 2; accepted = true;
                submission.Attacked = true; reason = "Attack executed (hit or evasion resolved by the world).";
            }
        } else { reason = "Unknown action."; }
        actor.LastTurn.RemainingEnergy -= spent;
        actor.LastTurn.Outcomes.Add(new(this.Turn, index, action, accepted, spent, reason));
        return accepted;
    }
}
