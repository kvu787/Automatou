# Create Automapolis

Date: 2026-08-31

## User

> Create this game:
>
> - sci-fi fantasy themed
> - square grid
> - zero or one player
> - in zero player mode, runs autonomously, but player must press some input to advance to the next turn
> - one player mode means that the player can affect the world in various interesting sandboxy ways (god/creator mode)
> - turn-based world sim
> - must run on any major pc/laptop setup but doesn't have to run on smartphones
> - development will be heavily AI driven
> - code and assets that must be human reviewable
> - the game will be 100% text based with an option to connect a typical game UI and IO based on the human player/dev preference
>
> The architecture should strictly separate the single mandatory game engine from any number of optional UI/GUI/IO interfaces. Let's call the former the `Kernel` and the latter `Shells`.
>
> Use C# 14 and .NET 10 for the Kernel.
>
> Create a Shell using Godot 4.7.3. Use a visual style that is not pure text but still optimizes for AI-driven development.

## User correction

> Correction: I meant Godot 4.7.2, not 4.7.3.

## Assistant

Created a working first version of Automapolis with a strict Kernel/Shell boundary.
The C# 14 and .NET 10 Kernel implements deterministic world generation,
terrain ecology, autonomous beings and settlements, rifts, observer mode,
Creator interventions, immutable snapshots, a text renderer, and a JSON-lines
host. The Kernel contains no Godot, rendering, or input dependencies.

Created an optional Godot 4.7.2 Shell with a colored square glyph grid, tile and
being inspection, a world chronicle, mode and seed controls, six Creator tools,
and explicit one-turn advancement. Its layout, styling, scene, and SVG icon are
all text-authored and human-reviewable. Added Windows, Linux, and macOS export
presets plus `Run.cmd`, which builds, tests, exports, publishes the Kernel host,
and launches the standalone Windows game.

Verified the .NET solution with a clean warning-free build, five passing
behavioral tests, a live Kernel/Shell process integration run, a Godot 4.7.2
standalone export, and visual QA of the rendered application.
