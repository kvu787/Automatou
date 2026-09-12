using System.Text;

namespace Automatou.Kernel;

/// <summary>
/// A reference text interface. Optional Shells may render the same snapshot differently.
/// </summary>
public static class TextWorldRenderer
{
    public static string Render(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var forcesByPosition = snapshot.Forces
            .GroupBy(static force => force.Position)
            .ToDictionary(static group => group.Key, static group => group.OrderByDescending(Priority).First());
        var tilesByPosition = snapshot.Tiles.ToDictionary(static tile => tile.Position);
        var builder = new StringBuilder();

        builder.AppendLine($"{snapshot.Name} // TURN {snapshot.Turn:000}");
        builder.AppendLine("HEX GRID // odd rows shifted right // coordinates: column,row");
        for (var y = 0; y < snapshot.Height; y++)
        {
            builder.Append($"{y:00} ").Append((y & 1) == 1 ? "  " : "");
            for (var x = 0; x < snapshot.Width; x++)
            {
                var point = new GridPoint(x, y);
                var glyph = forcesByPosition.TryGetValue(point, out var force) ? force.Glyph : tilesByPosition[point].Glyph;
                builder.Append('[').Append(glyph).Append("] ");
            }
            builder.AppendLine();
        }

        builder.AppendLine($"HUMAN {snapshot.Metrics.HumanForces}  ALIEN {snapshot.Metrics.AlienForces}  POP {snapshot.Metrics.HumanPopulation}  RESONANCE {snapshot.Metrics.TotalResonance}  INTEGRITY {snapshot.Metrics.TheaterIntegrity}%");
        if (snapshot.Chronicle.Count > 0)
        {
            builder.AppendLine(snapshot.Chronicle[0]);
        }

        return builder.ToString();
    }

    private static int Priority(ForceSnapshot force) => force.Kind switch
    {
        ForceKind.Bastion => 5,
        ForceKind.BroodNode => 4,
        ForceKind.Enclave => 3,
        ForceKind.Ravener => 2,
        _ => 1
    };
}
