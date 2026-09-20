# Specification

## Overview

- Godot 4.7.2 .NET C#
- .NET 10, C# version 14
- 2D
- Hex grid
- Turn based

## The style of this game

The genre of this game isn't well represented in the current landscape of games. Basically, it is
for people like me who like to program, understand, create, experiment with, and watch worlds and
their diverse inhabitants rise/fall, interaction with each other, etc. For example, I enjoy
setting up large scale battles in strategy games such as Age of Empires and Homeworld and watching
the armies fight without explicitly controlling any faction or unit. However, most strategy games
are poorly suited to that style of creative sandbox play because they have poor tooling for
creating your own factions, units, game systems, AI logic, etc., and they aren't open source. Also,
they are built around an experience of a player managing their own faction and fighting other
factions, instead of watching factions fight from an omniscient perspective.

Although I want to gradually add increasing complex simulation mechanics and systems to this game,
this game isn't like Dwarf Fortress, Rimworld, and Caves of Qud or other games that have complex
simulation systems in that the player's focus isn't to control something and achieve certain
objectives or favorable conditions for that thing. (A player *can* do that, but the game isn't
specifically created to curate that experience.)

The concept of "emergence" is key to this game, in that fun, interesting, and complex experiences
can be found when relatively simple components interact with each other.

The most succinct way I can describe this game is a "world-builder-and-runner" game.
John Conway's "Game of Life" could be viewed as the fundamental progenitor of this style of game.

## Automata

What most games refer to as "artificial intelligence (AI)", this game refers to as "automata (AT)"
Basically this is the logic for how agents in the world act without explicit player input.

For now, this game is focused on combat, similar to games like Starcraft and Age of Empires.
Other mechanics will be added later to make this a proper world simulator instead of just a battle
simulator.

The ATs for factions should focus on "thematic" gameplay instead of "optimal" gameplay.

## Visual style

Everything is represented simply using colors, shapes, and symbols.
There is no 2D or 3D "art".

## Spatial model

- The world is a 2D hexagonal grid
- All player-facing representations of the hex grid should use coordinates that are natural to people
  - This means that +x means right and +y means up
- Every row that has an odd index is offset to the east.
  - Let's say that cell (0, 0) is at the bottom-left corner of the screen. Then:
    - Cell (1, 0) is directly east of cell (0, 0).
    - Cell (0, 1) is northeast of the cell (0, 0).
    - Cell (0, 2) is northwest of the cell (0, 1).

## World

- Each cell has one terrain type
- A cell can be occupied or unoccupied
- Occupied cells may contain a unit cell or a buliding cell

## Units

- All units have a regular hexagonal shape.
- The origin of a unit is its central cell.
- A unit has a size. It defines its "radius".
  - Size 1 = 1 occupied cells
  - Size 2 = 7 occupied cells
  - Size 3 = 19 occupied cells
  - Size 4 = 37 occupied cells
  - Etc...
- 

## Buildings

- A building has an origin.
  - The positions of the building's cells are defined with respect to the origin.
  - The position and rotation of the building uses the building's origin.
- A building is something that occupies cells and doesn't move.
- A building has a single health pool.
- A single building consists of one island of connected cells.
- When a cell is occupied by a building, it's terrain is irrelevant.
- When a buliding is destroyed, the terrain of the underlying cells returns.

## Position and rotation

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

## World view

- This is the standard gameplay view
- The player sees the grid map with terrain, units, and buildings
- Player can pan by holding the middle mouse button and moving the mouse
- Player can zoom in/out with the scroll wheel.

## World creator

- When creating a new map, you can start with:
  - a hexagonal map and specify a positive integer for the size
  - a rectangular map and specify a positive integer for the width and a positive integer for the height
- This is used by players to manually author maps.
- These maps are an alternative to auto-generated maps.

## Building creator

- This is used to create buildings.
- All buildings must be manually created
- There are no "auto-generated" buildings.
you can change the origin (pivot) point of the building
start with a 20x10 grid. user can click the edge of the current grid to add a section
when opening a building, grid patches are created until building fits.
a setting controls the grid patch size
when saving, the position of the building's origin on the grid is recorded
when placing a building on the map, you may rotate it about its origin

## Unit creator

- This is used to create units.
- Unit size is defined as an integer 1 or greater

## Terrain types

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

## Theme

Takes place in a universe that has both futuristic/sci-fi technology and fantasy magic systems.

## Factions

### Bastions

- Human souls that embody extremely technologically and magically advanced humanoid bodies.
- Ultra-heavy one-man-army super-soldier infantry.
- Imbued with a human soul, but contains zero human body or biological parts.
- Armed with one ranged weapon and one melee weapon.
- Hulking, bulky, extremely heavily armored figure.

#### Culture

- Reproduction is done through a crafting- and engineering-like process with magical elements
- There are very few Bastions in the universe 
- Although the form and characteristics of Bastions are extremely distinct and instantly recognizable,
  Bastions themselves are highly individualistic. They have a much stronger tendency to live alone
  or in much smaller groups than other factions.

### Travelers

Human-like beings
Will get sick and die if they roam for too long or stay in the same place for too long.
Have a large mobile town called the H.O.M.E.: Habitation, Operation, Mobility, Environment
Can switch between mobile and stationary base very quickly
High mobility is a key part of defense and offense
Big on evasion as opposed to high health or armor
Strong on kiting, fast in-and-out strikes, surprise attacks, stealth, as opposed to direct assaults
Naturally strong in trading since they travel to many places (like the Bentusi)
Weak on long/medium ranged combat because that armament requires slower move speed for accurate targeting

### Mech and tank faction

- Major focus on vehicles such as wheeled vehicles, tanks, aircraft, mechs (vehicles that use some arrangement of legs to move)
- Strong close and medium range capabilities
- No long range capability so they remain distinct from the infantry+artillery faction (lore reason tbd)

### Infantry and artillery faction

#### Culture

- Massive cloning facilities to produce huge numbers of soldiers
- Infertile by default so that they cannot have children
- All reproduction is done through cloning
- This makes it so that soldiers do not have familial attachments so they more easily die for the war cause
- Leadership is currently a mystery

#### Strategy and tactics

- Little concern for infantry casualties, which forms the basis of their artillery+infantry strategy
- Absurdly powerful long-range artillery that causes massive damage to everything at impact site
- Massive numbers of weak infantry are used as fodder to scout enemies and keep them busy until artillery can target them and fire
- Infantry are adept at moving quickly through dangerous terrain at the cost of taking damage

### Prytu

Led Prytus, a super-powerful, super-intelligent entity obsessed with destroying all sentient life which it deems "lesser", which is all sentient life except Prytus
Similar to ridley scott's Alien, Zerg, Tyranids
Several "manifestations" of Prytus as extremely powerful Prytu creatures have been seen
No one knows the "true form" of Prytus
Prytus controls all Prytu units directly. There are no Prytu intelligences which are independent from Prytus.
