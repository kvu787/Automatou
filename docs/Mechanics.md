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
- A logical footprint does not need to trace an entity's visible silhouette.

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

A **unit** is an object that may translate and rotate.

- Every unit occupies a square `N×N` footprint, where `N` is a positive integer.
- A unit's footprint moves as one indivisible shape.
- Movable rectangular footprints such as 1×2, 1×3, or 2×4 are not allowed.
- Footprint size is independent of strength, durability, range, and narrative
  importance. A Bastion can therefore remain human-sized despite exceptional
  power.

Units have an orientation with four possible facings aligned to the square
grid's cardinal directions. This is a design decision, not yet an implemented
system. It replaces the earlier assumption that unit orientation is purely
visual. The main Shell will rotate unit models to represent their facing.

Square footprints keep occupied cells unchanged across the four facings;
introducing facing does not change the square-footprint requirement. Turning
costs, how facing changes during movement and attacks, and any directional
combat effects remain undecided.

Rectangular unit footprints and alternative movement/turning rules are deferred
to separate experiments. For now, all units retain square footprints, including
units with oblong models.

### Buildings

A **building** is a stationary object: it does not translate or rotate.

- A building may have any footprint shape, aligned to base-cell boundaries.
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

- unit models show their four-direction facing; facing does not change the
  cells occupied by a square unit footprint;
- an entity's appearance may extend beyond its occupied area without changing
  collision or movement;
- logical occupation remains defined by the square unit and arbitrary building
  footprints above.

Camera and model composition choices are part of [Art direction](ArtDirection.md).
The standard 3D view is orthographic 3/4 overhead, a hard aesthetic requirement.
Model height can occlude cells outside the ground footprint: a one-cell tower
may hide other cells without occupying them. This camera occlusion does not by
itself establish gameplay line-of-sight or cover rules. Occlusion handling
remains an unresolved presentation and interaction decision.

Unit facing is an explicit design decision described above. Camera composition
alone does not determine turning rules or orientation-aware footprint masks.

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

## Unresolved facing rules

- Whether turning is automatic, explicitly commanded, or both.
- Whether turning costs movement or actions, and whether costs vary by unit.
- How movement direction, final facing, and attacks interact.
- Whether facing affects attack arcs, defense, vision, or other mechanics.
- How turns are animated and how visual clearance is handled during a turn.
- How a building's fixed orientation is chosen at placement; buildings remain
  stationary after placement.
