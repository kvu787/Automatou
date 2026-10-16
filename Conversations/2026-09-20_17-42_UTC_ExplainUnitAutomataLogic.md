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

## User

we need to rearchitect how units and their automata are defined:
- remove the unit creator entirely. for now, all unit stuff should live directly in the source code
- there should be a c# class for each unit type
- each unit's class must define their own automata class. the purpose of the automata class is basically the "brain" of the unit. each turn, it receives information about the world (sensing), executes logic on that, and then outputs the actions it wants to take (actuation). it also stores the "memory" of the unit, which can include anything that needs to be persisted beyond a single turn, such as longer term goals.

## Assistant

I’ll inspect the unit definitions, creator UI, and save format, then move units into source-defined classes with their own persistent brains and explicit sensing/action interfaces.

## Assistant

The current unit blueprints mix stats, behavior selection, and saved state. I’m replacing them with eight concrete unit classes, each containing its own automaton class and owning a separate brain instance.

The world will enforce action costs, movement, and combat rules. Brains will receive read-only observations and request actions, with fresh observations after each action. Their memory will persist across turns and world saves.

## Assistant

The new structure is in place, and the unit creator has been removed from the UI and startup loading. Each unit now has its own nested automaton and target memory; shared helpers handle reusable tactics.

I’m checking that saves restore the correct brain and memory, and that the world rejects invalid actions or actions beyond a unit’s budget.

## Assistant

The build and all 16 simulation test groups pass, including memory restoration, independent brains, fresh sensing, and action limits. The automated UI run also completed with the three remaining menu choices and source-defined unit placement.

I’m reviewing the captured UI and final diff before committing.

## Assistant

Implemented and committed.

- Removed the unit creator and unit blueprint loading.
- Added eight unit classes, each with its own nested automaton and independent memory.
- Brains sense the world and request actions; the world validates and executes them.
- Memory persists across turns, saves, and checkpoints.

See [TravelerOutrider.cs](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Units/TravelerOutrider.cs) for a complete example.

Verified: clean build, 16 simulation test groups, and automated UI checks. Old blueprint-based world saves are no longer supported.
