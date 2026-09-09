using System.Text.Json;
using Automapolis.Kernel;

var tests = new (string Name, Action Run)[]
{
    ("same seed and commands are deterministic", Determinism),
    ("passive play can accept interventions at any time", PassivePlayAllowsInterventions),
    ("front advances only on explicit input", ExplicitAdvanceOnly),
    ("command interventions change the front", CommandInterventionsWork),
    ("every theater begins with exactly one Bastion", EveryFrontHasOneBastion),
    ("a living Bastion prevents a second commitment", BastionIsRare),
    ("Bastion hunts and fights alien forces", BastionFightsAliens),
    ("text renderer emits staggered hex rows", TextRendererWorks),
    ("hex neighbors are reciprocal on both row parities", HexNeighbors),
    ("hex distance agrees with shortest paths", HexDistances),
    ("purges cover hex rings and clip at boundaries", HexPurges),
    ("autonomous movement stays within one hex step", HexMovement)
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
    var first = new WorldKernel(new(10, 8, 1234, "Mirror Front"));
    var second = new WorldKernel(new(10, 8, 1234, "Mirror Front"));
    WorldCommand[] commands =
    [
        new AdvanceTurn(),
        new ChannelResonance(new(2, 3), 18),
        new AdvanceTurn(),
        new DeployForce(new(4, 4), ForceKind.Soldier),
        new AdvanceTurn()
    ];

    foreach (var command in commands)
    {
        first.Execute(command);
        second.Execute(command);
    }

    Equal(JsonSerializer.Serialize(first.Snapshot()), JsonSerializer.Serialize(second.Snapshot()));
}

static void PassivePlayAllowsInterventions()
{
    var kernel = new WorldKernel(new(8, 8, 7, "Passive Front"));
    for (var turn = 0; turn < 5; turn++) kernel.Execute(new AdvanceTurn());
    Equal(5, kernel.Turn);
    var result = kernel.Execute(new EstablishEnclave(new(2, 2), "Player Intervention"));
    True(result.Accepted);
    Equal(5, result.Snapshot.Turn);
    True(result.Snapshot.Forces.Any(force => force.Name == "Player Intervention"));
    Equal(6, kernel.Execute(new AdvanceTurn()).Snapshot.Turn);
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
    var kernel = new WorldKernel(new(8, 8, 44, "Command Test"));
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
        var snapshot = new WorldKernel(new(8, 8, seed, "Front")).Snapshot();
        Equal(1, snapshot.Forces.Count(force => force.Kind is ForceKind.Bastion));
        Equal(1, snapshot.Metrics.Bastions);
    }
}

static void BastionIsRare()
{
    var kernel = new WorldKernel(new(8, 8, 17, "Rare Test"));
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
    var kernel = new WorldKernel(new(6, 6, 91, "War Test"));
    var initial = kernel.Snapshot();
    var bastion = initial.Forces.Single(force => force.Kind is ForceKind.Bastion);
    var initialDistance = initial.Forces
        .Where(force => force.Kind is ForceKind.Ravener or ForceKind.BroodNode)
        .Min(force => HexGrid.Distance(force.Position, bastion.Position));

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
    var snapshot = new WorldKernel(new(6, 6, 1, "Glyph Test")).Snapshot();
    var text = TextWorldRenderer.Render(snapshot);
    True(text.Contains("Glyph Test", StringComparison.Ordinal));
    True(text.Contains("HUMAN", StringComparison.Ordinal));
    var rows = text.Split('\n').Where(line => line.Length > 2 && char.IsDigit(line[0])).ToArray();
    Equal(6, rows.Length);
    for (var row = 0; row < 6; row++)
    {
        True(rows[row].StartsWith($"{row:00} " + (row % 2 == 1 ? "  [" : "["), StringComparison.Ordinal));
        Equal(6, rows[row].Count(character => character == '['));
    }
    Equal("hexagonal", snapshot.Topology);
    Equal("oddRowOffset", snapshot.Coordinates);
}

static void HexNeighbors()
{
    GridPoint[] evenExpected = [new(4, 2), new(3, 3), new(2, 3), new(2, 2), new(2, 1), new(3, 1)];
    GridPoint[] oddExpected = [new(4, 3), new(4, 4), new(3, 4), new(2, 3), new(3, 2), new(4, 2)];
    True(evenExpected.SequenceEqual(HexGrid.Neighbors(new(3, 2), 8, 8)));
    True(oddExpected.SequenceEqual(HexGrid.Neighbors(new(3, 3), 8, 8)));
    for (var y = -2; y < 8; y++)
    for (var x = -2; x < 8; x++)
    for (var direction = 0; direction < 6; direction++)
    {
        var point = new GridPoint(x, y);
        var neighbor = HexGrid.Neighbor(point, direction);
        Equal(point, HexGrid.Neighbor(neighbor, (direction + 3) % 6));
        Equal(1, HexGrid.Distance(point, neighbor));
    }
    Equal(2, HexGrid.Neighbors(new(0, 0), 8, 8).Count());
    Throws<ArgumentOutOfRangeException>(() => HexGrid.Neighbor(new(0, 0), 6));
}

static void HexDistances()
{
    // BFS provides an independent shortest-path check for the distance formula.
    for (var row = 0; row < 6; row++)
    for (var column = 0; column < 6; column++)
    {
        var origin = new GridPoint(column, row);
        var distances = new Dictionary<GridPoint, int> { [origin] = 0 };
        var queue = new Queue<GridPoint>();
        queue.Enqueue(origin);
        while (queue.TryDequeue(out var current))
        foreach (var neighbor in HexGrid.Neighbors(current, 6, 6))
            if (distances.TryAdd(neighbor, distances[current] + 1)) queue.Enqueue(neighbor);
        Equal(36, distances.Count);
        foreach (var (target, distance) in distances)
        {
            Equal(distance, HexGrid.Distance(origin, target));
            Equal(distance, HexGrid.Distance(target, origin));
        }
    }
}

static void HexPurges()
{
    foreach (var center in new GridPoint[] { new(5, 4), new(5, 5), new(0, 0), new(11, 11) })
    for (var radius = 0; radius <= 3; radius++)
    {
        var kernel = new WorldKernel(new(12, 12, 5));
        var before = kernel.Snapshot();
        var after = kernel.Execute(new InvokePurge(center, radius)).Snapshot;
        var affected = 0;
        foreach (var tile in after.Tiles)
        {
            var old = before.Tiles.Single(candidate => candidate.Position == tile.Position);
            if (HexGrid.Distance(center, tile.Position) <= radius)
            {
                affected++;
                Equal(TerrainKind.AshWaste, tile.Terrain);
                Equal(Math.Max(0, old.Biomass - 70), tile.Biomass);
                Equal(Math.Max(0, old.Resonance - 18), tile.Resonance);
            }
            else Equal(old, tile);
        }
        if (center.X == 5) Equal(1 + 3 * radius * (radius + 1), affected);
        else True(affected <= 1 + 3 * radius * (radius + 1));
    }
}

static void HexMovement()
{
    var sawOffsetDiagonal = false;
    for (var seed = 0; seed < 8; seed++)
    {
        var kernel = new WorldKernel(new(8, 8, seed));
        for (var turn = 0; turn < 12; turn++)
        {
            var before = kernel.Snapshot().Forces.ToDictionary(force => force.Id);
            var after = kernel.Execute(new AdvanceTurn()).Snapshot;
            foreach (var force in after.Forces)
            {
                True(HexGrid.Contains(force.Position, 8, 8));
                if (!before.TryGetValue(force.Id, out var old)) continue;
                True(HexGrid.Distance(old.Position, force.Position) <= 1);
                if (force.Kind is ForceKind.Enclave or ForceKind.BroodNode) Equal(old.Position, force.Position);
                if (old.Position.X != force.Position.X && old.Position.Y != force.Position.Y) sawOffsetDiagonal = true;
            }
        }
    }
    True(sawOffsetDiagonal);
}

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
