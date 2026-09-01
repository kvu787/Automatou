using System.Text.Json;
using System.Text.Json.Serialization;
using Automapolis.Kernel;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};

var kernel = new WorldKernel(new WorldConfig());
WriteResponse(true, "ready", "Bastion Front Kernel ready.", kernel.Snapshot());

string? line;
while ((line = Console.ReadLine()) is not null)
{
    try
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        var commandName = RequiredString(root, "command").ToLowerInvariant();

        if (commandName is "new")
        {
            var modeText = OptionalString(root, "mode", "command");
            var mode = modeText.Equals("witness", StringComparison.OrdinalIgnoreCase)
                ? PlayerMode.Witness
                : PlayerMode.Command;
            var config = new WorldConfig(
                OptionalInt(root, "width", 16),
                OptionalInt(root, "height", 12),
                OptionalLong(root, "seed", 475_023),
                mode,
                OptionalString(root, "name", "The Bastion Front"));
            kernel = new WorldKernel(config);
            WriteResponse(true, "new", $"Opened {config.Name}.", kernel.Snapshot());
            continue;
        }

        var command = ParseCommand(commandName, root);
        var result = kernel.Execute(command);
        WriteResponse(result.Accepted, commandName, result.Message, result.Snapshot);
    }
    catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException)
    {
        WriteResponse(false, "error", exception.Message, kernel.Snapshot());
    }
}

return;

WorldCommand ParseCommand(string commandName, JsonElement root) => commandName switch
{
    "advance" => new AdvanceTurn(),
    "channel" => new ChannelResonance(Point(root), OptionalInt(root, "amount", 25)),
    "fortify" => new FortifyTerrain(Point(root), RequiredEnum<TerrainKind>(root, "terrain")),
    "deploy" => new DeployForce(Point(root), OptionalEnum(root, "kind", ForceKind.Soldier)),
    "establish" => new EstablishEnclave(Point(root), RequiredString(root, "name")),
    "purge" => new InvokePurge(Point(root), OptionalInt(root, "radius", 1)),
    _ => throw new ArgumentException($"Unknown command '{commandName}'.")
};

GridPoint Point(JsonElement root) => new(RequiredInt(root, "x"), RequiredInt(root, "y"));

void WriteResponse(bool ok, string type, string message, WorldSnapshot snapshot)
{
    var response = new
    {
        ok,
        type,
        message,
        snapshot,
        text = TextWorldRenderer.Render(snapshot)
    };
    Console.WriteLine(JsonSerializer.Serialize(response, jsonOptions));
}

static string RequiredString(JsonElement element, string property) =>
    element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.String
        ? value.GetString()!
        : throw new ArgumentException($"'{property}' must be a string.");

static string OptionalString(JsonElement element, string property, string fallback) =>
    element.TryGetProperty(property, out var value) ? value.GetString() ?? fallback : fallback;

static int RequiredInt(JsonElement element, string property) =>
    element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed)
        ? parsed
        : throw new ArgumentException($"'{property}' must be an integer.");

static int OptionalInt(JsonElement element, string property, int fallback) =>
    element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;

static long OptionalLong(JsonElement element, string property, long fallback) =>
    element.TryGetProperty(property, out var value) && value.TryGetInt64(out var parsed) ? parsed : fallback;

static T RequiredEnum<T>(JsonElement element, string property) where T : struct, Enum
{
    var text = RequiredString(element, property);
    return Enum.TryParse<T>(text, true, out var parsed)
        ? parsed
        : throw new ArgumentException($"'{text}' is not a valid {typeof(T).Name}.");
}

static T OptionalEnum<T>(JsonElement element, string property, T fallback) where T : struct, Enum =>
    element.TryGetProperty(property, out var value) &&
    value.ValueKind is JsonValueKind.String &&
    Enum.TryParse<T>(value.GetString(), true, out var parsed)
        ? parsed
        : fallback;
