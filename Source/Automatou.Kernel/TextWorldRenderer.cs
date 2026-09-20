using System.Globalization;
using System.Text;

namespace Automatou.Kernel;

/// <summary>
/// A reference text interface. Optional Shells may render the same snapshot differently.
/// </summary>
public static class TextWorldRenderer {
    public static string Render(WorldSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);
        Dictionary<GridPoint, ForceSnapshot> forcesByPosition = snapshot.Forces
            .GroupBy(static force => force.Position)
            .ToDictionary(static group => group.Key, static group => group.OrderByDescending(Priority).First());
        Dictionary<GridPoint, TileSnapshot> tilesByPosition = snapshot.Tiles.ToDictionary(static tile => tile.Position);
        StringBuilder builder = new();

        _ = builder.AppendLine(CultureInfo.CurrentCulture, $"{snapshot.Name} // TURN {snapshot.Turn:000}");
        _ = builder.AppendLine("HEX GRID // odd rows shifted right // coordinates: column,row");
        for (int y = 0; y < snapshot.Height; y++) {
            _ = builder.Append(CultureInfo.CurrentCulture, $"{y:00} ").Append((y & 1) == 1 ? "  " : "");
            for (int x = 0; x < snapshot.Width; x++) {
                GridPoint point = new(x, y);
                string glyph = forcesByPosition.TryGetValue(point, out ForceSnapshot? force) ? force.Glyph : tilesByPosition[point].Glyph;
                _ = builder.Append('[').Append(glyph).Append("] ");
            }
            _ = builder.AppendLine();
        }

        _ = builder.AppendLine(CultureInfo.CurrentCulture, $"HUMAN {snapshot.Metrics.HumanForces}  ALIEN {snapshot.Metrics.AlienForces}  POP {snapshot.Metrics.HumanPopulation}  RESONANCE {snapshot.Metrics.TotalResonance}  INTEGRITY {snapshot.Metrics.TheaterIntegrity}%");
        if (snapshot.Chronicle.Count > 0) {
            _ = builder.AppendLine(snapshot.Chronicle[0]);
        }

        return builder.ToString();
    }

    private static int Priority(ForceSnapshot force) {
        return force.Kind switch {
            ForceKind.Bastion => 5,
            ForceKind.BroodNode => 4,
            ForceKind.Enclave => 3,
            ForceKind.Ravener => 2,
            ForceKind.Soldier => 1,
            _ => 1
        };
    }
}
