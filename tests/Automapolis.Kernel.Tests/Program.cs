using System.Text.Json;
using Automapolis.Kernel;

var tests = new (string Name, Action Run)[]
{
    ("same seed and commands are deterministic", Determinism),
    ("observer mode rejects creator powers", ObserverRejectsCreatorPowers),
    ("world advances only on explicit input", ExplicitAdvanceOnly),
    ("creator commands change world state", CreatorCommandsWork),
    ("text renderer emits the whole square grid", TextRendererWorks)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL  {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
return failures;

static void Determinism()
{
    var first = new WorldKernel(new(10, 8, 1234, PlayerMode.Creator, "Mirror"));
    var second = new WorldKernel(new(10, 8, 1234, PlayerMode.Creator, "Mirror"));
    WorldCommand[] commands =
    [
        new AdvanceTurn(),
        new InfuseAether(new(2, 3), 18),
        new AdvanceTurn(),
        new CreateLife(new(4, 4), BeingKind.Oracle),
        new AdvanceTurn()
    ];

    foreach (var command in commands)
    {
        first.Execute(command);
        second.Execute(command);
    }

    Equal(JsonSerializer.Serialize(first.Snapshot()), JsonSerializer.Serialize(second.Snapshot()));
}

static void ObserverRejectsCreatorPowers()
{
    var kernel = new WorldKernel(new(8, 8, 7, PlayerMode.Observer, "Quiet"));
    var before = kernel.Snapshot();
    var result = kernel.Execute(new InvokeCataclysm(new(2, 2)));
    False(result.Accepted);
    Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(result.Snapshot));
}

static void ExplicitAdvanceOnly()
{
    var kernel = new WorldKernel(new());
    var first = kernel.Snapshot();
    var second = kernel.Snapshot();
    Equal(0, first.Turn);
    Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    Equal(1, kernel.Execute(new AdvanceTurn()).Snapshot.Turn);
}

static void CreatorCommandsWork()
{
    var kernel = new WorldKernel(new(8, 8, 44, PlayerMode.Creator, "Workshop"));
    var point = new GridPoint(1, 1);
    var before = kernel.Snapshot().Tiles.Single(tile => tile.Position == point);
    var infused = kernel.Execute(new InfuseAether(point, 20));
    True(infused.Accepted);
    True(infused.Snapshot.Tiles.Single(tile => tile.Position == point).Aether >= before.Aether);

    var founded = kernel.Execute(new FoundSettlement(point, "Second Lantern"));
    True(founded.Snapshot.Beings.Any(being => being.Name == "Second Lantern"));

    var ruined = kernel.Execute(new InvokeCataclysm(point, 0));
    True(ruined.Snapshot.Beings.Any(being => being.Kind is BeingKind.Rift && being.Position == point));
}

static void TextRendererWorks()
{
    var snapshot = new WorldKernel(new(6, 6, 1, PlayerMode.Observer, "Glyph Test")).Snapshot();
    var text = TextWorldRenderer.Render(snapshot);
    True(text.Contains("Glyph Test", StringComparison.Ordinal));
    Equal(8, text.Split('\n').Count(line => line.StartsWith('│') || line.StartsWith('┌') || line.StartsWith('└')));
}

static void True(bool condition)
{
    if (!condition) throw new InvalidOperationException("Expected true.");
}

static void False(bool condition) => True(!condition);

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
    }
}
