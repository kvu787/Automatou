using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Automapolis.PixelPerfect;

internal readonly record struct Rgba32(byte R, byte G, byte B, byte A)
{
    public static Rgba32 Transparent { get; } = new(0, 0, 0, 0);
}

internal sealed class Raster
{
    public Raster(int width, int height)
        : this(width, height, new Rgba32[checked(width * height)])
    {
    }

    public Raster(int width, int height, Rgba32[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (pixels.Length != checked(width * height))
        {
            throw new ArgumentException("Pixel count does not match raster dimensions.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }
    public Rgba32[] Pixels { get; }

    public ref Rgba32 this[int x, int y] => ref Pixels[y * Width + x];

    public static Raster Load(string path)
    {
        using var source = new Bitmap(path);
        using var converted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(converted))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        var rectangle = new Rectangle(0, 0, converted.Width, converted.Height);
        var data = converted.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var rowBytes = converted.Width * 4;
            var buffer = new byte[rowBytes];
            var pixels = new Rgba32[converted.Width * converted.Height];
            for (var y = 0; y < converted.Height; y++)
            {
                var sourceY = data.Stride >= 0 ? y : converted.Height - y - 1;
                Marshal.Copy(IntPtr.Add(data.Scan0, sourceY * Math.Abs(data.Stride)), buffer, 0, rowBytes);
                for (var x = 0; x < converted.Width; x++)
                {
                    var offset = x * 4;
                    pixels[y * converted.Width + x] = new(
                        buffer[offset + 2],
                        buffer[offset + 1],
                        buffer[offset],
                        buffer[offset + 3]);
                }
            }

            return new(converted.Width, converted.Height, pixels);
        }
        finally
        {
            converted.UnlockBits(data);
        }
    }

    public void Save(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (parent is not null)
        {
            Directory.CreateDirectory(parent);
        }

        using var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        var rectangle = new Rectangle(0, 0, Width, Height);
        var data = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var rowBytes = Width * 4;
            var buffer = new byte[rowBytes];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var pixel = this[x, y];
                    var offset = x * 4;
                    buffer[offset] = pixel.B;
                    buffer[offset + 1] = pixel.G;
                    buffer[offset + 2] = pixel.R;
                    buffer[offset + 3] = pixel.A;
                }

                var targetY = data.Stride >= 0 ? y : Height - y - 1;
                Marshal.Copy(buffer, 0, IntPtr.Add(data.Scan0, targetY * Math.Abs(data.Stride)), rowBytes);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        bitmap.Save(path, ImageFormat.Png);
    }

    public Raster Crop(IntRect bounds)
    {
        if (bounds.X < 0 || bounds.Y < 0 || bounds.Right > Width || bounds.Bottom > Height || bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds), $"Crop {bounds} is outside {Width}x{Height}.");
        }

        var result = new Raster(bounds.Width, bounds.Height);
        for (var y = 0; y < bounds.Height; y++)
        {
            Array.Copy(Pixels, (bounds.Y + y) * Width + bounds.X, result.Pixels, y * bounds.Width, bounds.Width);
        }

        return result;
    }

    public void Blit(Raster source, int targetX, int targetY)
    {
        for (var y = 0; y < source.Height; y++)
        {
            if (targetY + y < 0 || targetY + y >= Height)
            {
                continue;
            }

            for (var x = 0; x < source.Width; x++)
            {
                if (targetX + x < 0 || targetX + x >= Width)
                {
                    continue;
                }

                var foreground = source[x, y];
                if (foreground.A == 0)
                {
                    continue;
                }

                this[targetX + x, targetY + y] = foreground;
            }
        }
    }

    public static Raster ResizeAreaPremultiplied(Raster source, int targetWidth, int targetHeight)
    {
        if (targetWidth >= source.Width && targetHeight >= source.Height)
        {
            return ResizeNearest(source, targetWidth, targetHeight);
        }

        var result = new Raster(targetWidth, targetHeight);
        var scaleX = source.Width / (double)targetWidth;
        var scaleY = source.Height / (double)targetHeight;
        for (var targetY = 0; targetY < targetHeight; targetY++)
        {
            var sourceTop = targetY * scaleY;
            var sourceBottom = (targetY + 1) * scaleY;
            var firstY = (int)Math.Floor(sourceTop);
            var lastY = Math.Min(source.Height - 1, (int)Math.Ceiling(sourceBottom) - 1);
            for (var targetX = 0; targetX < targetWidth; targetX++)
            {
                var sourceLeft = targetX * scaleX;
                var sourceRight = (targetX + 1) * scaleX;
                var firstX = (int)Math.Floor(sourceLeft);
                var lastX = Math.Min(source.Width - 1, (int)Math.Ceiling(sourceRight) - 1);
                double alphaSum = 0;
                double redSum = 0;
                double greenSum = 0;
                double blueSum = 0;
                double areaSum = 0;
                for (var sourceY = firstY; sourceY <= lastY; sourceY++)
                {
                    var overlapY = Math.Min(sourceBottom, sourceY + 1) - Math.Max(sourceTop, sourceY);
                    for (var sourceX = firstX; sourceX <= lastX; sourceX++)
                    {
                        var overlapX = Math.Min(sourceRight, sourceX + 1) - Math.Max(sourceLeft, sourceX);
                        var weight = overlapX * overlapY;
                        var pixel = source[sourceX, sourceY];
                        var alpha = pixel.A / 255.0;
                        areaSum += weight;
                        alphaSum += alpha * weight;
                        redSum += pixel.R * alpha * weight;
                        greenSum += pixel.G * alpha * weight;
                        blueSum += pixel.B * alpha * weight;
                    }
                }

                if (alphaSum <= double.Epsilon)
                {
                    result[targetX, targetY] = Rgba32.Transparent;
                    continue;
                }

                result[targetX, targetY] = new(
                    ClampByte(redSum / alphaSum),
                    ClampByte(greenSum / alphaSum),
                    ClampByte(blueSum / alphaSum),
                    ClampByte(alphaSum / areaSum * 255));
            }
        }

        return result;
    }

    public static Raster ResizeNearest(Raster source, int targetWidth, int targetHeight)
    {
        var result = new Raster(targetWidth, targetHeight);
        for (var y = 0; y < targetHeight; y++)
        {
            var sourceY = Math.Min(source.Height - 1, y * source.Height / targetHeight);
            for (var x = 0; x < targetWidth; x++)
            {
                var sourceX = Math.Min(source.Width - 1, x * source.Width / targetWidth);
                result[x, y] = source[sourceX, sourceY];
            }
        }

        return result;
    }

    public Raster CompositeOnChecker(int tileSize)
    {
        var result = new Raster(Width, Height);
        var light = new Rgba32(216, 224, 232, 255);
        var dark = new Rgba32(135, 146, 165, 255);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var pixel = this[x, y];
                result[x, y] = pixel.A == 0
                    ? ((x / tileSize + y / tileSize) % 2 == 0 ? light : dark)
                    : pixel;
            }
        }

        return result;
    }

    private static byte ClampByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);
}
