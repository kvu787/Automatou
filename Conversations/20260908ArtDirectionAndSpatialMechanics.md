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

## User

another thing to record: this shell we've been discussing will be the main shell. so, when i say "shell" unqualified, that refers to the 3d godot simplepaint 3/4 camera view shell. text shell refers to terminal-only io shell. the currently implemented symbolic visuals godot shell will be called the "2d shell". other shells in the future may include a pixel art 2d shell, non-pixel art 2d shell, other visual styles of 3d shells, etc.

## Assistant

I’ll record those shell names and update the docs to distinguish the main shell from the currently implemented 2d shell.

## Assistant

Recorded and committed. Going forward:

- **Shell**: the main 3D Godot + SimplePaint shell, with the required orthographic 3/4 overhead view.
- **Text shell**: terminal-only input/output.
- **2d shell**: the currently implemented symbolic Godot visuals.

Also recorded the possible future pixel-art, non-pixel-art 2D, and alternative 3D shells. Updated existing documentation references and committed the conversation separately.

## User

okay, let's get back to discussing the main shell.

since i'm using 3d models now, i would also like most angles of the models to be viewable. a simple way to do this is to keep the local rotations of the models static, but make the camera rotatable. however, i don't like this approach. instead, i would like the more natural but more complex approach of introducing unit orientations, meaning that units can face one of four directions.

## Assistant

I’ll record four-direction unit facing and update the earlier assumption that unit orientation is only visual. I’ll keep the effects of turning on movement and combat open for discussion.

## Assistant

**Four-direction facing fits the miniature-world aesthetic: units turn within the world, revealing different sides as they act.** I’ve recorded it and updated the earlier orientation assumptions in the design docs, with the conversation committed separately.

Your square unit footprints make this easier. A 2×2 unit still occupies the same four cells when facing north, east, south, or west. You can introduce orientation without immediately introducing rectangular footprints or complicated turning geometry.

The key decision is **what commitment facing represents**:

- **Movement:** Does a unit automatically face its direction of travel, or can it strafe and reverse while maintaining its facing?
- **Attacking:** Does it freely turn toward a target, or must the target already lie within its firing arc?
- **Ending a turn:** Can the player choose its final facing freely, or does turning consume movement or an action?

These choices determine whether facing mostly makes behavior look natural or becomes a tactical resource. For example, a tank facing east could reverse west while keeping its frontal armor toward an enemy. That only matters mechanically if movement direction and facing can differ, and armor or weapons care about direction.

My starting proposal would be **automatic facing during ordinary movement, with an explicit choice of final facing**. That keeps routine orders simple while leaving room for directional combat. I’d decide turning costs alongside those combat rules: charging for turns needs a useful tactical purpose.

The Shell can animate smoothly between the four legal orientations. One visual wrinkle is that a long model may sweep outside its footprint midway through a turn, even though its start and end positions fit; we’ll need to accommodate that in model proportions or animation.

Do you want facing to affect combat from the outset—for example, front/side/rear protection or firing arcs—or initially just determine how units move and present themselves?

## User

let's not discuss mechanics yet. we still have visual questions to answer.

for models that naturally fill in a square-ish area, it's straightforward to to determine their n*n cell footprint. orientation and rotation is also straightforward.

for models that are oblong, we have the same decision to make as before: should they have a n*n cell footprint which results in large visual empty space, or should they have a non n*n footprint which results in more complex mechanics?

## Assistant

I’ll keep this focused on visual fit and record the footprint shape as an open question for oblong models.

## Assistant

Yes—**four-direction facing brings the oblong-footprint question back into focus.** I moved into mechanics too early.

Visually, the choice is between giving each model a square area of breathing room and letting its footprint follow its proportions more closely.

- **Square footprint:** A long, narrow model sits inside a comparatively wide reservation of ground. That can work for slightly oblong vehicles, especially with broad tracks, legs, or a wide stance. For something very slender, it can make groups look artificially spread out and make the selection outline feel disconnected from the object.
- **Rectangular footprint:** The occupied area fits the model more convincingly. Long vehicles look long without also claiming equal width, and groups can form a denser, more natural-looking composition. At the four cardinal facings, a rectangle still fits the grid cleanly; the visual complication comes during the animated turn between them.

For example, a model whose ground dimensions are roughly **2 cells wide and 6 cells long** fills only one-third of a 6×6 square. That is a substantial amount of visible empty ground. A 3×4 model inside a 4×4 square is a much milder mismatch.

There is also an art-direction compromise: **make oblong models shorter and broader**. That could suit the LEGO-like aesthetic, but it would constrain which silhouettes you can use. I wouldn’t assume you want that constraint.

My visual preference would be **square footprints for roughly square models and rectangular footprints for distinctly oblong ones**. The miniature-world aesthetic gives us a reason to preserve recognizable proportions. We can establish that visual preference before deciding whether its mechanical cost is acceptable.

I’ve recorded this as an open design question; no replacement footprint rule has been chosen.


## User

what are potential mechanics for units for rectangular footprints

## Assistant

I’ll lay out a few movement and turning models, with examples of how each would make rectangular units behave. These will be options, not decisions.

## Assistant

**Rectangular footprints can support anything from simple board-game movement to vehicles that need room to maneuver.** The central choice is how a unit changes from, say, a 2×4 footprint to a 4×2 footprint when turning.

**1. Slide and pivot freely**

A unit can move in any cardinal direction without changing its facing, and rotate in place when the destination footprint fits.

- Easy to control; a player chooses a destination and final facing.
- Length matters for fitting through passages and occupying space.
- Checking only the final footprint can allow a turn whose animation passes through nearby walls or units.

This suits an abstract tactical game, or creatures and walkers that can step sideways.

**2. Slide and pivot, but require turning clearance**

Movement works as above, but a turn also requires clear space around the unit. You could check the area swept by the rotating rectangle, or use a simpler predefined clearance pattern.

- A long unit can enter a narrow passage but cannot turn around inside it.
- Courtyards and wider intersections become useful maneuvering spaces.
- A blanket “clear square around the unit” rule is easy to explain but may reject turns that would physically fit. Its dimensions also need care: the rotating rectangle can extend beyond either endpoint footprint.

This makes bulk tangible without requiring vehicle-style steering.

**3. Forward movement, reversing, and stationary turns**

A unit normally moves along its facing. It can reverse, and it can pivot where there is sufficient clearance; sideways movement is unavailable or reserved for certain unit types.

- A tank can back out of an alley while continuing to face its entrance.
- Repositioning a long unit takes more planning.
- Turning and reversing can have different movement costs, if that produces useful choices.

This is a plausible stylized starting point for tracked vehicles.

**4. Turns that also move the unit**

A turn is a predefined maneuver that changes both position and facing—such as advancing through a right-angle bend. The maneuver needs a clear route through the cells it crosses.

- Long vehicles need wide corners and approach space.
- Some vehicles might need a multi-point turn to reverse direction.
- Movement previews become essential: players should see the complete maneuver before committing.

This suits wheeled vehicles or anything whose maneuverability should be a defining limitation.

Across these options, a few rules need explicit choices:

- **Pivot location:** center, front, rear, or a defined grid anchor. A 2×3 rectangle cannot rotate 90° about its exact center and remain aligned to the same cell boundaries without shifting its center, so mixed odd/even dimensions need a convention.
- **Occupied space:** normally the entire rectangle blocks other ground units, including visually empty corners.
- **Combat:** front/side/rear armor, firing arcs, and separate turret facing are possible additions. Rectangular shape alone does not require them.

**For Automapolis, I’d explore option 2 first:** rectangular occupation with deliberate turning clearance. It would let long models fit naturally into the world and make their size meaningful. Option 3 could then distinguish vehicles from units that can sidestep, without making every unit use elaborate steering.


## User

i will need to do separate experiments to evaluate these and other options for rectangular footprints.

for now, let's stick with the simple rule: units (objects that may translate and rotate) must have square n*n foot prints. buildings (stationary objects) may have have any footprint.

## Assistant

I’ll record square footprints for units and unrestricted footprint shapes for stationary buildings, with rectangular-unit options deferred to separate experiments.

## Assistant

Recorded and committed:

- **Units:** may translate and rotate; must have square `N×N` footprints, including oblong models.
- **Buildings:** stationary; may have any footprint shape, still aligned to base-cell boundaries.
- **Rectangular units:** deferred to separate experiments.

Updated the mechanics and art-direction docs, and committed the conversation separately.
