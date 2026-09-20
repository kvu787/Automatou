Godot 4.7.2 .NET C#
.NET 10, C# version 14
Turn-based strategy
2D
hex grid, pointy top
combat-focused

Spatial model
- The world is a 2D hexagonal grid
- All player-facing representations of the hex grid should use coordinates that are natural to people
  - This means that +x means right and +y means up
- Every row that has an odd index is offset to the east.
  - Let's say that cell (0, 0) is at the bottom-left corner of the screen. Then:
    - Cell (1, 0) is directly east of cell (0, 0).
    - Cell (0, 1) is northeast of the cell (0, 0).
    - Cell (0, 2) is northwest of the cell (0, 1).

Map
- Each cell has one terrain type

Units
- All units have a regular hexagonal shape.
- The origin of a unit is its central cell.
- A unit has a size. It defines its "radius".
  - Size 1 = 1 occupied cells
  - Size 2 = 7 occupied cells
  - Size 3 = 19 occupied cells
  - Size 4 = 37 occupied cells
  - Etc...
- 

Buildings
- A building has an origin.
  - The positions of the building's cells are defined with respect to the origin.
  - The position and rotation of the building uses the building's origin.
- A building is something that occupies cells and doesn't move.
- A building has a single health pool.
- A single building consists of one island of connected cells.
- When a cell is occupied by a building, it's terrain is irrelevant.
- When a buliding is destroyed, the terrain of the underlying cells returns.

Terrain

Position and rotation
- Position defines where something is in the world
- Rotation defines what direction something is facing.
  - Rotation has 6 values: northwest, northeast, east, southeast, southwest, west
- Terrain has a position but no rotation.
  - However, we don't really treat each terrain as a separate "thing". It's better represented as an attribute of a cell.
- Units and buildings have positions and rotations.
- The position of a unit/building is the position of its origin in the world.
- Building rotation
  - This is used to determine where to place its cells with respect to the origin.
  - Unlike unit rotation, it doesn't matter for other stuff.
- Unit rotation
  - Unlike building rotation, it doesn't matter spatially because units are always regular hexagons.
  - However, unit rotation is used to for many other things.
  - Attack region: Most units can only attack in the direction they are facing.
  - Defense stats: Most units have strong defense when attacked from the front, medium at the front sides, and weak at the rear sides and rear.
  - Movement: Most units can only travel in the direction they are facing. Turning costs action points.

Map view
- This is the standard gameplay view
- The player sees the grid map with terrain, units, and buildings
- Player can pan by holding the middle mouse button and moving the mouse
- Player can zoom in/out with the scroll wheel.

Map creator
- When creating a new map, you can start with:
  - a hexagonal map and specify a positive integer for the size
  - a rectangular map and specify a positive integer for the width and a positive integer for the height
- This is used by players to manually author maps.
- These maps are an alternative to auto-generated maps.

Building creator
- This is used to create buildings.
- All buildings must be manually created
- There are no "auto-generated" buildings.
you can change the origin (pivot) point of the building
start with a 20x10 grid. user can click the edge of the current grid to add a section
when opening a building, grid patches are created until building fits.
a setting controls the grid patch size
when saving, the position of the building's origin on the grid is recorded
when placing a building on the map, you may rotate it about its origin


Unit creator
- This is used to create units.
- Unit size is defined as an integer 1 or greater

Factions
- 

Terrain types
- Water
- Air
- Space
- Forest
- Plains
- Mountain
- Wetlands
- Paved
- Desert
- Tundra
- Exclusion zone
