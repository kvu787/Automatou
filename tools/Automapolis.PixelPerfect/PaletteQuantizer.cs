using System.Globalization;
using System.Text;

namespace Automapolis.PixelPerfect;

internal static class PaletteQuantizer
{
    public static IReadOnlyList<Rgba32> Quantize(IReadOnlyList<Raster> images, int maximumColors)
    {
        var histogram = new Dictionary<int, int>();
        foreach (var image in images)
        {
            foreach (var pixel in image.Pixels)
            {
                if (pixel.A == 0)
                {
                    continue;
                }

                var key = pixel.R << 16 | pixel.G << 8 | pixel.B;
                histogram[key] = histogram.GetValueOrDefault(key) + 1;
            }
        }

        if (histogram.Count == 0)
        {
            return [];
        }

        var entries = histogram
            .Select(static pair => new ColorEntry(FromKey(pair.Key), pair.Value))
            .ToArray();
        Rgba32[] palette;
        if (maximumColors <= 0 || entries.Length <= maximumColors)
        {
            palette = entries
                .OrderBy(static entry => entry.Lab.L)
                .ThenBy(static entry => Math.Atan2(entry.Lab.B, entry.Lab.A))
                .Select(static entry => entry.Color)
                .ToArray();
        }
        else
        {
            palette = BuildPalette(entries, maximumColors);
        }

        var paletteLab = palette.Select(Oklab.FromSrgb).ToArray();
        foreach (var image in images)
        {
            foreach (ref var pixel in image.Pixels.AsSpan())
            {
                if (pixel.A == 0)
                {
                    continue;
                }

                var lab = Oklab.FromSrgb(pixel);
                var best = 0;
                var bestDistance = double.MaxValue;
                for (var index = 0; index < paletteLab.Length; index++)
                {
                    var distance = lab.DistanceSquared(paletteLab[index]);
                    if (distance < bestDistance)
                    {
                        best = index;
                        bestDistance = distance;
                    }
                }

                pixel = palette[best] with { A = byte.MaxValue };
            }
        }

        return palette;
    }

    public static void WriteGimpPalette(string path, IReadOnlyList<Rgba32> palette)
    {
        var text = new StringBuilder();
        text.AppendLine("GIMP Palette");
        text.AppendLine("Name: Automapolis PixelPerfect");
        text.AppendLine("Columns: 8");
        text.AppendLine("#");
        for (var index = 0; index < palette.Count; index++)
        {
            var color = palette[index];
            text.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{color.R,3} {color.G,3} {color.B,3}\tColor {index + 1:00}"));
        }

        File.WriteAllText(path, text.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static Raster BuildSwatches(IReadOnlyList<Rgba32> palette, int swatchSize = 16, int columns = 8)
    {
        var rows = Math.Max(1, (int)Math.Ceiling(palette.Count / (double)columns));
        var image = new Raster(columns * swatchSize, rows * swatchSize);
        for (var index = 0; index < palette.Count; index++)
        {
            var left = index % columns * swatchSize;
            var top = index / columns * swatchSize;
            for (var y = 0; y < swatchSize; y++)
            {
                for (var x = 0; x < swatchSize; x++)
                {
                    image[left + x, top + y] = palette[index] with { A = byte.MaxValue };
                }
            }
        }

        return image;
    }

    private static Rgba32[] BuildPalette(IReadOnlyList<ColorEntry> entries, int count)
    {
        var centers = new List<Oklab>(count)
        {
            entries.MaxBy(static entry => entry.Weight)!.Lab,
        };
        while (centers.Count < count)
        {
            ColorEntry? selected = null;
            var selectedScore = double.MinValue;
            foreach (var entry in entries)
            {
                var distance = centers.Min(center => entry.Lab.DistanceSquared(center));
                var score = distance * Math.Pow(entry.Weight, 0.35);
                if (score > selectedScore)
                {
                    selected = entry;
                    selectedScore = score;
                }
            }

            centers.Add(selected!.Lab);
        }

        for (var iteration = 0; iteration < 24; iteration++)
        {
            var sums = new (double L, double A, double B, long Weight)[count];
            foreach (var entry in entries)
            {
                var nearest = FindNearest(entry.Lab, centers);
                var sum = sums[nearest];
                sums[nearest] = (
                    sum.L + entry.Lab.L * entry.Weight,
                    sum.A + entry.Lab.A * entry.Weight,
                    sum.B + entry.Lab.B * entry.Weight,
                    sum.Weight + entry.Weight);
            }

            var movement = 0.0;
            for (var index = 0; index < count; index++)
            {
                if (sums[index].Weight == 0)
                {
                    continue;
                }

                var updated = new Oklab(
                    sums[index].L / sums[index].Weight,
                    sums[index].A / sums[index].Weight,
                    sums[index].B / sums[index].Weight);
                movement += centers[index].DistanceSquared(updated);
                centers[index] = updated;
            }

            if (movement < 1e-10)
            {
                break;
            }
        }

        return centers
            .OrderBy(static center => center.L)
            .ThenBy(static center => Math.Atan2(center.B, center.A))
            .Select(static center => center.ToSrgb())
            .Distinct()
            .ToArray();
    }

    private static int FindNearest(Oklab color, IReadOnlyList<Oklab> palette)
    {
        var best = 0;
        var bestDistance = double.MaxValue;
        for (var index = 0; index < palette.Count; index++)
        {
            var distance = color.DistanceSquared(palette[index]);
            if (distance < bestDistance)
            {
                best = index;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static Rgba32 FromKey(int key) => new((byte)(key >> 16), (byte)(key >> 8), (byte)key, 255);

    private sealed record ColorEntry(Rgba32 Color, int Weight)
    {
        public Oklab Lab { get; } = Oklab.FromSrgb(Color);
    }

    private readonly record struct Oklab(double L, double A, double B)
    {
        public double DistanceSquared(Oklab other)
        {
            var lightness = L - other.L;
            var greenRed = A - other.A;
            var blueYellow = B - other.B;
            return lightness * lightness + greenRed * greenRed + blueYellow * blueYellow;
        }

        public static Oklab FromSrgb(Rgba32 color)
        {
            var red = ToLinear(color.R / 255.0);
            var green = ToLinear(color.G / 255.0);
            var blue = ToLinear(color.B / 255.0);
            var l = 0.4122214708 * red + 0.5363325363 * green + 0.0514459929 * blue;
            var m = 0.2119034982 * red + 0.6806995451 * green + 0.1073969566 * blue;
            var s = 0.0883024619 * red + 0.2817188376 * green + 0.6299787005 * blue;
            var lRoot = Math.Cbrt(l);
            var mRoot = Math.Cbrt(m);
            var sRoot = Math.Cbrt(s);
            return new(
                0.2104542553 * lRoot + 0.7936177850 * mRoot - 0.0040720468 * sRoot,
                1.9779984951 * lRoot - 2.4285922050 * mRoot + 0.4505937099 * sRoot,
                0.0259040371 * lRoot + 0.7827717662 * mRoot - 0.8086757660 * sRoot);
        }

        public Rgba32 ToSrgb()
        {
            var lRoot = L + 0.3963377774 * A + 0.2158037573 * B;
            var mRoot = L - 0.1055613458 * A - 0.0638541728 * B;
            var sRoot = L - 0.0894841775 * A - 1.2914855480 * B;
            var l = lRoot * lRoot * lRoot;
            var m = mRoot * mRoot * mRoot;
            var s = sRoot * sRoot * sRoot;
            var red = +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
            var green = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
            var blue = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;
            return new(ToByte(ToSrgbChannel(red)), ToByte(ToSrgbChannel(green)), ToByte(ToSrgbChannel(blue)), 255);
        }

        private static double ToLinear(double value) => value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);

        private static double ToSrgbChannel(double value) => value <= 0.0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - 0.055;

        private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
    }
}
