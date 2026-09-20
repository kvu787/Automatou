namespace Automatou.Simulation;

// Axial coordinates internally; odd rows offset east in the public X/Y representation.
public readonly record struct Hex(int Q, int R)
{
    public int X => Q + (R - (R & 1)) / 2;
    public int Y => R;
    public static Hex FromOffset(int x, int y) => new(x - (y - (y & 1)) / 2, y);
    public static readonly Hex[] Directions = [new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1), new(1, -1)];
    public static readonly string[] DirectionNames = ["East", "Northeast", "Northwest", "West", "Southwest", "Southeast"];
    public static Hex operator +(Hex a, Hex b) => new(a.Q + b.Q, a.R + b.R);
    public static Hex operator -(Hex a, Hex b) => new(a.Q - b.Q, a.R - b.R);
    public int Distance(Hex other) => (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(Q + R - other.Q - other.R)) / 2;
    public Hex Rotate(int turns)
    {
        var result = this;
        for (int i = 0; i < ((turns % 6) + 6) % 6; i++) result = new(-result.R, result.Q + result.R);
        return result;
    }
    public static IEnumerable<Hex> Disk(int size)
    {
        int radius = size - 1;
        for (int q = -radius; q <= radius; q++)
            for (int r = Math.Max(-radius, -q - radius); r <= Math.Min(radius, -q + radius); r++)
                yield return new(q, r);
    }
    public int DirectionTo(Hex other)
    {
        if (this == other) return 0;
        var delta = other - this;
        double x = delta.Q + delta.R * .5, y = delta.R * Math.Sqrt(3) / 2;
        return ((int)Math.Round(Math.Atan2(y, x) / (Math.PI / 3)) + 6) % 6;
    }
    public static int TurnDistance(int a, int b) => Math.Min((a - b + 6) % 6, (b - a + 6) % 6);
    public override string ToString() => $"({X}, {Y})";
}
