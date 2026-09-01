# Automapolis

Automapolis is a deterministic, turn-based science-fantasy war simulation about humanity's fight against an alien bioswarm. Humanity answers overwhelming biological adaptation with advanced magitech, fortified enclaves, ordinary legionaries, and the rare **Bastion**: a single super-soldier capable of changing an entire front.

Each generated theater begins in crisis with one Bastion, one human enclave, supporting cohorts, and a spreading alien ecology. In Witness mode the war resolves autonomously one deliberate turn at a time. In Command mode the player can channel resonance, fortify terrain, deploy human forces, establish enclaves, and authorize destructive purges.

The game is split strictly into a mandatory [.NET Kernel](src/Automapolis.Kernel) and optional [Shells](Shells). The included Shell is built with Godot 4.7.2. See [the architecture](docs/Architecture.md), [world foundation](docs/World.md), and [art direction](docs/ArtDirection.md).

Every terrain and force has a deterministic 16x16 tactical pixel-art sprite. The palette-indexed source, compiler, generated PNGs, and browser-viewable catalog live in [`Shells/Godot/assets/sprites`](Shells/Godot/assets/sprites).

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
