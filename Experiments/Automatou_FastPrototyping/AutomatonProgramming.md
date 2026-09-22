# Writing an automaton

An automaton is **Sense → Remember → Think → Act**. Its inputs are purchased observations and its own memory. Its output is a list of requests. The world decides what actually happens.

## Start here

1. Read [`IAutomatonSystemCalls` and `UnitAutomaton`](Source/AutomatonContract/SystemCalls.cs): the entire execution contract and the default four-stage lifecycle.
2. Read [`LoneWolfAutomaton`](Source/AutomatonPrograms/LoneWolfAutomaton.cs) or [`HuntTheWeakestAutomaton`](Source/AutomatonPrograms/HuntTheWeakestAutomaton.cs): complete, small examples of hard priorities.
3. Read [`ActionPlanning`](Source/AutomatonPrograms/ActionPlanning.cs) only if you want the optional movement and attack helpers. It predicts actions using purchased data; it does not call into the world.
4. Read [`BehaviorMemory`](Source/AutomatonContract/BehaviorMemory.cs) for settings, persistent state, and independent copying.

You do not need to read the resolver or any other automaton to write a program. The world rules below are the behavioral contract that programs can rely on.

## The boundary

```mermaid
flowchart LR
    Host[World host] --> Programs[Automaton programs]
    Host --> Contract[Public contract]
    Programs --> Contract
```

These arrows are assembly references. `AutomatonPrograms.csproj` references only `AutomatonContract.csproj`. `World`, `Entity`, `Unit`, Godot, and host implementations do not exist in its compile-time vocabulary. The host creates a one-turn `IAutomatonSystemCalls` capability, invokes the program, copies its returned action list, and closes the capability. A retained capability cannot sense in later turns.

Observations contain values and read-only collections, not live world objects or other automatons' memory. Statistics and action records are immutable. World-mechanic settings are detached copies. Other units' protective bonds are not exposed. Sensing operates on a frozen physical snapshot; memory from other programs is not part of that snapshot.

This is an architectural boundary for authored C# programs. It is not a security sandbox for hostile code: reflection, filesystem access, arbitrary threads, and an infinite loop require a separate runtime or process sandbox. Execution is currently synchronous. Built-in route searches and action lists are bounded; arbitrary C# execution time is not metered. Gameplay energy prices information and actions, not CPU instructions.

## System calls and energy

The body defines `TurnEnergy` and `SightRange`. Energy is replenished each turn; unused energy expires. Sensing and actions use the same pool.

| Operation                    | Cost                       | Result or constraint                                        |
| ---------------------------- | -------------------------- | ----------------------------------------------------------- |
| `RemainingEnergy`            | Free                       | Remaining budget only; no world information                 |
| `ScanVision(extraRange: 0)`  | 1                          | Self, observed entities, mechanics, previous action results |
| `ScanVision(extraRange: N)`  | 1 + N                      | Body sight range + N; N must be between 0 and 12            |
| `SurveyTerrain()`            | 1                          | Public map terrain; no occupancy or hidden-unit queries     |
| `TurnAction(-1 or +1)`       | 1                          | Rotate 60 degrees                                           |
| `MoveForwardAction()`        | 1, or 2 for ground forest  | Move the entire footprint one hex forward                   |
| `AttackAction(targetId)`     | 2                          | At most one executed attack per turn                        |

Infantry-and-artillery faction units move through forest for one energy and take two damage. Flight and spaceflight ignore the ground movement surcharge. Rejected and skipped actions cost no action energy. Sensing already paid for is never refunded.

Every successful sense call is charged, even if repeated. If a call is unaffordable it returns `null`, reveals nothing, and does not spend energy. Invalid range arguments throw; the host records program exceptions and discards that program's action list. Base vision plus terrain costs two, so the default lifecycle leaves `TurnEnergy - 2 - ExtraSightRange` for acting. A program may override `RunTurn` to skip terrain, use memory, or choose whether extra vision is worth its cost.

A Bastion has base sight range 14 and six energy. Its normal scan plus terrain leaves four energy. Adding two hexes of sight costs three for vision, leaving two after terrain. More sight does not defeat concealment: forest hides occupants farther than two hexes away and blocks sight through intermediate forest. Sight and attack distance use nearest footprint cells. Allies and enemies obey the same sight rules. The spectator still sees the whole map.

Vision is a current visible-entity scan. There are no sector scans, remote ally telemetry, pathfinding system calls, or hidden-target lookup calls. Terrain is deliberately public information in this prototype, purchased through its own call. A future sensor can extend the contract without exposing world internals.

## Four stages

- **Sense:** call the capability and decide what information is worth buying. The default lifecycle buys vision and terrain.
- **Remember:** persist only information the program has acquired. Default memory holds up to eight enemy contacts, expires unseen contacts after eight turns, and remembers previous action results. It never refreshes an invisible enemy's location.
- **Think:** compute against these observations and private memory. The default programs use projected position, facing, energy, and observed occupancy to plan. No world mutation occurs here.
- **Act:** submit one ordered, finite `ActionPlan`. These are intentions, not completed actions. The default implementation copies the list into read-only storage.

The default `Remember` hook runs as soon as vision succeeds, even if the later terrain survey is unaffordable. Programs can replace individual hooks or the complete `RunTurn` lifecycle. Reading existing memory and computing locally do not cost turn energy. Copying a previously purchased observation is not another sense call; it still describes its original turn.

## World resolution

1. Cool weapons and take one physical snapshot.
2. Run every living program against that snapshot. Sensing does not run actions. Each unit gets its own energy account and private program instance.
3. Copy and validate each submitted list, with a maximum of 32 requests. Close every capability.
4. Resolve request index 0 across units, then index 1, and so on. Remaining energy is enforced by the host regardless of a program's estimates.
5. Record accepted, rejected, and skipped results. The next paid vision call returns the previous turn's results.

Movement checks the physical map and full footprints. Within an action slot, all otherwise-valid requests whose destination footprints overlap are rejected. Both contenders stay. Occupancy at the start of the slot is reserved: swaps and movement into a cell being vacated in that slot are blocked. Programs may retry next turn. These rules live entirely in the world.

Nonconflicting actions, including attacks, resolve in deterministic unit-ID order rotated each turn. Attacks are not simultaneous: an earlier attack can destroy a later actor or target. The target must have been observed through paid vision that turn and must still satisfy visibility, range, facing, faction, heat, and energy rules at execution. A paid sight extension applies to that turn's attacks. Missing or moved targets do not cause automatic retargeting. Ranged splash can damage other units; choosing one target does not disable physical splash.

The first rejected request stops that unit's plan. Later requests are reported as skipped. Already executed actions and paid senses stay committed. This prevents an automaton from continuing a route under an assumption that its failed move succeeded. All feedback arrives next turn; there is no coroutine resuming after each action.

## Programs supplied

| Program            | Rule                                                                                                                          |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------- |
| Standard           | Score engagement, withdrawal, exploration, recovery, and escort using body defaults and preferences.                          |
| Lone wolf          | First reach minimum N distance from every observed ally. Once safe, standard planning cannot move inside N.                   |
| Keep your distance | Same hard priority for every observed enemy; only ranged bodies; `1 <= N < attack range`.                                     |
| Hunt the weakest   | Choose the visible enemy with the lowest current-health / maximum-health ratio. Only approach or attack it. Ties use unit ID. |

Spacing is distance between nearest occupied cells, not center-to-center distance. Adjacent footprints have distance 1, so N = 3 means two empty cells along their shortest separation. While crowded, a spacing program spends the whole turn separating or waiting, even if it reaches N with energy left. It cannot choose attacking instead. When already safe, every proposed movement step must keep N from each observed relevant unit. If full separation is unreachable, it tries to improve the minimum distance without crossing the starting separation floor for another relevant unit. Its route may take several turns; the affordable prefix can consist only of turning.

The guarantee is about priorities and requested movement against observations. Hidden units, world conflicts, and other units moving afterward can defeat the desired final distance. Programs do not receive omniscience or special collision privileges to enforce spacing.

Hunt the weakest ignores distance, commitment, and bonds when ranking enemies. It does not fall back to a healthier enemy if its chosen enemy is unreachable, disappears, or cannot be attacked. It re-evaluates visible enemies next turn. With no visible enemy it waits. Weapon readiness and heat policy can still prevent firing. Aggression controls that heat tolerance; caution, commitment, and bond controls are hidden for this program.

## Add a program

Create a class in `Source/AutomatonPrograms`, deriving from `UnitAutomaton`. This example uses the standard lifecycle and optional planning helper, but no other program:

```csharp
namespace Automatou.Simulation;

public sealed class NearestAutomaton : UnitAutomaton {
    public override string ProgramName => "Nearest";

    protected override UnitAutomaton CreateInstance() {
        return new NearestAutomaton();
    }

    protected override IReadOnlyList<UnitAction> Think(
        VisionObservation vision, TerrainObservation terrain, int energy) {
        ActionPlanning plan = new(vision, terrain, energy);
        EntityObservation? target = plan.Enemies
            .OrderBy(enemy => ActionPlanning.Distance(plan.Self, enemy))
            .ThenBy(enemy => enemy.Id).FirstOrDefault();
        this.TargetId = target?.Id;
        this.State.Intention = target is null ? "Wait" : "Engage nearest";
        this.State.Reason = target is null ? "No visible enemy." : $"Nearest is #{target.Id}.";
        if (target is not null) { plan.Engage(target); }
        return plan.Actions;
    }
}
```

Register its name and factory in `Source/Simulation/AutomatonCatalog.cs` to expose it in the inspector. The catalog is host wiring, not a policy switch inside the resolver. To make it a body's default, assign it in that unit's constructor. A program can also submit `new ActionPlan([new TurnAction(1)])` directly and omit all planning helpers.

Lasting custom data can live in private fields on your program. Override `CopyTo(UnitAutomaton copy)`, call `base.CopyTo(copy)`, then deep-copy your added mutable data into the matching program type. `CreateInstance` must return the same concrete program. This keeps rewind/save/load independent. The shared settings and memory are already copied. No other program or world system needs to understand your custom memory.

Use **Load world → Lone wolf / Keep your distance / Hunt the weakest** for paired comparisons: upper group special program, lower group Standard. Select the automaton, open **Tuning**, select its program, and adjust N or extra vision. Step and inspect energy receipts, submitted actions, and outcomes. Rewind with tuning preserves your selection; exact rewind restores the original program and memory.

Run `Run.ps1 -Verify -BuildOnly` for model checks and `Run.ps1 -Verify -VerifyInterface` for real rendered interface checks. Add a deterministic behavior test and a small scenario when introducing a new rule. The kernel files (`AutomatonKernel.cs`, `TurnResolution.cs`) are useful when changing world rules, but are not required reading to author a program.
