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

## User

i notice that your list doesn't include any majorly popular video games. i am curious about this, because this architecture seems like the most natural way to write AIs for a wide variety of games. however, i only have limited experience as a game developer, so i may be missing or misunderstanding some key things. explain

## Assistant

I’ll check how major games handle AI perception and action requests, then separate those common patterns from the stricter, energy-metered interface you’re proposing.

## Assistant

Halo 2 and Left 4 Dead both document substantial parts of the structure you described. My earlier list leaned toward games where players write the programs, which made the general architecture look less common than it is. The key distinction is between separating perception, memory, decisions, and execution—and making every observation an explicitly paid gameplay operation.

## Assistant

**Your core intuition is sound: separating sensing, memory, decisions, and execution is well established in major games.** My earlier list overemphasized matches for *player-programmable agents with metered sensing*. That made the broader architecture look less common than it is.

You are proposing several related choices:

- **Modularity:** perception, memory, decision-making, and world execution have distinct responsibilities.
- **Information restrictions:** an AT receives only observations it is allowed to obtain.
- **Enforced isolation:** its program cannot bypass that interface.
- **An information economy:** obtaining observations spends resources that could otherwise support actions.

The first two have clear mainstream precedents. The last two explain why programming games were prominent in my list.

**Major games do use much of this structure.**

In **Halo 2**, each actor maintains representations of perceived objects, including last-seen information and records of previous interactions. These representations are distinct from actual world state and filtered by perception. Decisions can therefore rely on outdated or mistaken beliefs. That is very close to your “sense, remember, think” model. Damian Isla’s account also discusses the difficulty of keeping behavior configuration understandable as the system grows—your encapsulation concern. [Halo 2 architecture, by its developer](https://www.gamedeveloper.com/programming/gdc-2005-proceeding-handling-complexity-in-the-i-halo-2-i-ai).

**Left 4 Dead** separates Vision, Intention, Locomotion, and Body components. Vision owns visibility queries and recognized entities; Intention manages behaviors; Locomotion owns movement and collision resolution. This substantially resembles your separation between decisions and execution. It is not evidence that every observation costs gameplay energy or that each agent runs behind a security boundary. [Valve’s architecture presentation, slide 37](https://cdn.akamai.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf#page=38).

**Unreal Engine** provides perception components with sight and hearing limits, remembered stimuli, and update events that can drive decision logic. Its sight implementation also exposes limits on traces and processing time per tick. These are concrete examples of restricted perception and computational budgeting. [Perception documentation](https://dev.epicgames.com/documentation/unreal-engine/ai-perception-in-unreal-engine), [sight processing limits](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/AIModule/Perception/UAISense_Sight?application_version=5.5).

**Why doesn’t every game go all the way to your model?** The following are engineering and design tradeoffs, rather than claims that every studio follows one approach.

1. **A modular interface and an enforced sandbox solve different problems.**  
   For developer-authored AI, interfaces, ownership rules, and testing may provide sufficient separation. A sandbox adds work around execution limits, data exchange, debugging, and persistence. With player-authored programs, enforcement becomes much more valuable: the rules must hold even when someone tries to bypass them.

2. **Paying for every observation changes the game’s rules.**  
   “Looking again costs energy that I could have used to move” is a meaningful mechanic for Automatou. For a shooter guard, the designer may simply want continuous awareness within a vision cone. Adding an energy bill would introduce another failure mode to balance and explain, without necessarily improving that game.

3. **CPU cost and fictional energy serve different purposes.**  
   A runtime might spread visibility checks across frames to meet performance limits. That does not require characters to expend stamina. Your design deliberately makes information gathering a tactical resource decision; that is an additional game-design choice.

4. **Some AI exists to manage the experience beyond any individual character.**  
   Left 4 Dead’s Director adjusts threat populations using estimates of survivor intensity. Such a system needs a broader perspective than one creature’s senses. Your architecture can accommodate this naturally as a separate world-level system. [Valve’s Director presentation](https://cdn.akamai.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf#page=79).

One practical consequence deserves attention: **the distinction between acquiring information and reading already-acquired information must be explicit.** Two helper functions independently requesting vision could accidentally spend twice as much energy. Acquiring an observation once, storing it, and letting both helpers consult that memory makes the cost understandable.

Also, behavior trees, state machines, utility scoring, and planners can all fit inside your **Think** stage. They are choices about how an AT decides; your syscall boundary governs what information and capabilities any of those implementations can use.

For Automatou, the stronger boundary has a clear purpose: **the player is designing the decision-making program itself, so its interface and resource rules are part of the game players learn and master.**

## User

let's approach this from a different angle.

my main concern is that i've found ai behavior in the many different games i've played to be very boring. i am also confused about why ai behavior continues to be "boring" throughout the decades of tremendous technological advancement of games. (i've played video games from 2000 to present day.) to me, it seems obvious that this type of architecture can relatively easily facilitate much more interesting video game ai behavior. so, i assume there is something wrong or inaccurate about my intuition, because there has been a huge amount of resources and intellect devoted to game development over the years, and if this is clearly better way to develop interesting game AIs, it should be much more widespread.

## Assistant

I’ll look at developer accounts of what makes AI behavior interesting, and where richer simulation runs into design or production constraints. The key question is how much your proposed architecture helps—and what it leaves unsolved.

## Assistant

One developer account is especially relevant: S.T.A.L.K.E.R.’s AI developer described prototype encounters where unarmed enemies raced for weapons and ammunition, with the loser panicking. Those situations disappeared when the shipped game gave NPCs unlimited ammunition. That supports part of your intuition: richer behavior can come from ordinary code operating under meaningful constraints. It also shows how a game rule can remove that behavior regardless of the AI architecture.

## Assistant

**You don’t have to conclude that your intuition is wrong because the industry hasn’t delivered what you want.** But I would qualify one part: a good architecture can make interesting behavior easier to build; it cannot, by itself, make that behavior interesting or make it inexpensive to ship.

My previous answer concentrated too much on implementation boundaries. Your underlying interest seems to be **agents whose decisions are worth observing, understanding, and interacting with**. That is a different goal from merely making opponents harder to beat.

**There is concrete evidence for your intuition.** S.T.A.L.K.E.R.’s AI developer described prototype encounters where two unarmed enemies raced to acquire weapons and ammunition. Whichever gained the advantage could cause the other to panic. Those situations did not survive into the released game because NPCs received unlimited ammunition. The interesting sequence depended on resources, alternative actions, and changing circumstances—not some unavailable breakthrough in computation. [Developer interview](https://www.gamedeveloper.com/game-platforms/interview-inside-the-ai-of-i-s-t-a-l-k-e-r-i-).

Several distinctions help explain the apparent contradiction.

**1. Architecture creates room for interesting decisions; the game must supply those decisions.**

Suppose every AT can sense, remember, think, and act, but its world offers only one useful policy: approach the nearest enemy and attack. Excellent encapsulation will faithfully produce repetitive behavior.

Now introduce incomplete information, competing objectives, scarce energy, defensible terrain, retreat, and allies with useful information. The same basic decision machinery has more consequential choices.

Your sensing proposal helps because it creates an opportunity cost. But the numbers and circumstances matter:

- If scanning is almost always worthwhile, every competent AT scans every turn.
- If scanning is almost never worthwhile, competent ATs largely ignore it.
- If its usefulness depends on memory, danger, position, and intended action, deciding whether to scan becomes interesting.

The interface makes that third possibility implementable and enforceable. Designing situations in which it actually occurs remains separate work.

**2. Encapsulation reduces code complexity without eliminating interaction complexity.**

This is the biggest limitation in the “relatively easily” intuition.

You can understand one AT without reading every other AT. That is a substantial benefit. But many individually reasonable programs can still collectively produce traffic jams, synchronized retreats, endless pursuit, or everyone waiting for someone else to scout.

World systems can resolve those situations consistently. Consistent resolution does not guarantee enjoyable outcomes.

A useful example comes from *Mark of the Ninja*: having each guard independently investigate detected events made whole groups converge on the same disturbance. The developers introduced differentiated responses, and reported that prioritizing interests required ongoing tuning because plausible priorities produced unexpected behavior. Their architecture helped them iterate; it did not remove that work. [Developer’s technical account](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter32_How_to_Catch_a_Ninja_NPC_Awareness_in_a_2D_Stealth_Platformer.pdf).

**3. An interesting demonstration and a consistently good game are different accomplishments.**

It may be relatively easy to produce a memorable encounter in a small simulation. Making the system remain interesting across maps, player strategies, unit combinations, and many hours is harder.

Consider a unit that retreats to preserve itself. That can produce a fascinating reversal. It can also drag out an already-decided battle for ten minutes. Neither outcome requires a programming error.

This is my engineering assessment: much of the work moves from implementing the decision rule to shaping the conditions under which it produces worthwhile play. More autonomy expands both the possibilities and the situations that need evaluation.

**4. Interesting internal decisions must have visible consequences.**

An AT might withdraw because it remembers being ambushed, lacks enough energy to scan, and suspects a second enemy. If the player only sees it walk backward, that reasoning may be indistinguishable from a basic distance threshold.

For your game, showing observations, memories, commitments, and resource expenditure could make this complexity accessible. So could recognizable behavioral consequences: a unit checks a previously dangerous route, seeks a scout, or changes its approach after receiving information.

This does not require explaining every decision aloud. It requires giving players enough evidence to discover that circumstances and history matter.

**5. Technological progress does not automatically address these bottlenecks.**

More computation can support larger simulations and more demanding planning. It does not automatically create useful alternative actions, appropriate incentives, readable behavior, or satisfying encounters.

Consequently, “hardware improved enormously” does not imply “the part of AI behavior I care about should have improved proportionally.” Some of the mechanisms we are discussing are computationally modest; their difficulty lies in integration and design.

**6. Industry investment is evidence of tradeoffs, not proof that your preference is mistaken.**

A project can rationally prioritize authored pacing, reliable mission progression, animation quality, or predictable encounter duration. A more autonomous simulation might complicate those goals while serving yours.

That does not establish that current choices are optimal, or that players universally prefer simpler behavior. It means “better” depends on the experience being pursued. There are also deliberate efforts in this direction: Ubisoft explicitly framed *Watch Dogs 2*’s crowd AI around reactive characters generating unscripted, memorable situations. [Developer presentation](https://www.gdcvault.com/play/1024152/Helping-It-All-Emerge-Managing).

**My assessment is that your architectural idea is a credible foundation, while “it will relatively easily produce sustained interesting behavior” remains a hypothesis.** Automatou is well positioned to test it because designing and examining AT behavior is central to the experience.

A useful first test would be small: create a situation where remembering an earlier sighting changes whether an AT should spend energy scanning or act immediately. Then vary the circumstances. Look for understandable, consequential differences—and whether interacting with those differences stays interesting after the initial surprise.
