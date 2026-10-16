# Artificial Intelligence Approaches Primer

## User

provide a primer on key fundamental overarching approaches to artificial intelligence to help me brainstorm directions to take C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping in

## Assistant

I’ll look through the prototype to ground the primer in what you already have, then map the main AI approaches to possible directions.

## Assistant

The prototype already uses utility scores, persistent intentions, limited perception, and remembered sightings. That makes it a useful base for comparing approaches without replacing everything. I’ll distinguish how an automaton decides, how it learns, and how group behavior emerges—those are separate choices that can be combined.

## Assistant

**For Automatou, the useful question is: where should interesting behavior come from—authored rules, competing needs, reasoning, experience, or interactions between units?** Each suggests a different direction.

Your current prototype already combines **utility-based decisions, persistent intentions, and limited memory**: units score options such as engagement and escorting, sometimes retain their existing intention, and investigate remembered sightings. You can see this in [BehaviorPlanning.cs](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/BehaviorPlanning.cs). That is a substantial starting point.

The following are overlapping approaches, not mutually exclusive replacements. The examples are proposed experiments, not descriptions of existing features.

**1. Explicit rules and symbolic behavior: “Follow a structured procedure.”**

You directly describe situations, actions, and transitions.

- **Finite-state machines:** a unit occupies a mode such as patrol, pursue, or recover; conditions change the mode.
- **Behavior trees:** reusable checks and routines compose into sequences, priorities, and fallbacks.
- **Rule systems:** explicit facts trigger conclusions or actions.

These make authored behavior inspectable. A behavior tree organizes execution; it does not inherently learn or anticipate consequences. [Behavior Trees in Robotics and AI](https://arxiv.org/abs/1709.00084)

*Automatou experiment:* a hunter stalks until an ally is nearby, approaches, strikes, and disengages. Interruptions are explicit.

**Best for:** recognizable fighting styles and dependable routines.  
**Main limit:** you must anticipate situations; growing interactions can produce an unwieldy collection of exceptions.

**2. Utility and decision theory: “Choose what matters most right now.”**

Give available choices comparable scores. Health, danger, loyalty, opportunity, and personality influence their value. Decision theory extends this to uncertain outcomes through expected utility; ordinary game utility scores are often simpler heuristics.

This supports competing motivations without specifying every combination of circumstances. Commitment or switching costs prevent constant changes of mind. [An Introduction to Utility Theory](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf)

*Automatou experiment:* a protector weighs finishing a vulnerable enemy against returning to an injured companion. Two protectors with different loyalties make different choices.

**Best for:** personality expressed through tradeoffs.  
**Main limit:** scoring an immediate option does not automatically reveal a useful sequence of future actions. Weights can also become difficult to interpret.

This is the approach your prototype already uses most directly.

**3. Search and planning: “Imagine consequences before acting.”**

Provide a model of what actions change, then search possible sequences.

Goal-oriented action planning searches toward desired conditions. Hierarchical planning decomposes a task into smaller tasks. Tactical lookahead simulates possible moves and responses. These differ in machinery, but all add reasoning about intermediate steps. Search, planning, and adversarial reasoning are major classical AI families. [Artificial Intelligence: A Modern Approach](https://aima.cs.berkeley.edu/3rd-ed/contents.html)

*Automatou experiment:* compare a few two-turn plans: fire immediately, rotate and reposition, or retreat while cooling. A unit may accept a worse position now to obtain a better firing opportunity later.

**Best for:** maneuvering, preparation, and purposeful sequences.  
**Main limit:** branching cost and inaccurate predictions. Start with a small horizon and a few candidate actions.

Your existing route search solves a navigation problem; it is not yet general tactical foresight.

**4. Probabilistic inference: “Maintain beliefs about what might be happening.”**

Separate reality from the unit’s estimate of reality. Observations update that estimate; missing or unreliable information leaves uncertainty.

This is primarily an approach to *knowledge*. It can feed rules, utility, or planning. Partially observable decision models combine beliefs with action selection. [AIMA’s uncertainty and decision-making chapters](https://aima.cs.berkeley.edu/3rd-ed/contents.html)

*Automatou experiment:* after an enemy disappears, distribute belief over three plausible destinations. A scout checks a location because doing so could eliminate an important possibility.

**Best for:** scouting, deception, mistaken pursuit, and useful communication.  
**Main limit:** representing and updating uncertainty can grow expensive. A few hypotheses are enough for a first experiment; arbitrary confidence scores should not be presented as calibrated probabilities.

The interesting addition beyond remembered positions is **acting to acquire information**.

**5. Feedback control and internal regulation: “Keep important conditions within tolerable bounds.”**

Behavior continually responds to internal conditions: heat, fatigue, fear, stability, or energy. Feedback changes actions as those conditions depart from desirable ranges. Delays and separate entry/exit thresholds can create rhythms and persistence.

Computational neuroscience has explored connecting internal regulation to reward and learning, although a useful game adaptation can use ordinary authored control rules. [Homeostatic reinforcement learning](https://elifesciences.org/articles/04811)

*Automatou experiment:* a machine’s weapon becomes less stable while moving. Its operator alternates movement, settling, and firing, with an emergency override.

**Best for:** machines and creatures whose bodies visibly shape their behavior.  
**Main limit:** an internal variable only creates interesting behavior when the unit has meaningful ways to manage it.

Your heat system already opens this direction. Another promising change would be giving recovery meaningful positional choices.

**6. Learning from experience: “Change behavior because of what happened.”**

Several distinct ideas live here:

- **Supervised or imitation learning:** learn predictions or actions from examples.
- **Unsupervised/self-supervised learning:** learn useful structure or representations from observations.
- **Reinforcement learning:** improve decisions using rewards from interaction.

A neural network is one possible representation, not a synonym for learning. Learning can also modify a small table or a handful of parameters. Reinforcement learning specifically introduces the problem of balancing exploration with actions already believed useful. [Sutton and Barto](https://incompleteideas.net/book/bookdraft2016sep.pdf)

*Automatou experiment:* a skirmisher gradually learns which of three engagement distances works against an opponent type. Keep the rest of its behavior fixed.

**Best for:** adaptation and individual histories that affect future choices.  
**Main limit:** the training objective may reward behavior you find dull or exploitative. Training across many matches and learning during one unit’s lifetime also produce very different experiences.

**7. Evolutionary search: “Generate variants, test them, and retain useful differences.”**

Instead of designing every policy, mutate parameters or small programs and evaluate populations of candidates.

Ordinary optimization seeks high-performing candidates. **Quality-diversity** methods seek a collection of capable but behaviorally different candidates—particularly relevant when the objective is interesting variety. [Quality-Diversity Optimization](https://arxiv.org/abs/2012.04322)

*Automatou experiment:* vary existing aggression, caution, and commitment settings. Preserve examples that behave differently in retreat frequency and companion proximity, subject to a basic effectiveness requirement.

**Best for:** discovering styles you would not have authored.  
**Main limit:** results depend heavily on what you measure as quality and difference.

Evolution can happen offline as a design tool; it does not require reproduction in the simulated world.

**8. Distributed intelligence and emergence: “Let interactions create the larger pattern.”**

An individual can follow simple local rules while the group produces complex behavior. Swarm systems, cellular automata, and agent-based models explore this idea with different representations.

Reynolds’s flocking work demonstrates coordinated motion from local separation, alignment, and cohesion rules. It does not require each individual to plan the entire flock. [Flocks, Herds, and Schools](https://www.red3d.com/cwr/papers/1987/boids.html)

*Automatou experiment:* units avoid crowding allies, stay near support, and move toward locally reported threats. Observe whether fronts, clusters, and retreats emerge.

**Best for:** collective behavior and societies larger than any one agent’s understanding.  
**Main limit:** emergence is not guaranteed to be interesting; local rules can produce deadlocks, oscillation, or uniform blobs.

Explicit coordination is another possibility: units could bid for scouting and escort assignments. That adds communication and allocation rules rather than relying entirely on spontaneous organization.

**9. Language-mediated agents: “Reason using descriptions, memories, and social concepts.”**

A language model can interpret events, retrieve written memories, and propose plans. Research on generative agents combines memory, reflection, and planning to support simulated social behavior. [Generative Agents](https://arxiv.org/abs/2304.03442)

*Automatou experiment:* a commander occasionally selects a structured strategic intention from a short list, while ordinary automata execute movement and combat.

**Best for:** open-ended interpretation, dialogue, and socially expressive decisions.  
**Main limit:** enforcing consistent world knowledge, legal actions, repeatability, and understandable causality needs additional machinery. A convincing explanation alone does not demonstrate a sound decision.

I would explore this when language and social interpretation become central to your intended experience.

**How I would choose a direction**

The first decision is what you want to find interesting as an observer:

| Desired experience                        | Strong starting approach              |
| ----------------------------------------- | ------------------------------------- |
| “I recognize that unit’s fighting style”  | Rules, utility, internal regulation   |
| “That was a clever maneuver”              | Short-horizon planning                |
| “It acted on a plausible mistaken belief” | Uncertainty and information gathering |
| “Its experience changed it”               | Learning                              |
| “I never designed that behavior”          | Evolutionary search                   |
| “The group became something larger”       | Local interaction and coordination    |
| “These characters interpret events”       | Language-mediated agents              |

**My recommendation is to keep your current architecture and compare three small experiments: a foresighted duelist, an uncertain scout, and a locally coordinated squad.** Each tests a different source of intelligence using the existing observation/action boundary.

Use the existing rewind tools to compare each with its simpler baseline. Judge recognizable behavior, situational variety, and understandable failure—not just combat effectiveness. These experiments should help you decide whether Automatou’s next emphasis belongs in the individual’s reasoning, its understanding of the world, or its relationships with other units.

