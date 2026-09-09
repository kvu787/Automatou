namespace Automapolis.Kernel;

internal sealed class TileState
{
    public required GridPoint Position { get; init; }
    public TerrainKind Terrain { get; set; }
    public int Resonance { get; set; }
    public int Biomass { get; set; }
    public int Integrity { get; set; }
}

internal sealed class ForceState
{
    public int Id { get; init; }
    public required GridPoint Position { get; set; }
    public ForceKind Kind { get; init; }
    public required string Name { get; init; }
    public int Strength { get; set; }
    public int Population { get; set; }
    public int ServiceTurns { get; set; }
    public string Intent { get; set; } = "Awaiting orders";
}
