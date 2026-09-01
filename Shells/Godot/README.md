# Godot Shell

This optional presentation layer targets Godot 4.7.2. It renders a responsive, colored glyph grid with inspection panels and Creator controls, but contains no simulation rule. There are no raster assets or binary scenes: layout, theme, and the SVG sigil are all source-reviewable.

The Shell starts the adjacent `KernelHost` process and exchanges one JSON object per line. It can therefore be replaced without changing the game.

## Controls

- Click a cell to inspect it and target Creator instruments.
- Press Space, Enter, or **Resolve Next Turn** to advance exactly one autonomous turn.
- Select Observer or Creator, enter an integer seed, and click **Reforge** to start a new deterministic world.
- Observer mode hides Creator instruments and the Kernel rejects any attempted intervention.

## Platform exports

Windows, Linux, and macOS export presets are included. Publish `Automapolis.Kernel.Host` for the target .NET runtime into a `KernelHost` folder beside the Godot executable. The Windows `Run.cmd` automates the local build and launch.
