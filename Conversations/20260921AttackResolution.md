# Attack Resolution

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping

explain how attacks are resolved. the main thing i'm curious about is how units attack each other if they can't be certain of the attack target's position, as in the target may submit move actions in the turn that the unit intends to attack

## Assistant

I’ll read the prototype’s turn and combat logic to see how attacks interact with movement submitted in the same turn.

## Assistant

Attacks target a unit ID, and the simulation checks that unit’s current position when the attack executes. Movement can invalidate the attack, but moving to another hex won’t by itself dodge it. Actions in the same slot execute in a rotating order, so whether the move or attack happens first matters.

## Assistant

**An attack targets a unit ID, not a hex.** The attacker chooses the target using its observations at the start of the turn; the simulation uses the target’s **current position when the attack executes**. This effectively gives attacks automatic tracking, provided the target remains eligible.

The turn works like this:

1. **Everyone plans from the same frozen world state.** Units can observe enemies and submit lists such as `[MoveForward, Turn, Attack(targetId)]`. They cannot see the other units’ submitted plans or replan during execution.
2. **The world executes one action slot at a time:** everyone’s first action, then everyone’s second action, and so on.
3. **Within each slot, actions execute sequentially in a rotating order.** The units start in ID order, shifted by `turn number % active unit count`. Moves and attacks share this order; there isn’t a separate “all movement, then all attacks” phase. Movement conflicts are determined before executing the slot.

These rules are in [TurnResolution.cs](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/TurnResolution.cs:42).

For example, suppose A submits `Attack(B)` and B submits `MoveForward` in the same slot:

- **A executes first:** A attacks B at its old position. If B survives, B can then move.
- **B executes first and stays within sight, weapon range, and A’s forward attack arc:** A attacks B at its new position.
- **B executes first and leaves range, sight, or the attack arc:** A’s attack is rejected.

The attacker’s own earlier moves and turns also affect these checks. A target killed by an earlier action is no longer available.

**A rejected attack costs no attack energy, but stops the attacker’s remaining actions for that turn.** There is no automatic chase, replacement target, or mid-turn replanning. An accepted attack costs 2 energy, with at most one attack per unit per turn. The target must have been observed through a vision scan that turn; guessing its ID is insufficient. See [attack validation](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/TurnResolution.cs:114).

Once an attack is accepted, [combat resolution](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/World.cs:139) is immediate:

- **Hit or evasion:** a random roll checks the defender’s evasion statistic. A deployed (`Stationary`) defender has zero evasion. Actually submitting or executing movement does not itself increase evasion.
- **Damage:** adjacent units use melee damage; otherwise ranged damage. Directional armor is subtracted, with a minimum of 1 damage. The defender’s current facing determines armor protection.
- **Blast:** ranged explosive attacks that hit damage units within the blast radius, including allies. Deaths take effect immediately.

So the program does face uncertainty about whether its planned attack will still be valid. However, **it does not need to predict an impact position**: the world supplies the target’s live position at execution. There is no projectile travel time or shot left heading toward a vacated hex in the current implementation.

## User

i'm thinking of simplifying this to say that all movement occurs after attack or attack-like actions. would that simplify things?

## Assistant

I’ll consider how that phase order would affect the existing action lists, especially moving or turning before an attack.

## Assistant

**Yes—resolving all attacks before any movement would make targeting substantially easier to understand.** Everyone attacks the positions they observed when planning. A target cannot escape an otherwise valid attack by moving earlier in the resolution order.

A clear turn structure would be:

1. **Observe and plan.** All units see the same starting state.
2. **Resolve attacks and similar effects.** Positions remain fixed throughout this phase.
3. **Resolve movement.** Surviving units carry out their movement plans.

The main gameplay consequence is that **you can attack and then move, but cannot move into range and attack during the same turn.** Approaching an enemy prepares an attack for a later turn. That is a meaningful change, but a straightforward rule.

There are three details I would settle explicitly:

- **Turning:** I would put turning alongside movement, after attacks. Otherwise an earlier turn could still change attack eligibility or directional armor during the attack phase. Units would attack using their starting facing.
- **Units attacking each other:** Attacks-first does not itself resolve this. Sequential attacks still let the first attacker kill the second before it fires. For predictable exchanges, I would evaluate attacks against the starting state, collect damage, and apply it together. Two units could then destroy each other.
- **Existing action lists:** The current planner can submit `move → turn → attack`. Simply rearranging those requests would break its assumptions about range and facing. The planner should explicitly choose an attack for the current position and facing, then plan movement separately, within the shared energy budget.

“Attack-like” would also need a defined scope as abilities are added: an effect that pushes, teleports, or changes visibility could reintroduce ordering questions.

**For this prototype, I would favor attacks evaluated from the starting state, damage applied together, then movement and turning by survivors.** That removes the target-position uncertainty and the first-shooter advantage from ordinary combat, while leaving movement conflicts as a separate problem.
