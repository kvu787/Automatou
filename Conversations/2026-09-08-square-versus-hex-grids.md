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


## User

> compare the optimality of algorithms and datastructures for square vs hex grids

## Assistant

**Square and hex grids have essentially the same asymptotic efficiency and shortest-path guarantees.** Squares simplify storage and some spatial operations; hexes simplify movement rules compared with squares that allow diagonals.

You need to compare **four-way squares, six-way hexes, and eight-way squares** separately.

**Algorithm costs**

Let `N` be the number of traversable tiles. Assume local movement, constant-time tile access, and nonnegative movement costs.

| Operation | Four-way square | Hex | Eight-way square |
|---|---|---|---|
| Candidate neighbors per expansion | 4 | 6 | 8 |
| Tile lookup in a dense array | O(1) | O(1) | O(1) |
| BFS / flood fill, worst case | O(N) | O(N) | O(N) |
| Dijkstra with a binary heap, worst case | O(N log N) | O(N log N) | O(N log N) |
| A* with a consistent heuristic and binary heap, worst case | O(N log N) | O(N log N) | O(N log N) |
| Search working memory | O(N) | O(N) | O(N) |

These bounds follow because each tile has a fixed maximum number of edges: all three graphs have O(N) edges.

Hexes examine 50% more neighbor candidates than four-way squares **for the same number of expanded tiles**, but that does not mean 50% longer searches. Different connectivity, obstacles, and heuristics change how many tiles get expanded.

**Shortest-path optimality**

All three support equally strong correctness guarantees:

- **BFS** finds minimum-cost paths when every move costs the same.
- **Dijkstra** handles varying nonnegative costs.
- **A*** also finds minimum-cost paths with the appropriate heuristic and termination rules. A consistent heuristic allows closing nodes without reopening them; an admissible but inconsistent heuristic can require reopening.

The search algorithm operates on a graph; the grid changes its neighbors and heuristic. [Red Blob Games’ implementation guide](https://www.redblobgames.com/pathfinding/a-star/implementation.html)

For obstacle-free grids, these distances are exact:

| Movement rules | Distance |
|---|---|
| Four-way squares, cost 1 | `abs(dx) + abs(dy)` — Manhattan |
| Eight-way squares, all moves cost 1 | `max(abs(dx), abs(dy))` — Chebyshev |
| Eight-way squares, straight cost 1, diagonal cost √2 | `max(abs(dx), abs(dy)) + (√2 − 1) × min(abs(dx), abs(dy))` — octile |
| Hexes, cost 1, axial coordinates | `max(abs(dq), abs(dr), abs(dq + dr))` |

The hex formula uses an implicit third coordinate, `s = -q-r`. [Hex coordinate and distance reference](https://www.redblobgames.com/grids/hexagons/#distances)

Obstacles preserve these distances as lower bounds. For weighted terrain, scale by a valid minimum movement cost for the chosen metric. Teleports and other shortcuts can invalidate geometric lower bounds.

**An optimal path means cheapest under your movement rules**, not necessarily shortest in physical space. Equal-cost square diagonals are perfectly compatible with optimal A*.

**Data structures**

| Structure | Comparison |
|---|---|
| **Flat tile array** | Excellent for both. Squares map directly to `y * width + x`. Rectangular hex maps can use offset rows with the same indexing. |
| **Coordinates** | Squares need two integers. Hexes also need only two using axial coordinates; storing all three cube coordinates is optional. |
| **Neighbor storage** | Both can calculate neighbors on demand. Axial hexes use six fixed offsets; offset-row hexes use parity-dependent offsets. |
| **Hash map** | Useful for sparse or irregular maps of either type. Hexes do not require hashing. |
| **Search state arrays** | Both can use integer tile IDs with contiguous cost, predecessor, and visited arrays. |
| **Chunks and bitsets** | Available to both. Square row/column operations are simpler; offset hexes sometimes need separate odd/even-row logic. |

Hex coordinate systems can separate convenient storage from convenient geometry. [Hex storage reference](https://www.redblobgames.com/grids/hexagons/#map-storage)

**Movement-range queries have a real constant-factor difference.** On an unlimited empty grid, the number of tiles within `r` unit-cost steps, including the origin, is:

| Grid | Tiles within range | At range 10 |
|---|---:|---:|
| Four-way square | `1 + 2r(r + 1)` | 221 |
| Hex | `1 + 3r(r + 1)` | 331 |
| Eight-way square | `(2r + 1)²` | 441 |

These counts follow by summing successive rings. Enumerating every reachable tile therefore requires O(r²) work in all three, but equal movement budgets produce different amounts of output. This compares **steps**, not equal physical areas.

For a turn-based game, I would use **dense tile arrays, integer tile IDs, and implicit neighbors on either grid**. Use BFS for uniform-cost movement ranges, bounded Dijkstra for weighted ranges, and A* for individual routes. Reuse a search result when many queries share an origin or destination.

**Grid shape is usually a smaller performance decision than how many searches you run, how much you allocate, and whether you reuse results.** Choose the topology for gameplay; benchmark representative turns before trading away that design for presumed speed.

