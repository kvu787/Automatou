namespace Automapolis.PixelPerfect;

internal enum ScaleMode
{
    Shared,
    Individual,
}

internal enum SpriteAlignment
{
    Center,
    Bottom,
}

internal sealed record SpriteBuildOptions(
    int Size,
    int Padding,
    int AlphaThreshold,
    ScaleMode ScaleMode,
    SpriteAlignment Alignment);

internal static class SpriteBuilder
{
    public static IReadOnlyList<Raster> Build(IReadOnlyList<ExtractedSprite> extracted, SpriteBuildOptions options)
    {
        if (extracted.Count == 0)
        {
            return [];
        }

        var available = options.Size - options.Padding * 2;
        if (available <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Padding leaves no room for sprite pixels.");
        }

        var sharedScale = Math.Min(
            available / (double)extracted.Max(static sprite => sprite.Image.Width),
            available / (double)extracted.Max(static sprite => sprite.Image.Height));
        var result = new List<Raster>(extracted.Count);
        foreach (var item in extracted)
        {
            var scale = options.ScaleMode == ScaleMode.Shared
                ? sharedScale
                : Math.Min(available / (double)item.Image.Width, available / (double)item.Image.Height);
            var width = Math.Clamp((int)Math.Round(item.Image.Width * scale), 1, available);
            var height = Math.Clamp((int)Math.Round(item.Image.Height * scale), 1, available);
            var resized = Raster.ResizeAreaPremultiplied(item.Image, width, height);
            foreach (ref var pixel in resized.Pixels.AsSpan())
            {
                pixel = pixel.A >= options.AlphaThreshold
                    ? pixel with { A = byte.MaxValue }
                    : Rgba32.Transparent;
            }

            FillSmallTransparentHoles(resized, Math.Max(3, options.Size * options.Size / 700));
            RemoveSmallIslands(resized, minimumArea: 2);
            var canvas = new Raster(options.Size, options.Size);
            var x = (options.Size - width) / 2;
            var y = options.Alignment == SpriteAlignment.Bottom
                ? options.Size - options.Padding - height
                : (options.Size - height) / 2;
            canvas.Blit(resized, x, y);
            result.Add(canvas);
        }

        return result;
    }

    private static void RemoveSmallIslands(Raster image, int minimumArea)
    {
        var visited = new bool[image.Pixels.Length];
        var queue = new Queue<int>();
        for (var start = 0; start < image.Pixels.Length; start++)
        {
            if (visited[start] || image.Pixels[start].A == 0)
            {
                continue;
            }

            var component = new List<int>();
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                component.Add(index);
                var x = index % image.Width;
                var y = index / image.Width;
                Visit(x - 1, y);
                Visit(x + 1, y);
                Visit(x, y - 1);
                Visit(x, y + 1);

                void Visit(int neighborX, int neighborY)
                {
                    if (neighborX < 0 || neighborX >= image.Width || neighborY < 0 || neighborY >= image.Height)
                    {
                        return;
                    }

                    var neighbor = neighborY * image.Width + neighborX;
                    if (visited[neighbor] || image.Pixels[neighbor].A == 0)
                    {
                        return;
                    }

                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }

            if (component.Count >= minimumArea)
            {
                continue;
            }

            foreach (var index in component)
            {
                image.Pixels[index] = Rgba32.Transparent;
            }
        }
    }

    private static void FillSmallTransparentHoles(Raster image, int maximumArea)
    {
        var visited = new bool[image.Pixels.Length];
        var queue = new Queue<int>();
        for (var start = 0; start < image.Pixels.Length; start++)
        {
            if (visited[start] || image.Pixels[start].A != 0)
            {
                continue;
            }

            var component = new List<int>();
            var touchesBoundary = false;
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                component.Add(index);
                var x = index % image.Width;
                var y = index / image.Width;
                touchesBoundary |= x == 0 || y == 0 || x == image.Width - 1 || y == image.Height - 1;
                Visit(x - 1, y);
                Visit(x + 1, y);
                Visit(x, y - 1);
                Visit(x, y + 1);

                void Visit(int neighborX, int neighborY)
                {
                    if (neighborX < 0 || neighborX >= image.Width || neighborY < 0 || neighborY >= image.Height)
                    {
                        return;
                    }

                    var neighbor = neighborY * image.Width + neighborX;
                    if (visited[neighbor] || image.Pixels[neighbor].A != 0)
                    {
                        return;
                    }

                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }

            if (touchesBoundary || component.Count > maximumArea)
            {
                continue;
            }

            var red = 0;
            var green = 0;
            var blue = 0;
            var samples = 0;
            foreach (var index in component)
            {
                var x = index % image.Width;
                var y = index / image.Width;
                Sample(x - 1, y);
                Sample(x + 1, y);
                Sample(x, y - 1);
                Sample(x, y + 1);

                void Sample(int neighborX, int neighborY)
                {
                    if (neighborX < 0 || neighborX >= image.Width || neighborY < 0 || neighborY >= image.Height)
                    {
                        return;
                    }

                    var pixel = image[neighborX, neighborY];
                    if (pixel.A == 0)
                    {
                        return;
                    }

                    red += pixel.R;
                    green += pixel.G;
                    blue += pixel.B;
                    samples++;
                }
            }

            if (samples == 0)
            {
                continue;
            }

            var fill = new Rgba32((byte)(red / samples), (byte)(green / samples), (byte)(blue / samples), 255);
            foreach (var index in component)
            {
                image.Pixels[index] = fill;
            }
        }
    }
}
