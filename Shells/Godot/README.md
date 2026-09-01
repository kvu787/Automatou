# Godot Shell

This optional presentation layer targets Godot 4.7.2. It renders the Bastion Front as a responsive tactical sprite grid with sector inspection, front dispatches, and Command controls, but contains no simulation rules.

The Shell starts the adjacent `KernelHost` process and exchanges one JSON object per line. It can therefore be replaced without changing the game.

## Controls

- Click a sector to inspect its terrain and all forces occupying it.
- Press Space, Enter, or **Advance the Front** to resolve exactly one autonomous turn.
- Select Witness or Command, enter an integer seed, and click **Open Front** to start a new deterministic theater.
- Witness mode hides field authority controls and the Kernel rejects attempted intervention.
- Command mode can channel resonance, fortify sectors, deploy cohorts, replace a lost Bastion, establish enclaves, and authorize magitech purges.

## Platform exports

Windows, Linux, and macOS export presets are included. Publish `Automapolis.Kernel.Host` for the target .NET runtime into a `KernelHost` folder beside the Godot executable. The Windows `Run.cmd` automates the local build and launch.
