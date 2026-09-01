using System.Text.Json;
using Automapolis.Kernel;

var tests = new (string Name, Action Run)[]
{
    ("same seed and commands are deterministic", Determinism),
    ("witness mode rejects field command", WitnessRejectsFieldCommand),
    ("front advances only on explicit input", ExplicitAdvanceOnly),
    ("command interventions change the front", CommandInterventionsWork),
    ("every theater begins with exactly one Bastion", EveryFrontHasOneBastion),
    ("a living Bastion prevents a second commitment", BastionIsRare),
    ("Bastion hunts and fights alien forces", BastionFightsAliens),
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
    var first = new WorldKernel(new(10, 8, 1234, PlayerMode.Command, "Mirror Front"));
    var second = new WorldKernel(new(10, 8, 1234, PlayerMode.Command, "Mirror Front"));
    WorldCommand[] commands =
    [
        new AdvanceTurn(),
        new ChannelResonance(new(2, 3), 18),
        new AdvanceTurn(),
        new DeployForce(new(4, 4), ForceKind.Legionary),
        new AdvanceTurn()
    ];

    foreach (var command in commands)
    {
        first.Execute(command);
        second.Execute(command);
    }

    Equal(JsonSerializer.Serialize(first.Snapshot()), JsonSerializer.Serialize(second.Snapshot()));
}

static void WitnessRejectsFieldCommand()
{
    var kernel = new WorldKernel(new(8, 8, 7, PlayerMode.Witness, "Sealed Front"));
    var before = kernel.Snapshot();
    var result = kernel.Execute(new InvokePurge(new(2, 2)));
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

static void CommandInterventionsWork()
{
    var kernel = new WorldKernel(new(8, 8, 44, PlayerMode.Command, "Command Test"));
    var point = new GridPoint(1, 1);
    var before = kernel.Snapshot().Tiles.Single(tile => tile.Position == point);
    var channeled = kernel.Execute(new ChannelResonance(point, 20));
    True(channeled.Accepted);
    True(channeled.Snapshot.Tiles.Single(tile => tile.Position == point).Resonance >= before.Resonance);

    var established = kernel.Execute(new EstablishEnclave(point, "Vigil Annex"));
    True(established.Snapshot.Forces.Any(force => force.Name == "Vigil Annex"));

    var alienPosition = established.Snapshot.Forces.First(force => force.Kind is ForceKind.Ravener).Position;
    var purged = kernel.Execute(new InvokePurge(alienPosition, 0));
    False(purged.Snapshot.Forces.Any(force => force.Kind is ForceKind.Ravener && force.Position == alienPosition));
}

static void EveryFrontHasOneBastion()
{
    for (var seed = 0; seed < 20; seed++)
    {
        var snapshot = new WorldKernel(new(8, 8, seed, PlayerMode.Witness, "Front")).Snapshot();
        Equal(1, snapshot.Forces.Count(force => force.Kind is ForceKind.Bastion));
        Equal(1, snapshot.Metrics.Bastions);
    }
}

static void BastionIsRare()
{
    var kernel = new WorldKernel(new(8, 8, 17, PlayerMode.Command, "Rare Test"));
    Throws<InvalidOperationException>(() => kernel.Execute(new DeployForce(new(0, 0), ForceKind.Bastion)));

    var bastionPosition = kernel.Snapshot().Forces.Single(force => force.Kind is ForceKind.Bastion).Position;
    for (var strike = 0; strike < 4; strike++)
    {
        kernel.Execute(new InvokePurge(bastionPosition, 0));
    }

    False(kernel.Snapshot().Forces.Any(force => force.Kind is ForceKind.Bastion));
    True(kernel.Execute(new DeployForce(new(0, 0), ForceKind.Bastion)).Accepted);
    Equal(1, kernel.Snapshot().Metrics.Bastions);
}

static void BastionFightsAliens()
{
    var kernel = new WorldKernel(new(6, 6, 91, PlayerMode.Witness, "War Test"));
    var initial = kernel.Snapshot();
    var bastion = initial.Forces.Single(force => force.Kind is ForceKind.Bastion);
    var initialDistance = initial.Forces
        .Where(force => force.Kind is ForceKind.Ravener or ForceKind.BroodNode)
        .Min(force => Manhattan(force.Position, bastion.Position));

    for (var turn = 0; turn < Math.Max(1, initialDistance); turn++)
    {
        kernel.Execute(new AdvanceTurn());
    }

    var after = kernel.Snapshot();
    True(after.Chronicle.Any(line => line.Contains("ENGAGEMENT", StringComparison.Ordinal)) ||
         after.Metrics.AlienForces < initial.Metrics.AlienForces);
}

static void TextRendererWorks()
{
    var snapshot = new WorldKernel(new(6, 6, 1, PlayerMode.Witness, "Glyph Test")).Snapshot();
    var text = TextWorldRenderer.Render(snapshot);
    True(text.Contains("Glyph Test", StringComparison.Ordinal));
    True(text.Contains("HUMAN", StringComparison.Ordinal));
    Equal(8, text.Split('\n').Count(line => line.StartsWith('│') || line.StartsWith('┌') || line.StartsWith('└')));
}

static int Manhattan(GridPoint left, GridPoint right) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

static void True(bool condition)
{
    if (!condition) throw new InvalidOperationException("Expected true.");
}

static void False(bool condition) => True(!condition);

static void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
    }
}
