namespace Automapolis.Kernel;

public abstract record WorldCommand;

public sealed record AdvanceTurn : WorldCommand;

public sealed record ChannelResonance(GridPoint Position, int Amount = 25) : WorldCommand;

public sealed record FortifyTerrain(GridPoint Position, TerrainKind Terrain) : WorldCommand;

public sealed record DeployForce(GridPoint Position, ForceKind Kind = ForceKind.Soldier) : WorldCommand;

public sealed record EstablishEnclave(GridPoint Position, string Name) : WorldCommand;

public sealed record InvokePurge(GridPoint Position, int Radius = 1) : WorldCommand;
