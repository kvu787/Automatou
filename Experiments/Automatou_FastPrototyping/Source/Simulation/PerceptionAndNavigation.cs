namespace Automatou.Simulation;

public sealed partial class World {
    // Terrain is public knowledge. Occupants are revealed only by current senses.
    public bool CanObserve(Entity observer, Entity target) {
        if (observer == target || !this.Settings.LimitedPerception) {
            return true;
        }

        Hex[] origins = observer.OccupiedCells().ToArray();
        return target.OccupiedCells().Any(destination => origins.Any(origin => this.CanSeeCell(observer, origin, destination)));
    }

    public IReadOnlyList<Hex> SightCells(Entity observer) {
        if (!this.Settings.LimitedPerception) {
            return Array.AsReadOnly(this.Terrain.Keys.ToArray());
        }

        Hex[] origins = observer.OccupiedCells().ToArray();
        return Array.AsReadOnly(this.Terrain.Keys.Where(cell => origins.Any(origin => this.CanSeeCell(observer, origin, cell))).ToArray());
    }

    private bool CanSeeCell(Entity observer, Hex origin, Hex destination) {
        int distance = origin.Distance(destination);
        if (distance > observer.Unit.SightRange) {
            return false;
        }

        if (this.Terrain.GetValueOrDefault(destination) == Simulation.Terrain.Forest && distance > 2) {
            return false;
        }
        // Interpolate cube coordinates and round back onto the hex grid. The tiny
        // fixed nudge resolves edge ties identically after checkpoint restoration.
        for (int step = 1; step < distance; step++) {
            double fraction = (double)step / distance;
            double q = origin.Q + ((destination.Q - origin.Q) * fraction) + 0.000001;
            double r = origin.R + ((destination.R - origin.R) * fraction) + 0.000002;
            double s = -q - r;
            int roundedQ = (int)Math.Round(q), roundedR = (int)Math.Round(r), roundedS = (int)Math.Round(s);
            double qError = Math.Abs(roundedQ - q), rError = Math.Abs(roundedR - r), sError = Math.Abs(roundedS - s);
            if (qError > rError && qError > sError) {
                roundedQ = -roundedR - roundedS;
            } else if (rError > sError) {
                roundedR = -roundedQ - roundedS;
            }

            if (this.Terrain.GetValueOrDefault(new Hex(roundedQ, roundedR)) == Simulation.Terrain.Forest) {
                return false;
            }
        }
        return true;
    }

    internal int MovementCost(Entity actor, Hex destination) {
        return this.Terrain.GetValueOrDefault(destination) == Simulation.Terrain.Forest &&
        actor.Unit.Mobility == Mobility.Ground && actor.Faction != Faction.InfantryAndArtillery ? 2 : 1;
    }

    internal bool CanOccupyKnown(Entity actor, Hex position, ISet<Hex>? knownOccupancy = null) {
        // Planning ignores unseen occupants. Actual movement uses World.CanOccupy,
        // so an apparently clear route can still produce a rejected move and end the turn.
        knownOccupancy ??= this.KnownOccupancy(actor);
        foreach (Hex cell in Hex.Disk(actor.Unit.Size).Select(offset => position + offset)) {
            if (!this.Terrain.TryGetValue(cell, out Terrain terrain) || !Traversable(actor.Unit, terrain) || knownOccupancy.Contains(cell)) {
                return false;
            }
        }

        return true;
    }

    private HashSet<Hex> KnownOccupancy(Entity actor) {
        return [.. this.Entities
        .Where(entity => entity != actor && this.CanObserve(actor, entity)).SelectMany(entity => entity.OccupiedCells())];
    }

    internal int FindDirection(Entity actor, Entity target) {
        return !this.CanObserve(actor, target)
            ? -1
            : this.FindDirection(actor, target.OccupiedCells().ToArray(), actor.Unit.Range + actor.Unit.Size - 1);
    }

    internal int FindDirection(Entity actor, Hex destination, int stoppingDistance) {
        return this.FindDirection(actor, [destination], Math.Max(0, stoppingDistance));
    }

    private int FindDirection(Entity actor, IReadOnlyList<Hex> destinations, int stoppingDistance) {
        // A* searches (position, facing), charging for turns as well as movement.
        // Return only the heading of the first forward move, not a persistent route;
        // execution turns toward it and replans from the next observation. The search
        // can span multiple turns, but stops after 3,000 expanded poses or no route.
        if (actor.Stationary) {
            return -1;
        }

        int Distance(Hex position) {
            return Math.Max(0, destinations.Min(position.Distance) - stoppingDistance);
        }

        if (Distance(actor.Position) == 0) {
            return -1;
        }

        HashSet<Hex> knownOccupancy = this.KnownOccupancy(actor);
        PriorityQueue<(Hex Position, int Facing, int FirstDirection, int Cost), (int Score, int Sequence)> frontier = new();
        Dictionary<(Hex Position, int Facing), int> costs = new() { [(actor.Position, actor.Facing)] = 0 };
        int sequence = 0, expanded = 0;
        frontier.Enqueue((actor.Position, actor.Facing, -1, 0), (Distance(actor.Position), sequence++));
        while (frontier.TryDequeue(out (Hex Position, int Facing, int FirstDirection, int Cost) current, out _) && expanded < 3000) {
            if (costs[(current.Position, current.Facing)] != current.Cost) {
                continue;
            }

            expanded++;
            if (Distance(current.Position) == 0) {
                return current.FirstDirection;
            }

            void Visit(Hex position, int facing, int actionCost, int firstDirection) {
                int cost = current.Cost + actionCost;
                (Hex position, int facing) pose = (position, facing);
                if (costs.TryGetValue(pose, out int old) && old <= cost) {
                    return;
                }

                costs[pose] = cost;
                frontier.Enqueue((position, facing, firstDirection, cost), (cost + Distance(position), sequence++));
            }
            Hex next = current.Position + Hex.Directions[current.Facing];
            int movementCost = this.MovementCost(actor, next);
            if (movementCost <= actor.Unit.ActionPoints && this.CanOccupyKnown(actor, next, knownOccupancy)) {
                Visit(next, current.Facing, movementCost, current.FirstDirection < 0 ? current.Facing : current.FirstDirection);
            }

            Visit(current.Position, (current.Facing + 1) % 6, 1, current.FirstDirection);
            Visit(current.Position, (current.Facing + 5) % 6, 1, current.FirstDirection);
        }
        return -1;
    }
}
