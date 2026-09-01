# Architecture

Automapolis has one mandatory game engine, the **Kernel**, and any number of optional **Shells**.

```text
keyboard / mouse / file / bot / network
                    │
        ┌───────────▼───────────┐
        │ optional Shell        │  renders snapshots; sends commands
        │ Godot, terminal, etc. │
        └───────────┬───────────┘
                    │ JSON Lines (one request, one response)
        ┌───────────▼───────────┐
        │ Kernel Host           │  transport adapter only
        └───────────┬───────────┘
                    │ C# calls
        ┌───────────▼───────────┐
        │ mandatory Kernel      │  state, rules, turns, text vocabulary
        │ .NET 10 / C# 14       │
        └───────────────────────┘
```

## Boundary rules

- `Automapolis.Kernel` references only the .NET base class library. It cannot know Godot exists.
- A Shell never implements game rules. It sends `WorldCommand` equivalents and renders the returned `WorldSnapshot`.
- Time cannot advance in the background. Exactly one `AdvanceTurn` command resolves exactly one autonomous turn.
- Observer mode accepts only `AdvanceTurn`. Creator mode additionally exposes world-editing commands.
- The JSON Lines host is replaceable transport, not a second engine. A test, terminal, server, or another UI can call the Kernel directly or implement the same small protocol.
- Every authoritative visual token is text: terrain and beings have glyphs, descriptions, names, metrics, and chronicle entries. A Shell may add layout, color, borders, and animation without hiding information in graphical assets.

## Command protocol

Write one JSON object per line to standard input. Read one response object per line from standard output. A response contains `ok`, `type`, `message`, `snapshot`, and the reference `text` rendering.

```json
{"command":"new","width":16,"height":12,"seed":475023,"mode":"creator","name":"Automapolis"}
{"command":"advance"}
{"command":"infuse","x":5,"y":3,"amount":25}
{"command":"transmute","x":5,"y":3,"terrain":"crystalForest"}
{"command":"create_life","x":5,"y":3,"kind":"oracle"}
{"command":"found","x":5,"y":3,"name":"The Last Lantern"}
{"command":"cataclysm","x":5,"y":3,"radius":1}
```

Enum input is case-insensitive. Invalid commands return an error response without ending the host process.
