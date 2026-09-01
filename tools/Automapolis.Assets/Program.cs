using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;

const int spriteSize = 16;
var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
var sourcePath = Path.Combine(repositoryRoot, "Shells", "Godot", "assets", "sprites", "sprites.json");
var outputDirectory = Path.Combine(repositoryRoot, "Shells", "Godot", "assets", "sprites", "generated");
var source = JsonSerializer.Deserialize<SpriteSource>(File.ReadAllText(sourcePath), new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
}) ?? throw new InvalidDataException("Sprite source is empty.");

var palette = source.Palette.ToDictionary(
    static pair => RequireSingleCharacter(pair.Key),
    static pair => Rgba32.Parse(pair.Value));
var sprites = new List<CompiledSprite>();

foreach (var definition in source.Sprites)
{
    if (definition.Rows.Length != spriteSize)
    {
        throw new InvalidDataException($"Sprite '{definition.Id}' has {definition.Rows.Length} rows; expected {spriteSize}.");
    }

    var pixels = new Rgba32[spriteSize * spriteSize];
    for (var y = 0; y < spriteSize; y++)
    {
        var row = string.Concat(definition.Rows[y].Where(static character => !char.IsWhiteSpace(character)));
        if (row.Length != spriteSize)
        {
            throw new InvalidDataException($"Sprite '{definition.Id}' row {y + 1} has {row.Length} pixels; expected {spriteSize}.");
        }

        for (var x = 0; x < spriteSize; x++)
        {
            if (!palette.TryGetValue(row[x], out var color))
            {
                throw new InvalidDataException($"Sprite '{definition.Id}' uses unknown palette key '{row[x]}' at {x},{y}.");
            }

            pixels[y * spriteSize + x] = color;
        }
    }

    sprites.Add(new(definition.Id, definition.Kind, pixels));
}

Directory.CreateDirectory(outputDirectory);
foreach (var sprite in sprites)
{
    PngWriter.Write(Path.Combine(outputDirectory, $"{sprite.Id}.png"), spriteSize, spriteSize, sprite.Pixels);
}

var atlasColumns = 4;
var atlasRows = (int)Math.Ceiling(sprites.Count / (double)atlasColumns);
var atlasWidth = atlasColumns * spriteSize;
var atlasHeight = atlasRows * spriteSize;
var atlasPixels = Enumerable.Repeat(Rgba32.Transparent, atlasWidth * atlasHeight).ToArray();
var manifestSprites = new List<object>();
for (var index = 0; index < sprites.Count; index++)
{
    var sprite = sprites[index];
    var atlasX = index % atlasColumns;
    var atlasY = index / atlasColumns;
    for (var y = 0; y < spriteSize; y++)
    {
        Array.Copy(sprite.Pixels, y * spriteSize, atlasPixels, (atlasY * spriteSize + y) * atlasWidth + atlasX * spriteSize, spriteSize);
    }

    manifestSprites.Add(new { sprite.Id, sprite.Kind, atlasX, atlasY, width = spriteSize, height = spriteSize });
}

PngWriter.Write(Path.Combine(outputDirectory, "atlas.png"), atlasWidth, atlasHeight, atlasPixels);

var unitSprites = sprites.Where(static sprite => sprite.Kind.EndsWith("-unit", StringComparison.Ordinal)).ToArray();
var unitAtlas = BuildAtlas(unitSprites, atlasColumns);
PngWriter.Write(Path.Combine(outputDirectory, "units.png"), unitAtlas.Width, unitAtlas.Height, unitAtlas.Pixels);
var unitPreview = CompositeOnChecker(ScaleNearest(unitAtlas.Pixels, unitAtlas.Width, unitAtlas.Height, 8), unitAtlas.Width * 8, unitAtlas.Height * 8, 16);
PngWriter.Write(Path.Combine(outputDirectory, "units-preview.png"), unitAtlas.Width * 8, unitAtlas.Height * 8, unitPreview);

File.WriteAllText(
    Path.Combine(outputDirectory, "manifest.json"),
    JsonSerializer.Serialize(
        new { tileSize = spriteSize, columns = atlasColumns, rows = atlasRows, sprites = manifestSprites },
        new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) + Environment.NewLine);

Console.WriteLine($"Generated {sprites.Count} sprites, a {atlasWidth}x{atlasHeight} atlas, and a {unitAtlas.Width}x{unitAtlas.Height} unit sheet from {Path.GetRelativePath(repositoryRoot, sourcePath)}.");
return;

static (int Width, int Height, Rgba32[] Pixels) BuildAtlas(IReadOnlyList<CompiledSprite> sprites, int columns)
{
    var rows = (int)Math.Ceiling(sprites.Count / (double)columns);
    var width = columns * spriteSize;
    var height = rows * spriteSize;
    var pixels = Enumerable.Repeat(Rgba32.Transparent, width * height).ToArray();
    for (var index = 0; index < sprites.Count; index++)
    {
        var sprite = sprites[index];
        var atlasX = index % columns;
        var atlasY = index / columns;
        for (var y = 0; y < spriteSize; y++)
        {
            Array.Copy(sprite.Pixels, y * spriteSize, pixels, (atlasY * spriteSize + y) * width + atlasX * spriteSize, spriteSize);
        }
    }

    return (width, height, pixels);
}

static Rgba32[] ScaleNearest(IReadOnlyList<Rgba32> source, int width, int height, int scale)
{
    var resultWidth = width * scale;
    var result = new Rgba32[resultWidth * height * scale];
    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var pixel = source[y * width + x];
            for (var offsetY = 0; offsetY < scale; offsetY++)
            {
                var targetRow = (y * scale + offsetY) * resultWidth;
                for (var offsetX = 0; offsetX < scale; offsetX++)
                {
                    result[targetRow + x * scale + offsetX] = pixel;
                }
            }
        }
    }

    return result;
}

static Rgba32[] CompositeOnChecker(IReadOnlyList<Rgba32> source, int width, int height, int tileSize)
{
    var light = new Rgba32(216, 224, 232, byte.MaxValue);
    var dark = new Rgba32(135, 146, 165, byte.MaxValue);
    var result = new Rgba32[source.Count];
    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var pixel = source[y * width + x];
            result[y * width + x] = pixel.A == 0
                ? ((x / tileSize + y / tileSize) % 2 == 0 ? light : dark)
                : pixel;
        }
    }

    return result;
}

static string FindRepositoryRoot(string start)
{
    var current = new DirectoryInfo(start);
    while (current is not null && !File.Exists(Path.Combine(current.FullName, "Automapolis.slnx")))
    {
        current = current.Parent;
    }

    return current?.FullName ?? throw new DirectoryNotFoundException("Could not locate the Automapolis repository root.");
}

static char RequireSingleCharacter(string value) =>
    value.Length == 1 ? value[0] : throw new InvalidDataException($"Palette key '{value}' must be one character.");

internal sealed record SpriteSource(Dictionary<string, string> Palette, SpriteDefinition[] Sprites);

internal sealed record SpriteDefinition(string Id, string Kind, string[] Rows);

internal sealed record CompiledSprite(string Id, string Kind, Rgba32[] Pixels);

internal readonly record struct Rgba32(byte R, byte G, byte B, byte A)
{
    public static Rgba32 Transparent { get; } = new(0, 0, 0, 0);

    public static Rgba32 Parse(string value)
    {
        if (value.Length is not (7 or 9) || value[0] != '#')
        {
            throw new InvalidDataException($"Color '{value}' must use #RRGGBB or #RRGGBBAA notation.");
        }

        return new(
            Convert.ToByte(value.Substring(1, 2), 16),
            Convert.ToByte(value.Substring(3, 2), 16),
            Convert.ToByte(value.Substring(5, 2), 16),
            value.Length == 9 ? Convert.ToByte(value.Substring(7, 2), 16) : byte.MaxValue);
    }
}

internal static class PngWriter
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static void Write(string path, int width, int height, IReadOnlyList<Rgba32> pixels)
    {
        if (pixels.Count != width * height)
        {
            throw new ArgumentException("Pixel count does not match image dimensions.", nameof(pixels));
        }

        using var file = File.Create(path);
        file.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header[..4], (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.Slice(4, 4), (uint)height);
        header[8] = 8;
        header[9] = 6;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(file, "IHDR"u8, header);

        using var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                raw.WriteByte(pixel.R);
                raw.WriteByte(pixel.G);
                raw.WriteByte(pixel.B);
                raw.WriteByte(pixel.A);
            }
        }

        raw.Position = 0;
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            raw.CopyTo(zlib);
        }

        WriteChunk(file, "IDAT"u8, compressed.ToArray());
        WriteChunk(file, "IEND"u8, []);
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> integer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(integer, (uint)data.Length);
        stream.Write(integer);
        stream.Write(type);
        stream.Write(data);

        var crc = uint.MaxValue;
        foreach (var value in type)
        {
            crc = UpdateCrc(crc, value);
        }

        foreach (var value in data)
        {
            crc = UpdateCrc(crc, value);
        }

        BinaryPrimitives.WriteUInt32BigEndian(integer, ~crc);
        stream.Write(integer);
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
        {
            crc = (crc >> 1) ^ (0xEDB88320U & (uint)-(int)(crc & 1));
        }

        return crc;
    }
}
