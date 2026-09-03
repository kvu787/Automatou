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
