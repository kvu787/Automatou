# Repository work refresher

## User

refresh my memory on what i was working on in this repo

## Assistant

I’ll check the recent commits, working tree, and conversation logs to piece together where you left off.

## Assistant

**You were most recently reconsidering the game’s grid—square versus hex—while working out spatial mechanics.**

Automapolis is your turn-based science-fantasy war sim: humanity and a powerful Bastion fighting an alien bioswarm, with a C# simulation Kernel and a Godot front end.

- **Latest discussion, September 8:** square/hex gameplay tradeoffs, algorithm efficiency, and hex storage using offset, axial, and cube coordinates. No switch to hex was recorded; the mechanics document still specifies squares.
- **Latest mechanics design, September 2–3:** variable-sized entities. Humans occupy 2×2 base cells, cats/dogs 1×1, movable units must have square footprints, and stationary buildings can be rectangular. Footprint size is independent of combat power.
- **Still undecided:** footprint anchoring, stacking, movement across mixed terrain, pathfinding clearance, range/adjacency, area damage, and building interiors. These are listed as decisions needed before implementing variable footprints.
- **Your other major blocker was art consistency.** You had experimented with AI sprites and built a pixel-perfect converter, but consistent perspective and style remained unresolved. The last discussion proposed testing PixelLab and a fixed 3D-to-sprite pipeline; it did not record a chosen solution.

The clearest place to resume is [Mechanics.md](C:/Users/k/Repository/CSharp/Automapolis/docs/Mechanics.md), alongside the [latest grid discussion](C:/Users/k/Repository/CSharp/Automapolis/Conversations/2026-09-08-square-versus-hex-grids.md). Your working tree was clean when I checked.

