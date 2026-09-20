# Explain Unit Automata Logic

## User

```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping\Source\Simulation\World.cs:125:1-186:6
```

is this the entirety of the unit automata logic?

## Assistant

I’ll inspect that section and the related unit behavior to see which logic it covers.

## Assistant

**Yes for the core decision-making; no for all supporting mechanics.** In this prototype, the selected [Step() method](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/World.cs:125) contains the entire shared automaton decision loop: target selection, attacking, retreating, turning, moving, and spending action points.

The five behaviors are small variations within that loop:

- **Advance:** default approach-and-attack behavior.
- **Hold:** stops pursuing when the selected target is out of range.
- **Skirmish / Artillery:** use the same retreat rule when too close.
- **Swarm:** biases target selection toward injured enemies.

Supporting logic sits elsewhere in the same file: [pathfinding](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/World.cs:187), [combat resolution](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/World.cs:93), and [occupancy / terrain restrictions](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/World.cs:49). Unit stats and behavior choices are defined in [Definitions.cs](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Definitions.cs:8).

There are no separate per-unit automaton implementations in this prototype.
