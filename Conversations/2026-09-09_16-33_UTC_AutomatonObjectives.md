# Automaton Objectives

## User

for this conversation, don't edit the repo other than recording convos

okay, we've resolved the blockers for the 3d shell. now, we can discuss mechanics more.

the first thing i'd like to investigate is overall "automaton" logic. i use the words automaton/automata (abbreviated AT) instead of AI (artificial intelligence) to refer to the logic that drives agent choices within the game.

a natural starting point for developing ATs is to draw from my own experience of playing strategy games and other peoples' experiences. in combat oriented games such as age of empires, many strategies fall into "fast" and "slow" buckets. fast strats involve rushing, early aggression, optimizing for victory in early game, and focusing resources on offensive units/building/tech. "Slow" strats are the opposite and focus on either surviving a fast opponent's early attacks and then outpacing them later in the game, or building up and successfully countering each exchange in late game if facing a slow opponent.

we can develop very simple ATs inspired by these strats. unlike in most multiplayer human games, these ATs don't have to optimize for a win condition. the entire point of automapolis is a sandbox simulation game in which players can mess around and watch "interesting" behavior unfold, where interesting is subjective and depends on the player.

so two very simple ATs to start with would be "aggressive" and "defensive". aggressive ATs only attack until they die. defensive ATs only care about accumulating resources and will never seek out enemies to attack. i.e. aggressive ATs only hunt enemies and defensive ATs only hunt resources.

## Assistant

I’ll keep repository changes limited to recording this conversation. I’ll check the existing log format, then work through what these two AT rules imply.

## Assistant

That gives us two ATs defined by **what they pursue**:

- **Aggressive:** seek enemies, approach them, and attack. Continue regardless of danger.
- **Defensive:** seek resources, approach them, and accumulate them. Never pursue an enemy.

Their simplicity is useful for a sandbox: a player can understand an agent’s behavior, change its surroundings, and see how the same rule produces different outcomes. Neither AT needs a concept of victory, a long-term plan, or even an assessment of whether its actions are sensible.

I would keep **aggressive/defensive separate from fast/slow**. Your strategy-game experience motivates these behaviors, but their timing can emerge from the simulation. An aggressive AT might spend a long time finding someone to attack; a defensive AT might accumulate resources very quickly. The defining difference is the target, rather than the pace.

There’s also a useful distinction between **the AT’s objective and the actions needed to pursue it**. “Only hunts enemies” could mean that an aggressive AT never deliberately gathers resources, even if that eventually leaves it unable to fight. That is a coherent starting rule. If we later allow gathering to support aggression, we have introduced a more elaborate AT, with intermediate objectives.

The main ambiguity is what “defensive” does when attacked. Never seeking enemies still permits retaliation, whereas literally only hunting resources could mean ignoring an attacker entirely. Those would produce quite different interactions:

- **Ignore attacks:** keep gathering, even while being killed.
- **Flee:** preserve the ability to keep gathering.
- **Retaliate:** fight an immediate attacker, then return to gathering.

Which of those do you intend for the initial defensive AT?


## User

another interesting AT domain is individual units rather than overall factions. in typical multiplayer strategy games, each player commands units like pieces on a chessboard. it is rare to have mechanics in which units "disobey" orders or other similar mechanics.

automapolis is very different in that we can simulate autonomy at any level. we can have any combination of the following:
- a whole faction controlled by a single AT, which mirrors typical multiplayer strategy games
- individual units have their own ATs but also have faction allegiances
- certain units have a greater degree of power/authority, so a commander or president can issue orders to other units that have a high likelihood of obeying them
- ATs that don't have a unit representation such as god-like entities, mother nature, etc that have powerful systemic abilities

## Assistant

I’ll record this extension to the conversation. It introduces a useful distinction between what an AT wants and what it has the authority to control.

## Assistant

Yes—this makes **the location of decision-making a simulation variable**. A faction can act as one mind, as many minds with shared allegiances, or as a hierarchy whose members retain some autonomy. Those arrangements can coexist in the same world.

I’d distinguish a few independent properties of an AT:

- **Objectives:** what it pursues, such as enemies or resources.
- **Information:** what it can perceive or know.
- **Capabilities:** what it can directly change in the world.
- **Authority:** whose behavior it can influence by issuing orders.
- **Embodiment:** whether it inhabits a unit, building, or other physical entity, or has no physical representation.

Faction allegiance is then a relationship, rather than a requirement that everyone share one decision-maker. Likewise, an AT without a body need not be omniscient or omnipotent; its information and capabilities can have their own limits.

The consequential distinction is **direct control versus issuing an order**. With direct control, a faction AT chooses a unit’s actions. With orders, a commander supplies an instruction that the receiving unit’s AT evaluates. “Disobedience” becomes one possible outcome of that evaluation.

For example, an aggressive commander could order a defensive unit to attack. Depending on the unit’s rules, it might obey unconditionally, obey only while the commander is nearby, refuse when carrying valuable resources, or abandon the attack when threatened. Even very simple rules could make authority visible through behavior.

A high likelihood of obedience could be a literal probability, but it could also emerge from conditions: units usually obey because the conditions for refusal are uncommon. The latter gives players something they can discover and manipulate. Randomness could still contribute.

This also gives the earlier aggressive/defensive distinction more reach. An aggressive faction with defensive individuals could behave very differently from a defensive faction with aggressive individuals. A resource-seeking entity without a body might alter rainfall to encourage growth, if rainfall is among its capabilities.

The central mechanics question becomes: **when several ATs want different things from the same unit or part of the world, how are their competing intentions resolved?** That is where hierarchy, allegiance, obedience, and systemic intervention start producing interactions.

## User

given the conversation so far, provide examples of mechanics in other games that may be relevant or good inspiration to draw from or spark new ideas

## Assistant

I’ll look for examples of unit autonomy, authority, faction behavior, and systemic powers, with an emphasis on mechanics that could translate into simple AT rules.

## Assistant

Majesty is a close match for autonomous units responding to incentives, while Total War shows how a simple morale threshold can interrupt orders. RimWorld adds another useful model: an AT that controls events and pacing without belonging to a faction.

## Assistant

**Majesty, Rain World, and RimWorld are especially close to different parts of your idea.** There are also useful mechanics in more conventional strategy games that could be borrowed individually.

Below, the game descriptions are sourced; the Automapolis possibilities are my extrapolations.

1. **Majesty 2 — motivate autonomous subjects with rewards**

   Heroes make their own decisions, and the monarch uses reward flags to encourage particular tasks. Different heroes respond differently: rangers favor exploration, while warriors favor fighting. Increasing the reward can attract interest that the task alone would not. [Game manual](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/73020/manuals/M2%20Manual%20English.pdf)

   **For Automapolis:** authority could include offering incentives. A faction AT might post a bounty and leave individuals to decide whether to pursue it. This creates an interesting extension of your resource hunter: it could hunt resources directly, or earn them by performing someone else’s requested action. Two commanders could compete for the same units’ attention.

2. **Total War: Rome II — obedience can fail at a threshold**

   Units rout when morale becomes too low. Casualties, fatigue, and overwhelming opposition can cause this; a nearby general supports morale and can attempt to rally fleeing units. [Official morale guide](https://r2enc.totalwar.com/en/manual/single-player/0087_enc_page_battle_play_phase_conflict_morale/)

   **For Automapolis:** a unit could follow orders until fear exceeds courage, then flee. This gives you conditional autonomy with very little decision logic. A commander’s physical position matters, and killing or separating that commander can change many units’ behavior without changing their faction allegiance.

3. **Crusader Kings III — a political organization contains competing people**

   Characters have traits and personal schemes; rulers manage vassals, influence others, inspire fear, and face rivals who can seek their position. Membership in the same realm does not eliminate personal ambition. [Official game overview](https://www.paradoxinteractive.com/games/crusader-kings-iii/about)

   **For Automapolis:** separate allegiance to a faction from loyalty to its current leader. A unit might defend its faction while opposing its commander. Leadership succession could then change the behavior of an entire group even though its membership and resources remain identical.

4. **Dominions 6 — give instructions, then watch their execution**

   Players assign battle orders before combat instead of directly controlling units during it. Squads can receive different orders under the same commander, including attacking, guarding a commander, or holding before attacking. [Game manual](https://illwinter.com/dom6/dom6manual.pdf)

   **For Automapolis:** orders could set a temporary policy: “guard this unit,” “attack anyone entering this area,” or “gather until carrying ten resources.” Individual ATs handle the details. This gives a middle ground between a faction choosing every action and units acting entirely independently. An order can also become inappropriate as circumstances change.

5. **Rain World — creatures have lives and relationships beyond the player**

   Creatures independently seek food and shelter, hunt one another, and compete over territory. Interactions can affect subsequent encounters: helping a scavenger can build trust, while threatening creatures can produce hostility or fear. [Developer explanation](https://blog.playstation.com/2017/01/16/rain-world-surviving-in-a-living-ecosystem/), [behavior and memory](https://blog.playstation.com/2017/03/28/theres-always-something-bigger-and-hungrier-in-rain-world/)

   **For Automapolis:** “enemy” could be a relationship that changes through experience. An otherwise aggressive unit might spare one individual that helped it. A resource hunter might repeatedly cross a territorial unit’s boundary, generating conflict without either AT having a conquest objective.

6. **Factorio — economic activity produces the conditions for conflict**

   Factory pollution spreads through the environment. Pollution reaching enemy spawners is used to organize attacks; production therefore affects the threats the factory encounters. [Pollution mechanics](https://wiki.factorio.com/Pollution)

   **For Automapolis:** resource gathering could leave traces that other ATs respond to: noise, waste, scent, depleted land, or visible stockpiles. Your defensive AT could provoke conflict simply by accumulating resources. Aggressive ATs could locate activity through those traces, giving them limited information instead of automatic knowledge of every enemy.

7. **Black & White — behavior can be shaped through experience**

   The player’s creature learns through feedback, including rewards and punishment, rather than receiving only explicit instructions for every action. [Gameplay description](https://en.wikipedia.org/wiki/Black_%26_White_%28video_game%29)

   **For Automapolis:** AT preferences could change during a simulation. Rewarding a unit after it gathers might make gathering more attractive; repeated injury near a location might make that location less attractive. A simple initial personality could acquire a history. This also suggests experiments where identically initialized units develop differently because of their experiences.

8. **RimWorld — individual autonomy and an AT controlling world events**

   Colonists have needs, relationships, and backgrounds that affect their behavior and willingness to work; excessive stress can cause breakdowns. Separately, the storyteller controls events such as raids, storms, and traders, with different storytellers producing different pacing. [Official game description](https://rimworldgame.com/)

   **For Automapolis:** this supplies two distinct ideas. First, personal needs can interrupt faction objectives. Second, an AT without a body can pursue an objective through systemic events. A “nature” AT might encourage vegetation when plant coverage falls below a threshold; a “discord” AT might create resource shortages near otherwise peaceful factions. Those objectives can operate alongside unit objectives.

For the first small experiments, I’d take **reward-driven task choice from Majesty, morale-based refusal from Total War, and remembered relationships from Rain World**. Each adds one understandable reason for agents to behave differently, and their interactions could already produce hiring, desertion, grudges, and unexpected cooperation.
