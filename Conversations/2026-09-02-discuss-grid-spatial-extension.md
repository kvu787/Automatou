# Discuss a grid spatial extension

Date: 2026-09-02

## User

> i would like to discuss an extension to the game's grid-based spatial design.
>
> we start with an advance wars style grid. every unit, building, terrain, and "thing" occupies one cell. this is excellent for simplicity, but a major drawback is that large and powerful units occupy the same space as weak units.

## Assistant

Agreed that this exposes a real limitation, while distinguishing three concepts
that should not be collapsed into one statistic: physical footprint, combat
power, and spatial control. A Bastion can be vastly more powerful than a soldier
while remaining one person; a leviathan can be both powerful and physically
enormous.

Recommended retaining the square grid and one-cell terrain while allowing
genuinely large entities to own a small footprint of cells. The entity would
still be one game object, selected and damaged as a whole, with an anchor cell
and a footprint such as 1x1, 1x2, 2x2, or a deliberately chosen mask. This would
make large entities affect pathing, chokepoints, frontage, adjacency, placement,
and area attacks merely by existing.

Recommended a conservative first version: no rotation unless orientation earns
its complexity; entity-wide health rather than destructible individual cells;
atomic movement of the entire footprint; bounded footprint sizes; and explicit
occupancy layers for terrain, structures, ground forces, air forces, and effects.
Noted that the current prototype permits multiple forces to share a coordinate
and resolves combat through co-location, even though the renderer shows only one
force glyph, so adopting physical footprints would also require making overlap
and engagement semantics explicit.

Contrasted footprints with a cell-capacity system in which small forces consume
fewer slots and can stack. Capacity better represents concentration of several
small formations, while footprints better represent a giant entity visibly and
tactically occupying the battlefield. Recommended footprints for truly enormous
units and structures, leaving ordinary units at 1x1, rather than immediately
combining both systems.

Identified the next design question as whether the desired payoff is primarily
visual scale or actual spatial consequences. If it is spatial, the most important
consequences to choose are blocking, frontage, facing, multi-point adjacency,
and vulnerability to area attacks.

## User

> my concern about this is that it then becomes natural to model very oblong things (like a long serpent beast or a long space cruiser) as 1x2, 1x3, 1x4, etc. if we have those kinds of units, it becomes natural to expect to "rotate" the units.
>
> another issue is that the art style is this: three-quarter top-down view, facing the "bottom-right" of the image. if we support "long" units, it becomes awkward to also follow that art style strictly.
>
> so, let's avoid these complications by saying that all movable units must have square NxN dimensions

## Assistant

Agreed that requiring every movable unit to have a square N-by-N footprint is a
strong simplifying constraint. Rotation then has no effect on occupancy, so the
simulation does not need orientation-aware placement, cornering, pathfinding, or
multiple footprint masks. The art can retain one canonical three-quarter,
bottom-right presentation without implying four directional sprite sets.

Clarified that the logical footprint should describe the maneuvering room claimed
by a unit rather than trace its hull or body pixel for pixel. Long creatures can
coil, curl, crouch, or run diagonally through their square composition; long
vehicles can be shown with bottom-right foreshortening. The opaque sprite may
also rise or overhang outside its ground footprint as long as its base and shadow
make the occupied square legible.

Proposed treating orientation as absent from the authoritative game state, with
facing used only as a consistent visual convention. Suggested 1x1 for ordinary
units, 2x2 for large units, and 3x3 for colossal units, with larger squares
reserved for exceptional set pieces because the current theater is only 16x12.
Physical footprint remains separate from power, so a Bastion can stay 1x1 while
a less individually powerful but physically enormous organism occupies 2x2.

Noted a few rules that will eventually need precise definitions: the anchor cell
for even-sized footprints; full-footprint passability; range measured between
the nearest occupied cells; and area effects damaging an entity once rather than
once per covered cell unless large-target vulnerability is an intentional rule.
Recommended applying the square requirement specifically to movable units while
leaving immovable structures undecided, rather than accidentally committing
buildings to the same rule before their placement behavior is explored.

## User

> okay, now we need to discuss the smallest "unit size" for units and buildings. units are things that can move and buildings are things that cannot move. units must be NxN. buildings can be any dimension.
>
> the natural starting point is for a human to be 1x1. however, i would also like to have units that are smaller than humans. at the very least, there should be dogs/cats. so at the least, we have dogs/cats at 1x1 and humans at 2x2.
>
> from here, we basically need to decide how many units a human should be. let's call this quantity H. it should be at least 2, but probably no greater than 10.
>
> how do different of values of H affect other aspects of the game? how do we decide H?

## Assistant

Identified `H` as the game's spatial resolution rather than merely an art-scale
choice. A human occupies `H x H`, or `H squared`, base cells. Consequently, `H`
directly controls how many smallest units fit into the same ground area, how many
can contact a human's perimeter, how finely buildings can be laid out, and how
many grid nodes the map and pathfinder must process.

Explained the principal scaling effects:

- a human covers `H squared` base cells;
- `H - 1` integer footprint sizes exist below a human;
- `4H` one-cell positions share an orthogonal edge with a human footprint in
  open space;
- maintaining the same theater measured in human widths multiplies each map
  dimension by `H` and the cell count by `H squared`;
- movement distances, weapon ranges, road widths, doors, and other physical
  measurements must generally scale linearly with `H`;
- at fixed screen size, each base cell becomes proportionally smaller and the
  smallest units become harder to read; keeping cell pixels fixed instead makes
  the map proportionally larger and increases the need for zooming or panning.

Noted that higher `H` also makes multi-cell terrain interactions more common. A
human can straddle many terrain cells, so passability, movement cost, cover, area
damage, and heterogeneous ground beneath one entity require clearer aggregation
rules. Procedural generation should express feature widths in human-relative
units so it does not create meaningless one-cell cracks everywhere.

Distinguished the ground footprint from literal body measurements and sprite
dimensions. `H` measures the maneuvering envelope claimed by a standing human,
not human height; a three-quarter-view sprite may extend beyond that footprint.
Therefore real-world height ratios between a person and a cat are not a sound
way to select `H`.

Also distinguished enterable from non-enterable buildings. If buildings remain
whole exterior game pieces, increasing `H` adds little beyond finer silhouettes
and adjacency. If tactical movement occurs inside buildings, `H` determines the
resolution of walls, doors, corridors, rooms, and creature-only passages and can
justify a larger value. Each additional integer size below `H` also creates a
potential clearance class for passages that admit smaller units but block humans.

Compared the most plausible choices. `H = 2` gives four base cells per human,
one subhuman size class, eight orthogonally adjacent one-cell attackers, and four
times as many map cells as an equivalent human-scale grid. It preserves the
board-game character and naturally supports one-cell walls and two-cell doors or
corridors. `H = 3` gives nine cells per human, two subhuman size classes, twelve
orthogonally adjacent attackers, a true center cell, and nine times the map
nodes. It offers substantially better interior and micro-unit resolution. `H =
4` gives sixteen cells per human and sixteen times the map nodes; it is justified
only if small-unit swarms and detailed building interiors are central. Values
from five through ten increasingly turn the game into a fine-grained spatial
simulation, make cats and dogs extremely small relative to people, and erode the
clarity gained from the Advance Wars foundation.

Recommended deciding `H` using four concrete questions: how many one-cell units
should pack into a human-sized area; how many should be able to surround and
engage one human; whether a distinct intermediate size below humans is needed;
and how much architectural resolution is needed for walls, doors, corridors,
and rooms. The governing principle is to choose the smallest `H` that satisfies
actual roster and building requirements.

Recommended `H = 2` as the provisional default because it creates the requested
human-versus-animal distinction at the lowest possible complexity cost. `H = 3`
is the strongest alternative if the design needs a meaningful 2x2 unit smaller
than a human, a central cell, or more detailed building interiors. Recommended
not choosing `H >= 4` without first identifying a specific mechanic that requires
it.

## User

> okay, let's lock in at 2x2 for a human for now.
>
> we need to start a mechanics design doc. create a document called Mechanics.md in the docs folder and fill it in with what we have so far.

## Assistant

Created `docs/Mechanics.md` as the authoritative starting point for game
mechanics. Recorded the Advance Wars-style orthogonal base grid, terrain at
base-cell resolution, `H = 2`, 2x2 humans, and 1x1 cats and dogs.

Defined movable entities as units with positive-integer square `NxN`
footprints, while buildings are immovable and may use different integer widths
and heights. Preserved the separation between spatial footprint and combat
power, allowing a human-sized Bastion to be far stronger than physically larger
entities.

Recorded the connection to the established three-quarter, down-right art
direction: logical footprints are independent of sprite silhouettes and do not
rotate, while long movable subjects can be coiled, diagonally composed, or
foreshortened within square compositions.

Kept proposals from becoming accidental decisions by listing unresolved rules
for anchoring even footprints, stacking, movement, pathfinding, range,
adjacency, area effects, mixed terrain, building interiors, maximum dimensions,
human-relative scaling, and occupancy layers.
