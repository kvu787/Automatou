# 2d shell (Godot)

This is the currently implemented symbolic 2d shell, distinct from the planned main 3D Shell. This optional presentation layer targets Godot 4.7.2. It renders the Bastion Front as a responsive tactical hex grid with symbols with sector inspection, front dispatches, and Command controls, but contains no simulation rules.

The 2d shell starts the adjacent `KernelHost` process and exchanges one JSON object per line. It can therefore be replaced without changing the game.

## Controls

- Click a sector to inspect its terrain and all forces occupying it.
- Press Space, Enter, or **Advance the Front** to resolve exactly one autonomous turn.
- Enter an integer seed and click **Open Front** to start a new deterministic theater.
- For passive play, use only **Advance the Front**. Player commands remain available at any time.
- The player can channel resonance, fortify sectors, deploy soldiers, replace a lost Bastion, establish enclaves, and authorize magitech purges.

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

Production art is being created in Blender with SimplePaint. The 2d shell
uses font glyphs and code-authored controls, with no generated image dependency.

## Platform exports

Windows, Linux, and macOS export presets are included. Publish `Automapolis.Kernel.Host` for the target .NET runtime into a `KernelHost` folder beside the Godot executable. The Windows `Run.cmd` automates the local build and launch.

## Hex layout

The board uses pointy-top hexagons in odd-row offset coordinates. Odd rows are
shifted half a hex to the right. Selection and hover follow the actual hex
polygon, including where adjacent button bounding boxes overlap. The inspector
shows column and row; field commands use those same coordinates. Purge radius
one covers the selected hex and its six neighbors, clipped at map boundaries.

The current prototype has single-hex forces. It does not yet implement the
planned six-direction facing, rotation costs, or variable footprints.

## Integration check

Publish the Kernel host into `Shells/Godot/KernelHost`, then run Godot with
`--headless --path Shells/Godot --script Tests/HexGridSmoke.gd` from the repository
root. This checks the live protocol, hex selection (including overlapping cell
bounding boxes), turn advancement, resized maps, and player interventions. The test
scripts are excluded from standalone exports.
