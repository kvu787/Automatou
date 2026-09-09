# Mechanics

This document records the current mechanical design of Automapolis. It begins
with the spatial foundation and will grow as additional systems are decided.
Rules stated as current decisions are authoritative for new design work;
unresolved details are listed separately rather than decided by implication.

## Player interaction

There is one player configuration, with optional field interventions. Advancing
turns without submitting other commands provides passive observation. There is
no separate zero-player mode or mode switch; intervention remains available.

## Grid and scale

Automapolis uses a **pointy-top hexagonal grid**. A base cell is one hexagon.
Each interior cell has six edge-sharing neighbors, with travel axes 60 degrees
apart. This replaces the previous orthogonal square grid.

- Coordinates are odd-row offset: `X` is column, `Y` is row, and odd rows are
  shifted half a hex to the right. Width and height count columns and rows.
- The Kernel converts offset coordinates to axial coordinates for hex distance.
  Movement, threat range, diffusion, spawning adjacency, and purge radii use
  this topology. Radius counts hex steps, not Manhattan distance.
- Terrain is defined one hex at a time. Footprints are sets of occupied hexes.
- Footprint measures physical occupation and maneuvering room, not combat power.
- Variable entity sizes remain a design requirement. The current prototype
  still represents each force or building at a single hex and permits sharing.
- The previous `N×N` square footprint rule and `H = 2` human scale are superseded.
  Units must have regular hexagonal footprints at different sizes; buildings
  may have arbitrary footprints. Unit size counts the center cell plus complete
  surrounding rings, as specified below.

The six axes are considered an acceptable directional resolution. Paid turns
can still favor long straight segments over alternating hex steps; that
remaining limitation is understood and is not a reason to retain squares.

## Spatial entities

### Units

A **unit** is an object that may translate and rotate.

The intended orientation system has six facings along the hex neighbor axes:
east, southeast, southwest, west, northwest, and northeast. Adjacent facings
are 60 degrees apart. The main Shell will rotate models to represent facing.
The prototype does not yet store facing or implement paid rotation.

The high-level motion design remains that most units translate forward and
rotate to change travel direction, with both translation and rotation costing
action. Exact costs, exceptions, and attack interactions remain undecided.
The current autonomous prototype instead advances a mobile force up to one
neighboring hex per explicit turn, without an action-point or facing system.

Units must have regular hexagonal footprints, small or large; oblong unit
footprints are not allowed. Neither units nor buildings are limited to one hex.
Footprint size remains independent of combat strength.

A unit's position identifies its center hex. Size is a positive integer:
size `S` occupies the center plus `S - 1` complete rings, equivalently every
base hex at hex distance at most `S - 1` from the center. The occupied-cell
count is `1 + 3 * S * (S - 1)`, giving 1, 7, 19, 37, ... cells.

- Pets are size 1: one occupied cell.
- Ordinary humans are size 2: seven occupied cells.
- Larger unit sizes continue the same complete-ring sequence.

The footprint follows the actual base-cell boundaries. Its sixfold symmetry
means that rotating about its center in 60-degree increments leaves the
occupied-cell set unchanged. **Do not perform swept collision checks during
rotation or require additional swept clearance.** Intermediate model angles
are presentation, not additional logical occupation.

Earlier rectangular-footprint experiments remain separate from the implemented
game. The complete-ring footprints and reference sizes are design decisions;
the prototype still uses single-cell entities.

### Buildings

A **building** is a stationary object: it does not translate or rotate.

- A building may have any footprint shape, composed of base hexes.
- Building footprints are not restricted to squares or rectangles.
- Whether buildings are enterable remains undecided.

### Terrain

Terrain belongs to individual base cells and exists beneath units and buildings.
The rules for an entity whose footprint covers multiple terrain types remain
undecided.

## Spatial presentation

Production art will use Blender models with SimplePaint materials. The current
2d shell represents entities with symbols. Both presentations are independent
of the spatial rules:

- the main Shell will show six-direction unit facing; turns preserve the
  complete-ring occupied footprint and do not use swept collision checks;
- an entity's appearance may extend beyond its occupied area without changing
  collision or movement;
- logical occupation is defined by occupied hexes, independently of projected
  screen coverage.

Camera and model composition choices are part of [Art direction](ArtDirection.md).
The standard 3D view is orthographic 3/4 overhead, a hard aesthetic requirement.
Model height can occlude cells outside the ground footprint: a one-cell tower
may hide other cells without occupying them. This camera occlusion does not by
itself establish gameplay line-of-sight or cover rules. Occlusion handling
remains an unresolved presentation and interaction decision.

Unit facing is an explicit design decision described above. Camera composition
alone does not determine turning rules or orientation-aware footprint masks.

## Deferred spatial TODOs

The following questions are deferred; they do not reopen the footprint, scale,
or no-sweep decisions above:

- whether allied or opposing entities may overlap or stack;
- how movement cost and passability combine across every covered cell;
- how paths account for the complete moving footprint and narrow clearances;
- how range, adjacency, engagement, and zones of control interact with footprint
  edges;
- whether an area effect damages a large entity once or once for each covered
  cell;
- how cover, hazards, and other terrain effects combine beneath a multi-cell
  entity;
- whether buildings contain traversable interior cells;
- maximum unit and building footprint dimensions;
- how movement speed, weapon range, and generated feature widths scale relative
  to the size-2 human reference;
- whether air, ground, underground, structure, and effect layers can share the
  same cells.

## Deferred facing TODOs

- Whether turning is automatic, explicitly commanded, or both.
- The action costs of translation and rotation, their shared or separate budgets,
  and whether costs vary by unit.
- Which units may reverse or move sideways as exceptions to forward-only travel.
- How final facing and attacks interact with paid rotation.
- Whether facing affects attack arcs, defense, vision, or other mechanics.
- How turns are animated and visual overlap is presented, without swept
  collision checks.
- How a building's fixed orientation is chosen at placement; buildings remain
  stationary after placement.

## Other deferred TODOs

- Placement and boundaries of arbitrary building footprints, including holes,
  interiors, and the visual fit of rectangular architecture to hex cells.
- Corridor, road, doorway, and bridge widths for different unit sizes.
- Whether translation distance is measured per base hex independently of size.
- Facing sectors and boundary cases if directional combat or vision is added.
- Footprint overlays and occlusion handling in the required main Shell camera.

The six-facing design, paid translation/rotation direction, variable-size
requirement, stationary buildings, and orthographic 3/4 miniature-world aesthetic
remain in effect. The prototype still uses single-cell forces; these footprint
requirements are design decisions rather than implemented features.
