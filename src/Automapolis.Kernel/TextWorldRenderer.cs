using System.Text;

namespace Automapolis.Kernel;

/// <summary>
/// A reference text interface. Optional Shells may render the same snapshot differently.
/// </summary>
public static class TextWorldRenderer
{
    public static string Render(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var beingsByPosition = snapshot.Beings
            .GroupBy(static being => being.Position)
            .ToDictionary(static group => group.Key, static group => group.OrderByDescending(Priority).First());
        var tilesByPosition = snapshot.Tiles.ToDictionary(static tile => tile.Position);
        var builder = new StringBuilder();

        builder.AppendLine($"{snapshot.Name} // TURN {snapshot.Turn:000} // {snapshot.Mode.ToString().ToUpperInvariant()}");
        builder.Append('┌').Append(new string('─', snapshot.Width * 2)).AppendLine("┐");
        for (var y = 0; y < snapshot.Height; y++)
        {
            builder.Append('│');
            for (var x = 0; x < snapshot.Width; x++)
            {
                var point = new GridPoint(x, y);
                var glyph = beingsByPosition.TryGetValue(point, out var being) ? being.Glyph : tilesByPosition[point].Glyph;
                builder.Append(glyph).Append(' ');
            }

            builder.AppendLine("│");
        }

        builder.Append('└').Append(new string('─', snapshot.Width * 2)).AppendLine("┘");
        builder.AppendLine($"POP {snapshot.Metrics.Population}  BEINGS {snapshot.Metrics.Beings}  AETHER {snapshot.Metrics.TotalAether}  STABILITY {snapshot.Metrics.WorldStability}%");
        if (snapshot.Chronicle.Count > 0)
        {
            builder.AppendLine(snapshot.Chronicle[0]);
        }

        return builder.ToString();
    }

    private static int Priority(BeingSnapshot being) => being.Kind switch
    {
        BeingKind.Rift => 5,
        BeingKind.Settlement => 4,
        BeingKind.Oracle => 3,
        BeingKind.SynthBeast => 2,
        _ => 1
    };
}
