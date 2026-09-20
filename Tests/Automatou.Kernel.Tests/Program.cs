using Automatou.Kernel;
using System.Text.Json;

(string Name, Action Run)[] tests =
[
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
];

int failures = 0;
foreach ((string? Name, Action? Run) in tests) {
    try {
        Run();
        Console.WriteLine($"PASS  {Name}");
    } catch (Exception exception) {
        failures++;
        Console.Error.WriteLine($"FAIL  {Name}: {exception.Message}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
return failures;

static void Determinism() {
    WorldKernel first = new(new(10, 8, 1234, "Mirror Front"));
    WorldKernel second = new(new(10, 8, 1234, "Mirror Front"));
    WorldCommand[] commands =
    [
        new AdvanceTurn(),
        new ChannelResonance(new(2, 3), 18),
        new AdvanceTurn(),
        new DeployForce(new(4, 4), ForceKind.Soldier),
        new AdvanceTurn()
    ];

    foreach (WorldCommand command in commands) {
        _ = first.Execute(command);
        _ = second.Execute(command);
    }

    Equal(JsonSerializer.Serialize(first.Snapshot()), JsonSerializer.Serialize(second.Snapshot()));
}

static void PassivePlayAllowsInterventions() {
    WorldKernel kernel = new(new(8, 8, 7, "Passive Front"));
    for (int turn = 0; turn < 5; turn++) {
        _ = kernel.Execute(new AdvanceTurn());
    }

    Equal(5, kernel.Turn);
    CommandResult result = kernel.Execute(new EstablishEnclave(new(2, 2), "Player Intervention"));
    True(result.Accepted);
    Equal(5, result.Snapshot.Turn);
    True(result.Snapshot.Forces.Any(force => force.Name == "Player Intervention"));
    Equal(6, kernel.Execute(new AdvanceTurn()).Snapshot.Turn);
}

static void ExplicitAdvanceOnly() {
    WorldKernel kernel = new(new());
    WorldSnapshot first = kernel.Snapshot();
    WorldSnapshot second = kernel.Snapshot();
    Equal(0, first.Turn);
    Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    Equal(1, kernel.Execute(new AdvanceTurn()).Snapshot.Turn);
}

static void CommandInterventionsWork() {
    WorldKernel kernel = new(new(8, 8, 44, "Command Test"));
    GridPoint point = new(1, 1);
    TileSnapshot before = kernel.Snapshot().Tiles.Single(tile => tile.Position == point);
    CommandResult channeled = kernel.Execute(new ChannelResonance(point, 20));
    True(channeled.Accepted);
    True(channeled.Snapshot.Tiles.Single(tile => tile.Position == point).Resonance >= before.Resonance);

    CommandResult established = kernel.Execute(new EstablishEnclave(point, "Vigil Annex"));
    True(established.Snapshot.Forces.Any(force => force.Name == "Vigil Annex"));

    GridPoint alienPosition = established.Snapshot.Forces.First(force => force.Kind is ForceKind.Ravener).Position;
    CommandResult purged = kernel.Execute(new InvokePurge(alienPosition, 0));
    False(purged.Snapshot.Forces.Any(force => force.Kind is ForceKind.Ravener && force.Position == alienPosition));
}

static void EveryFrontHasOneBastion() {
    for (int seed = 0; seed < 20; seed++) {
        WorldSnapshot snapshot = new WorldKernel(new(8, 8, seed, "Front")).Snapshot();
        Equal(1, snapshot.Forces.Count(force => force.Kind is ForceKind.Bastion));
        Equal(1, snapshot.Metrics.Bastions);
    }
}

static void BastionIsRare() {
    WorldKernel kernel = new(new(8, 8, 17, "Rare Test"));
    Throws<InvalidOperationException>(() => kernel.Execute(new DeployForce(new(0, 0), ForceKind.Bastion)));

    GridPoint bastionPosition = kernel.Snapshot().Forces.Single(force => force.Kind is ForceKind.Bastion).Position;
    for (int strike = 0; strike < 4; strike++) {
        _ = kernel.Execute(new InvokePurge(bastionPosition, 0));
    }

    False(kernel.Snapshot().Forces.Any(force => force.Kind is ForceKind.Bastion));
    True(kernel.Execute(new DeployForce(new(0, 0), ForceKind.Bastion)).Accepted);
    Equal(1, kernel.Snapshot().Metrics.Bastions);
}

static void BastionFightsAliens() {
    WorldKernel kernel = new(new(6, 6, 91, "War Test"));
    WorldSnapshot initial = kernel.Snapshot();
    ForceSnapshot bastion = initial.Forces.Single(force => force.Kind is ForceKind.Bastion);
    int initialDistance = initial.Forces
        .Where(force => force.Kind is ForceKind.Ravener or ForceKind.BroodNode)
        .Min(force => HexGrid.Distance(force.Position, bastion.Position));

    for (int turn = 0; turn < Math.Max(1, initialDistance); turn++) {
        _ = kernel.Execute(new AdvanceTurn());
    }

    WorldSnapshot after = kernel.Snapshot();
    True(after.Chronicle.Any(line => line.Contains("ENGAGEMENT", StringComparison.Ordinal)) ||
         after.Metrics.AlienForces < initial.Metrics.AlienForces);
}

static void TextRendererWorks() {
    WorldSnapshot snapshot = new WorldKernel(new(6, 6, 1, "Glyph Test")).Snapshot();
    string text = TextWorldRenderer.Render(snapshot);
    True(text.Contains("Glyph Test", StringComparison.Ordinal));
    True(text.Contains("HUMAN", StringComparison.Ordinal));
    string[] rows = text.Split('\n').Where(line => line.Length > 2 && char.IsDigit(line[0])).ToArray();
    Equal(6, rows.Length);
    for (int row = 0; row < 6; row++) {
        True(rows[row].StartsWith($"{row:00} " + (row % 2 == 1 ? "  [" : "["), StringComparison.Ordinal));
        Equal(6, rows[row].Count(character => character == '['));
    }
    Equal("hexagonal", snapshot.Topology);
    Equal("oddRowOffset", snapshot.Coordinates);
}

static void HexNeighbors() {
    GridPoint[] evenExpected = [new(4, 2), new(3, 3), new(2, 3), new(2, 2), new(2, 1), new(3, 1)];
    GridPoint[] oddExpected = [new(4, 3), new(4, 4), new(3, 4), new(2, 3), new(3, 2), new(4, 2)];
    True(evenExpected.SequenceEqual(HexGrid.Neighbors(new(3, 2), 8, 8)));
    True(oddExpected.SequenceEqual(HexGrid.Neighbors(new(3, 3), 8, 8)));
    for (int y = -2; y < 8; y++) {
        for (int x = -2; x < 8; x++) {
            for (int direction = 0; direction < 6; direction++) {
                GridPoint point = new(x, y);
                GridPoint neighbor = HexGrid.Neighbor(point, direction);
                Equal(point, HexGrid.Neighbor(neighbor, (direction + 3) % 6));
                Equal(1, HexGrid.Distance(point, neighbor));
            }
        }
    }

    Equal(2, HexGrid.Neighbors(new(0, 0), 8, 8).Count());
    Throws<ArgumentOutOfRangeException>(() => HexGrid.Neighbor(new(0, 0), 6));
}

static void HexDistances() {
    // BFS provides an independent shortest-path check for the distance formula.
    for (int row = 0; row < 6; row++) {
        for (int column = 0; column < 6; column++) {
            GridPoint origin = new(column, row);
            Dictionary<GridPoint, int> distances = new() { [origin] = 0 };
            Queue<GridPoint> queue = new();
            queue.Enqueue(origin);
            while (queue.TryDequeue(out GridPoint current)) {
                foreach (GridPoint neighbor in HexGrid.Neighbors(current, 6, 6)) {
                    if (distances.TryAdd(neighbor, distances[current] + 1)) {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            Equal(36, distances.Count);
            foreach ((GridPoint target, int distance) in distances) {
                Equal(distance, HexGrid.Distance(origin, target));
                Equal(distance, HexGrid.Distance(target, origin));
            }
        }
    }
}

static void HexPurges() {
    foreach (GridPoint center in new GridPoint[] { new(5, 4), new(5, 5), new(0, 0), new(11, 11) }) {
        for (int radius = 0; radius <= 3; radius++) {
            WorldKernel kernel = new(new(12, 12, 5));
            WorldSnapshot before = kernel.Snapshot();
            WorldSnapshot after = kernel.Execute(new InvokePurge(center, radius)).Snapshot;
            int affected = 0;
            foreach (TileSnapshot tile in after.Tiles) {
                TileSnapshot old = before.Tiles.Single(candidate => candidate.Position == tile.Position);
                if (HexGrid.Distance(center, tile.Position) <= radius) {
                    affected++;
                    Equal(TerrainKind.AshWaste, tile.Terrain);
                    Equal(Math.Max(0, old.Biomass - 70), tile.Biomass);
                    Equal(Math.Max(0, old.Resonance - 18), tile.Resonance);
                } else {
                    Equal(old, tile);
                }
            }
            if (center.X == 5) {
                Equal(1 + (3 * radius * (radius + 1)), affected);
            } else {
                True(affected <= 1 + (3 * radius * (radius + 1)));
            }
        }
    }
}

static void HexMovement() {
    bool sawOffsetDiagonal = false;
    for (int seed = 0; seed < 8; seed++) {
        WorldKernel kernel = new(new(8, 8, seed));
        for (int turn = 0; turn < 12; turn++) {
            Dictionary<int, ForceSnapshot> before = kernel.Snapshot().Forces.ToDictionary(force => force.Id);
            WorldSnapshot after = kernel.Execute(new AdvanceTurn()).Snapshot;
            foreach (ForceSnapshot force in after.Forces) {
                True(HexGrid.Contains(force.Position, 8, 8));
                if (!before.TryGetValue(force.Id, out ForceSnapshot? old)) {
                    continue;
                }

                True(HexGrid.Distance(old.Position, force.Position) <= 1);
                if (force.Kind is ForceKind.Enclave or ForceKind.BroodNode) {
                    Equal(old.Position, force.Position);
                }

                if (old.Position.X != force.Position.X && old.Position.Y != force.Position.Y) {
                    sawOffsetDiagonal = true;
                }
            }
        }
    }
    True(sawOffsetDiagonal);
}

static void True(bool condition) {
    if (!condition) {
        throw new InvalidOperationException("Expected true.");
    }
}

static void False(bool condition) {
    True(!condition);
}

static void Throws<T>(Action action) where T : Exception {
    try {
        action();
    } catch (T) {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Equal<T>(T expected, T actual) {
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
        throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
    }
}
