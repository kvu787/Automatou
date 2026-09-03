# Mechanics

This document records the current mechanical design of Automapolis. It begins
with the spatial foundation and will grow as additional systems are decided.
Rules stated as current decisions are authoritative for new design work;
unresolved details are listed separately rather than decided by implication.

## Grid and scale

Automapolis uses an Advance Wars-style orthogonal square grid. A **base cell** is
the smallest addressable unit of ground area.

- Terrain is defined one base cell at a time.
- All footprints align to base-cell boundaries.
- Footprint measures physical occupation and maneuvering room, not combat power.
- A small, powerful entity can occupy less space than a large, weak entity.
- A logical footprint does not need to trace every visible pixel of an entity.

The scale reference is `H`, the side length of an ordinary human's square
footprint measured in base cells. The current design sets:

- `H = 2`;
- an ordinary human occupies 2×2 base cells;
- a cat or dog occupies 1×1 base cell.

Consequently, one human occupies the same ground area as four 1×1 entities.
Eight 1×1 positions can share an orthogonal boundary with a 2×2 human in open
ground. These are spatial consequences, not automatic rules for stacking or the
number of creatures allowed to attack at once.

`H = 2` is the current working scale because it provides one meaningful size
below a human while retaining the grid's board-game simplicity. A larger value
would increase positioning and architectural resolution, but would also increase
map area, pathfinding space, packing density, and visual demands approximately
with the square of `H`.

## Spatial entities

### Units

A **unit** is an entity that can move.

- Every unit occupies a square `N×N` footprint, where `N` is a positive integer.
- A unit's footprint moves as one indivisible shape.
- Movable rectangular footprints such as 1×2, 1×3, or 2×4 are not allowed.
- Footprint size is independent of strength, durability, range, and narrative
  importance. A Bastion can therefore remain human-sized despite exceptional
  power.

Square footprints make spatial occupation independent of orientation. The
mechanics do not currently require unit rotation or orientation-aware footprint
masks.

### Buildings

A **building** is an entity that cannot move.

- A building may have any positive integer width and height in base cells.
- A building is not required to be square.
- Whether buildings are enterable, and whether a building may use a footprint
  more complex than a rectangle, remain undecided.

### Terrain

Terrain belongs to individual base cells and exists beneath units and buildings.
The rules for an entity whose footprint covers multiple terrain types remain
undecided.

## Spatial presentation

Unit art uses the established three-quarter top-down view with the subject facing
down-right. Spatial mechanics and art composition are intentionally decoupled:

- the canonical visual facing does not rotate the logical footprint;
- long creatures may coil, curl, crouch, or pose diagonally within a square
  composition;
- long vehicles may use diagonal composition and foreshortening;
- transparent pixels and harmless visual overhang do not expand the logical
  footprint.

This preserves a consistent visual direction without requiring directional
sprite sets, turning rules, orientation-aware collision, or special pathfinding
for long movable entities. See [Art direction](ArtDirection.md) for the broader
visual language.

## Unresolved spatial rules

The following rules must be decided before variable footprints are implemented:

- how an entity's position identifies an even-sized footprint such as 2×2;
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
  to `H`;
- whether air, ground, underground, structure, and effect layers can share the
  same cells.
