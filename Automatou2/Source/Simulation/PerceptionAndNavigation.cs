namespace Automatou.Simulation;

public sealed partial class World {
    // Terrain is public knowledge. Occupants are revealed only by current senses.
    public bool CanObserve(Entity observer, Entity target, int? sightRange = null) {
        if (observer == target) {
            return true;
        }

        Hex[] origins = observer.OccupiedCells().ToArray();
        return target.OccupiedCells().Any(destination => origins.Any(origin => this.CanSeeCell(observer, origin, destination, sightRange)));
    }

    public IReadOnlyList<Hex> SightCells(Entity observer) {
        Hex[] origins = observer.OccupiedCells().ToArray();
        return Array.AsReadOnly(this.Terrain.Keys.Where(cell => origins.Any(origin => this.CanSeeCell(observer, origin, cell))).ToArray());
    }

    private bool CanSeeCell(Entity observer, Hex origin, Hex destination, int? sightRange = null) {
        int distance = origin.Distance(destination);
        if (distance > (sightRange ?? observer.Unit.SightRange)) {
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

}
