using Automapolis.PixelPerfect;

var tests = new (string Name, Action Run)[]
{
    ("detects a regular card grid", DetectsRegularCardGrid),
    ("keeps an individual render as one panel", KeepsIndividualRenderAsOnePanel),
    ("extracts a sprite from a smooth background", ExtractsSpriteFromSmoothBackground),
    ("area resize uses premultiplied color", AreaResizeUsesPremultipliedColor),
    ("sprite builder emits binary alpha", SpriteBuilderEmitsBinaryAlpha),
    ("palette quantizer enforces its limit", PaletteQuantizerEnforcesLimit),
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

Console.WriteLine($"Passed {tests.Length} pixel-perfect converter tests.");
return;

static void DetectsRegularCardGrid()
{
    var image = new Raster(400, 300);
    for (var y = 0; y < image.Height; y++)
    {
        for (var x = 0; x < image.Width; x++)
        {
            var cardX = x / 100;
            var cardY = y / 100;
            var value = (byte)(38 + cardX * 3 + cardY * 2 + x % 100 / 18);
            image[x, y] = new(value, value, value, 255);
        }
    }

    DrawSeparator(image, 100, vertical: true);
    DrawSeparator(image, 200, vertical: true);
    DrawSeparator(image, 300, vertical: true);
    DrawSeparator(image, 100, vertical: false);
    DrawSeparator(image, 200, vertical: false);
    var grid = PanelDetector.Detect(image, requestedGrid: null, insetFraction: 0.05);
    Equal(4, grid.Size.Columns, "column count");
    Equal(3, grid.Size.Rows, "row count");
    Equal(12, grid.Panels.Count, "panel count");
}

static void ExtractsSpriteFromSmoothBackground()
{
    var image = new Raster(160, 160);
    for (var y = 0; y < image.Height; y++)
    {
        for (var x = 0; x < image.Width; x++)
        {
            var centeredX = (x - 80) / 80.0;
            var centeredY = (y - 80) / 80.0;
            var value = (byte)Math.Clamp((int)Math.Round(58 - 8 * (centeredX * centeredX + centeredY * centeredY)), 0, 255);
            image[x, y] = new(value, value, value, 255);
        }
    }

    for (var y = 38; y < 133; y++)
    {
        for (var x = 44; x < 118; x++)
        {
            var border = x < 49 || x >= 113 || y < 43 || y >= 128;
            image[x, y] = border ? new(5, 8, 12, 255) : new(210, 72, 35, 255);
        }
    }

    var extracted = SpriteExtractor.Extract(image, new(BackgroundThreshold: 12, RemoveShadows: true));
    True(extracted.ForegroundBounds.X is >= 40 and <= 48, "foreground left bound");
    True(extracted.ForegroundBounds.Y is >= 34 and <= 42, "foreground top bound");
    True(extracted.Image.Pixels.Count(static pixel => pixel.A == 255) > 5_000, "opaque foreground area");
    True(extracted.Image.Pixels.Any(static pixel => pixel.A == 0), "transparent crop padding");
}

static void KeepsIndividualRenderAsOnePanel()
{
    var image = new Raster(240, 240);
    for (var y = 0; y < image.Height; y++)
    {
        for (var x = 0; x < image.Width; x++)
        {
            var value = (byte)(42 + (x + y) / 60);
            image[x, y] = new(value, value, value, 255);
            var offsetX = x - 124;
            var offsetY = y - 132;
            if (offsetX * offsetX / 2 + offsetY * offsetY < 2_800)
            {
                image[x, y] = new(35, (byte)(100 + y % 20), 175, 255);
            }
        }
    }

    var grid = PanelDetector.Detect(image, requestedGrid: null, insetFraction: 0.05);
    Equal(1, grid.Size.Columns, "individual column count");
    Equal(1, grid.Size.Rows, "individual row count");
}

static void AreaResizeUsesPremultipliedColor()
{
    var source = new Raster(2, 1, [new(255, 0, 0, 255), new(0, 0, 255, 0)]);
    var result = Raster.ResizeAreaPremultiplied(source, 1, 1);
    Equal((byte)255, result[0, 0].R, "premultiplied red");
    Equal((byte)0, result[0, 0].B, "transparent blue must not bleed");
    True(result[0, 0].A is >= 127 and <= 128, "half coverage alpha");
}

static void SpriteBuilderEmitsBinaryAlpha()
{
    var source = new Raster(12, 12);
    for (var y = 2; y < 10; y++)
    {
        for (var x = 2; x < 10; x++)
        {
            source[x, y] = new(30, 160, 220, 255);
        }
    }

    var preview = new Raster(12, 12);
    var extracted = new ExtractedSprite(source, preview, new(0, 0, 12, 12), 10);
    var result = SpriteBuilder.Build(
        [extracted],
        new(Size: 8, Padding: 1, AlphaThreshold: 96, ScaleMode.Individual, SpriteAlignment.Center))[0];
    Equal(8, result.Width, "sprite width");
    Equal(8, result.Height, "sprite height");
    True(result.Pixels.All(static pixel => pixel.A is 0 or 255), "binary alpha");
}

static void PaletteQuantizerEnforcesLimit()
{
    var image = new Raster(4, 1, [
        new(255, 0, 0, 255),
        new(0, 255, 0, 255),
        new(0, 0, 255, 255),
        new(255, 255, 255, 255),
    ]);
    var palette = PaletteQuantizer.Quantize([image], maximumColors: 2);
    True(palette.Count <= 2, "palette count");
    True(image.Pixels.Where(static pixel => pixel.A != 0).Distinct().Count() <= 2, "mapped color count");
}

static void DrawSeparator(Raster image, int position, bool vertical)
{
    for (var offset = -2; offset <= 2; offset++)
    {
        if (vertical)
        {
            for (var y = 0; y < image.Height; y++) image[position + offset, y] = new(2, 3, 5, 255);
        }
        else
        {
            for (var x = 0; x < image.Width; x++) image[x, position + offset] = new(2, 3, 5, 255);
        }
    }
}

static void Equal<T>(T expected, T actual, string message) where T : IEquatable<T>
{
    if (!expected.Equals(actual))
    {
        throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }
}

static void True(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
