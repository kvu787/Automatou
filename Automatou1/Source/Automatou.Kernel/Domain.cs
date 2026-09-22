namespace Automatou.Kernel;

public enum TerrainKind {
    ShatteredPlain,
    AshWaste,
    LeyChannel,
    Xenoforest,
    FortifiedReach,
    BroodMire
}

public enum ForceKind {
    Bastion,
    Soldier,
    Ravener,
    BroodNode,
    Enclave
}

/// <summary>Odd-row offset hex coordinates: column X and row Y.</summary>
public readonly record struct GridPoint(int X, int Y) {
    public override string ToString() {
        return $"{this.X},{this.Y}";
    }
}

public sealed record WorldConfig(
    int Width = 16,
    int Height = 12,
    long Seed = 475_023,
    string Name = "The Bastion Front") {
    public WorldConfig Validate() {
        if (this.Width is < 6 or > 80) {
            throw new ArgumentOutOfRangeException(nameof(this.Width), "Width must be between 6 and 80.");
        }

        if (this.Height is < 6 or > 50) {
            throw new ArgumentOutOfRangeException(nameof(this.Height), "Height must be between 6 and 50.");
        }

        return string.IsNullOrWhiteSpace(this.Name) || this.Name.Length > 48
            ? throw new ArgumentException("Name must contain 1 to 48 characters.", nameof(this.Name))
            : this;
    }
}

public sealed record TileSnapshot(
    GridPoint Position,
    TerrainKind Terrain,
    int Resonance,
    int Biomass,
    int Integrity,
    string Glyph,
    string Description);

public sealed record ForceSnapshot(
    int Id,
    GridPoint Position,
    ForceKind Kind,
    string Name,
    string Glyph,
    int Strength,
    int Population,
    int ServiceTurns,
    string Intent);

public sealed record WorldMetrics(
    int TotalResonance,
    int TotalBiomass,
    int HumanPopulation,
    int HumanForces,
    int AlienForces,
    int Bastions,
    int Enclaves,
    int TheaterIntegrity);

public sealed record WorldSnapshot(
    string Name,
    long Seed,
    int Width,
    int Height,
    int Turn,
    IReadOnlyList<TileSnapshot> Tiles,
    IReadOnlyList<ForceSnapshot> Forces,
    WorldMetrics Metrics,
    IReadOnlyList<string> Chronicle) {
    public string Topology { get; } = HexGrid.Topology;
    public string Coordinates { get; } = HexGrid.Coordinates;
}

public sealed record CommandResult(bool Accepted, string Message, WorldSnapshot Snapshot);
