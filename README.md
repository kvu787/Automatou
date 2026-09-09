# Automapolis

Automapolis is a deterministic, turn-based science-fantasy war simulation about humanity's fight against an alien bioswarm. Humanity answers overwhelming biological adaptation with advanced magitech, fortified enclaves, ordinary soldiers, and the rare **Bastion**: a single super-soldier capable of changing an entire front.

Each generated theater begins in crisis with one Bastion, one human enclave, supporting soldiers, and a spreading alien ecology. The war resolves autonomously one deliberate turn at a time. The player can channel resonance, fortify terrain, deploy human forces, establish enclaves, and authorize destructive purges. For passive play, submit only **Advance Turn**; no separate mode is needed.

The game is split strictly into a mandatory [.NET Kernel](Source/Automapolis.Kernel) and optional [Shells](Shells). The included 2d shell is built with Godot 4.7.2. See [the architecture](Documentation/Architecture.md), [world foundation](Documentation/World.md), [mechanics](Documentation/Mechanics.md), and [art direction](Documentation/ArtDirection.md).

The 2d shell represents terrain with colored fills and forces with symbols on a pointy-top hex grid. Empty cells show only terrain color. The Kernel supplies the force symbols and uses six-neighbor movement and hex-distance ranges. The main Shell will use 3D Godot visuals with SimplePaint and an orthographic 3/4 overhead camera. Production art will be modeled by Kevin in Blender using his SimplePaint shader; see [art direction](Documentation/ArtDirection.md).

## Play

On Windows, double-click `Run.cmd`. It publishes the Kernel host, exports the Godot game, and launches the standalone executable. Godot 4.7.2 must be installed at `%UserProfile%\Program\Godot_v4.7.2-stable_win64.exe`, or its path can be supplied through `GODOT_EXE`.

## Develop

```powershell
dotnet build Automapolis.slnx
dotnet run --project Tests/Automapolis.Kernel.Tests
```

To use the text protocol directly:

```powershell
dotnet run --project Source/Automapolis.Kernel.Host
```

Then type one JSON command per line, such as `{"command":"advance"}`.
