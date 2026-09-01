using System.Text.Json;

namespace Automapolis.PixelPerfect;

internal static class Program
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    public static int Main(string[] args)
    {
        try
        {
            var options = CommandLine.Parse(args);
            if (options.ShowHelp)
            {
                Console.WriteLine(CommandLine.HelpText);
                return 0;
            }

            Run(options);
            return 0;
        }
        catch (Exception error) when (error is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"error: {error.Message}");
            return 1;
        }
    }

    private static void Run(ConverterOptions options)
    {
        var inputPaths = ExpandInputs(options.Inputs);
        if (inputPaths.Count == 0)
        {
            throw new ArgumentException("At least one supported input image is required. Use --input PATH.");
        }

        var outputPath = Path.GetFullPath(options.Output!);
        if (options.Clean && Directory.Exists(outputPath))
        {
            ValidateCleanTarget(outputPath);
            Directory.Delete(outputPath, recursive: true);
        }

        Directory.CreateDirectory(outputPath);
        var spriteDirectory = Path.Combine(outputPath, "sprites");
        Directory.CreateDirectory(spriteDirectory);
        var diagnosticsDirectory = Path.Combine(outputPath, "diagnostics");
        if (options.Diagnostics)
        {
            Directory.CreateDirectory(diagnosticsDirectory);
        }

        var sources = new List<SourceResult>();
        var work = new List<SpriteWork>();
        foreach (var inputPath in inputPaths)
        {
            var source = Raster.Load(inputPath);
            var grid = PanelDetector.Detect(source, options.Grid, options.PanelInset);
            var sourceIndex = sources.Count;
            sources.Add(new(inputPath, source.Width, source.Height, grid.Size));
            Console.WriteLine($"{Path.GetFileName(inputPath)}: {source.Width}x{source.Height}, grid {grid.Size}");
            foreach (var panel in grid.Panels)
            {
                var panelImage = source.Crop(panel.Bounds);
                var extracted = SpriteExtractor.Extract(
                    panelImage,
                    new(options.BackgroundThreshold, options.RemoveShadows));
                work.Add(new(sourceIndex, panel, extracted));
                if (options.Diagnostics)
                {
                    var diagnosticStem = $"{Path.GetFileNameWithoutExtension(inputPath)}-r{panel.Row + 1}-c{panel.Column + 1}";
                    panelImage.Save(Path.Combine(diagnosticsDirectory, $"{diagnosticStem}-panel.png"));
                    extracted.MaskPreview.Save(Path.Combine(diagnosticsDirectory, $"{diagnosticStem}-mask.png"));
                    extracted.Image.Save(Path.Combine(diagnosticsDirectory, $"{diagnosticStem}-extracted.png"));
                }
            }
        }

        var names = ResolveNames(options.NamesPath, sources, work);
        var sprites = SpriteBuilder.Build(
            work.Select(static item => item.Extracted).ToArray(),
            new(options.Size, options.Padding, options.AlphaThreshold, options.ScaleMode, options.Alignment));
        var palette = PaletteQuantizer.Quantize(sprites, options.PaletteColors);
        for (var index = 0; index < sprites.Count; index++)
        {
            sprites[index].Save(Path.Combine(spriteDirectory, $"{names[index]}.png"));
        }

        var sheetColumns = options.SheetColumns ?? sources[0].Grid.Columns;
        var sheet = BuildSheet(sprites, options.Size, sheetColumns);
        sheet.Save(Path.Combine(outputPath, "sheet.png"));
        var previewScale = Math.Max(1, 256 / options.Size);
        var preview = Raster.ResizeNearest(sheet, sheet.Width * previewScale, sheet.Height * previewScale)
            .CompositeOnChecker(Math.Max(8, previewScale * 4));
        preview.Save(Path.Combine(outputPath, "preview.png"));
        PaletteQuantizer.WriteGimpPalette(Path.Combine(outputPath, "palette.gpl"), palette);
        PaletteQuantizer.BuildSwatches(palette).Save(Path.Combine(outputPath, "palette.png"));
        WriteManifest(outputPath, options, sources, work, names, palette.Count, sheetColumns);

        Console.WriteLine($"Built {sprites.Count} pixel-perfect {options.Size}x{options.Size} sprites, a {sheet.Width}x{sheet.Height} sheet, and a {palette.Count}-color shared palette in {outputPath}.");
    }

    private static Raster BuildSheet(IReadOnlyList<Raster> sprites, int size, int columns)
    {
        var rows = Math.Max(1, (int)Math.Ceiling(sprites.Count / (double)columns));
        var sheet = new Raster(columns * size, rows * size);
        for (var index = 0; index < sprites.Count; index++)
        {
            sheet.Blit(sprites[index], index % columns * size, index / columns * size);
        }

        return sheet;
    }

    private static void WriteManifest(
        string outputPath,
        ConverterOptions options,
        IReadOnlyList<SourceResult> sources,
        IReadOnlyList<SpriteWork> work,
        IReadOnlyList<string> names,
        int paletteCount,
        int sheetColumns)
    {
        var sheetRows = (int)Math.Ceiling(work.Count / (double)sheetColumns);
        var manifest = new
        {
            version = 1,
            tileSize = options.Size,
            padding = options.Padding,
            scaleMode = options.ScaleMode.ToString().ToLowerInvariant(),
            alignment = options.Alignment.ToString().ToLowerInvariant(),
            alpha = "binary",
            paletteColors = paletteCount,
            dithering = false,
            sheet = new { file = "sheet.png", columns = sheetColumns, rows = sheetRows },
            sources = sources.Select(source => new
            {
                file = DisplayPath(source.Path),
                source.Width,
                source.Height,
                columns = source.Grid.Columns,
                rows = source.Grid.Rows,
            }),
            sprites = work.Select((item, index) => new
            {
                id = names[index],
                file = $"sprites/{names[index]}.png",
                source = DisplayPath(sources[item.SourceIndex].Path),
                sourceRow = item.Panel.Row,
                sourceColumn = item.Panel.Column,
                panel = item.Panel.Bounds,
                foreground = item.Extracted.ForegroundBounds,
                backgroundThreshold = Math.Round(item.Extracted.BackgroundThreshold, 3),
                atlasX = index % sheetColumns,
                atlasY = index / sheetColumns,
                width = options.Size,
                height = options.Size,
            }),
        };
        File.WriteAllText(
            Path.Combine(outputPath, "manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }) + Environment.NewLine);
    }

    private static IReadOnlyList<string> ResolveNames(
        string? namesPath,
        IReadOnlyList<SourceResult> sources,
        IReadOnlyList<SpriteWork> work)
    {
        if (namesPath is not null)
        {
            var supplied = File.ReadLines(namesPath)
                .Select(static line => line.Trim())
                .Where(static line => line.Length > 0 && !line.StartsWith('#'))
                .Select(SanitizeName)
                .ToArray();
            if (supplied.Length != work.Count)
            {
                throw new InvalidDataException($"Names file contains {supplied.Length} names; {work.Count} sprites were detected.");
            }

            EnsureUnique(supplied);
            return supplied;
        }

        var generated = work.Select(item => SanitizeName(
            $"{Path.GetFileNameWithoutExtension(sources[item.SourceIndex].Path)}-r{item.Panel.Row + 1}-c{item.Panel.Column + 1}"))
            .ToArray();
        EnsureUnique(generated);
        return generated;
    }

    private static List<string> ExpandInputs(IReadOnlyList<string> inputs)
    {
        var result = new List<string>();
        foreach (var input in inputs)
        {
            var path = Path.GetFullPath(input);
            if (Directory.Exists(path))
            {
                result.AddRange(Directory.EnumerateFiles(path)
                    .Where(IsSupportedImage)
                    .Order(StringComparer.OrdinalIgnoreCase));
            }
            else if (File.Exists(path) && IsSupportedImage(path))
            {
                result.Add(path);
            }
            else
            {
                throw new FileNotFoundException($"Input image or directory not found or unsupported: {input}");
            }
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsSupportedImage(string path) => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static string SanitizeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var characters = value.Trim().ToLowerInvariant()
            .Select(character => invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : character)
            .ToArray();
        var result = string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return result.Length > 0 ? result : throw new InvalidDataException("A sprite name became empty after filename sanitization.");
    }

    private static void EnsureUnique(IReadOnlyList<string> names)
    {
        var duplicate = names.GroupBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Duplicate sprite name: {duplicate.Key}");
        }
    }

    private static void ValidateCleanTarget(string path)
    {
        var root = Path.GetPathRoot(path);
        if (string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), root?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), Environment.CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to clean unsafe output directory: {path}");
        }
    }

    private static string DisplayPath(string path)
    {
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, path);
        return relative.StartsWith("..", StringComparison.Ordinal) ? Path.GetFileName(path) : relative.Replace('\\', '/');
    }

    private sealed record SourceResult(string Path, int Width, int Height, GridSize Grid);

    private sealed record SpriteWork(int SourceIndex, DetectedPanel Panel, ExtractedSprite Extracted);
}

internal sealed record ConverterOptions(
    IReadOnlyList<string> Inputs,
    string? Output,
    int Size,
    int Padding,
    int PaletteColors,
    int AlphaThreshold,
    GridSize? Grid,
    int? SheetColumns,
    double PanelInset,
    double? BackgroundThreshold,
    bool RemoveShadows,
    ScaleMode ScaleMode,
    SpriteAlignment Alignment,
    string? NamesPath,
    bool Diagnostics,
    bool Clean,
    bool ShowHelp);

internal static class CommandLine
{
    public const string HelpText = """
        Automapolis.PixelPerfect
        Convert AI-generated pixel-style sheets into true, one-data-pixel-per-art-pixel sprites.

        Usage:
          dotnet run --project tools/Automapolis.PixelPerfect -- \
            --input SHEET_OR_DIRECTORY [--input ANOTHER_SHEET] --output DIRECTORY [options]

        Required:
          --input PATH                  PNG/JPEG/BMP/GIF image or a directory; repeatable.
          --output DIRECTORY            Destination for sprites, sheet, preview, palette, and manifest.

        Output:
          --size PIXELS                 Square sprite size. Default: 64.
          --padding PIXELS              Transparent canvas padding. Default: 3.
          --palette-colors COUNT        Shared OKLab palette size; 0 keeps all colors. Default: 32.
          --sheet-columns COUNT         Output sheet columns. Default: first input's detected columns.
          --names FILE                  One sprite filename stem per nonempty line, in reading order.

        Detection and cleanup:
          --grid COLUMNSxROWS           Override automatic card-grid detection.
          --panel-inset FRACTION        Trim around each detected card. Default: 0.08.
          --background-threshold VALUE  RGB background distance; default is robust automatic fitting.
          --alpha-threshold 0..255      Area-resampled alpha cutoff. Default: 96.
          --keep-shadows                Keep smooth, neutral cast shadows.
          --diagnostics                 Save detected panels, masks, and extracted source sprites.

        Composition:
          --scale-mode shared|individual  Preserve relative scale or fill every canvas. Default: individual.
          --align center|bottom         Vertical placement. Default: center.
          --clean                       Delete the exact output directory before building it.
          --help                        Show this help.
        """;

    public static ConverterOptions Parse(string[] args)
    {
        var inputs = new List<string>();
        string? output = null;
        var size = 64;
        var padding = 3;
        var paletteColors = 32;
        var alphaThreshold = 96;
        GridSize? grid = null;
        int? sheetColumns = null;
        var panelInset = 0.08;
        double? backgroundThreshold = null;
        var removeShadows = true;
        var scaleMode = ScaleMode.Individual;
        var alignment = SpriteAlignment.Center;
        string? names = null;
        var diagnostics = false;
        var clean = false;
        var showHelp = args.Length == 0;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--input": inputs.Add(NextValue(args, ref index, argument)); break;
                case "--output": output = NextValue(args, ref index, argument); break;
                case "--size": size = ParseInt(NextValue(args, ref index, argument), argument, 8, 1024); break;
                case "--padding": padding = ParseInt(NextValue(args, ref index, argument), argument, 0, 256); break;
                case "--palette-colors": paletteColors = ParseInt(NextValue(args, ref index, argument), argument, 0, 256); break;
                case "--alpha-threshold": alphaThreshold = ParseInt(NextValue(args, ref index, argument), argument, 0, 255); break;
                case "--sheet-columns": sheetColumns = ParseInt(NextValue(args, ref index, argument), argument, 1, 128); break;
                case "--panel-inset": panelInset = ParseDouble(NextValue(args, ref index, argument), argument, 0, 0.2); break;
                case "--background-threshold": backgroundThreshold = ParseDouble(NextValue(args, ref index, argument), argument, 1, 255); break;
                case "--grid": grid = ParseGrid(NextValue(args, ref index, argument)); break;
                case "--scale-mode": scaleMode = ParseEnum<ScaleMode>(NextValue(args, ref index, argument), argument); break;
                case "--align": alignment = ParseEnum<SpriteAlignment>(NextValue(args, ref index, argument), argument); break;
                case "--names": names = NextValue(args, ref index, argument); break;
                case "--keep-shadows": removeShadows = false; break;
                case "--diagnostics": diagnostics = true; break;
                case "--clean": clean = true; break;
                case "--help" or "-h" or "/?": showHelp = true; break;
                default: throw new ArgumentException($"Unknown option: {argument}");
            }
        }

        if (!showHelp && output is null)
        {
            throw new ArgumentException("--output is required.");
        }

        return new(inputs, output, size, padding, paletteColors, alphaThreshold, grid, sheetColumns, panelInset, backgroundThreshold, removeShadows, scaleMode, alignment, names, diagnostics, clean, showHelp);
    }

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (++index >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        return args[index];
    }

    private static int ParseInt(string value, string option, int minimum, int maximum) =>
        int.TryParse(value, out var parsed) && parsed >= minimum && parsed <= maximum
            ? parsed
            : throw new ArgumentException($"{option} must be an integer from {minimum} through {maximum}.");

    private static double ParseDouble(string value, string option, double minimum, double maximum) =>
        double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var parsed) && parsed >= minimum && parsed <= maximum
            ? parsed
            : throw new ArgumentException($"{option} must be a number from {minimum} through {maximum}.");

    private static GridSize ParseGrid(string value)
    {
        var parts = value.Split('x', 'X');
        return parts.Length == 2 && int.TryParse(parts[0], out var columns) && int.TryParse(parts[1], out var rows) && columns > 0 && rows > 0
            ? new(columns, rows)
            : throw new ArgumentException("--grid must use COLUMNSxROWS, for example 4x3.");
    }

    private static T ParseEnum<T>(string value, string option) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid value for {option}: {value}.");
}
