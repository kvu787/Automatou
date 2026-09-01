namespace Automapolis.PixelPerfect;

internal sealed record ExtractionOptions(double? BackgroundThreshold, bool RemoveShadows);

internal sealed record ExtractedSprite(Raster Image, Raster MaskPreview, IntRect ForegroundBounds, double BackgroundThreshold);

internal static class SpriteExtractor
{
    private const double BorderSampleFraction = 0.16;

    public static ExtractedSprite Extract(Raster panel, ExtractionOptions options)
    {
        var background = QuadraticBackground.Fit(panel);
        var residuals = SampleBorderResiduals(panel, background).Order().ToArray();
        var median = Percentile(residuals, 0.50);
        var deviations = residuals.Select(value => Math.Abs(value - median)).Order().ToArray();
        var mad = Percentile(deviations, 0.50);
        var threshold = options.BackgroundThreshold ?? Math.Clamp(median + 6.0 * 1.4826 * mad, 9.0, 34.0);
        var candidate = new bool[panel.Width * panel.Height];
        for (var y = 0; y < panel.Height; y++)
        {
            for (var x = 0; x < panel.Width; x++)
            {
                var index = y * panel.Width + x;
                var pixel = panel.Pixels[index];
                if (pixel.A == 0)
                {
                    continue;
                }

                var expected = background.Predict(x, y, panel.Width, panel.Height);
                var difference = ColorDistance(pixel, expected);
                if (difference < threshold)
                {
                    continue;
                }

                if (options.RemoveShadows && LooksLikeCastShadow(panel, x, y, pixel, expected, difference, threshold))
                {
                    continue;
                }

                candidate[index] = true;
            }
        }

        candidate = Erode(Dilate(candidate, panel.Width, panel.Height, 1), panel.Width, panel.Height, 1);
        var components = FindComponents(candidate, panel.Width, panel.Height);
        if (components.Count == 0)
        {
            throw new InvalidDataException("No foreground could be separated from a panel. Try --background-threshold with a lower value.");
        }

        var primary = components.MaxBy(static component => component.Area)!;
        var minimumIndependentArea = Math.Max(8, (int)Math.Round(primary.Area * 0.012));
        var proximity = Math.Max(4, Math.Min(panel.Width, panel.Height) / 12);
        var kept = new bool[candidate.Length];
        foreach (var component in components)
        {
            if (!ReferenceEquals(component, primary) && options.RemoveShadows &&
                IsDetachedCastShadow(panel, component, primary, background, threshold))
            {
                continue;
            }

            var nearby = RectangleDistance(component.Bounds, primary.Bounds) <= proximity;
            if (component.Area < minimumIndependentArea && !nearby)
            {
                continue;
            }

            foreach (var index in component.Pixels)
            {
                kept[index] = true;
            }
        }

        if (options.RemoveShadows)
        {
            RemoveUnsupportedCastShadows(panel, kept, background, threshold);
        }

        var bounds = FindBounds(kept, panel.Width, panel.Height)
            ?? throw new InvalidDataException("Foreground cleanup removed every pixel in a panel.");
        bounds = bounds.Expand(2, panel.Width, panel.Height);
        var extracted = new Raster(bounds.Width, bounds.Height);
        for (var y = 0; y < bounds.Height; y++)
        {
            for (var x = 0; x < bounds.Width; x++)
            {
                var sourceX = bounds.X + x;
                var sourceY = bounds.Y + y;
                var source = panel[sourceX, sourceY];
                extracted[x, y] = kept[sourceY * panel.Width + sourceX]
                    ? source with { A = byte.MaxValue }
                    : Rgba32.Transparent;
            }
        }

        var preview = new Raster(panel.Width, panel.Height);
        for (var index = 0; index < kept.Length; index++)
        {
            preview.Pixels[index] = kept[index]
                ? new Rgba32(255, 255, 255, 255)
                : new Rgba32(12, 16, 24, 255);
        }

        return new(extracted, preview, bounds, threshold);
    }

    private static bool IsDetachedCastShadow(
        Raster panel,
        Component component,
        Component primary,
        QuadraticBackground background,
        double threshold)
    {
        if (component.Bounds.Width < component.Bounds.Height * 2.2 ||
            component.Bounds.Y < primary.Bounds.Y + primary.Bounds.Height * 0.52)
        {
            return false;
        }

        double saturationSum = 0;
        double darknessSum = 0;
        var stride = Math.Max(1, component.Pixels.Count / 256);
        var samples = 0;
        for (var index = 0; index < component.Pixels.Count; index += stride)
        {
            var pixelIndex = component.Pixels[index];
            var x = pixelIndex % panel.Width;
            var y = pixelIndex / panel.Width;
            var pixel = panel.Pixels[pixelIndex];
            var maximum = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
            var minimum = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
            saturationSum += maximum == 0 ? 0 : (maximum - minimum) / (double)maximum;
            darknessSum += Luminance(background.Predict(x, y, panel.Width, panel.Height)) - Luminance(pixel);
            samples++;
        }

        return samples > 0 && saturationSum / samples <= 0.20 && darknessSum / samples >= threshold * 0.25;
    }

    private static bool LooksLikeCastShadow(
        Raster panel,
        int x,
        int y,
        Rgba32 pixel,
        Rgba32 expected,
        double difference,
        double threshold)
    {
        var maximum = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
        var minimum = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
        var saturation = maximum == 0 ? 0 : (maximum - minimum) / (double)maximum;
        var luminance = Luminance(pixel);
        var expectedLuminance = Luminance(expected);
        if (expectedLuminance - luminance < threshold * 0.45 || saturation > 0.14 || difference > threshold * 2.6)
        {
            return false;
        }

        var localContrast = 0.0;
        for (var offsetY = -2; offsetY <= 2; offsetY += 2)
        {
            for (var offsetX = -2; offsetX <= 2; offsetX += 2)
            {
                var neighborX = Math.Clamp(x + offsetX, 0, panel.Width - 1);
                var neighborY = Math.Clamp(y + offsetY, 0, panel.Height - 1);
                localContrast = Math.Max(localContrast, ColorDistance(pixel, panel[neighborX, neighborY]));
            }
        }

        return localContrast < threshold * 0.85;
    }

    private static void RemoveUnsupportedCastShadows(
        Raster panel,
        bool[] foreground,
        QuadraticBackground background,
        double threshold)
    {
        var bounds = FindBounds(foreground, panel.Width, panel.Height);
        if (bounds is null)
        {
            return;
        }

        var shadowLike = new bool[foreground.Length];
        var lowerLimit = bounds.Value.Y + (int)Math.Round(bounds.Value.Height * 0.52);
        for (var y = lowerLimit; y < bounds.Value.Bottom; y++)
        {
            for (var x = bounds.Value.X; x < bounds.Value.Right; x++)
            {
                var index = y * panel.Width + x;
                if (!foreground[index])
                {
                    continue;
                }

                var pixel = panel.Pixels[index];
                var expected = background.Predict(x, y, panel.Width, panel.Height);
                var maximum = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
                var minimum = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
                var saturation = maximum == 0 ? 0 : (maximum - minimum) / (double)maximum;
                var difference = ColorDistance(pixel, expected);
                shadowLike[index] = saturation <= 0.18 &&
                    Luminance(expected) - Luminance(pixel) >= threshold * 0.30 &&
                    difference <= threshold * 3.6;
            }
        }

        var supportDepth = Math.Max(4, Math.Min(panel.Width, panel.Height) / 36);
        var remove = new bool[foreground.Length];
        for (var y = lowerLimit; y < bounds.Value.Bottom; y++)
        {
            for (var x = bounds.Value.X; x < bounds.Value.Right; x++)
            {
                var index = y * panel.Width + x;
                if (!shadowLike[index])
                {
                    continue;
                }

                var supported = false;
                for (var supportY = Math.Max(bounds.Value.Y, y - supportDepth); supportY < y - 1 && !supported; supportY++)
                {
                    for (var supportX = Math.Max(bounds.Value.X, x - 2); supportX <= Math.Min(bounds.Value.Right - 1, x + 2); supportX++)
                    {
                        var supportIndex = supportY * panel.Width + supportX;
                        if (foreground[supportIndex] && !shadowLike[supportIndex])
                        {
                            supported = true;
                            break;
                        }
                    }
                }

                remove[index] = !supported;
            }
        }

        for (var index = 0; index < foreground.Length; index++)
        {
            if (remove[index])
            {
                foreground[index] = false;
            }
        }
    }

    private static IReadOnlyList<double> SampleBorderResiduals(Raster panel, QuadraticBackground background)
    {
        var values = new List<double>();
        var step = Math.Max(1, Math.Min(panel.Width, panel.Height) / 180);
        for (var y = 0; y < panel.Height; y += step)
        {
            var normalizedY = y / (double)Math.Max(1, panel.Height - 1);
            for (var x = 0; x < panel.Width; x += step)
            {
                var normalizedX = x / (double)Math.Max(1, panel.Width - 1);
                if (normalizedX > BorderSampleFraction && normalizedX < 1 - BorderSampleFraction &&
                    normalizedY > BorderSampleFraction && normalizedY < 1 - BorderSampleFraction)
                {
                    continue;
                }

                values.Add(ColorDistance(panel[x, y], background.Predict(x, y, panel.Width, panel.Height)));
            }
        }

        return values;
    }

    private static List<Component> FindComponents(bool[] mask, int width, int height)
    {
        var visited = new bool[mask.Length];
        var queue = new Queue<int>();
        var result = new List<Component>();
        for (var start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || visited[start])
            {
                continue;
            }

            var pixels = new List<int>();
            var left = width;
            var top = height;
            var right = 0;
            var bottom = 0;
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                pixels.Add(index);
                var x = index % width;
                var y = index / width;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x + 1);
                bottom = Math.Max(bottom, y + 1);
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        var neighborX = x + offsetX;
                        var neighborY = y + offsetY;
                        if (neighborX < 0 || neighborX >= width || neighborY < 0 || neighborY >= height)
                        {
                            continue;
                        }

                        var neighbor = neighborY * width + neighborX;
                        if (mask[neighbor] && !visited[neighbor])
                        {
                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            result.Add(new(pixels.Count, new(left, top, right - left, bottom - top), pixels));
        }

        return result;
    }

    private static bool[] Dilate(bool[] source, int width, int height, int radius)
    {
        var result = new bool[source.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!source[y * width + x])
                {
                    continue;
                }

                for (var offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (var offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        var targetX = x + offsetX;
                        var targetY = y + offsetY;
                        if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                        {
                            result[targetY * width + targetX] = true;
                        }
                    }
                }
            }
        }

        return result;
    }

    private static bool[] Erode(bool[] source, int width, int height, int radius)
    {
        var result = new bool[source.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var keep = true;
                for (var offsetY = -radius; offsetY <= radius && keep; offsetY++)
                {
                    for (var offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        var targetX = x + offsetX;
                        var targetY = y + offsetY;
                        if (targetX < 0 || targetX >= width || targetY < 0 || targetY >= height || !source[targetY * width + targetX])
                        {
                            keep = false;
                            break;
                        }
                    }
                }

                result[y * width + x] = keep;
            }
        }

        return result;
    }

    private static IntRect? FindBounds(bool[] mask, int width, int height)
    {
        var left = width;
        var top = height;
        var right = 0;
        var bottom = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!mask[y * width + x])
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x + 1);
                bottom = Math.Max(bottom, y + 1);
            }
        }

        return right <= left || bottom <= top ? null : new(left, top, right - left, bottom - top);
    }

    private static int RectangleDistance(IntRect first, IntRect second)
    {
        var horizontal = Math.Max(0, Math.Max(first.X - second.Right, second.X - first.Right));
        var vertical = Math.Max(0, Math.Max(first.Y - second.Bottom, second.Y - first.Bottom));
        return Math.Max(horizontal, vertical);
    }

    private static double ColorDistance(Rgba32 first, Rgba32 second)
    {
        var red = first.R - second.R;
        var green = first.G - second.G;
        var blue = first.B - second.B;
        return Math.Sqrt((red * red + green * green + blue * blue) / 3.0);
    }

    private static double Luminance(Rgba32 pixel) => pixel.R * 0.2126 + pixel.G * 0.7152 + pixel.B * 0.0722;

    private static double Percentile(IReadOnlyList<double> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        return sorted[Math.Clamp((int)Math.Round((sorted.Count - 1) * percentile), 0, sorted.Count - 1)];
    }

    private sealed record Component(int Area, IntRect Bounds, IReadOnlyList<int> Pixels);

    private sealed class QuadraticBackground
    {
        private readonly double[][] coefficients;

        private QuadraticBackground(double[][] coefficients) => this.coefficients = coefficients;

        public static QuadraticBackground Fit(Raster panel)
        {
            var samples = new List<Sample>();
            var step = Math.Max(1, Math.Min(panel.Width, panel.Height) / 180);
            for (var y = 0; y < panel.Height; y += step)
            {
                var unitY = y / (double)Math.Max(1, panel.Height - 1) * 2 - 1;
                for (var x = 0; x < panel.Width; x += step)
                {
                    var normalizedX = x / (double)Math.Max(1, panel.Width - 1);
                    var normalizedY = y / (double)Math.Max(1, panel.Height - 1);
                    if (normalizedX > BorderSampleFraction && normalizedX < 1 - BorderSampleFraction &&
                        normalizedY > BorderSampleFraction && normalizedY < 1 - BorderSampleFraction)
                    {
                        continue;
                    }

                    var unitX = normalizedX * 2 - 1;
                    samples.Add(new(Features(unitX, unitY), panel[x, y]));
                }
            }

            IReadOnlyList<Sample> active = samples;
            QuadraticBackground? model = null;
            for (var iteration = 0; iteration < 3; iteration++)
            {
                var currentModel = new QuadraticBackground([
                    FitChannel(active, static color => color.R),
                    FitChannel(active, static color => color.G),
                    FitChannel(active, static color => color.B),
                ]);
                model = currentModel;
                var ranked = samples
                    .Select(sample => (Sample: sample, Residual: FeatureDistance(sample.Color, currentModel.Predict(sample.Features))))
                    .OrderBy(static item => item.Residual)
                    .ToArray();
                var cutoff = ranked[Math.Clamp((int)Math.Round((ranked.Length - 1) * 0.70), 0, ranked.Length - 1)].Residual;
                active = ranked.Where(item => item.Residual <= Math.Max(2.0, cutoff)).Select(static item => item.Sample).ToArray();
            }

            return model ?? throw new InvalidOperationException("Background fitting failed.");
        }

        public Rgba32 Predict(int x, int y, int width, int height)
        {
            var unitX = x / (double)Math.Max(1, width - 1) * 2 - 1;
            var unitY = y / (double)Math.Max(1, height - 1) * 2 - 1;
            return Predict(Features(unitX, unitY));
        }

        private Rgba32 Predict(double[] features) => new(
            ClampByte(Dot(coefficients[0], features)),
            ClampByte(Dot(coefficients[1], features)),
            ClampByte(Dot(coefficients[2], features)),
            255);

        private static double[] FitChannel(IReadOnlyList<Sample> samples, Func<Rgba32, byte> selector)
        {
            const int size = 6;
            var matrix = new double[size, size];
            var vector = new double[size];
            foreach (var sample in samples)
            {
                var value = selector(sample.Color);
                for (var row = 0; row < size; row++)
                {
                    vector[row] += sample.Features[row] * value;
                    for (var column = 0; column < size; column++)
                    {
                        matrix[row, column] += sample.Features[row] * sample.Features[column];
                    }
                }
            }

            for (var index = 0; index < size; index++)
            {
                matrix[index, index] += 1e-6;
            }

            return Solve(matrix, vector);
        }

        private static double[] Solve(double[,] matrix, double[] vector)
        {
            var size = vector.Length;
            for (var pivot = 0; pivot < size; pivot++)
            {
                var best = pivot;
                for (var row = pivot + 1; row < size; row++)
                {
                    if (Math.Abs(matrix[row, pivot]) > Math.Abs(matrix[best, pivot]))
                    {
                        best = row;
                    }
                }

                if (best != pivot)
                {
                    for (var column = pivot; column < size; column++)
                    {
                        (matrix[pivot, column], matrix[best, column]) = (matrix[best, column], matrix[pivot, column]);
                    }

                    (vector[pivot], vector[best]) = (vector[best], vector[pivot]);
                }

                var divisor = matrix[pivot, pivot];
                for (var column = pivot; column < size; column++)
                {
                    matrix[pivot, column] /= divisor;
                }

                vector[pivot] /= divisor;
                for (var row = 0; row < size; row++)
                {
                    if (row == pivot)
                    {
                        continue;
                    }

                    var factor = matrix[row, pivot];
                    for (var column = pivot; column < size; column++)
                    {
                        matrix[row, column] -= factor * matrix[pivot, column];
                    }

                    vector[row] -= factor * vector[pivot];
                }
            }

            return vector;
        }

        private static double[] Features(double x, double y) => [1, x, y, x * x, y * y, x * y];

        private static double Dot(IReadOnlyList<double> first, IReadOnlyList<double> second)
        {
            var sum = 0.0;
            for (var index = 0; index < first.Count; index++)
            {
                sum += first[index] * second[index];
            }

            return sum;
        }

        private static double FeatureDistance(Rgba32 color, Rgba32 predicted) => ColorDistance(color, predicted);

        private static byte ClampByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

        private sealed record Sample(double[] Features, Rgba32 Color);
    }
}
