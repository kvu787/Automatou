# Explain JsonIgnore

## User

what are these jsonignore tags for?
```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping\Source\Simulation\Units\Bastion.cs:7:1-8:21
```

## Assistant

I’ll check those attributes and how this project uses JSON serialization.

## Assistant

`[JsonIgnore]` tells the JSON serializer to **skip that property when saving or loading JSON**. It doesn’t affect normal gameplay access to the property.

In [Bastion.cs](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Units/Bastion.cs:7):

- **`Statistics`** is ignored because its values are defined in source code. Creating a Bastion during loading initializes those values automatically; the save doesn’t need to contain them.
- **`Brain`** is ignored because it simply returns `Memory`. Saving both would duplicate the same brain data.
- **`Memory`** is **not ignored**, so its `TurnsObserved` and `TargetId` are saved and restored.

This keeps fixed unit definitions in code while preserving the automaton’s changing memory in saves.

## User

where is the save/load logic

## Assistant

I’ll locate the save/load methods and where the UI calls them.

## Assistant

The core logic is in [Storage.cs](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Storage.cs:7):

- **[Encode, line 19](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Storage.cs:19):** converts the world to JSON.
- **[Decode, line 24](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Storage.cs:24):** reads JSON, validates it, and reconstructs the world.
- **[SaveWorld / LoadWorld, lines 42–43](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Storage.cs:42):** write/read the actual file.

The UI calls these from [Creators.cs:42](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Interface/Creators.cs:42) to save, and [Navigation.cs:66](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Interface/Navigation.cs:66) to load.

Unit properties and automaton memory are handled automatically by `System.Text.Json` within `Encode` and `Decode`; there’s no separate Bastion-specific save/load method.
