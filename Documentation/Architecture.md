# Architecture

Automapolis has one mandatory game engine, the **Kernel**, and any number of optional **Shells**.

```text
keyboard / mouse / file / bot / network
                    |
        +-----------v------------+
        | optional Shell         |  renders snapshots; sends commands
        | Godot, terminal, etc.  |
        +-----------+------------+
                    | JSON Lines (one request, one response)
        +-----------v------------+
        | Kernel Host            |  transport adapter only
        +-----------+------------+
                    | C# calls
        +-----------v------------+
        | mandatory Kernel       |  state, rules, turns, lore vocabulary
        | .NET 10 / C# 14        |
        +------------------------+
```

## Shell terminology

- **Shell**, unqualified, means the main Shell: the 3D Godot presentation using
  SimplePaint and the required orthographic 3/4 overhead camera.
- **Text shell** means a terminal-only input/output Shell.
- **2d shell** means the current Godot 4.7.2 .NET / C# presentation with symbolic
  visuals, located in `Shells/SimplePaint3DShell`. It is distinct from the main Shell.
- Future Shells may include a pixel-art 2D Shell, a non-pixel-art 2D Shell, and
  3D Shells with other visual styles. These are possibilities, not implemented
  presentations or selected production directions.

The main Shell is the intended primary presentation; its 3D production model
pipeline is not yet implemented. Generic references to the Shell architecture or
multiple Shells still describe the shared presentation/input role below.

## Boundary rules

- `Automapolis.Kernel` references only the .NET base class library. It cannot know Godot exists.
- A Shell never implements game rules. It sends `WorldCommand` equivalents and renders the returned `WorldSnapshot`.
- Time cannot advance in the background. Exactly one `AdvanceTurn` command resolves exactly one autonomous war turn.
- There is one player configuration. Field interventions are always available;
  submitting only `AdvanceTurn` provides passive play without a separate mode.
- The JSON Lines host is replaceable transport, not a second engine.
- Every authoritative visual token is text: terrains and forces have glyphs, descriptions, names, metrics, intents, and dispatches. A Shell may add layout, color, borders, models, and animation without hiding rules in assets.

## Simulation model

The Kernel generates resonance, biomass, and integrity for each sector; a human enclave, soldiers, and exactly one Bastion; and an opposing mix of raveners and brood nodes. On each explicit turn, alien terrain spreads, enclaves grow or suffer, both sides maneuver, co-located forces fight, brood nodes spawn organisms, and a deterministic dispatch records the new state.

## Command protocol

Write one JSON object per line to standard input. Read one response object per line from standard output. A response contains `ok`, `type`, `message`, `snapshot`, and the reference `text` rendering.

```json
{"command":"new","width":16,"height":12,"seed":475023,"name":"The Bastion Front"}
{"command":"advance"}
{"command":"channel","x":5,"y":3,"amount":25}
{"command":"fortify","x":5,"y":3,"terrain":"fortifiedReach"}
{"command":"deploy","x":5,"y":3,"kind":"soldier"}
{"command":"establish","x":5,"y":3,"name":"Vigil Annex"}
{"command":"purge","x":5,"y":3,"radius":1}
```

Enum input is case-insensitive. Invalid commands return an error response without ending the host process.

## Hex spatial contract

Snapshots declare `topology: "hexagonal"` and `coordinates: "oddRowOffset"`.
All command and snapshot `x,y` positions mean zero-based column and row, with
odd rows shifted right by half a hex. Width and height bound the offset array.
`HexGrid` owns six-neighbor adjacency and hex-step distance in the Kernel.
Shells map these coordinates to pointy-top hex centers for presentation;
text output indents odd rows. Renderers do not determine simulation adjacency.

The implementation still uses single-hex forces and autonomous movement, and
permits forces to share a hex, including with enclaves and brood nodes. This
prototype co-location does not represent entering a building.

[Mechanics](Mechanics.md) defines the design for future spatial implementation:
units use complete-ring hex footprints, six facings, and paid translation and
rotation, with no swept collision checks during rotation. Buildings are
stationary and non-enterable, with arbitrary footprints composed of base hexes.
These footprint and facing systems are not yet implemented; their remaining
open questions are listed in Mechanics.
