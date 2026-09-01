namespace Automapolis.Kernel;

internal sealed class TileState
{
    public required GridPoint Position { get; init; }
    public TerrainKind Terrain { get; set; }
    public int Aether { get; set; }
    public int Vitality { get; set; }
    public int Stability { get; set; }
}

internal sealed class BeingState
{
    public int Id { get; init; }
    public required GridPoint Position { get; set; }
    public BeingKind Kind { get; init; }
    public required string Name { get; init; }
    public int Energy { get; set; }
    public int Population { get; set; }
    public int Age { get; set; }
    public string Intent { get; set; } = "Awakening";
}
