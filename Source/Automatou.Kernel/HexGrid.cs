namespace Automatou.Kernel;

/// <summary>Pointy-top hexagons in odd-row offset coordinates: X is column,
/// Y is row, and odd rows are shifted half a hex to the right.</summary>
public static class HexGrid {
    public const string Topology = "hexagonal";
    public const string Coordinates = "oddRowOffset";

    // Clockwise from east: east, southeast, southwest, west, northwest, northeast.
    private static readonly (int Q, int R)[] Directions =
        [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)];

    public static GridPoint Neighbor(GridPoint point, int direction) {
        if (direction is < 0 or >= 6) {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        int q = point.X - ((point.Y - (point.Y & 1)) / 2);
        (int dq, int dr) = Directions[direction];
        int row = point.Y + dr;
        return new(q + dq + ((row - (row & 1)) / 2), row);
    }

    public static IEnumerable<GridPoint> Neighbors(GridPoint point, int width, int height) {
        for (int direction = 0; direction < 6; direction++) {
            GridPoint neighbor = Neighbor(point, direction);
            if (Contains(neighbor, width, height)) {
                yield return neighbor;
            }
        }
    }

    public static bool Contains(GridPoint point, int width, int height) {
        return point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height;
    }

    public static int Distance(GridPoint left, GridPoint right) {
        int leftQ = left.X - ((left.Y - (left.Y & 1)) / 2);
        int rightQ = right.X - ((right.Y - (right.Y & 1)) / 2);
        int dq = leftQ - rightQ;
        int dr = left.Y - right.Y;
        return Math.Max(Math.Abs(dq), Math.Max(Math.Abs(dr), Math.Abs(dq + dr)));
    }
}
