namespace Automatou.Simulation;

// Optional user-space library: every calculation uses purchased observations only.
// Projected positions describe the requested plan, never a claim about execution success.
public sealed class ActionPlanning(VisionObservation vision, TerrainObservation terrain, int energy) {
    public EntityObservation Self { get; private set; } = vision.World.Self;
    public IReadOnlyList<EntityObservation> Entities => vision.World.Entities;
    public int Energy { get; private set; } = energy;
    public List<UnitAction> Actions { get; } = [];
    public Func<Hex, bool> PositionAllowed { get; set; } = _ => true;
    public static int Distance(EntityObservation a, EntityObservation b) {
        return a.Cells.Min(cell => b.Cells.Min(cell.Distance));
    }

    public int DistanceFrom(Hex position, EntityObservation other) {
        return Math.Max(0, other.Cells.Min(position.Distance) - this.Self.Unit.Size + 1);
    }

    public EntityObservation[] Enemies => [.. this.Entities.Where(entity => entity.Faction != this.Self.Faction)];
    private bool attacked;

    public bool CanOccupy(Hex position) {
        foreach (Hex cell in Hex.Disk(this.Self.Unit.Size).Select(offset => position + offset)) {
            if (!terrain.Cells.TryGetValue(cell, out Terrain ground) || !Traversable(this.Self.Unit.Mobility, ground) ||
                this.Entities.Any(entity => entity.Id != this.Self.Id && entity.Cells.Contains(cell))) { return false; }
        }
        return this.PositionAllowed(position);
    }
    public static bool Traversable(Mobility mobility, Terrain ground) {
        return ground != Terrain.ExclusionZone &&
        (mobility is Mobility.Flight or Mobility.Spaceflight || (mobility == Mobility.Amphibious ? ground != Terrain.Mountain : ground is not (Terrain.Mountain or Terrain.Water)));
    }

    private int MovementCost(Hex position) {
        return terrain.Cells.GetValueOrDefault(position) == Terrain.Forest &&
            this.Self.Unit.Mobility == Mobility.Ground && this.Self.Faction != Faction.InfantryAndArtillery ? 2 : 1;
    }

    private bool Add(UnitAction action) {
        int cost = action is AttackAction ? 2 : action is MoveForwardAction ? this.MovementCost(this.Self.Position + Hex.Directions[this.Self.Facing]) : 1;
        if (cost > this.Energy || this.Actions.Count >= AutomatonCosts.MaximumActions) { return false; }
        if (action is TurnAction turn) {
            if (this.Self.Stationary) { return false; }
            this.Self = this.Self with { Facing = (this.Self.Facing + turn.Direction + 6) % 6 };
        } else if (action is MoveForwardAction) {
            Hex next = this.Self.Position + Hex.Directions[this.Self.Facing];
            if (this.Self.Stationary || !this.CanOccupy(next)) { return false; }
            this.Self = this.Self with { Position = next, Cells = Array.AsReadOnly(Hex.Disk(this.Self.Unit.Size).Select(offset => next + offset).ToArray()) };
        } else if (action is AttackAction) {
            if (this.attacked) { return false; }
            this.attacked = true;
        }
        this.Actions.Add(action); this.Energy -= cost;
        return true;
    }

    public void Engage(EntityObservation target) {
        if (Distance(this.Self, target) > this.Self.Unit.Range) {
            this.MoveTo(position => this.DistanceFrom(position, target) <= this.Self.Unit.Range);
        }
        if (Distance(this.Self, target) > this.Self.Unit.Range || (vision.Settings.HeatEnabled && this.Self.WeaponLocked)) { return; }
        Hex aim = target.Cells.MinBy(cell => cell.Distance(this.Self.Position));
        int facing = this.Self.Position.DirectionTo(aim);
        while (Hex.TurnDistance(this.Self.Facing, facing) > 1) {
            if (!this.Add(new TurnAction((facing - this.Self.Facing + 6) % 6 <= 3 ? 1 : -1))) { return; }
        }
        _ = this.Add(new AttackAction(target.Id));
    }

    public void MoveTo(Hex destination, int stoppingDistance = 0) {
        this.MoveTo(position => position.Distance(destination) <= stoppingDistance);
    }

    // Dijkstra on position + facing. The first reachable goal gives a deterministic,
    // energy-aware route; a route may span turns, but only its affordable prefix is submitted.
    public void MoveTo(Func<Hex, bool> goal) {
        if (this.Self.Stationary || goal(this.Self.Position)) { return; }
        (Hex Position, int Facing) start = (this.Self.Position, this.Self.Facing);
        PriorityQueue<(Hex Position, int Facing), (int Cost, int Sequence)> frontier = new();
        Dictionary<(Hex Position, int Facing), int> costs = new() { [start] = 0 };
        Dictionary<(Hex Position, int Facing), ((Hex Position, int Facing) Parent, UnitAction Action)> previous = [];
        int sequence = 0, expanded = 0;
        frontier.Enqueue(start, (0, sequence++));
        while (frontier.TryDequeue(out (Hex Position, int Facing) current, out (int Cost, int Sequence) priority) && expanded++ < 3000) {
            if (costs[current] != priority.Cost) { continue; }
            if (goal(current.Position)) {
                List<UnitAction> route = [];
                while (current != start) { ((Hex Position, int Facing) Parent, UnitAction Action) edge = previous[current]; route.Add(edge.Action); current = edge.Parent; }
                route.Reverse();
                foreach (UnitAction action in route) { if (!this.Add(action)) { break; } }
                return;
            }
            void Visit((Hex Position, int Facing) next, UnitAction action, int cost) {
                int total = priority.Cost + cost;
                if (costs.TryGetValue(next, out int old) && old <= total) { return; }
                costs[next] = total; previous[next] = (current, action); frontier.Enqueue(next, (total, sequence++));
            }
            Hex forward = current.Position + Hex.Directions[current.Facing];
            if (this.CanOccupy(forward)) { Visit((forward, current.Facing), new MoveForwardAction(), this.MovementCost(forward)); }
            Visit((current.Position, (current.Facing + 1) % 6), new TurnAction(1), 1);
            Visit((current.Position, (current.Facing + 5) % 6), new TurnAction(-1), 1);
        }
    }

    // Hard priority, not a score competing with attack. When crowded, spend this turn
    // separating (or waiting if blocked). Once safe, subsequent plans cannot cross N.
    public bool MaintainSpacing(IReadOnlyList<EntityObservation> others, int spacing) {
        bool Safe(Hex position) {
            return others.All(other => this.DistanceFrom(position, other) >= spacing);
        }

        if (Safe(this.Self.Position)) { this.PositionAllowed = Safe; return true; }
        int initial = others.Min(other => this.DistanceFrom(this.Self.Position, other));
        Hex origin = this.Self.Position;
        this.PositionAllowed = position => others.All(other => this.DistanceFrom(position, other) >= Math.Min(spacing, this.DistanceFrom(origin, other)));
        this.MoveTo(Safe);
        if (this.Actions.Count == 0) {
            this.MoveTo(position => others.Min(other => this.DistanceFrom(position, other)) > initial);
        }
        return false;
    }
}
