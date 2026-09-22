using Automatou.Kernel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automatou.TerminalShell;

/// <summary>Translates terminal input to Kernel commands and renders snapshots.</summary>
public sealed class TerminalSession(TextWriter output) {
    private WorldKernel _kernel = new(new WorldConfig());
    private static readonly JsonSerializerOptions SnapshotOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public const string Help = """
        AUTOMATOU / TerminalShell

        INSPECT THE FRONT
          map                         Hex map, coordinates, and latest dispatch.
          status                      World configuration and all metrics.
          inspect X Y                 Terrain, resources, and all forces in a sector.
          forces [kind]               List forces, optionally filtered by kind.
          force ID                    Inspect one force by its numeric ID.
          legend                      Explain glyphs present in this world.
          chronicle [count]           Recent dispatches, newest first.
          snapshot                    Complete current state as readable JSON.

        EDIT THE WORLD (does not advance time)
          channel X Y [amount]        Channel resonance (default 25).
          fortify X Y terrain         Change terrain.
          deploy X Y [kind]           Deploy Soldier (default) or Bastion.
          establish X Y name          Establish a named human enclave.
          purge X Y [radius]          Burn a hex radius (default 1); hits both sides.

        ADVANCE THE SIMULATION
          advance [count]             Resolve 1 turn by default, or 1..1000 turns.
          step [count]                Same as advance.

        SESSION
          new                         Replace the world with the default theater.
          new width height [seed [name]]
                                      Replace the world with a generated theater.
          help / ?                    Show this command reference.
          quit / exit                 End this session.

        Terrain: ShatteredPlain, AshWaste, LeyChannel, Xenoforest,
                 FortifiedReach, BroodMire.
        Force kinds: Bastion, Soldier, Ravener, BroodNode, Enclave.
        The Kernel enforces deployment restrictions and intervention limits.
        Coordinates: zero-based column X, row Y; odd hex rows shift right.
        Names can contain spaces, with optional matching single or double quotes.
        Commands and kind names ignore case. Blank lines do nothing.
        Only advance/step moves time. New/quit discards the current world.

        TRY IT
          inspect 2 3
          channel 2 3 25
          fortify 2 3 FortifiedReach
          deploy 2 3 Soldier
          establish 2 3 "Vigil Annex"
          advance
          chronicle
        """;

    public WorldSnapshot Snapshot => this._kernel.Snapshot();

    public void Run(TextReader input, bool interactive) {
        output.WriteLine("AUTOMATOU / TerminalShell");
        output.WriteLine("Hold the human enclaves against the bioswarm. Time waits for your command.");
        output.WriteLine("Type help for commands. Try: inspect 2 3, channel 2 3, then advance.");
        output.WriteLine();
        TerminalRenderer.Map(output, this.Snapshot);

        while (true) {
            if (interactive) {
                output.Write($"\r\nTurn {this._kernel.Turn}> ");
                output.Flush();
            }
            string? line = input.ReadLine();
            if (line is null || !this.ExecuteLine(line)) {
                break;
            }
        }
        output.WriteLine("Session ended.");
    }

    /// <returns>False when the user requests a clean exit.</returns>
    public bool ExecuteLine(string line) {
        try {
            CommandInput command = new(line);
            switch (command.Name) {
            case "":
                break;
            case "help":
            case "?":
                command.RequireCount(0, 0, "help");
                output.WriteLine(Help);
                break;
            case "quit":
            case "exit":
                command.RequireCount(0, 0, "quit");
                return false;
            case "map":
                command.RequireCount(0, 0, "map");
                TerminalRenderer.Map(output, this.Snapshot);
                break;
            case "status":
                command.RequireCount(0, 0, "status");
                TerminalRenderer.Status(output, this.Snapshot);
                break;
            case "inspect":
                command.RequireCount(2, 2, "inspect X Y");
                TerminalRenderer.Inspect(output, this.Snapshot, Point(command));
                break;
            case "forces":
                command.RequireCount(0, 1, "forces [kind]");
                IEnumerable<ForceSnapshot> forces = this.Snapshot.Forces.AsEnumerable();
                if (command.Count == 1) {
                    ForceKind kind = command.EnumName<ForceKind>(0);
                    forces = forces.Where(force => force.Kind == kind);
                }
                TerminalRenderer.Forces(output, forces);
                break;
            case "force":
                command.RequireCount(1, 1, "force ID");
                int identifier = command.Integer(0, "ID");
                ForceSnapshot found = this.Snapshot.Forces.FirstOrDefault(force => force.Id == identifier)
                    ?? throw new ArgumentException($"No force has ID {identifier}. Use forces to list current IDs.");
                TerminalRenderer.Forces(output, [found]);
                break;
            case "legend":
                command.RequireCount(0, 0, "legend");
                TerminalRenderer.Legend(output, this.Snapshot);
                break;
            case "chronicle":
                command.RequireCount(0, 1, "chronicle [count]");
                int count = command.Count == 0 ? this.Snapshot.Chronicle.Count : command.Integer(0, "count");
                if (count < 1) {
                    throw new ArgumentException("Chronicle count must be positive.");
                }

                foreach (string? entry in this.Snapshot.Chronicle.Take(count)) {
                    output.WriteLine(entry);
                }

                break;
            case "snapshot":
                command.RequireCount(0, 0, "snapshot");
                output.WriteLine(JsonSerializer.Serialize(this.Snapshot, SnapshotOptions));
                break;
            case "advance":
            case "step":
                command.RequireCount(0, 1, "advance [count]");
                int turns = command.Count == 0 ? 1 : command.Integer(0, "count");
                if (turns is < 1 or > 1000) {
                    throw new ArgumentException("Advance count must be between 1 and 1000.");
                }

                for (int turn = 0; turn < turns; turn++) {
                    output.WriteLine(this._kernel.Execute(new AdvanceTurn()).Message);
                }

                TerminalRenderer.Map(output, this.Snapshot);
                break;
            case "channel":
                command.RequireCount(2, 3, "channel X Y [amount]");
                this.Intervene(new ChannelResonance(Point(command), command.Count == 3 ? command.Integer(2, "amount") : 25), Point(command));
                break;
            case "fortify":
                command.RequireCount(3, 3, "fortify X Y terrain");
                this.Intervene(new FortifyTerrain(Point(command), command.EnumName<TerrainKind>(2)), Point(command));
                break;
            case "deploy":
                command.RequireCount(2, 3, "deploy X Y [kind]");
                this.Intervene(new DeployForce(Point(command), command.Count == 3 ? command.EnumName<ForceKind>(2) : ForceKind.Soldier), Point(command));
                break;
            case "establish":
                command.RequireCount(3, int.MaxValue, "establish X Y name");
                this.Intervene(new EstablishEnclave(Point(command), command.RemainingText(2)), Point(command));
                break;
            case "purge":
                command.RequireCount(2, 3, "purge X Y [radius]");
                this.Intervene(new InvokePurge(Point(command), command.Count == 3 ? command.Integer(2, "radius") : 1), Point(command));
                break;
            case "new":
                this.NewWorld(command);
                break;
            default:
                throw new ArgumentException($"Unknown command '{command.Name}'. Type help for commands.");
            }
        } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) {
            output.WriteLine($"Error: {exception.Message}");
        }
        return true;
    }

    private void NewWorld(CommandInput command) {
        if (command.Count == 1) {
            throw new ArgumentException("Usage: new [width height [seed [name]]]");
        }

        WorldConfig defaults = new();
        WorldConfig config = command.Count == 0 ? defaults : new WorldConfig(
            command.Integer(0, "width"),
            command.Integer(1, "height"),
            command.Count >= 3 ? command.LongInteger(2, "seed") : defaults.Seed,
            command.Count >= 4 ? command.RemainingText(3) : defaults.Name);
        // Construct first so invalid configuration leaves the current session intact.
        this._kernel = new WorldKernel(config);
        output.WriteLine($"Opened {config.Name}.");
        TerminalRenderer.Map(output, this.Snapshot);
    }

    private static GridPoint Point(CommandInput command) {
        return new(command.Integer(0, "X"), command.Integer(1, "Y"));
    }

    private void Intervene(WorldCommand command, GridPoint position) {
        CommandResult result = this._kernel.Execute(command);
        output.WriteLine(result.Message);
        TerminalRenderer.Inspect(output, result.Snapshot, position);
        output.WriteLine($"Turn {result.Snapshot.Turn} (unchanged). Use advance to resolve the simulation.");
    }
}
