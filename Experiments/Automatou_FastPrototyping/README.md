# Automatou — World laboratory

A self-contained, native Godot prototype of the world-builder-and-runner described in [Specification.md](Specification.md). Create worlds, place source-defined units, populate factions, and observe their behavior. There is no player faction, score, or victory screen.

## Run

Double-click **Run.cmd**. It restores the bundled Godot packages, builds the C# project, and launches the game. The game starts at a main menu with **Load world** and **World creator**. Load world lists the built-in five-faction encounter and every saved player world. Worlds open paused.

Requirements: Windows 11 x64, .NET SDK 10.0.400 or newer in the .NET 10 family, and Godot **4.7.2 .NET x64**. The default Godot location is `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`. Set `GODOT_EXE` to the .NET console executable to use another installation. The launcher uses that installation's local NuGet packages; no additional game assets, external repositories, export templates, or network services are required.

All session logs are written under `MyLogOutput/yyyy-MM-dd_HH-mm-ss`. `Launcher.log` records the build, `Godot.log` records engine output, and `Session.log` records actions and combat. Opening the project directly in Godot also creates a timestamped application session log.

Rendering uses Godot's Mobile renderer with 4× multisample anti-aliasing for smoother hexagon edges, unit arrows, and other 2D geometry. This requires a graphics driver supported by the Mobile renderer; the OpenGL Compatibility renderer does not support 2D MSAA. VSync is off and the frame rate is limited to 60 FPS.

## First experiment

1. Choose **Load world**, then **Five-faction encounter**. Press **Step** to advance one turn, or **Run simulation** to watch continuously. Choose one to eight turns per second.
2. Select an entity with **Inspect**. Its arrow shows its facing; the gold region shows its forward attack region. The inspector reports health, weapons, armor, and other unit statistics.
3. Open **Edit in World creator**. Use **Paint terrain**, **Place unit**, or **Erase entity** to modify the world. Editing pauses the simulation. Placement previews become red where a footprint cannot fit.
4. Choose a built-in unit in World creator. Choose its faction when placing.
5. Save a named world before replacing it with a blank map, generated map, or the demonstration encounter.

The starting world has two Bastions, five Traveler outriders and a mobile H.O.M.E., two siege walkers, eight clone infantry and two artillery units, seven Prytu hunters and a manifestation.

| Control           | Action                                           |
| ----------------- | ------------------------------------------------ |
| Middle mouse drag | Pan                                              |
| Mouse wheel       | Zoom around the pointer                          |
| Left click / drag | Use the selected world tool                      |
| Right click       | Inspect a unit or cell                           |
| Space             | Run / pause                                      |
| N                 | Pause and advance one turn                       |
| R                 | Rotate selection or placement                    |
| F                 | Fit the current world                            |
| Delete            | Remove the selected entity                       |
| Escape            | Return to main menu                              |

Shortcuts are suspended while typing in a text or number field. Sidebars scroll independently. The window can be resized down to 1100 × 700.

## World creator and storage

The lower section of the World creator toolbar creates either a rectangle from width and height, or a hexagon from size. Positive sizes are required; maps are limited to 20,000 cells. A size-one hexagonal map has one cell. Rectangular coordinates begin at the lower left, with positive X rightward and positive Y upward. Odd rows are offset east. Hexagonal maps are centered at coordinate (0, 0), so they also use negative coordinates.

**Blank** creates plains for manual authoring. **Generate** produces repeatable terrain from the chosen seed. All eleven specified terrain types can be painted, including exclusion zones. Painting incompatible terrain under a unit is rejected.

**Checkpoint** stores the current state in memory; **Rewind** restores it. Edits at turn zero also refresh the starting checkpoint. **Save** uses the name in the world field. Names already saved overwrite that file. **Load** opens the world browser and returns the selected world to World creator. The browser refreshes its saved-world list whenever opened. Returning to the main menu pauses the simulation and retains the current map.

- `UserContent/Worlds`: map terrain, living entities, faction, facing, health, turn, casualty count, unit type identifiers, automaton memory, and deterministic random state.

Saving writes a temporary file before replacing the destination. Loading validates the world before replacing the active simulation. Saves are paused on load. User content and logs are ignored by Git. Content formats have no compatibility or migration layer between repository revisions.

## Source-defined units and automata

Each unit type has a class in `Source/Simulation/Units`: Bastion, TravelerOutrider, Home, SiegeWalker, CloneInfantry, LongbowArtillery, PrytuHunter, and PrytuManifestation. Its immutable `UnitStatistics` defines its body and combat properties. There is no unit creator or unit blueprint loading.

An automaton is the memory and logic a unit uses to decide what to do each turn. The plural is automata; these terms refer only to unit decision-making, never to units, their statistics, or the simulation as a whole. Each unit class contains its own nested `Automaton` class. Each placed unit owns a separate instance, exposed through `Brain` and saved through its concrete `Memory` property. `CreateFresh()` creates a new unit with empty memory for placement. The initial brains remember their target (retaining it on equal target scores) and how many turns they have observed. Add serializable properties to a unit's automaton for longer-term goals and other memory.

On each turn, `World.Step()` invokes the living unit's `Brain.Act(UnitSenses)` once. The brain yields typed requests (`TurnAction`, `MoveForwardAction`, `AttackAction`). The world validates and applies each request before resuming the brain. The brain can take a fresh observation after each action, so it can react to a destroyed target or a changed position during the same turn. Ending the iterator ends the turn; rejected requests also end the turn. Every successful action consumes points, and destroyed actors stop immediately.

`UnitSenses.Observe()` returns detached, read-only entity observations and immutable statistics, with no entity or brain references. Terrain, occupancy, and route queries are read-only. Sensing currently covers the whole world. `TacticalPlanning` supplies optional target selection and engagement helpers; each unit's automaton chooses its tactics and can implement completely different logic. The world contains action rules, not a switch selecting unit behavior.

Saves store a unit type identifier and the concrete brain's memory. Stats and executable logic come from source. Checkpoint/rewind and save/load restore independent brains and deterministic continuation. Suspended iterators are not saved; turns finish synchronously before a checkpoint can be taken. Old blueprint-based saves are not supported.

To add a unit, derive from `Unit`, define immutable statistics, implement a nested `UnitAutomaton`, expose its concrete memory, implement `CreateFresh()`, and register the class in `Catalog.Units()` and the JSON derived-type declarations in `Unit.cs`. Brain memory must consist of serializable data rather than live world references.

Movement domains:

- Ground: ordinary land; water, mountains, air, and space are blocked.
- Amphibious: ordinary land and water; mountains, air, and space are blocked.
- Flight: all terrain except space and exclusion zones.
- Spaceflight: all terrain except exclusion zones.

## Simulation rules

These are explicit prototype defaults for the combat mechanics left open by the specification. They can be changed in the C# simulation and unit classes.

- Every turn replenishes each unit's action points. Unused points expire.
- A 60-degree turn costs one point. Units move only forward, spending one point per cell, or two on forest, wetlands, and tundra for ground units.
- Infantry-and-artillery faction units cross rough terrain for one point and take two health damage. Other movement domains ignore the ground movement surcharge.
- An attack costs two points, with at most one attack per unit per turn. Adjacent targets take melee damage; more distant targets take ranged damage. Range is measured between occupied footprints.
- Attacks cover the facing direction and its two neighboring directions. Front armor is full strength, front-side armor is two-thirds strength, and rear-side/rear armor is one-quarter strength. Hits always deal at least one damage.
- Evasion is a deterministic seeded chance to avoid a hit. Ranged blast attacks damage every entity in the impact radius, including allies and potentially the attacker. Melee attacks do not splash.
- Bastions and siege walkers close with nearby opponents. Travelers and artillery attack and withdraw when too close. Clones and Prytu prioritize weakened opponents. Each unit class owns these decisions.
- Automata search routes around blocked terrain and occupied footprints. Searches are bounded to 3,000 expanded positions per decision. A route too complex for this bound causes the unit to wait and retry next turn.
- The first acting unit rotates each turn. Factions are mutually hostile. The simulation keeps running after only one faction remains.
- **Deploy / hold position** anchors a selected unit and removes its evasion until **Mobilize unit** is pressed. This provides the H.O.M.E. mobile/stationary toggle without changing its footprint.

The theme and named factions are represented through colors, geometric symbols and behavior. No artwork is used. This is the combat-oriented foundation: reproduction, trading, Traveler sickness, stealth, magic subsystems, line of sight, resource production, and programmable in-game automata graphs are not implemented. The faction prose in the specification provides direction for those future systems. Automata and definitions are ordinary editable C# code today.

## Verify and extend

From this folder in PowerShell:

```powershell
.\Run.ps1 -Verify -BuildOnly
.\Run.ps1 -Verify -VerifyInterface
```

The first command builds and runs dependency-free simulation checks. The second also launches the real Godot renderer, exercises the interface, captures views under the session log folder, and exits. Its authored test content is isolated inside that log folder.

Verification covers coordinates, footprint sizes, rotations, terrain and collision restrictions, attack arcs and armor, action points, splash damage, obstacle routing, unit movement, independent brain memory, read-only sensing, validated actions, deterministic world restoration, and a 120-turn five-faction encounter. Interface checks cover selection, pan, zoom, rotation, stepping, source-defined unit placement, terrain painting, world saving/loading, and the minimum window size.

`Source/Simulation` contains the engine-independent model and rules. `Source/Interface` contains the Godot renderer and world creator. `Tests` compiles the simulation directly without Godot or an external test framework. The experiment has its own build settings and does not reference the main repository application.
