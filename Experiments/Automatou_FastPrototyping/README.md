# Automatou

A self-contained, native Godot prototype of the world-builder-and-runner described in [Specification.md](Specification.md). Create worlds, place source-defined units, populate factions, and observe their behavior. There is no player faction, score, or victory screen.

## Run

Double-click **Run.cmd**. It restores the bundled Godot packages, builds the C# project, and launches the game. The game starts at a main menu with **Load world** and **World creator**. Load world lists focused behavior experiments, the five-faction encounter, and player worlds saved during the current session. Worlds open paused. Every launch starts fresh; application state is never read from or written to disk.

Requirements: Windows 11 x64, .NET SDK 10.0.400 or newer in the .NET 10 family, and Godot **4.7.2 .NET x64**. The default Godot location is `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`. Set `GODOT_EXE` to the .NET console executable to use another installation. The launcher uses that installation's local NuGet packages; no additional game assets, external repositories, export templates, or network services are required.

All session logs are written under `MyLogOutput/yyyy-MM-dd_HH-mm-ss`. `Launcher.log` records the build, `Godot.log` records engine output, and `Session.log` records actions, combat, per-turn decisions, tuning, and rewinds. Opening the project directly in Godot also creates a timestamped application session log.

Rendering uses Godot's Forward+ renderer with Direct3D 12 and 8× multisample anti-aliasing. Filled hexagons and unit arrows also blend across a one-pixel edge fringe to reduce the remaining stair steps during zooming and panning. The fringe follows physical screen pixels, keeping its width consistent across zoom levels and window scaling. Lines, circles, and dashed overlays enable their own edge smoothing. VSync is off and the frame rate is limited to 60 FPS.

## First experiment

1. Choose **Load world** and a focused experiment. Read its description, then select a unit with **Inspect**. The inspector explains its current intention and heat; its **Behavior** tab shows energy spending, submitted actions, resolution results, scores, remembered contacts, and recent decisions.
2. Press **Step** to advance one turn, **+10 turns** for a short batch, or **Run simulation** to watch continuously. Choose one to eight turns per second. **Show intentions** marks remembered contacts with their sighting turn, destinations, and protective bonds. **Dim unseen enemies** shows the selected unit's limited information while preserving the spectator's view.
3. Open the inspector's **Tuning** tab and choose an **Automaton program**, set **Spacing** for either spacing program, or purchase **Extra sight range**. Other preferences include **Aggression**, **Caution**, **Commitment**, **Heat reserve**, or **Remember contacts**. **Protect a particular ally** sets one directed bond. Heat reserve appears only for heat-producing weapons; inert training targets have no policy controls. Editing pauses the simulation. The **Unit** tab contains physical statistics and placement controls.
4. Press **Rewind with tuning** to restore the checkpoint's world and automaton memory while keeping the selected programs, edited preferences, bonds, and mechanism switches. A changed program starts with fresh memory. Run to the same turn and compare health, shots, and intention changes with the previous run. **Exact rewind** also restores the checkpoint's settings. **Checkpoint** replaces the comparison's starting point.
5. Open **Edit in World creator** to paint terrain, place source-defined units, or erase entities. Placement previews become red where a footprint cannot fit. Save a named world before replacing it.

The **Weapon heat** and **Protective bonds** switches isolate mechanisms. Vision limits and sensing costs are always enforced. Turn one off and rewind with tuning to compare the same starting state. The comparisons are descriptive: fewer shots or more survivors need not mean a more interesting automaton.

| Focused experiment   | Observe                                                                      |
| -------------------- | ---------------------------------------------------------------------------- |
| Heat and readiness   | Identical walkers space shots or accept lockouts against inert targets.      |
| Lost in the forest   | A pursuer investigates the last sighting of a fast, injured Traveler.        |
| Bonds under pressure | Changing a Bastion's directed bond changes which vulnerable ally it assists. |
| Lone wolf            | Upper artillery separates from allies; lower Standard artillery attacks.     |
| Keep your distance   | Upper artillery separates from enemies before firing; lower Standard fires.  |
| Hunt the weakest     | Upper artillery pursues lower health percentage; lower Standard fires near.  |

The five-faction encounter uses a fixed, source-defined map of plains, forest, mountains, and water, preserving its original terrain and starting clearings. It has two Bastions, five Traveler outriders and a mobile H.O.M.E., two siege walkers, eight clone infantry and two artillery units, seven Prytu hunters and a manifestation.

| Control           | Action                                                        |
| ----------------- | ------------------------------------------------------------- |
| Middle mouse drag | Pan                                                           |
| Mouse wheel       | Zoom around the pointer                                       |
| Left click / drag | Use the selected world tool                                   |
| Right click       | Switch to Inspect and select a unit                           |
| Space             | Run / pause in World mode                                     |
| N                 | Pause and advance one turn in World mode                      |
| B                 | Pause and advance ten turns in World mode                     |
| R                 | Rotate selected unit in Inspect, or facing in Place unit mode |
| F                 | Fit the current world                                         |
| Delete            | Remove selected entity in World creator's Inspect mode        |
| Escape            | Return to main menu                                           |

Shortcuts, including Escape, are suspended while typing in a text or number field. Rotation is available only in World creator. The sidebar lists shortcuts for the active mode and tool. The unit inspector appears in World mode and the creator's Inspect mode; the faction legend remains available on every inspector tab. The creator footer shows cell and unit counts; World mode also shows casualties. Sidebars scroll independently. The window can be resized down to 1100 × 700.

The cell readout follows the pointer and refreshes when terrain or the world changes. Leaving the board clears the readout and placement preview and ends any paint or pan drag.

## World creator and storage

The World creator's **File** mode contains **Save**, **Load**, **Play this world**, and **Create world**. Create either a rectangle from width and height, or a hexagon from size. Positive sizes are required; maps are limited to 20,000 cells. A size-one hexagonal map has one cell. Rectangular coordinates begin at the lower left, with positive X rightward and positive Y upward. Odd rows are offset east. Hexagonal maps are centered at coordinate (0, 0), so they also use negative coordinates.

**Create world** creates an empty map of plains for manual authoring. The five terrain types are forest (green), plains (ochre), mountains (gray), water (blue), and exclusion zone (purple). Each is shown only by its unique fill color, with no terrain symbols. All five can be painted. Painting incompatible terrain under a unit is rejected.

**Checkpoint** stores the current state in memory; **Exact rewind** restores it. **Rewind with tuning** restores the checkpoint while retaining the latest preferences and bonds for its units, including units lost during the run, along with the mechanism switches. Successful terrain, placement, rotation, removal, and deployment edits in World creator at turn zero also refresh the starting checkpoint. Rejected or unchanged actions leave it intact. Edits after turn zero leave the checkpoint intact until **Checkpoint** is pressed. Preference and mechanism changes remain available for comparison through **Rewind with tuning**.

**Save** uses the name in the world field. Names already saved replace that in-memory snapshot (case-insensitively). **Load** opens the world browser and returns the selected world to World creator. The browser refreshes its saved-world list whenever opened. Creating or loading a world replaces the workspace and its checkpoint, so save the current world first to retain it. Returning to the main menu pauses the simulation and retains the current map.

- Session snapshots in RAM: map terrain, living entities, faction, facing, health, heat, weapon lock, directed bonds, turn, casualty count, experiment switches, concrete unit instances, automaton settings and memory, and deterministic random state.

Saving retains an independent snapshot in RAM. Loading restores a fresh copy, so edits and simulation do not modify the saved snapshot. Saved worlds and checkpoints disappear when the application closes. Existing files in `UserContent` are ignored. Diagnostic logs remain on disk under `MyLogOutput` and are never used to restore state.

## Source-defined units and automata

Start with [AutomatonProgramming.md](AutomatonProgramming.md). It explains the complete program-facing contract, energy costs, resolution rules, and a small new-program example.

An automaton is **Sense → Remember → Think → Act**. A body supplies immutable statistics; each placed body owns a separately selectable program and private memory. Standard, Lone wolf, Keep your distance, and Hunt the weakest run through the same interface. The inert training target uses Hold. Unit definitions are in `Source/Simulation/Units`; programs are in `Source/AutomatonPrograms`.

`AutomatonPrograms` references only the separate `AutomatonContract` assembly. It cannot directly reference `World`, `Entity`, `Unit`, Godot, or the host's collision/attack implementation. Programs receive an `IAutomatonSystemCalls` capability for one turn. Vision costs energy, including repeat calls; extra range costs more. The terrain survey also costs energy and contains no occupancy. Observations are detached values and read-only collections. Forest still conceals units and blocks vision. There is no unlimited-perception bypass.

Every living automaton submits its complete action list against the same physical snapshot before world systems execute anything. There is no mid-turn resensing after an action. Programs predict their own actions using observed data; the world resolves actual movement, competing destinations, collision, attacks, damage, and heat. The next paid scan includes the previous turn's action outcomes. The Behavior inspector displays sensing receipts, submitted requests, remaining energy, outcomes, memory, and explanations.

Standard programs score engagement, withdrawal, investigation, patrol, recovery, and escort. Body defaults retain different preferences. The special programs impose hard priorities before optional standard behavior. Distance means the nearest occupied footprint cells: adjacent footprints are distance 1. Spacing considers observed units; unseen units and other units' later movement can prevent the desired separation from being achieved.

Saves and checkpoints copy the concrete program, settings, private memory, physical world state, reports, and random state. Changing programs starts fresh memory while keeping compatible settings. Rewind with tuning keeps the selected program; it restores checkpoint memory if that program is unchanged and starts fresh memory if changed. Exact rewind restores the original program as well. Copies exist only in the current application session.

To add a body, derive from `Unit`, supply `UnitStatistics`, implement `CreateFresh()`, assign a default program, and register it in `Catalog.Units()`. To add behavior, create a program in `AutomatonPrograms` and register it in `AutomatonCatalog`. No change to world resolution or other programs is needed. See the programming guide for custom persistent fields and their copy hook.

`AutomatonBehaviorExploration.md` is the earlier research proposal. Its old-implementation descriptions are historical; proposed systems are not promises of implemented features.

Movement domains:

- Ground: forest and plains; water, mountains, and exclusion zones are blocked.
- Amphibious: forest, plains, and water; mountains and exclusion zones are blocked.
- Flight: all terrain except exclusion zones.
- Spaceflight: all terrain except exclusion zones.

## Simulation rules

These are explicit prototype defaults for the combat mechanics left open by the specification. They can be changed in the C# simulation and unit classes.

- Every turn replenishes each unit's turn energy. Sensing and actions share this pool; unused energy expires.
- A 60-degree turn costs one energy. Units move only forward, spending one energy per cell, or two on forest for ground units.
- Infantry-and-artillery faction units cross rough terrain for one energy and take two health damage. Other movement domains ignore the ground movement surcharge.
- An attack costs two energy, with at most one attack per unit per turn. Adjacent targets take melee damage; more distant targets take ranged damage. Range is measured between occupied footprints.
- Attacks cover the facing direction and its two neighboring directions. Front armor is full strength, front-side armor is two-thirds strength, and rear-side/rear armor is one-quarter strength. Hits always deal at least one damage.
- Evasion is a deterministic seeded chance to avoid a hit. Ranged blast attacks damage every entity in the impact radius, including allies and potentially the attacker. Melee attacks do not splash.
- Weapons with a nonzero `HeatPerShot` accumulate heat on each shot, including misses. At 100 heat the weapon locks; it unlocks at 40 or below. All units cool once at the beginning of a turn. All terrain uses the source-defined cooling rate. A disabled heat experiment preserves stored heat while bypassing its restrictions and updates.
- Automata score engaging, withdrawing, investigating, patrolling, and escorting according to their unit's candidates and preferences. Heat-capable machines can recover. Commitment prevents small score changes from replacing a recent intention; urgent recovery overrides it. Travelers and artillery prefer distance; clones and Prytu favor weakened enemies; artillery considers allied blast exposure.
- Automata search routes around blocked terrain and occupied footprints. Searches are bounded to 3,000 expanded positions per decision. A route too complex for this bound causes the unit to wait and retry next turn.
- Programs plan together. Resolution runs one action slot at a time. All conflicting destination requests are rejected; swaps are blocked. Other actions use a rotating unit-ID order. Factions are mutually hostile. The simulation keeps running after only one faction remains.
- **Deploy / hold position** anchors a selected unit and removes its evasion until **Mobilize unit** is pressed. This provides the H.O.M.E. mobile/stationary toggle without changing its footprint.

The theme and named factions are represented through colors, geometric symbols and behavior. No artwork is used. This remains a combat-oriented laboratory: reproduction, trading, Traveler sickness, environmental sensing errors, diplomacy, learned policies, magic subsystems, resource production, and programmable in-game automata graphs are not implemented. The faction prose in the specification provides direction for future systems. Automata and definitions are ordinary editable C# code today.

## Verify and extend

From this folder in PowerShell:

```powershell
.\Run.ps1 -Verify -BuildOnly
.\Run.ps1 -Verify -VerifyInterface
```

The first command builds and runs dependency-free simulation checks. The second also launches the real Godot renderer, exercises the interface, captures views under the session log folder, and exits. Its authored test content is isolated inside that log folder.

Verification covers coordinates, footprints, terrain, combat, assembly dependencies, paid sensing and exhaustion, extended vision, observation isolation, shared snapshots, action energy, attack authorization, competing destinations, hidden blockers, fault isolation, previous outcomes, all three special policies, heat, bonds, private memory, deterministic replay, and a 120-turn five-faction encounter. Interface checks exercise program selection, spacing limits, tuned/exact rewinds, sensing traces, all six scenarios, editor controls, session saving/loading, and minimum-window layout.

`Source/AutomatonContract` is the complete public interface. `Source/AutomatonPrograms` contains programs and optional user-space planning helpers. `Source/Simulation` owns physical state and resolution. `Source/Interface` contains the Godot renderer and world creator. `Tests` compiles the simulation without Godot or an external test framework. The experiment does not reference the main repository application.
