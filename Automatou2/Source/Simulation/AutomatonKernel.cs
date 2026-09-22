using System.Collections.ObjectModel;

namespace Automatou.Simulation;

// Host-only syscall implementation. All sessions share one frozen pre-resolution world.
// No entities, automata, mutable world collections or navigation oracles are returned.
internal sealed class AutomatonKernel(World snapshot, Entity observer) : IAutomatonSystemCalls {
    private bool closed;
    public int RemainingEnergy { get; private set; } = observer.Unit.TurnEnergy;
    public int VisionRange { get; private set; }
    public HashSet<int> ObservedIds { get; } = [];
    public List<SensingReceipt> Receipts { get; } = [];
    public void Close() {
        this.closed = true;
    }

    private bool Charge(string call, int cost) {
        if (this.closed) { throw new InvalidOperationException("This turn's system-call session is closed."); }
        bool accepted = cost <= this.RemainingEnergy;
        if (accepted) { this.RemainingEnergy -= cost; }
        if (this.Receipts.Count < 64) { this.Receipts.Add(new(call, accepted ? cost : 0, this.RemainingEnergy, accepted)); }
        return accepted;
    }
    public VisionObservation? ScanVision(int extraRange = 0) {
        if (extraRange is < 0 or > AutomatonCosts.MaximumExtraRange) { throw new ArgumentOutOfRangeException(nameof(extraRange)); }
        if (!this.Charge($"Vision +{extraRange}", AutomatonCosts.VisionCost(extraRange))) { return null; }
        int range = observer.Unit.SightRange + extraRange;
        this.VisionRange = Math.Max(this.VisionRange, range);
        EntityObservation[] entities = snapshot.Entities.Where(entity => snapshot.CanObserve(observer, entity, range))
            .Select(entity => Copy(entity) with { BondedUnitId = entity.Id == observer.Id ? entity.BondedUnitId : null }).ToArray();
        this.ObservedIds.UnionWith(entities.Select(entity => entity.Id));
        return new(new(snapshot.Turn, Copy(observer), Array.AsReadOnly(entities)), snapshot.Settings with { }, range,
            Array.AsReadOnly(observer.LastTurn.Outcomes.ToArray()));
    }
    public TerrainObservation? SurveyTerrain() {
        if (!this.Charge("Terrain survey", AutomatonCosts.TerrainSurvey)) { return null; }
        return new(new ReadOnlyDictionary<Hex, Terrain>(new Dictionary<Hex, Terrain>(snapshot.Terrain)));
    }
    private static EntityObservation Copy(Entity entity) {
        return new(entity.Id, entity.Faction, entity.Position, entity.Facing,
        entity.Health, entity.MaximumHealth, entity.Stationary, entity.Unit.Statistics,
        Array.AsReadOnly(entity.OccupiedCells().ToArray()), entity.Heat, entity.WeaponLocked, entity.BondedUnitId);
    }
}
