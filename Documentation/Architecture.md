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

The kernel must not be coupled to any particular shell.
