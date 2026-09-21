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

