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

