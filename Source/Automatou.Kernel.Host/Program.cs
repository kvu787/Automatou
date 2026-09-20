using Automatou.Kernel;
using System.Text.Json;
using System.Text.Json.Serialization;

JsonSerializerOptions jsonOptions = new() {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};

WorldKernel kernel = new(new WorldConfig());
WriteResponse(true, "ready", "Bastion Front Kernel ready.", kernel.Snapshot());

string? line;
while ((line = Console.ReadLine()) is not null) {
    try {
        using JsonDocument document = JsonDocument.Parse(line);
        JsonElement root = document.RootElement;
        string commandName = RequiredString(root, "command").ToLowerInvariant();

        if (commandName is "new") {
            WorldConfig config = new(
                OptionalInt(root, "width", 16),
                OptionalInt(root, "height", 12),
                OptionalLong(root, "seed", 475_023),
                OptionalString(root, "name", "The Bastion Front"));
            kernel = new WorldKernel(config);
            WriteResponse(true, "new", $"Opened {config.Name}.", kernel.Snapshot());
            continue;
        }

        WorldCommand command = ParseCommand(commandName, root);
        CommandResult result = kernel.Execute(command);
        WriteResponse(result.Accepted, commandName, result.Message, result.Snapshot);
    } catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException) {
        WriteResponse(false, "error", exception.Message, kernel.Snapshot());
    }
}

return;

WorldCommand ParseCommand(string commandName, JsonElement root) {
    return commandName switch {
        "advance" => new AdvanceTurn(),
        "channel" => new ChannelResonance(Point(root), OptionalInt(root, "amount", 25)),
        "fortify" => new FortifyTerrain(Point(root), RequiredEnum<TerrainKind>(root, "terrain")),
        "deploy" => new DeployForce(Point(root), OptionalEnum(root, "kind", ForceKind.Soldier)),
        "establish" => new EstablishEnclave(Point(root), RequiredString(root, "name")),
        "purge" => new InvokePurge(Point(root), OptionalInt(root, "radius", 1)),
        _ => throw new ArgumentException($"Unknown command '{commandName}'.")
    };
}

GridPoint Point(JsonElement root) {
    return new(RequiredInt(root, "x"), RequiredInt(root, "y"));
}

void WriteResponse(bool ok, string type, string message, WorldSnapshot snapshot) {
    var response = new {
        ok,
        type,
        message,
        snapshot,
        text = TextWorldRenderer.Render(snapshot)
    };
    Console.WriteLine(JsonSerializer.Serialize(response, jsonOptions));
}

static string RequiredString(JsonElement element, string property) {
    return element.TryGetProperty(property, out JsonElement value) && value.ValueKind is JsonValueKind.String
        ? value.GetString()!
        : throw new ArgumentException($"'{property}' must be a string.");
}

static string OptionalString(JsonElement element, string property, string fallback) {
    return element.TryGetProperty(property, out JsonElement value) ? value.GetString() ?? fallback : fallback;
}

static int RequiredInt(JsonElement element, string property) {
    return element.TryGetProperty(property, out JsonElement value) && value.TryGetInt32(out int parsed)
        ? parsed
        : throw new ArgumentException($"'{property}' must be an integer.");
}

static int OptionalInt(JsonElement element, string property, int fallback) {
    return element.TryGetProperty(property, out JsonElement value) && value.TryGetInt32(out int parsed) ? parsed : fallback;
}

static long OptionalLong(JsonElement element, string property, long fallback) {
    return element.TryGetProperty(property, out JsonElement value) && value.TryGetInt64(out long parsed) ? parsed : fallback;
}

static T RequiredEnum<T>(JsonElement element, string property) where T : struct, Enum {
    string text = RequiredString(element, property);
    return Enum.TryParse<T>(text, true, out T parsed)
        ? parsed
        : throw new ArgumentException($"'{text}' is not a valid {typeof(T).Name}.");
}

static T OptionalEnum<T>(JsonElement element, string property, T fallback) where T : struct, Enum {
    return element.TryGetProperty(property, out JsonElement value) &&
    value.ValueKind is JsonValueKind.String &&
    Enum.TryParse<T>(value.GetString(), true, out T parsed)
        ? parsed
        : fallback;
}
