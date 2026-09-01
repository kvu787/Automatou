# Automapolis

A deterministic, turn-based sci-fi fantasy world simulation. Watch a text-authored world evolve one deliberate turn at a time, or enter Creator mode and reshape its terrain, life, cities, and reality itself.

The game is split strictly into a mandatory [.NET Kernel](src/Automapolis.Kernel) and optional [Shells](Shells). The included Shell is built with Godot 4.7.2. See [the architecture](docs/Architecture.md) for the boundary and protocol.

## Play

On Windows, double-click `Run.cmd`. It publishes the Kernel host, exports the Godot game, and launches the standalone executable. Godot 4.7.2 must be installed at `%UserProfile%\Program\Godot_v4.7.2-stable_win64.exe`, or its path can be supplied through `GODOT_EXE`.

## Develop

```powershell
dotnet build Automapolis.slnx
dotnet run --project tests/Automapolis.Kernel.Tests
```

To use the text protocol directly:

```powershell
dotnet run --project src/Automapolis.Kernel.Host
```

Then type one JSON command per line, such as `{"command":"advance"}`.
