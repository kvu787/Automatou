# Explore More Interesting Automata

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping

currently, the automata are very boring.

obviously, we can describe, at a high-level, much more complex and interesting behavior for units. for example, machines can have specific issues/quirks that their operators need to avoid or workaround which lead to fighting styles unique to specific machines, units can have stronger relationships or rivalries with other specific units to facilitate squad mechanics and political intrigue, different terrain types can positively or negatively influence the sensing ability of units (extremely hot climates can lead heat sickness in human-likes which lead them to inaccurately determine the position of enemies). the possibilities are vast.

however, i want to avoid letting imagination run wild and causing an explosion in implementation complexity. so, i want you to draw inspiration from the massive body of prior work on artificial intelligence across many domains (game development, simulation development, theoretical computer science, machine learning, cellular automata, dynamical systems, economics, neuroscience, sociology etc.) and propose some concrete approaches to explore to start making automata more interesting.

## Assistant

I’ll inspect how the prototype’s automata work, then draw on established research to propose a few concrete directions with bounded implementation costs.

## Assistant

The main limitation is clear: units currently know every enemy’s exact state and mostly choose between closing in, attacking, and keeping their distance. Their memory rarely affects those choices. I’m focusing the research on mechanisms that make history, uncertainty, and competing needs matter, while keeping each one small enough to test separately.

## Assistant

A useful distinction is emerging: an interesting quirk needs both a consequence and a way to compensate for it. Heat can create a fighting style if firing builds it up and repositioning helps manage it. Likewise, relationships need choices such as whom to protect, and imperfect sensing needs remembered positions.

I also found an important integration detail: attacks and route queries currently use the enemy’s true identity and position. Making observations inaccurate would have little effect unless those helpers also respect what the unit knows.

## Assistant

I’d start with three experiments: a heat-limited machine, pursuit based on remembered sightings, and a bonded pair deciding whether to stay together under pressure. Each has a visible behavior to evaluate and a small state model. I’m also documenting later options for squad coordination and adaptation, with clear limits on what each would require.

## Assistant

The proposal is drafted. It separates changes to decision-making from changes the world must support, and gives each experiment a scope limit and a way to judge the result. I also found a particularly relevant research example, Amorphous Fortress, which explores emergent behavior in an observer-oriented simulation using small state machines.

I’m checking the references and repository notes, then I’ll commit the proposal and conversation record separately.

## Assistant

**I would start by giving automata a few persistent needs, imperfect knowledge, and commitments.** Those create situations where the same unit behaves differently for understandable reasons, while keeping the decision machinery small.

The current prototype already has a useful architecture. Its main limitation is that all eight automata largely reuse the same engagement logic, know every enemy’s exact state, and remember little beyond a target identifier. That identifier only influences ties in target selection. There is little opportunity for experience, uncertainty, or competing priorities to change behavior.

These are the approaches I would explore. The specific adaptations and scope limits are my recommendations, rather than claims established by the cited research.

1. **Choose intentions using utility, then stick with them long enough to accomplish something.**

   Utility AI scores alternatives according to the current situation. For Automatou, score a handful of intentions—engage, withdraw, recover, investigate, escort—then execute the chosen intention through a small persistent routine. Utility systems commonly use inertia to prevent constant switching between similarly attractive choices. [Graham, *An Introduction to Utility Theory*](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf)

   For example, “escort this unit” might persist across several turns of moving, turning, and attacking. Interrupt it when the ward is safe, the task becomes impossible, or personal danger exceeds a threshold. The idea of treating extended behavior as a selectable action also appears in the reinforcement-learning **options** framework; using that idea does not require training anything. [Sutton, Precup, and Singh, 1999](https://www.sciencedirect.com/science/article/pii/S0004370299000521)

   **Scope limit:** start with three intentions and a few considerations per intention. Store the current intention, destination or target, and execution phase. This fits inside the existing per-unit automaton classes.

2. **Model machine quirks as small control problems with meaningful workarounds.**

   A useful connection to neuroscience and control theory is *homeostasis*: behavior helps keep internal conditions within desirable bounds. Computational models use internal state to explain why the attractiveness of an action changes with an organism’s needs. [Keramati and Gutkin, 2014](https://elifesciences.org/articles/04811)

   A concrete first experiment is a **heat-limited siege walker**:

   - Firing increases heat; time dissipates it.
   - Excessive heat locks the weapon until it cools sufficiently.
   - The automaton chooses between firing, repositioning while cooling, and waiting.
   - One operator policy tolerates lockouts for immediate damage; another spaces its shots to preserve readiness.

   One physical variable can therefore produce recognizable fighting styles. Later, terrain-dependent cooling can influence positioning.

   The important design requirement is **a consequence plus a compensating choice**. A quirk that only subtracts damage changes performance; a quirk that changes when and where firing is worthwhile changes behavior.

   **Scope limit:** one condition initially. Actual heat belongs to physical unit state enforced by the world; the automaton stores its intended response. There is no need to simulate a separate operator entity yet.

3. **Give units remembered sightings instead of continuous access to the truth.**

   Research on partial observability distinguishes the world’s actual state from what an agent knows about it. We can borrow that distinction with a small contact memory. [Kaelbling, Littman, and Cassandra, 1998](https://www.cassandra.org/arc/papers/aij98.pdf)

   Start with an enemy disappearing into forest. Its pursuer remembers the last sighting, travels there, searches nearby cells, and eventually gives up or regroups. This creates opportunities for escape, pursuit, scouting, and ambush.

   For your heat-sickness example, exposure could later cause a persistent position bias or less frequent sensor updates. Repeated observation calls should not reroll the error, or units could effectively sample their way back to accurate information.

   **Scope limit:** perhaps eight remembered contacts, each with an estimated location, observation time, and simple uncertainty category. Exact sightings followed by stale memories are enough for the first experiment.

   **This has a larger integration cost than it initially appears.** The current route helper looks up an enemy’s true location, and attacks target its real identity and footprint. Those operations must respect the unit’s information too. Inaccurate observations alone would not produce inaccurate pursuit or aiming.

4. **Build squad behavior from local preferences and a few exclusive roles.**

   Reynolds’s steering work shows how local separation, cohesion, and related behaviors can combine into group motion. For the hex grid, adapt those ideas into preferences over legal destinations: avoid crowding allies, remain near the group, and approach the objective. Retain route search around obstacles. [Reynolds, 1999](https://www.red3d.com/cwr/steer/gdc99/)

   An inexpensive combat example already supported by the world is artillery choosing shots according to both enemy damage and allied blast exposure. Different factions can deliberately tolerate different collateral costs.

   For more deliberate coordination, introduce one escort slot and one scout slot. Initially assign them directly. Later, nearby units can bid according to suitability and travel cost, with assignments expiring or reopening after a casualty. Task allocation through announcements and bids has a long history in distributed AI. [Smith, *The Contract Net Protocol*, 1980](https://www.reidgsmith.com/The_Contract_Net_Protocol_Dec-1980.pdf)

   **Scope limit:** two or three roles with explicit ownership and expiration. A full command hierarchy is unnecessary for testing whether specialization improves what we observe.

5. **Represent relationships as a small number of directed attachments, changed by events.**

   Social AI research such as *Comme il Faut* uses reusable social rules and interactions to avoid authoring every possible social situation individually. [McCoy and colleagues, 2011](https://ojs.aaai.org/index.php/AIIDE/article/view/12454)

   Start considerably smaller: unit A is attached to unit B. That attachment increases A’s willingness to stay nearby or attack a threat to B, but competes with self-preservation. A can abandon an advantageous pursuit to help B withdraw—or abandon B when danger becomes overwhelming.

   Once that works, add one witnessed event, such as friendly blast damage, that changes trust or grievance. Relationships can be asymmetric, and each can remember its most important explanatory event.

   **Scope limit:** one authored bond in the first experiment; later, perhaps four significant relationships per unit. Avoid an all-to-all relationship matrix and an unlimited biography.

   Political intrigue requires additional consequential choices: refusing an assignment, withholding information, choosing a leader, or honoring a truce. Relationship numbers become useful when units can act on them. The current mutually hostile faction rules would need explicit changes for diplomacy.

   A separate, small sociology-inspired experiment is a retreat cascade: units become more inclined to withdraw when enough nearby allies do so. Different thresholds could make a few steadfast units change a group’s response. This adapts Granovetter’s threshold model; it is not a claim of realistic combat psychology. [Granovetter, 1978](https://www.journals.uchicago.edu/doi/10.1086/226707)

6. **Let experience adjust one estimate before considering a learned policy.**

   Start with a small remembered estimate: danger associated with a terrain category, confidence in an ally, or the usefulness of a preferred engagement distance. Relevant experience shifts that estimate gradually, which then influences an existing decision.

   Two otherwise identical survivors can develop different preferences because they experienced different encounters. Incremental, recency-weighted estimation is a basic tool described in reinforcement-learning literature. [Sutton and Barto, Chapter 2](https://www.incompleteideas.net/book/bookdraft2018mar21.pdf)

   **Scope limit:** one small table with bounded influence. Make its meaning visible. Experiencing damage in forest may teach an association, but does not establish that forest caused the damage.

   I would explore this after useful alternative behaviors exist. Learning cannot compensate for having almost nothing meaningful to choose between.

**My recommended first three experiments are:**

| Experiment                  | Question it should answer                                              |
| --------------------------- | ---------------------------------------------------------------------- |
| Heat-limited walker         | Can one physical constraint produce recognizable fighting styles?      |
| Pursuit through concealment | Does limited information create understandable searching and mistakes? |
| Bonded pair under pressure  | Can one relationship change who receives help and who gets abandoned?  |

Build each separately, compare it with the current behavior, then combine a selected pair. Use checkpoint/rewind to change one condition at a time. Evaluate recognizable behavior, meaningful responses to circumstances, and faction identity alongside combat effectiveness.

I would include a small inspector explanation from the first experiment: **current intention, decisive reasons, relevant internal condition, and remembered destination**. In a game about observing and understanding worlds, making decisions readable is part of the experience.

One especially relevant research lead is [*Amorphous Fortress*](https://arxiv.org/abs/2306.13169), an open-ended spatial simulation that explores emergent interactions using generated and evolved finite-state machines. It is close to Automatou’s observer-oriented premise and worth examining after the first authored experiments.

I saved and committed the [full proposal](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/AutomatonBehaviorExploration.md), including integration details, failure modes, experimental limits, and approaches to defer.
