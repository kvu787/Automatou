# Automatou — World laboratory

A self-contained, native Godot prototype of the world-builder-and-runner described in [Specification.md](Specification.md). Create worlds, author unit and building blueprints, populate factions, and observe their automata. There is no player faction, score, or victory screen.

## Run

Double-click **Run.cmd**. It restores the bundled Godot packages, builds the C# project, and launches the game. The game starts at a main menu with **Load world**, **World creator**, **Unit creator**, and **Building creator**. Load world lists the built-in five-faction encounter and every saved player world. Worlds open paused.

Requirements: Windows 11 x64, .NET SDK 10.0.400 or newer in the .NET 10 family, and Godot **4.7.2 .NET x64**. The default Godot location is `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`. Set `GODOT_EXE` to the .NET console executable to use another installation. The launcher uses that installation's local NuGet packages; no additional game assets, external repositories, export templates, or network services are required.

All session logs are written under `MyLogOutput/yyyy-MM-dd_HH-mm-ss`. `Launcher.log` records the build, `Godot.log` records engine output, and `Session.log` records actions and combat. Opening the project directly in Godot also creates a timestamped application session log.

## First experiment

1. Choose **Load world**, then **Five-faction encounter**. Press **Step** to advance one turn, or **Run automata** to watch continuously. Choose one to eight turns per second.
2. Select an entity with **Inspect**. Its arrow shows its facing; the gold region shows its forward attack region. The inspector reports health, weapons, armor, and automaton.
3. Open **Edit in World creator**. Use **Paint terrain**, **Place unit**, **Place building**, or **Erase entity** to modify the world. Editing pauses the simulation. Placement previews become red where a footprint cannot fit.
4. Open Unit creator or Building creator, change a design, and use **Save & place in world**. Choose the faction independently of the blueprint.
5. Save a named world before replacing it with a blank map, generated map, or the demonstration encounter.

The starting world has two Bastions, five Traveler outriders and a mobile H.O.M.E., two siege walkers, eight clone infantry and two artillery units, seven Prytu hunters and a manifestation, plus one explicitly authored watch station.

| Control           | Action                                           |
| ----------------- | ------------------------------------------------ |
| Middle mouse drag | Pan                                              |
| Mouse wheel       | Zoom around the pointer                          |
| Left click / drag | Use the selected world or building tool          |
| Right click       | Inspect in world; remove cell in building editor |
| Space             | Run / pause                                      |
| N                 | Pause and advance one turn                       |
| R                 | Rotate selection, placement, or unit preview     |
| F                 | Fit the current world or creator grid            |
| Delete            | Remove the selected entity                       |
| Escape            | Return to main menu                       |

Shortcuts are suspended while typing in a text or number field. Sidebars scroll independently. The window can be resized down to 1100 × 700.

## World creator and storage

The lower section of the World creator toolbar creates either a rectangle from width and height, or a hexagon from size. Positive sizes are required; maps are limited to 20,000 cells. A size-one hexagonal map has one cell. Rectangular coordinates begin at the lower left, with positive X rightward and positive Y upward. Odd rows are offset east. Hexagonal maps are centered at coordinate (0, 0), so they also use negative coordinates.

**Blank** creates plains for manual authoring. **Generate** produces repeatable terrain from the chosen seed. All eleven specified terrain types can be painted, including exclusion zones. Painting under a building changes the terrain that will return when that building is removed. Painting incompatible terrain under a unit is rejected.

**Checkpoint** stores the current state in memory; **Rewind** restores it. Edits at turn zero also refresh the starting checkpoint. **Save** uses the name in the world field. Names already saved overwrite that file. **Load** opens the world browser and returns the selected world to World creator. The browser refreshes its saved-world list whenever opened. Returning to the main menu pauses the simulation and retains the current map and creator drafts.

- `UserContent/Worlds`: map terrain, living entities, faction, facing, health, turn, casualty count, embedded blueprints, and deterministic random state.
- `UserContent/Units`: reusable unit blueprints, loaded automatically at startup.
- `UserContent/Buildings`: reusable manually authored building blueprints, including the editor pivot and patch settings.

Saving writes a temporary file before replacing the destination. Loading validates the world before replacing the active simulation. Saves are paused on load. User content and logs are ignored by Git. Content formats have no compatibility or migration layer between repository revisions.

## Unit creator

Create a design by editing a blueprint and giving it a new name. Saving an existing name updates that blueprint; existing placed entities keep their own copies. Unsaved fields survive navigating between screens; choosing another blueprint replaces the draft.

The central view shows the live footprint, origin, facing, and attack region. Sizes 1, 2, 3, and 4 occupy 1, 7, 19, and 37 cells respectively. The prototype supports sizes 1–12. Set health, armor, ranged and melee damage, attack range, action points, evasion, splash radius, automaton, and mobility.

Movement domains:

- Ground: ordinary land; water, mountains, air, and space are blocked.
- Amphibious: ordinary land and water; mountains, air, and space are blocked.
- Flight: all terrain except space and exclusion zones.
- Spaceflight: all terrain except exclusion zones.

## Building creator

Start on a 20 × 10 grid. Paint occupied cells with the left mouse button and remove them with the right button. Clicking a border adds a patch to that side; clicking a corner expands both adjacent sides. Patch width and height are editable. Grid expansion is limited to 20,000 cells.

Choose **Set origin / pivot**, then click the desired cell. The gold cross marks it; the pivot can lie outside the footprint. Saving records that editor location and the occupied axial offsets relative to it. Loading expands the grid in patches until the entire shape and pivot fit. Placement and rotation use that pivot, including after reopening a saved design.

A saved building must contain one connected island of 1–2,000 distinct cells. Buildings have one health pool, never move, and can cover any terrain. Attacks hit occupied cells even when the origin is outside the footprint. Their underlying terrain is preserved. No buildings are procedurally generated; the starting watch station is an explicit authored footprint in `Source/Simulation/Definitions.cs`.

## Simulation rules

These are explicit prototype defaults for the combat mechanics left open by the specification. They can be changed in the C# simulation and through blueprint fields.

- Every turn replenishes each unit's action points. Unused points expire.
- A 60-degree turn costs one point. Units move only forward, spending one point per cell, or two on forest, wetlands, and tundra for ground units.
- Infantry-and-artillery faction units cross rough terrain for one point and take two health damage. Other movement domains ignore the ground movement surcharge.
- An attack costs two points, with at most one attack per unit per turn. Adjacent targets take melee damage; more distant targets take ranged damage. Range is measured between occupied footprints.
- Attacks cover the facing direction and its two neighboring directions. Front armor is full strength, front-side armor is two-thirds strength, and rear-side/rear armor is one-quarter strength. Hits always deal at least one damage.
- Evasion is a deterministic seeded chance to avoid a hit. Ranged blast attacks damage every entity in the impact radius, including allies and potentially the attacker. Melee attacks do not splash.
- **Advance** closes with the nearest opponent; **Skirmish** attacks and withdraws; **Hold** stays in place until an enemy enters range; **Artillery** prefers greater separation; **Swarm** gives additional priority to weakened opponents.
- Automata search routes around blocked terrain and occupied footprints. Searches are bounded to 3,000 expanded positions per decision. A route too complex for this bound causes the unit to wait and retry next turn.
- The first acting unit rotates each turn. Factions are mutually hostile. Buildings are passive targets. The simulation keeps running after only one faction remains.
- **Deploy / hold position** anchors a selected unit and removes its evasion until **Mobilize unit** is pressed. This provides the H.O.M.E. mobile/stationary toggle without changing its footprint.

The theme and named factions are represented through colors, geometric symbols, blueprints, and behavior. No artwork is used. This is the combat-oriented foundation: reproduction, trading, Traveler sickness, stealth, magic subsystems, line of sight, resource production, and programmable in-game automata graphs are not implemented. The faction prose in the specification provides direction for those future systems. Automata and definitions are ordinary editable C# code today.

## Verify and extend

From this folder in PowerShell:

```powershell
.\Run.ps1 -Verify -BuildOnly
.\Run.ps1 -Verify -VerifyInterface
```

The first command builds and runs dependency-free simulation checks. The second also launches the real Godot renderer, exercises the interface, captures views under the session log folder, and exits. Its authored test content is isolated inside that log folder.

Verification covers coordinates, footprint sizes, rotations, terrain and collision restrictions, building connectivity and pivot persistence, attack arcs and armor, action points, splash damage, obstacle routing, automaton movement, remote building pivots, blueprint files, deterministic world restoration, and a 120-turn five-faction encounter. Interface checks cover selection, pan, zoom, rotation, stepping, live unit sizes, unit saving/placement, building expansion/pivot/save/reopening/placement, terrain painting, world saving/loading, and the minimum window size.

`Source/Simulation` contains the engine-independent model and rules. `Source/Interface` contains the Godot renderer and creators. `Tests` compiles the simulation directly without Godot or an external test framework. The experiment has its own build settings and does not reference the main repository application.
