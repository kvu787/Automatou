Godot 4.7.2 .NET C#
.NET 10, C# version 14
Turn-based strategy
2D
hex grid, pointy top
combat-focused

Hex grid
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

Buildings
- A building is something that occupies cells and doesn't move.
- A building has a single health pool.
- A single building consists of one island of connected cells.
- When a cell is occupied by a building, it's terrain is irrelevant.
- When a buliding is destroyed, the terrain of the underlying cells returns.

Terrain

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
