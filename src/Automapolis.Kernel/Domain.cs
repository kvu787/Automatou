namespace Automapolis.Kernel;

public enum PlayerMode
{
    Observer,
    Creator
}

public enum TerrainKind
{
    StarGlass,
    AshDunes,
    AetherSea,
    CrystalForest,
    IronSteppe,
    DreamMarsh
}

public enum BeingKind
{
    Wanderer,
    SynthBeast,
    Oracle,
    Settlement,
    Rift
}

public readonly record struct GridPoint(int X, int Y)
{
    public override string ToString() => $"{X},{Y}";
}

public sealed record WorldConfig(
    int Width = 16,
    int Height = 12,
    long Seed = 475_023,
    PlayerMode Mode = PlayerMode.Creator,
    string Name = "Automapolis")
{
    public WorldConfig Validate()
    {
        if (Width is < 6 or > 80)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "Width must be between 6 and 80.");
        }

        if (Height is < 6 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), "Height must be between 6 and 50.");
        }

        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 48)
        {
            throw new ArgumentException("Name must contain 1 to 48 characters.", nameof(Name));
        }

        return this;
    }
}

public sealed record TileSnapshot(
    GridPoint Position,
    TerrainKind Terrain,
    int Aether,
    int Vitality,
    int Stability,
    string Glyph,
    string Description);

public sealed record BeingSnapshot(
    int Id,
    GridPoint Position,
    BeingKind Kind,
    string Name,
    string Glyph,
    int Energy,
    int Population,
    int Age,
    string Intent);

public sealed record WorldMetrics(
    int TotalAether,
    int TotalVitality,
    int Population,
    int Beings,
    int Settlements,
    int WorldStability);

public sealed record WorldSnapshot(
    string Name,
    long Seed,
    PlayerMode Mode,
    int Width,
    int Height,
    int Turn,
    IReadOnlyList<TileSnapshot> Tiles,
    IReadOnlyList<BeingSnapshot> Beings,
    WorldMetrics Metrics,
    IReadOnlyList<string> Chronicle);

public sealed record CommandResult(bool Accepted, string Message, WorldSnapshot Snapshot);
