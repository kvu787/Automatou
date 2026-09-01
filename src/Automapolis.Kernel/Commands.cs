namespace Automapolis.Kernel;

public abstract record WorldCommand;

public sealed record AdvanceTurn : WorldCommand;

public sealed record InfuseAether(GridPoint Position, int Amount = 25) : WorldCommand;

public sealed record TransmuteTerrain(GridPoint Position, TerrainKind Terrain) : WorldCommand;

public sealed record CreateLife(GridPoint Position, BeingKind Kind = BeingKind.Wanderer) : WorldCommand;

public sealed record FoundSettlement(GridPoint Position, string Name) : WorldCommand;

public sealed record InvokeCataclysm(GridPoint Position, int Radius = 1) : WorldCommand;
