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
