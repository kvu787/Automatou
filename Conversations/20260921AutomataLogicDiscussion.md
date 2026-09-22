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

## User

before we continue this discussion, remove all usage of "brain" in C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping. this is misleading terminology that should be replaced with with "automaton" or terms involving "automatons" 

## Assistant

I’ll replace “brain” throughout the prototype with automaton terminology, including code identifiers, comments, documentation, and test descriptions, then build and verify it.

## User

before we continue this discussion, remove all usage of "brain" in C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping. this is misleading terminology that should be replaced with with "automaton" or terms involving "automaton"

## Assistant

I’ll use `AutomatonInstance` for the unit property, since `Automaton` is already the nested class name, and `automaton` for local variables and prose. I’ll also update occurrences in local text logs and saved source copies under that folder.

## Assistant

Replaced the terminology throughout the prototype’s code, documentation, tests, and local text logs. The property is now `AutomatonInstance`; local variables use `automaton`.

No remaining text matches for “brain” in that folder. Build succeeded and all 35 verification groups passed. Changes committed; behavior is unchanged.

## User

returning to our discussion:

an AT program should be kind of like a user program that runs on an OS that uses a user/kernel boundary abstraction. for example, for a bastion AT program, i should only be allowed to read world data by calling specific methods that encode limitations. for a bastion, it should be able to see a fairly long distance by default, and it should be able to use extra energy to see farther. so, the vision "system call" should reflect that.

another way this system call architecture should work is that all sense system calls should use some of the unit's turn energy. this facilitates a natural way of programming ATs in which the programmer needs to carefully manage the unit's energy and cannot spam sense system calls to gain complete information about the world.

list existing work to look into that relates to this kind of AI architecture

## Assistant

I’ll look for architectures that combine restricted world access, energy-budgeted sensing, and action requests resolved by the simulation, with an emphasis on programmable-agent games and active-perception research.

## Assistant

Grobots looks especially close to your energy-costed sensing idea. Battlecode is a useful reference for restricted APIs and metered calls, while the influence/reaction model directly addresses your separation between requested actions and world resolution. I’m checking the differences so these don’t get presented as exact matches.

## Assistant

**Grobots and Battlecode are the closest game references I found.** Your proposal also connects directly to *active sensing*, *capability-based isolation*, and the *influence/reaction* model of simulation. These cover different parts of the architecture.

1. **Grobots — energy-costed sensors and programmable units.**  
   Robots have configurable hardware and programs. Sensors detect particular categories of objects within limited range, and firing them consumes energy; longer-range sensors cost more to operate. Computation also consumes energy. This is a particularly close precedent for making information acquisition compete with other uses of a unit’s resources. Its documented sensor range is a hardware parameter, so it is not an exact match for your per-call vision boost. Start with the [hardware documentation](https://grobots.sourceforge.net/docs/hardware.html) and [programming interface](https://grobots.sourceforge.net/docs/hw-interface.html).

2. **MIT Battlecode — restricted world APIs with metered calls.**  
   Each robot runs its own program and accesses its surroundings through methods such as `senseNearbyRobots`. In the 2020 rules, sensing range varies by unit type and environmental conditions; sensing functions have fixed bytecode costs against a per-turn computation budget. **Study its API boundary, cost schedule, and treatment of exhausted budgets.** The important difference is that bytecodes measure computation, while physical actions use a separate cooldown mechanism; it does not directly implement your shared turn-energy pool. [Battlecode 2020 specification](https://github.com/battlecode/battlecode20/blob/master/specs/specs.md).

3. **Screeps — collect requests, then resolve the world.**  
   Programs operate against an unchanged start-of-tick state. Commands accumulate, and the engine resolves them afterward—including conflicts where multiple units request the same destination. This closely matches your world-system boundary. It also illustrates a consequence worth deciding explicitly: requesting movement does not change the position used by another command in that tick. **Study its action semantics and conflict resolution**, rather than treating its player-level CPU allowance as per-unit sensing energy. [Game loop](https://docs.screeps.com/game-loop.html), [CPU limits](https://docs.screeps.com/cpu-limit.html).

4. **Robocode / Tank Royale — sensing as something a program actively controls.**  
   A bot directs its radar; detections depend on range and the arc swept. Enemy updates arrive when scanned, and programs retain previous observations between scans. **Study the gameplay produced by choosing where and when to look**, particularly tracking one opponent versus searching for others. This is a reference for constrained attention and memory, not an exact energy-per-query model. [Radar tutorial](https://robocode.dev/tutorial/getting-started.html).

5. **Active sensing and POMDP research — deciding whether information is worth its cost.**  
   *Active Sensing for Partially Observable Markov Decision Processes* models sensor selection according to its longer-term effect on decisions. *Sensor Scheduling for Energy-Efficient Target Tracking* explicitly trades tracking quality against energy consumption. These address your Bastion’s question: **“Will spending more energy to see farther improve my decision enough to justify it?”** They provide design concepts even if players write simple rules rather than probabilistic planners. [Koltunova’s thesis](https://uwspace.uwaterloo.ca/items/6b741509-4931-41f1-b486-89164f27e164), [Atia, Veeravalli, and Fuemmeler’s paper](https://arxiv.org/abs/1009.2997).

6. **Ferber and Müller’s influence/reaction model — formal separation of attempts from outcomes.**  
   Agents produce *influences*; the environment combines them under world laws to determine the resulting state. It explicitly addresses simultaneous interacting actions. **This is the strongest conceptual reference for your “AT requests; world resolves” distinction.** [*Influences and Reaction: a Model of Situated Multiagent Systems*](https://cdn.aaai.org/ICMAS/1996/ICMAS96-009.pdf).

7. **WebAssembly / Wasmtime — enforcing the user/kernel analogy.**  
   WebAssembly provides isolated program memory and explicit imported functionality. A host can expose only the permitted sensing and action operations; Wasmtime also supports execution limits through fuel. **Study this for enforceable encapsulation when running player programs.** Your game would still implement sensing permissions and energy charges inside those host calls; runtime fuel does not automatically represent gameplay energy. [Sandbox architecture](https://docs.wasmtime.dev/security.html), [fuel example](https://docs.wasmtime.dev/examples-interrupting-wasm.html).

I’d start with **Grobots → Battlecode → Screeps**. One design distinction I would preserve is **gameplay energy versus execution budget**: sensing spends the unit’s energy, while a separate execution limit bounds loops and computation that never call a sensing method.
