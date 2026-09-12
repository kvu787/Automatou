using System.Text.Json;
using Automatou.Kernel;
using Automatou.TerminalShell;

var tests = new (string Name, Action Run)[]
{
    ("inspection leaves the complete world unchanged", InspectionDoesNotMutate),
    ("every intervention matches the Kernel without advancing time", InterventionsMatchKernel),
    ("batched turns match individual Kernel turns", AdvanceMatchesKernel),
    ("invalid input and rejected actions preserve state and recover", InvalidInputRecovers),
    ("new worlds honor dimensions, long seeds, names, and defaults", NewWorldConfiguration),
    ("sector inspection includes terrain and every stacked force", InspectStackedForces),
    ("force filtering and ID inspection show the requested forces", ForceInspection),
    ("map preserves hex staggering and adds column coordinates", MapCoordinates),
    ("snapshot emits the complete world as JSON", SnapshotOutput),
    ("quoted, escaped, and Unicode names survive input", QuotedNames),
    ("redirected sessions handle blank lines, errors, quit, and EOF", RedirectedSessions),
    ("interactive sessions show the current turn in the prompt", InteractivePrompt)
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

static void InspectionDoesNotMutate()
{
    var session = new TerminalSession(TextWriter.Null);
    var before = Json(session.Snapshot);
    foreach (var line in new[] { "", "   ", "help", "?", "map", "status", "inspect 2 3", "forces", "forces Soldier", "force 1", "legend", "chronicle", "chronicle 1", "snapshot" })
    {
        True(session.ExecuteLine(line));
        Equal(before, Json(session.Snapshot));
    }
}

static void InterventionsMatchKernel()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    var kernel = new WorldKernel(new WorldConfig());
    (string Line, WorldCommand Command)[] commands =
    [
        ("channel 2 3", new ChannelResonance(new(2, 3))),
        ("channel 2 3 18", new ChannelResonance(new(2, 3), 18)),
        ("FoRtIfY 2 3 lEyChAnNeL", new FortifyTerrain(new(2, 3), TerrainKind.LeyChannel)),
        ("deploy 2 3", new DeployForce(new(2, 3))),
        ("deploy 2 3 soldier", new DeployForce(new(2, 3), ForceKind.Soldier)),
        ("establish 2 3 \"Vigil Annex\"", new EstablishEnclave(new(2, 3), "Vigil Annex")),
        ("purge 2 3 0", new InvokePurge(new(2, 3), 0)),
        ("purge 2 3", new InvokePurge(new(2, 3))),
        ("channel 2 3 -10", new ChannelResonance(new(2, 3), -10)),
        ("purge 2 3 99", new InvokePurge(new(2, 3), 99))
    ];
    foreach (var (line, command) in commands)
    {
        True(session.ExecuteLine(line));
        kernel.Execute(command);
        Equal(Json(kernel.Snapshot()), Json(session.Snapshot));
        Equal(0, session.Snapshot.Turn);
    }
    False(output.ToString().Contains("Error:", StringComparison.Ordinal));
}

static void AdvanceMatchesKernel()
{
    var session = new TerminalSession(TextWriter.Null);
    var kernel = new WorldKernel(new WorldConfig());
    session.ExecuteLine("advance 5");
    session.ExecuteLine("step");
    session.ExecuteLine("advance");
    for (var turn = 0; turn < 7; turn++)
        kernel.Execute(new AdvanceTurn());
    Equal(7, session.Snapshot.Turn);
    Equal(Json(kernel.Snapshot()), Json(session.Snapshot));
}

static void InvalidInputRecovers()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    session.ExecuteLine("advance");
    var before = Json(session.Snapshot);
    string[] invalid =
    [
        "unknown", "help extra", "map extra", "status extra", "snapshot extra", "inspect",
        "inspect -1 0", "inspect 0 999", "inspect no 3", "force 999999", "forces 0",
        "channel 2", "channel 2 3 oops", "channel 999 3", "channel 2 3 4 extra",
        "fortify 2 3 999", "fortify 2 3 1", "fortify 2 3 water",
        "deploy 2 3 Ravener", "deploy 2 3 Bastion", "deploy 2 3 unknown",
        "establish 2 3 \"\"", "establish 2 3 \"unterminated", "establish 2 3 '" + new string('x', 33) + "'",
        "purge -1 0", "purge 0 0 no", "advance 0", "advance -1", "advance 1001",
        "advance 2147483648", "advance 1 extra", "chronicle 0", "new 8", "new 5 8",
        "new 8 51", "new 8 8 nope", "new 8 8 9223372036854775808", "new 8 8 1 ''",
        "new 8 8 1 '" + new string('x', 49) + "'", "quit extra", "exit extra"
    ];
    foreach (var line in invalid)
    {
        output.GetStringBuilder().Clear();
        True(session.ExecuteLine(line));
        Contains(output.ToString(), "Error:");
        Equal(before, Json(session.Snapshot));
    }
    output.GetStringBuilder().Clear();
    session.ExecuteLine("advance");
    Equal(2, session.Snapshot.Turn);
    False(output.ToString().Contains("Error:", StringComparison.Ordinal));
}

static void NewWorldConfiguration()
{
    var session = new TerminalSession(TextWriter.Null);
    session.ExecuteLine("new 8 6 -9223372036854775808 The Quiet Front");
    Equal(Json(new WorldKernel(new(8, 6, long.MinValue, "The Quiet Front")).Snapshot()), Json(session.Snapshot));
    session.ExecuteLine("advance 2");
    session.ExecuteLine("new 6 7");
    Equal(Json(new WorldKernel(new(6, 7)).Snapshot()), Json(session.Snapshot));
    session.ExecuteLine("new");
    Equal(Json(new WorldKernel(new()).Snapshot()), Json(session.Snapshot));
}

static void InspectStackedForces()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    var enclave = session.Snapshot.Forces.First(force => force.Kind == ForceKind.Enclave);
    var point = enclave.Position;
    session.ExecuteLine($"deploy {point.X} {point.Y}");
    output.GetStringBuilder().Clear();
    session.ExecuteLine($"inspect {point.X} {point.Y}");
    Contains(output.ToString(), session.Snapshot.Tiles.First(tile => tile.Position == point).Description);
    foreach (var force in session.Snapshot.Forces.Where(force => force.Position == point))
    {
        Contains(output.ToString(), $"#{force.Id} [{force.Glyph}] {force.Name}");
        Contains(output.ToString(), $"Intent: {force.Intent}");
    }
}

static void ForceInspection()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    session.ExecuteLine("forces soldier");
    foreach (var force in session.Snapshot.Forces)
        Equal(force.Kind == ForceKind.Soldier, output.ToString().Contains($"#{force.Id} [", StringComparison.Ordinal));
    output.GetStringBuilder().Clear();
    session.ExecuteLine("force 1");
    Contains(output.ToString(), "#1 [");
    False(output.ToString().Contains("#2 [", StringComparison.Ordinal));
}

static void MapCoordinates()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    session.ExecuteLine("map");
    var lines = output.ToString().Replace("\r\n", "\n").Split('\n');
    True(lines[2].StartsWith("    00  01  02", StringComparison.Ordinal));
    for (var row = 0; row < session.Snapshot.Height; row++)
    {
        True(lines[row + 3].StartsWith($"{row:00} " + (row % 2 == 1 ? "  [" : "["), StringComparison.Ordinal));
        Equal(session.Snapshot.Width, lines[row + 3].Count(character => character == '['));
    }
}

static void SnapshotOutput()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    session.ExecuteLine("snapshot");
    using var document = JsonDocument.Parse(output.ToString());
    Equal("hexagonal", document.RootElement.GetProperty("topology").GetString()!);
    Equal(session.Snapshot.Width * session.Snapshot.Height, document.RootElement.GetProperty("tiles").GetArrayLength());
    Equal(session.Snapshot.Forces.Count, document.RootElement.GetProperty("forces").GetArrayLength());
    Equal(session.Snapshot.Metrics.TotalBiomass, document.RootElement.GetProperty("metrics").GetProperty("totalBiomass").GetInt32());
}

static void QuotedNames()
{
    var session = new TerminalSession(TextWriter.Null);
    session.ExecuteLine("""establish 2 3 "Vigil \"North\" Annex" """);
    True(session.Snapshot.Forces.Any(force => force.Name == "Vigil \"North\" Annex"));
    session.ExecuteLine("""establish 2 3 'Lumière 東' """);
    True(session.Snapshot.Forces.Any(force => force.Name == "Lumière 東"));
    session.ExecuteLine("""establish 2 3 "Vigil \\ Annex" """);
    True(session.Snapshot.Forces.Any(force => force.Name == @"Vigil \ Annex"));
}

static void RedirectedSessions()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    session.Run(new StringReader("\ninvalid\nadvance 2\nquit\nadvance\n"), interactive: false);
    Equal(2, session.Snapshot.Turn);
    Contains(output.ToString(), "Error:");
    Contains(output.ToString(), "Session ended.");
    False(output.ToString().Contains("Turn 0>", StringComparison.Ordinal));

    session.Run(new StringReader("advance\n"), interactive: false);
    Equal(3, session.Snapshot.Turn);
}

static void InteractivePrompt()
{
    var output = new StringWriter();
    var session = new TerminalSession(output);
    session.Run(new StringReader("advance\nexit\n"), interactive: true);
    Contains(output.ToString(), "Turn 0>");
    Contains(output.ToString(), "Turn 1>");
}

static string Json(WorldSnapshot snapshot) => JsonSerializer.Serialize(snapshot);
static void Contains(string actual, string expected) => True(actual.Contains(expected, StringComparison.Ordinal));
static void True(bool value) { if (!value) throw new InvalidOperationException("Expected true."); }
static void False(bool value) => True(!value);
static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}
