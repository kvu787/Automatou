# Automata Logic Discussion

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping

i want to get ramped up on the current automata logic as quickly as possible. we'll use this thread as a discussion and q&a

- provide an starter overview
- add comments to the relevant parts of the code

## Assistant

I’ll read the prototype’s automata logic, add comments where they help explain the behavior, and give you a short overview to anchor our Q&A.

## Assistant

The key distinction is between choosing an **intention** once per turn and issuing individual **actions** within that turn. Each unit type defines its own automaton, but most use shared scoring and execution helpers. I’m commenting that flow, including how memory, visibility, and action points affect what a unit actually does.

## Assistant

The comments are in place, and the build plus all 35 simulation verification groups passed. Two useful details for our Q&A: commitment can keep an intention even when another scores slightly higher, and spotting an enemy during patrol stops movement but defers the new intention choice until the next turn.

## Assistant

**Each unit has its own brain, which chooses an intention once per turn and then requests actions to carry it out.** Most brains currently share the same planning helpers, with different preferences and available intentions.

The flow is:

1. **Sense and remember.** Read visible entities and update remembered enemy sightings. Terrain is globally known; hidden enemies’ current positions are unavailable.
2. **Select a target.** Rank visible enemies by distance, with optional bonuses for wounded enemies and the previous target, or penalties for allied blast exposure.
3. **Choose an intention.** Score Engage, Withdraw, Escort, Recover, or exploration options. The highest score generally wins; commitment can preserve a recent choice.
4. **Request actions.** Translate that intention into turning, moving forward, or attacking. Observe again after each action, while keeping the same intention for the turn.
5. **Resolve actions.** The world validates requests and spends action points. Turning costs 1, movement costs 1–2, and attacking costs 2 with at most one attack per turn. A rejected request ends that unit’s turn.

**For the quickest code tour, read these in order:**

- [CloneInfantry.Act](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/Units/CloneInfantry.cs:16): a compact example of the complete decision pipeline.
- [BehaviorPlanning.Choose](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/BehaviorPlanning.cs:165) and [Execute](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/BehaviorPlanning.cs:206): intention selection and action generation.
- [World.Step](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/World.cs:170): turn order and action enforcement.
- [AutomatonMemory](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/BehaviorMemory.cs:27): what persists between turns and through rewinds.

The main personality differences: Travelers favor spacing and withdrawal; clones and Prytu favor wounded targets; artillery accounts for friendly blast damage; siege walkers can wait to cool. Without visible enemies, mobile units investigate remembered contacts or patrol.

To see the reasoning live, select a unit and open **Inspect → Behavior**, then **Step**. It shows candidate scores, the chosen intention, contacts, and recent decisions.

Added explanatory comments across the relevant code and committed them, with this discussion recorded separately. Behavior is unchanged. Build passed with no warnings; all 35 simulation verification groups passed.

## User

the mental model for automata that i want to express in this game for myself and players is "automata = sense, remember, think, act". the concept of an automaton should be well-encapsulated by in that structure. i want to strongly enforce this encapsulation so that players can design and tune new automata without having to reason about how every other automata and system works (i.e. avoid a combinatorial explosion in required understanding).

separate from the automata are the world systems. for example, for a given turn, each AT will submit a list of actions they want to perform. the world systems are in charge of resolving and executing those actions. for example, if two different units want to move into the same space, the logic for resolving what happens will be in a world system.

## Assistant

I’ll record this as the architectural model for our discussion and spell out where the current code differs.

## Assistant

**The automaton’s boundary should be: observations and private memory in; updated memory and requested actions out.** Everything that determines what actually happens belongs to world systems.

Within that boundary, your four stages have clear responsibilities:

- **Sense:** receive the information this unit is allowed to know. World systems determine visibility; the automaton receives observations without access to the mutable world.
- **Remember:** update its own memory from observations and previous action outcomes. An unseen enemy’s remembered location remains a past sighting.
- **Think:** use those observations and memories to choose what it wants to do. Target selection, intentions, and preferences are internal implementation choices.
- **Act:** submit an ordered list of requested actions. Submission expresses intent; it does not move the unit or guarantee success.

The world’s turn structure would then be:

```text
World snapshot + previous outcomes
                 ↓
Each automaton: Sense → Remember → Think → Act
                 ↓
Collect every unit’s requested actions
                 ↓
World systems resolve and execute them
                 ↓
Updated world + action outcomes for the next turn
```

For your movement example, both automata may request the same destination. Neither needs to know the other’s decision logic. The movement system decides the outcome under a shared rule and reports the result. That rule could use priority, block both moves, or something else; it can be chosen separately from the automata design.

**Encapsulation means players learn one stable contract:** what they can observe, what memory they own, which actions they can request, and which outcomes they can receive. They still need to understand the game’s movement and combat rules, but not the implementations of other brains or world systems. Shared planning helpers should be optional tools behind that boundary.

**The current implementation only partially fits this.** It already gives each unit private brain memory, restricted sensing, and action requests that the world validates. But `Act()` currently yields one action at a time, the world immediately executes it, and the brain can sense again before yielding another. Units take their turns sequentially. It does **not** collect everyone’s action lists before resolution.

Your model therefore implies a meaningful change: separate the decision phase from the resolution phase. A submitted action can fail, and later actions in its list may then become impossible; world systems need a defined policy for that and explicit outcome feedback. The automaton should never treat a requested action as an accomplished fact.

