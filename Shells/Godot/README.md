# Godot Shell

This optional presentation layer targets Godot 4.7.2. It renders the Bastion Front as a responsive tactical symbol grid with sector inspection, front dispatches, and Command controls, but contains no simulation rules.

The Shell starts the adjacent `KernelHost` process and exchanges one JSON object per line. It can therefore be replaced without changing the game.

## Controls

- Click a sector to inspect its terrain and all forces occupying it.
- Press Space, Enter, or **Advance the Front** to resolve exactly one autonomous turn.
- Select Witness or Command, enter an integer seed, and click **Open Front** to start a new deterministic theater.
- Witness mode hides field authority controls and the Kernel rejects attempted intervention.
- Command mode can channel resonance, fortify sectors, deploy soldiers, replace a lost Bastion, establish enclaves, and authorize magitech purges.

## Symbols

The grid uses the Kernel's glyphs, with terrain colors and distinct force colors.
Occupied cells show a large force symbol, the terrain symbol in the lower-left,
and an occupant count in the upper-right when multiple forces share the cell.
The legend identifies every terrain and force type. Hover or select a cell to
inspect all occupants.

| Symbol | Terrain or force  |
| ------ | ----------------- |
| ·      | Shattered plain   |
| :      | Ash waste         |
| ≈      | Ley channel       |
| ^      | Xenoforest        |
| #      | Fortified reach   |
| ~      | Brood mire        |
| B      | Bastion           |
| S      | Soldier           |
| r      | Ravener           |
| N      | Brood node        |
| E      | Enclave           |

Production art is being created in Blender with SimplePaint. The current Shell
uses font glyphs and code-authored controls, with no generated image dependency.

## Platform exports

Windows, Linux, and macOS export presets are included. Publish `Automapolis.Kernel.Host` for the target .NET runtime into a `KernelHost` folder beside the Godot executable. The Windows `Run.cmd` automates the local build and launch.
