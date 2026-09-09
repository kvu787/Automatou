# TerminalShell

A C# command-line Shell for playing Automapolis. Inspect the hex theater, edit
world state through field interventions, and resolve the war one explicit turn
at a time. All simulation rules and state remain in `Automapolis.Kernel`;
TerminalShell calls its C# API directly.

## Build and run

On Windows x64, double-click **Run.cmd** in this folder. It opens a console,
runs `Run.ps1` to publish a self-contained executable into `Build`, and starts
the interactive session in that console. The x64 .NET 10 SDK is needed to
build; Godot is not needed. After building, `Build/TerminalShell.exe` can also
be launched directly without installing a .NET runtime.

From PowerShell:

```powershell
.\Run.cmd
.\Run.cmd -BuildOnly
```

`-BuildOnly` publishes without starting a session. Build failures stay visible
when launching interactively. Close a running published TerminalShell before
rebuilding it.

To develop without publishing, from the repository root:

```powershell
dotnet run --project Shells/TerminalShell
dotnet build Shells/TerminalShell/TerminalShell.slnx
dotnet run --project Tests/Automapolis.TerminalShell.Tests
```

## First commands

A new session starts with the default generated theater at turn zero. Type
`help` for the full command reference, or try this sequence:

```text
status
legend
inspect 2 3
channel 2 3 25
fortify 2 3 FortifiedReach
deploy 2 3 Soldier
establish 2 3 "Vigil Annex"
inspect 2 3
advance
forces
chronicle
```

Coordinates are zero-based `X Y` (column, row). Odd hex rows shift right.
The map labels both axes and uses the Kernel's terrain and force glyphs.
A displayed force can cover terrain and other forces; `inspect X Y` shows
everything in that sector.

## Commands

| Command                           | Effect                                                     |
| --------------------------------- | ---------------------------------------------------------- |
| `help` or `?`                     | Show syntax, choices, and examples.                        |
| `map`                             | Show the hex map and latest dispatch.                      |
| `status`                          | Show configuration and every world metric.                 |
| `inspect X Y`                     | Show a sector's terrain, resources, and all its forces.    |
| `forces [kind]`                   | List forces and their details, optionally by kind.         |
| `force ID`                        | Inspect one force by the ID shown in `forces`.             |
| `legend`                          | Explain the glyphs currently present in the world.         |
| `chronicle [count]`               | Show recent dispatches, newest first.                      |
| `snapshot`                        | Print the complete current snapshot as readable JSON.      |
| `channel X Y [amount]`            | Channel resonance; default amount is 25.                   |
| `fortify X Y terrain`             | Change a sector's terrain.                                 |
| `deploy X Y [kind]`               | Deploy a Soldier (default) or a Bastion.                   |
| `establish X Y name`              | Establish a named human enclave.                           |
| `purge X Y [radius]`              | Burn sectors and forces in a hex radius; default is 1.     |
| `advance [count]`, `step [count]` | Resolve 1 turn by default, or explicitly request 1..1000.  |
| `new`                             | Replace the current world with the default theater.        |
| `new width height [seed [name]]`  | Generate a replacement world with the given configuration. |
| `quit` or `exit`                  | End the session.                                           |

Commands and enum names ignore case. Multiword names can be quoted with single
or double quotes, or written as the remaining text on the line. Inside quotes,
a backslash escapes a matching quote or another backslash. Blank lines do
nothing. Invalid commands print an error and leave the session usable.

Available terrain names are `ShatteredPlain`, `AshWaste`, `LeyChannel`,
`Xenoforest`, `FortifiedReach`, and `BroodMire`. Force filters accept `Bastion`,
`Soldier`, `Ravener`, `BroodNode`, and `Enclave`. The Kernel permits deployment
of human field forces only, and a living Bastion prevents a second deployment.

World edits do not advance time. `advance 5` submits exactly five `AdvanceTurn`
commands, reports each completion, and renders the final map. Purges affect
both sides. The Kernel clamps channel amounts to 1..100 and purge radii to
0..4; the command response reports the applied value.

`new 10 8 1234 "Northern Front"` creates a reproducible theater. Width must be
6..80, height 6..50, and the world name 1..48 characters. Enclave names must be
1..32 characters. Omitted seed and world name use the Kernel defaults. Large
maps may need a wider terminal window.

World state lasts for the current process only. `new` replaces it, and
`quit`/end-of-input closes it. There is no save/load feature. The chronicle
contains only the Kernel's most recent 14 dispatches.

## Scripted input

The same commands also work through standard input. Redirected sessions omit
interactive prompts and end cleanly at end-of-input:

```powershell
@('new 8 8 42 "Scripted Front"', 'advance 3', 'status', 'quit') |
    & .\Shells\TerminalShell\Build\TerminalShell.exe
```

Output is human-readable; the existing
[Kernel Host](../../Source/Automapolis.Kernel.Host) remains the JSON-lines
request/response interface for other clients.
