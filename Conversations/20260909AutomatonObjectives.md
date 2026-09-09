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
