using System.Text.Json;

namespace SimplePaint3DShell;

// Presentation DTOs for the JSON-lines boundary. Simulation stays in the Kernel process.
internal static class KernelProtocol
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

internal sealed record KernelResponse(bool Ok, string Message, KernelSnapshot? Snapshot);

internal sealed record KernelSnapshot(
    string Name, long Seed, int Width, int Height, int Turn,
    string Topology, string Coordinates,
    CellSnapshot[] Tiles, ForceSnapshot[] Forces, string[] Chronicle);

internal readonly record struct CellPosition(int X, int Y);

internal sealed record CellSnapshot(CellPosition Position, string Terrain, string Glyph, string Description);

internal sealed record ForceSnapshot(
    CellPosition Position, string Kind, string Name, string Glyph, int Strength, string Intent);
