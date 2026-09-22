# Exploring More Interesting Automata

Research and design proposal, 2026-09-20. The first implementation now includes heat and readiness, pursuit through concealment, protective bonds, persistent intentions, and inspection/replay tools; see [README.md](README.md) for the actual controls and rules. The proposals below retain the broader research directions, including future work. Numerical limits are proposed starting budgets, not results established by the cited research.

The recommendation is to introduce a few consequential internal states, imperfect knowledge, and commitments that persist across turns. Use small decision procedures to connect them to visible behavior. Judge success by understandable differences in behavior under changing circumstances, consistent with the specification's preference for thematic behavior.

## What the current implementation makes possible

The architecture already provides a good boundary: each unit owns its automaton; the automaton observes, remembers, and requests actions; the world executes and validates those actions. Keep this boundary.

The present limitations are more specific:

- Every call to `UnitSenses.Observe()` returns exact observations of every entity, including health, position, footprint, and statistics. There is little reason to search, investigate, communicate, or remember an enemy's location.
- All eight automata delegate to the same target-selection and engagement helpers. Their principal decision differences are wounded-target preference and whether to keep distance. Statistics create additional differences.
- Memory consists of `TurnsObserved` and `TargetId`. The old target only wins ties in target selection; it is not a lasting intention.
- `TacticalPlanning.Engage()` gives an available attack priority over withdrawal. A richer choice such as delaying a shot to protect an ally cannot be expressed by adjusting the two existing flags.
- Automata can request turning, moving forward, and attacking an entity. Ending the iterator ends the turn. Autonomous deployment, communication, repair, negotiation, and attacks aimed at an estimated cell are absent.
- Terrain already influences movement. It does not provide sensory concealment, line of sight, or environmental exposure.
- Factions are mutually hostile. Combat rules reject directly attacking a same-faction entity. A relationship score alone cannot create diplomacy or betrayal.

See [UnitAutomaton.cs](Source/Simulation/UnitAutomaton.cs), [World.cs](Source/Simulation/World.cs), [SiegeWalker.cs](Source/Simulation/Units/SiegeWalker.cs), and [Specification.md](Specification.md).

There is no need to replace the architecture before trying richer behavior. Some proposals need new world mechanics as well as new automaton logic; those costs are identified below.

## 1. Choose intentions with utility; execute them with small persistent routines

**Prior work.** Utility systems compare the suitability of actions using a common scale, and use inertia or cooldowns to avoid switching too readily. See Graham's [An Introduction to Utility Theory](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf). Sutton, Precup, and Singh's [options framework](https://www.sciencedirect.com/science/article/pii/S0004370299000521) formalizes actions that extend over time. Borrowing temporal abstraction does not require implementing reinforcement learning.

**Small adaptation.** Give one automaton three intentions initially: engage, withdraw, and recover. Later experiments can add investigate, escort, or regroup. Each intention has conditions for starting, a small execution routine, and explicit completion or interruption conditions. Store the current intention, target or destination, and phase.

Compare a few normalized considerations: expected opportunity to damage an enemy, estimated incoming danger, strain relief, or ally need. Use a small palette of linear and threshold curves. Separate hard action legality from preferences: a high utility score cannot make a physically forbidden shot legal. Lewis's [Choosing Effective Utility-Based Considerations](https://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter13_Choosing_Effective_Utility-Based_Considerations.pdf) discusses mandatory considerations and the practical value of a limited palette of curves.

Retain the current intention until a challenger exceeds it by a meaningful margin, it completes, it becomes impossible, or an explicit emergency interrupts it. A timeout prevents permanent commitment to an unreachable destination. Continue observing after each action for safety, without selecting a different long-term goal after every 60-degree turn.

For example, an escort can close the gap to its ward, attack a nearby threat, then keep pace with the ward over several turns. That behavior needs a few persistent values and stages. It need not search all future action sequences.

**Bound.** Three initial intentions; at most six in a subsequent experiment. At most four considerations per intention, with a displayed explanation for each. Candidate targets and destinations also need caps: six intentions multiplied by every entity and every cell is still a large search.

**Failure to watch for.** Utility can become a tangle of uncalibrated weights. Start with authored, distinguishable profiles and deterministic tie-breaking. Adding random noise every decision would make the behavior harder to interpret.

## 2. Treat quirks as small control problems

**Prior work.** Homeostatic models connect behavior to maintaining internal variables within desirable bounds. Keramati and Gutkin's [homeostatic reinforcement learning paper](https://elifesciences.org/articles/04811) is a useful example from computational neuroscience. The proposed game adaptation borrows internal regulation, not the paper's complete learning model or a claim of physiological realism.

**First experiment: a heat-limited siege walker.** Add one physical variable, heat, and a cooling rate. Firing increases heat; each world turn dissipates some. Crossing a high threshold locks the weapon until heat falls below a lower threshold. Separate entry and exit thresholds provide hysteresis and prevent immediate toggling at a boundary.

The automaton can fire now, move while cooling, or wait. It can estimate whether firing on this turn will cause an inconvenient lockout next turn. Since the prototype permits only one attack per turn, its distinctive rhythm would be consecutive firing turns followed by recovery, rather than multiple shots within one turn.

Compare two authored operator policies on identical machines: one favors immediate damage and tolerates lockouts; one preserves readiness by spacing shots. The physical quirk is the same, but the fighting styles differ. Operator policy can initially be a few preferences in the machine's automaton; a separate simulated operator entity is unnecessary.

Once this produces a recognizable rhythm, optionally give a known terrain type a cooling modifier. Now retreat destinations and route choice matter. That extension requires navigation to a chosen location, not just the existing enemy-directed route helper.

Other candidates use the same design pattern: a weapon that needs a turn without movement to settle, or Traveler travel strain that makes returning toward H.O.M.E. desirable. Start with one condition. Do not add heat, fatigue, morale, ammunition, hunger, and stress together.

**State ownership.** Actual heat and weapon lockout are physical unit state, updated and enforced by the world. Desired heat reserve, planned recovery destination, and learned expectations belong to the automaton. Both must be saved where applicable. The automaton must not be able to erase an inconvenient physical condition.

**Success criterion.** Changing the cooling rate or operator preference changes the timing of advances, shots, and retreats in a readable way. A condition that merely reduces damage without changing choices is a weaker experiment.

## 3. Make units act on beliefs and remembered sightings

**Prior work.** Partially observable decision models distinguish the actual world from an agent's information about it. See Kaelbling, Littman, and Cassandra's [Planning and Acting in Partially Observable Stochastic Domains](https://www.cassandra.org/arc/papers/aij98.pdf). A practical first adaptation is a bounded contact memory, not a full probability distribution over the world or an optimal POMDP solver.

**First experiment: losing an enemy behind forest.** Start with a simple detection radius and one explicit forest concealment rule. On detection, remember a contact's last observed cell and turn. When contact is lost, pursue that location, search a few neighboring cells, then give up or regroup. New observations replace stale estimates; absence from the current observation is not proof that an enemy died.

Store at most eight contacts with identity if known, estimated position, last observation turn, and an uncertainty category. Confidence is a heuristic unless a calibrated sensor model is actually implemented. Do not silently treat it as a probability.

The first version can use exact positions when visible and stale positions when hidden. This already creates searching, escape, and ambush opportunities, without modeling hallucinations.

**Heat-sickness extension.** Add a persistent sensory bias or reduced update frequency associated with exposure. This is a fictional game rule. If a unit can call `Observe()` repeatedly after actions, sampling independent position error on every call would allow repeated sampling to reduce the intended uncertainty. Keep error stable for a defined observation interval, or explicitly model time and cost for a fresh scan. A move can legitimately reveal new information, but a repeated query should not reroll the sensor.

**The integration cost is real.** `FindDirection(targetId)` currently resolves the true target location. Route selection must instead use known terrain and an estimated destination. The current occupancy query also exposes true occupancy; separate planning over known occupancy from authoritative collision checks when a move is executed. Decide explicitly whether terrain itself is fully known.

Likewise, `AttackAction(TargetId)` resolves the real entity and aims at its actual footprint. Keep entity attacks restricted to sufficiently observed contacts in the first experiment. Shooting at an uncertain position later needs an explicit aimed-cell request and appropriate world resolution, including misses. Changing the observation alone cannot implement inaccurate aim.

**Success criterion.** A pursuer follows the last sighting instead of the invisible enemy. Changing only concealment changes whether contact is lost and reacquired. The inspector can display the unit's estimated enemy location alongside the true one for the omniscient observer.

## 4. Produce group behavior through local interaction and a few roles

**Prior work.** Reynolds's [steering work](https://www.red3d.com/cwr/steer/gdc99/) demonstrates composable local behaviors such as separation and cohesion. Smith's [Contract Net Protocol](https://www.reidgsmith.com/The_Contract_Net_Protocol_Dec-1980.pdf) describes task allocation through announcements, bids, and awards. These suggest two distinct experiments, with different costs.

**Local coordination first.** Have an automaton score a few reachable destinations for distance from allies, distance from its objective, and exposure to observed enemies. This adapts local steering preferences to legal hex moves; it does not directly apply continuous steering forces to turn-based units. Retain bounded route search around obstacles, since local preferences can get stuck.

For artillery, an especially small starting improvement is comparing possible shots using estimated enemy damage and allied blast exposure. Different faction preferences can deliberately tolerate different collateral costs. The simulation already has splash damage, so this creates a consequential choice using an existing mechanic.

**Role allocation second.** A squad could advertise one escort slot and one scout slot. Nearby candidates bid using travel cost, suitability, and current commitment; the task owner awards each slot to one candidate. A compact record stores task, owner, assignee, and expiration. Reassign on death, timeout, or inability to proceed. A three-turn lease is an initial tuning choice, not a requirement of Contract Net.

Define what communication reaches and when awards become visible. Resolving bids at a turn boundary and applying awards on the following turn is one way to prevent the sequential action order from accidentally deciding every role. Each automaton still chooses whether to bid and how to carry out its role; world bookkeeping enforces exclusivity and delivery rules.

For the first test, assignments can be authored at placement. Dynamic bidding is only justified after exclusive roles show useful behavior. A full commander hierarchy, auction economy, and joint planner can wait.

**Optional spatial-memory experiment.** A single local alarm or trail value per hex can be deposited, decay, and spread to adjacent cells using a cellular-automata-style neighborhood rule. Define whether it is a physical signal sensed locally or a faction's information map. A field calculated from unseen enemies would otherwise restore omniscience. Use old/new buffers for simultaneous propagation and clamp values. One field costs work proportional to map size per propagation step; it is not free because the individual rule is simple.

For Prytu, local executable routines can remain an implementation of Prytus's control. The specification does not give individual Prytu independent social personalities.

**Success criterion.** Units share space and responsibilities without all selecting the same destination or abandoning every role on a small score change. Test chokepoints and a destroyed role-holder as well as open terrain.

## 5. Use sparse relationships and witnessed events

**Prior work.** [Comme il Faut](https://ojs.aaai.org/index.php/AIIDE/article/view/12454) explores reusable social rules and interactions as a way to reduce the burden of authoring each social situation separately. Granovetter's [threshold models](https://www.journals.uchicago.edu/doi/10.1086/226707) show how individual thresholds and dependence on others' choices can produce collective outcomes. These are inspirations for separate small adaptations, not justification for importing a full social simulator.

**First experiment: a bonded pair under pressure.** Author one directed attachment from unit A to unit B. Make it increase A's preference to stay near B or attack an observed threat to B. Give A a competing self-preservation preference. A may abandon an advantageous pursuit to cover B's withdrawal, or eventually break away when its own danger becomes too high.

Protecting B initially means accompanying it or attacking a threat. The current world does not make a friendly unit intercept ranged fire, so positioning a guard in front cannot be assumed to provide a shield.

After fixed bonds work, add one typed, witnessed event such as friendly blast damage, and let it update a relationship. The current human-readable combat log is not a reliable event interface. A small structured event record needs actor, affected unit, event kind, time, and rules for who learns about it. Repeated observation must not process one event repeatedly.

Later, represent a few directed relationships using attachment, trust, and grievance. They need not be symmetric: A can trust B more than B trusts A. A record can keep one explanatory event and its turn instead of an unbounded biography. Cap each unit at four significant relationships, with an explicit eviction rule that protects current commitments. This bounds storage near four records per unit instead of all possible pairs.

**Optional collective reaction.** Add a morale-like state influenced by the observed fraction of nearby allies withdrawing, with different entry and recovery thresholds. Compare a homogeneous group with one containing a few steadfast members. This is a game adaptation of threshold interaction, not a validated model of real combat psychology. Snapshot neighbor states before updates to keep propagation from depending unintentionally on iteration order.

**Political intrigue needs a further choice.** Loyalty and resentment become political when units can act on them: refuse an assignment, withhold a report, choose one leader over another, or honor a truce. Start later with one such action, such as refusing an escort assignment when trust is low. A general diplomatic system would require replacing the existing faction-hostility assumptions in selection and action validation. It is not a free consequence of the relationship dictionary.

**Success criterion.** Swapping only the bond or prior grievance changes whom a unit helps. The inspector identifies the relationship and event responsible. Persistent relationships also need scenarios where units survive long enough for history to matter.

## 6. Let experience adjust one belief or preference

**Prior work.** Incremental value estimation and recency weighting are basic tools covered in Sutton and Barto's [Reinforcement Learning: An Introduction](https://www.incompleteideas.net/book/bookdraft2018mar21.pdf), especially Chapter 2. They suggest a much smaller experiment than training a complete combat policy.

Once sensing, events, and alternative behaviors work, allow a unit to update one bounded estimate after relevant experience. For example, record estimated danger in three terrain categories. An observed harmful outcome moves the estimate slightly toward the new sample; later experience can revise it. Limit how strongly the estimate influences destination preference so that one bad encounter does not dominate the rest of the run.

This demonstrates history dependence: two otherwise identical survivors can choose different routes because of different experiences. It is an associative heuristic unless its state, actions, feedback, and learning objective are specified more fully. Damage in forest does not establish that forest caused the damage, so the inspector should expose the estimate as a belief.

If learning chooses among tactics, define when an episode ends and how its outcome is scored. Blindly rewarding survival encourages indefinite retreat; rewarding kills alone suppresses escorts and recovery. Outcomes must reflect the intended faction behavior. Also handle exploration or belief decay explicitly, since an avoided option supplies no fresh evidence to correct a bad estimate.

**Bound.** One small table per automaton; no neural network, training service, or cross-session learning at first. Persist its values and any random state with the world.

**Related artificial-life direction.** [Amorphous Fortress](https://arxiv.org/abs/2306.13169) studies emergent interaction in an open-ended spatial simulation built from generated and evolved finite-state machines. This is unusually close to Automatou's observer-oriented premise. A later adaptation could search offline over a small palette of behavior profiles and retain visibly different results. Its relevance is the experiment format; it does not establish that greater state-machine complexity guarantees more interesting behavior here.

## How the pieces should fit

Keep the conceptual path small:

`World and physical state -> observations -> automaton memory -> intention -> action requests -> world`

Each unit keeps its nested automaton class. Shared code should provide a few ordinary helpers for scoring, contact memory, and navigation. Introduce abstractions after repeated uses establish their shape. Avoid creating a universal personality language, rule editor, or generic cognition framework as a prerequisite.

Physical constraints affect what actions succeed. Observations affect what the unit can know. Relationships and preferences affect what it chooses. This separation makes a new quirk easier to reason about and prevents policy code from bypassing world rules.

Use the same mechanisms selectively. A siege walker can emphasize heat and readiness; a Traveler can emphasize distance from H.O.M.E. and mobility; a Bastion can have a strong individual commitment; clone infantry and artillery can differ in collateral tolerance. These are proposed directions subject to design choice. They need not all acquire every subsystem.

## Recommended experiment order

These are relative scope estimates for this codebase, not calendar estimates. Each runnable experiment should be self-contained under the repository's experiment conventions. The following labels describe scenarios rather than prescribing new folders or duplicating projects now.

| Order | Experiment                        | Minimum addition                        | Relative scope | Evidence to seek                                      |
| ----- | --------------------------------- | --------------------------------------- | -------------- | ----------------------------------------------------- |
| 1     | Heat-limited walker               | Heat, lockout, three intentions         | Small-medium   | Firing/recovery rhythm changes with operator policy.  |
| 2     | Pursuit through concealment       | Detection, contact memory, sensing APIs | Medium         | Pursuer searches a stale location; prey can escape.   |
| 3     | Bonded pair under pressure        | One attachment, escort/leave choice     | Small-medium   | Swapping a bond changes who receives protection.      |
| 4     | Exclusive squad roles             | Authored slots, then claims and expiry  | Medium         | Roles survive movement and recover after a casualty.  |
| 5     | Collective retreat or alarm field | One threshold rule or one spatial field | Small-medium   | Local changes produce a readable collective response. |
| 6     | Learning from an encounter        | One bounded estimate and feedback rule  | Medium         | Different histories yield different later choices.    |

Scope in later rows assumes earlier supporting observations and diagnostics where needed. Try experiments independently before combining them. Compare baseline, each mechanism separately, then a selected pair. This reveals whether an apparent improvement came from the new mechanism or from an unrelated change.

For each experiment, define one observable claim before implementation. Use a few authored maps and a fixed set of seeds. Include an easy case, a constrained case, and a failure case. Track intention changes, idle turns, repeated route failures, separation from allies, and mechanism-specific events. Keep win rate as context rather than treating it as the measure of interestingness.

The existing checkpoint/rewind system is valuable. Re-run from the same state with one altered parameter or relationship and inspect the difference. Copy physical state, beliefs, intentions, relationships, leases, and randomness into independent C# snapshots. Test save/restore continuation for new state. Time-dependent updates such as cooling, forgetting, and field decay should run at declared simulation boundaries rather than depending on how often an inspector or automaton calls a query.

Add a compact inspector explanation with the first experiment: current intention; decisive considerations; relevant physical condition; remembered target/destination; and the reason for the last transition. For later experiments, show uncertainty and important relationships. This supports the game's purpose of observing and understanding worlds, and makes an interesting decision distinguishable from a bug.

## Approaches to defer

Small state machines or behavior trees are suitable ways to execute the persistent routines. They organize control flow; richer situations still depend on the information, mechanics, and choices supplied to them.

Goal-oriented action planning or hierarchical task planning becomes attractive when units need to combine reusable multi-step actions, such as acquiring supplies, repairing equipment, and returning to an assignment. My recommendation is to wait until several concrete routines are becoming difficult to author, then evaluate a planner against those examples. The present three primitive actions do not by themselves justify a planning framework.

A full POMDP solution, deep reinforcement learning, neural cognitive architecture, or unrestricted social reasoning would introduce modeling and evaluation obligations beyond these first experiments. Consider them when a specific limitation of the smaller model is demonstrated. Start by making a few choices consequential, persistent, and observable.
