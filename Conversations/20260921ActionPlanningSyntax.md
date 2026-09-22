# Action Planning Syntax

## User

```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping\Source\AutomatonPrograms\ActionPlanning.cs:5:1-5:103
```
explain this syntax

## Assistant

I’ll read the referenced line and explain how its syntax works.

## Assistant

This declares a class with a **primary constructor**, a feature introduced in C# 12:

```csharp
public sealed class ActionPlanning(VisionObservation vision, TerrainObservation terrain, int energy) {
```

- `public`: other code can access the class.
- `sealed`: other classes cannot inherit from it.
- `class ActionPlanning`: declares a class named `ActionPlanning`.
- `(VisionObservation vision, TerrainObservation terrain, int energy)`: declares the constructor’s three parameters directly beside the class name. Each parameter is written as `Type name`.
- `{`: begins the class body.

You create an instance by supplying those three arguments:

```csharp
var plan = new ActionPlanning(vision, terrain, 100);
```

The constructor parameters are available inside the class. For example, your code uses two of them to initialize properties:

```csharp
public EntityObservation Self { get; private set; } = vision.World.Self;
public int Energy { get; private set; } = energy;
```

That initialization is similar to writing a traditional constructor:

```csharp
public ActionPlanning(VisionObservation vision, TerrainObservation terrain, int energy)
{
    Self = vision.World.Self;
    Energy = energy;
}
```

**The parameters do not automatically become public properties.** When methods use `vision` or `terrain` later, the compiler retains those values in hidden instance storage. With a traditional constructor, you would normally store them in fields yourself.
