namespace Automapolis.PixelPerfect;

internal sealed record DetectedPanel(int Row, int Column, IntRect Bounds);

internal sealed record DetectedGrid(GridSize Size, IReadOnlyList<DetectedPanel> Panels);

internal static class PanelDetector
{
    private const int MaximumAutomaticCells = 12;

    public static DetectedGrid Detect(Raster image, GridSize? requestedGrid, double insetFraction)
    {
        var horizontal = MeasureAxis(image, horizontal: true);
        var vertical = MeasureAxis(image, horizontal: false);
        var columns = requestedGrid?.Columns ?? DetectCellCount(horizontal, image.Width);
        var rows = requestedGrid?.Rows ?? DetectCellCount(vertical, image.Height);
        if (columns <= 0 || rows <= 0)
        {
            throw new InvalidDataException("The sprite-sheet grid could not be detected. Supply --grid COLUMNSxROWS.");
        }

        var columnBounds = FindBoundaries(horizontal, image.Width, columns);
        var rowBounds = FindBoundaries(vertical, image.Height, rows);
        var panels = new List<DetectedPanel>(columns * rows);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var bounds = new IntRect(
                    columnBounds[column],
                    rowBounds[row],
                    columnBounds[column + 1] - columnBounds[column],
                    rowBounds[row + 1] - rowBounds[row]);
                var inset = Math.Max(1, (int)Math.Round(Math.Min(bounds.Width, bounds.Height) * insetFraction));
                panels.Add(new(row, column, bounds.Inset(inset)));
            }
        }

        return new(new(columns, rows), panels);
    }

    private static AxisMetrics MeasureAxis(Raster image, bool horizontal)
    {
        var length = horizontal ? image.Width : image.Height;
        var perpendicular = horizontal ? image.Height : image.Width;
        var sampleStep = Math.Max(1, perpendicular / 512);
        var scores = new double[length];
        for (var position = 1; position < length; position++)
        {
            double differenceSum = 0;
            double luminanceSum = 0;
            double luminanceSquareSum = 0;
            var count = 0;
            for (var sample = 0; sample < perpendicular; sample += sampleStep)
            {
                var first = horizontal ? image[position - 1, sample] : image[sample, position - 1];
                var second = horizontal ? image[position, sample] : image[sample, position];
                var red = second.R - first.R;
                var green = second.G - first.G;
                var blue = second.B - first.B;
                differenceSum += Math.Sqrt((red * red + green * green + blue * blue) / 3.0);
                var luminance = ((first.R + second.R) * 0.1063) + ((first.G + second.G) * 0.3576) + ((first.B + second.B) * 0.0361);
                luminanceSum += luminance;
                luminanceSquareSum += luminance * luminance;
                count++;
            }

            var difference = differenceSum / count;
            var mean = luminanceSum / count;
            var variance = Math.Max(0, luminanceSquareSum / count - mean * mean);
            var standardDeviation = Math.Sqrt(variance);
            scores[position] = difference * 24.0 / (24.0 + standardDeviation);
        }

        return new(scores);
    }

    private static int DetectCellCount(AxisMetrics metrics, int length)
    {
        var nonzero = metrics.Scores.Skip(1).Order().ToArray();
        var median = Percentile(nonzero, 0.50);
        var high = Percentile(nonzero, 0.95);
        var scale = Math.Max(0.001, high - median);
        var maximum = Math.Min(MaximumAutomaticCells, Math.Max(1, length / 96));
        var selected = 1;
        var selectedQuality = 3.0;
        for (var cells = 2; cells <= maximum; cells++)
        {
            var cellSize = length / (double)cells;
            var strengths = new List<double>(cells - 1);
            for (var divider = 1; divider < cells; divider++)
            {
                var expected = divider * cellSize;
                var radius = Math.Max(4, (int)Math.Round(cellSize * 0.09));
                var left = Math.Max(1, (int)Math.Round(expected) - radius);
                var right = Math.Min(length - 1, (int)Math.Round(expected) + radius);
                var best = 0.0;
                for (var position = left; position <= right; position++)
                {
                    best = Math.Max(best, metrics.Scores[position]);
                }

                strengths.Add((best - median) / scale);
            }

            if (strengths.Count == 0)
            {
                continue;
            }

            var minimumStrength = strengths.Min();
            var averageStrength = strengths.Average();
            var quality = minimumStrength > 0
                ? Math.Sqrt(minimumStrength * averageStrength) * Math.Pow(cells, 0.10)
                : 0;
            if (quality > selectedQuality)
            {
                selected = cells;
                selectedQuality = quality;
            }
        }

        return selected;
    }

    private static int[] FindBoundaries(AxisMetrics metrics, int length, int cells)
    {
        var boundaries = new int[cells + 1];
        boundaries[0] = 0;
        boundaries[^1] = length;
        var cellSize = length / (double)cells;
        for (var divider = 1; divider < cells; divider++)
        {
            var expected = divider * cellSize;
            var radius = Math.Max(4, (int)Math.Round(cellSize * 0.09));
            var left = Math.Max(1, (int)Math.Round(expected) - radius);
            var right = Math.Min(length - 1, (int)Math.Round(expected) + radius);
            var bestPosition = left;
            for (var position = left + 1; position <= right; position++)
            {
                if (metrics.Scores[position] > metrics.Scores[bestPosition])
                {
                    bestPosition = position;
                }
            }

            var best = metrics.Scores[bestPosition];
            var pairRadius = Math.Max(4, (int)Math.Round(cellSize * 0.035));
            double weightedPosition = 0;
            double weightSum = 0;
            for (var position = Math.Max(left, bestPosition - pairRadius); position <= Math.Min(right, bestPosition + pairRadius); position++)
            {
                if (metrics.Scores[position] < best * 0.52)
                {
                    continue;
                }

                var weight = metrics.Scores[position] * metrics.Scores[position];
                weightedPosition += position * weight;
                weightSum += weight;
            }

            var boundary = weightSum > 0 ? (int)Math.Round(weightedPosition / weightSum) : (int)Math.Round(expected);
            boundaries[divider] = Math.Clamp(boundary, boundaries[divider - 1] + 1, length - (cells - divider));
        }

        return boundaries;
    }

    private static double Percentile(IReadOnlyList<double> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var index = Math.Clamp((int)Math.Round((sorted.Count - 1) * percentile), 0, sorted.Count - 1);
        return sorted[index];
    }

    private sealed record AxisMetrics(double[] Scores);
}
