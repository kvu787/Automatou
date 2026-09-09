# Square versus hex grids for a turn-based game

Date: 2026-09-08

## User

> what are the pros/cons and tradeoffs between a square grid vs hex grid for a turn-based game

## Assistant

**Square grids fit buildings and spatial puzzles well; hex grids give you more consistent movement in open terrain.** Neither is inherently more strategic—the important difference is how adjacency shapes movement, combat, and map design.

| Consideration | Square grid | Hex grid |
|---|---|---|
| Movement | 4 directions, or 8 with diagonals; requires a diagonal policy | 6 equally spaced neighbors with equal center-to-center distance |
| Distance and range | Simple, but the chosen metric strongly changes reachable areas | Consistent neighbor steps; range areas still have a hexagonal shape |
| Buildings and interiors | Rectangular rooms, streets, doors, and furniture fit naturally | Right angles and rectangular footprints fit awkwardly |
| Open terrain | Four-way movement can feel restrictive; diagonals add exceptions | Smooth spread of movement directions without diagonal exceptions |
| Front lines | Four-way adjacency limits contact; eight-way adjacency permits more | Six neighbors permit more contact than four-way squares, less than eight-way squares |
| Terrain and art | Easy tiling, rectangular assets, and alignment | More specialized tile shapes, edge transitions, and asset placement |
| Implementation | Straightforward coordinates, editing, and hit detection | Less familiar coordinate math; established axial/cube methods keep it manageable |

**The biggest square-grid decision is whether diagonals exist.**

- **Four-way movement:** simple integer costs and strong chokepoints, but moving across open ground requires stair-step paths.
- **Eight-way movement, equal costs:** convenient, but diagonal travel covers more physical distance per action. Movement ranges become squares, and diagonal routes can be disproportionately valuable.
- **Eight-way movement, higher diagonal costs:** better approximates physical distance, but complicates movement budgets. A cost of about 1.414 times a straight step matches geometry; integer approximations introduce some distortion.

Allowing diagonals also means deciding whether units can squeeze between blocked corners and whether melee attacks, line of sight, and cover follow the same rules. Hexes remove that particular category of exception, although visibility and cover still require explicit rules.

**For combat, adjacency matters more than appearance.** With one unit per tile, surrounding a unit requires occupying 4, 6, or 8 neighboring tiles, depending on your rules. This affects blocking, retreat, melee concentration, and how easily a line holds. Hexes do not automatically create more flanking: map layout, movement allowances, zones of control, and attack ranges matter just as much.

**For development, hexes add some geometry work, not a fundamentally harder pathfinding problem.** Both work with standard graph algorithms such as A*. Squares usually make buildings, placement tools, and rectangular multi-tile units easier. Hexes offer natural 60-degree rotations; squares offer natural 90-degree rotations. That distinction becomes significant for footprints, facing, and directional attacks.

My design rule of thumb:

- **Choose squares** when rooms, streets, construction, cover placement, or tightly arranged objects are central.
- **Choose hexes** when maneuvering armies or squads across open terrain is central, and you want uniform adjacent movement costs.

For a game mixing cities and outdoor combat, I would let **the importance of building footprints versus open-field maneuvering** decide. Prototype the same small encounter on both grids; compare chokepoints, retreat routes, and movement previews before committing to the art pipeline.
