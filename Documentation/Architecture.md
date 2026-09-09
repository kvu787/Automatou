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
- **2d shell** means the currently implemented Godot presentation with symbolic
  visuals, located in `Shells/Godot`. It is distinct from the main Shell.
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
- Witness mode accepts only `AdvanceTurn`. Command mode additionally exposes field interventions.
- The JSON Lines host is replaceable transport, not a second engine.
- Every authoritative visual token is text: terrains and forces have glyphs, descriptions, names, metrics, intents, and dispatches. A Shell may add layout, color, borders, models, and animation without hiding rules in assets.

## Simulation model

The Kernel generates resonance, biomass, and integrity for each sector; a human enclave, soldiers, and exactly one Bastion; and an opposing mix of raveners and brood nodes. On each explicit turn, alien terrain spreads, enclaves grow or suffer, both sides maneuver, co-located forces fight, brood nodes spawn organisms, and a deterministic dispatch records the new state.

## Command protocol

Write one JSON object per line to standard input. Read one response object per line from standard output. A response contains `ok`, `type`, `message`, `snapshot`, and the reference `text` rendering.

```json
{"command":"new","width":16,"height":12,"seed":475023,"mode":"command","name":"The Bastion Front"}
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

The implementation still uses single-hex forces and autonomous movement.
Six-direction facing, paid turning, and variable footprints are future systems.
