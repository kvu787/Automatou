namespace Automapolis.PixelPerfect;

internal readonly record struct IntRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public IntRect Inset(int amount)
    {
        var inset = Math.Min(amount, Math.Max(0, Math.Min(Width, Height) / 2 - 1));
        return new(X + inset, Y + inset, Width - inset * 2, Height - inset * 2);
    }

    public IntRect Expand(int amount, int maximumWidth, int maximumHeight)
    {
        var left = Math.Max(0, X - amount);
        var top = Math.Max(0, Y - amount);
        var right = Math.Min(maximumWidth, Right + amount);
        var bottom = Math.Min(maximumHeight, Bottom + amount);
        return new(left, top, right - left, bottom - top);
    }

    public static IntRect Union(IntRect first, IntRect second)
    {
        var left = Math.Min(first.X, second.X);
        var top = Math.Min(first.Y, second.Y);
        var right = Math.Max(first.Right, second.Right);
        var bottom = Math.Max(first.Bottom, second.Bottom);
        return new(left, top, right - left, bottom - top);
    }
}

internal readonly record struct GridSize(int Columns, int Rows)
{
    public override string ToString() => $"{Columns}x{Rows}";
}
