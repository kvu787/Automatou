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
