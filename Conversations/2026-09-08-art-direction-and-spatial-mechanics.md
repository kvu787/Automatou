# Art direction and spatial mechanics

## User

now that i've changed the art direction, let's talk through how that will integrate with the mechanics.

one thing i definitely want to maintain is that units and buildings aren't all sized at one cell like in advance wars. this is because i want larger and more powerful things to occupy more visual and spatial space.

## Assistant

I’ll check the repo’s art direction and mechanics notes so we can ground the discussion in the current design.

## Assistant

**Variable footprints should remain a core mechanic, and the Blender/SimplePaint direction fits that well.** A large unit can visibly dominate a street while actually needing that much room to move through it. Its size becomes something you plan around.

Your current mechanics already support this: humans occupy 2×2 base cells, mobile units have square footprints, and buildings can have rectangular footprints. I’d preserve that foundation and make the art communicate it clearly.

- **Size should change where things can go.** Infantry can use passages that a heavy vehicle cannot. A large creature might block an entire street. Building placement determines whether a route accommodates infantry, heavy units, or both. This gives smaller units a useful spatial advantage even when larger units are stronger.
- **The model should make its occupied space believable.** A vehicle’s hull or a creature’s stance should roughly justify its footprint. Weapons, limbs, and decorative details can overhang, but a model that appears to fit through a gap should usually fit mechanically. Selection and movement previews should show the exact occupied area.
- **Height can convey scale without adding ground area.** A tower and a low bunker could occupy the same footprint yet feel very different. We can use height visually before deciding whether it affects sight, targeting, or cover. Tall models will also need a way to reveal units hidden behind them.
- **Large footprints should affect combat deliberately.** They create more boundary to approach and more ground that an explosion might intersect. I’d initially make an explosion damage each entity once, regardless of how many occupied cells it catches; otherwise size quietly becomes a damage multiplier.

One nuance: the existing design deliberately allows small, exceptionally powerful entities such as a Bastion. I’d keep **larger size as a common expression of power, with room for exceptions**, rather than making footprint a direct rating of strength.

The first question I’d settle is **how closely you want sizes to follow a shared physical scale**. Should a human beside a tank or building look roughly proportionate, or do you want tabletop-style exaggeration—larger infantry and compressed buildings for readability? Both preserve multi-cell footprints, but they lead to different map proportions and model design.


## User

another thing to record is that previously, with pixel art, there was no "occlusion". for example, a tall tower would simply have a larger square sprite instead of overlapping other sprites and cells due to is height.

with this switch to 3d models for the main visual shell, this is no longer the case. a tall tower might only occupy one cell, but it will occlude other cells if the camera is in the standard 3/4 orthographic view.

also record this: the standard camera view is orthographic 3/4 overhead. this is purely to achieve my desired visual aesthetic of looking over a large miniature or LEGO-like world. i realize this introduces issues such as occlusion and probably other stuff compared to a top-down view or a pure 2d board, but its visual aesthetic is a hard requirement, so we'll need to find ways to deal with the issues it introduces.

## Assistant

I’ll record the camera aesthetic as a hard requirement, distinguish height-driven occlusion from ground footprint, and commit the design notes separately from the conversation log.

## Assistant

Recorded and committed in [ArtDirection.md](C:/Users/k/Repository/CSharp/Automapolis/docs/ArtDirection.md) and [Mechanics.md](C:/Users/k/Repository/CSharp/Automapolis/docs/Mechanics.md):

- **Orthographic 3/4 overhead is a hard aesthetic requirement**, evoking a large miniature or LEGO-like world.
- Previous pixel art expressed tower height through a larger square sprite without height-driven overlap across cells.
- 3D height can obscure cells beyond the actual ground footprint.
- Occlusion and interaction solutions must accommodate the required aesthetic; specific solutions remain undecided.

The conversation was recorded in a separate `[cnv]` commit.
