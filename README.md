# Automapolis

Automapolis is a deterministic, turn-based science-fantasy war simulation about humanity's fight against an alien bioswarm. Humanity answers overwhelming biological adaptation with advanced magitech, fortified enclaves, ordinary soldiers, and the rare **Bastion**: a single super-soldier capable of changing an entire front.

Each generated theater begins in crisis with one Bastion, one human enclave, supporting soldiers, and a spreading alien ecology. In Witness mode the war resolves autonomously one deliberate turn at a time. In Command mode the player can channel resonance, fortify terrain, deploy human forces, establish enclaves, and authorize destructive purges.

The game is split strictly into a mandatory [.NET Kernel](src/Automapolis.Kernel) and optional [Shells](Shells). The included 2d shell is built with Godot 4.7.2. See [the architecture](docs/Architecture.md), [world foundation](docs/World.md), and [art direction](docs/ArtDirection.md).

The 2d shell represents terrain and forces with colored symbols supplied by the Kernel. The main Shell will use 3D Godot visuals with SimplePaint and an orthographic 3/4 overhead camera. Production art will be modeled by Kevin in Blender using his SimplePaint shader; see [art direction](docs/ArtDirection.md).

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
