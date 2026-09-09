# Art direction

## Production art

Kevin will create the game's 3D models in Blender using his **SimplePaint**
shader. The models and their SimplePaint materials establish the production
appearance.

The reference implementation and specification are in
[C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/SimplePaint](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/SimplePaint/README.md).

That implementation describes an opaque, unlit material whose color depends on
surface normals in an orthographic camera frame. Its controls are base color,
Brightness, Shift, Rotation, Dark Point, and Light Point. See the external
[specification](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/SimplePaint/Specification.md)
for the authoritative appearance and parameter definitions.

The referenced code is C++/HLSL. Blender integration, asset export, and the eventual
Godot presentation of those models remain separate work. No shader port or model
import is included in the current 2d shell.

## Standard camera and occlusion

The main visual Shell will present 3D models in an **orthographic 3/4 overhead
view**. This standard camera view is a hard aesthetic requirement: the world
should feel like a large miniature or LEGO-like world viewed from above.
Readability and interaction solutions must accommodate this aesthetic.

In the previous pixel-art direction, height did not produce occlusion across
cells: a tall tower would have a larger square sprite rather than overlap other
sprites and cells because of its height. With 3D models, height can project over
other cells in the standard camera view. A tall tower might occupy only one
base cell on the ground while visually occluding several cells behind it.

Ground footprint, model height, and projected screen coverage are distinct.
Occlusion and other readability or interaction issues introduced by this view
must be addressed within the required aesthetic. The specific solutions remain
undecided; the requirement does not prescribe an exact camera angle or an
occlusion treatment.

## Unit orientation and model visibility

The main Shell should allow most angles of unit models to be seen over the
course of play. The chosen approach is to give units six directional facings
and rotate their models accordingly. Keeping model rotations static and relying
on a rotatable camera to expose other sides is not the desired approach.
The standard orthographic 3/4 overhead camera aesthetic remains required.

Six facings expose different sides of a unit; this does not imply unrestricted
inspection from every elevation. Buildings do not translate or rotate; how their
fixed orientation is chosen at placement and other model-viewing controls remain
undecided. See [Mechanics](Mechanics.md) for the facing decision
and its unresolved gameplay effects.

## Current 2d shell presentation

The 2d shell uses colored fills for terrain and the Kernel's text symbols for
forces. Terrain names and descriptions remain available in the inspector and
hover text.

- Empty cells show only their terrain color, with no terrain symbol.
- Force symbols take the central position over the terrain color in occupied cells.
- A count identifies cells with multiple occupants; the inspector lists them all.
- The legend uses terrain color swatches and force symbols. The inspector and
  command controls retain the Kernel's terrain and force symbol vocabulary.
- Selection and hover remain separate interface treatments.

The symbol presentation allows gameplay work and playtesting to continue while
production models are created. See the [2d shell](../Shells/Godot/README.md)
for the symbol key.

## Physical footprint and appearance

Visual composition does not change an entity's logical footprint. Unit facing
is part of the intended game state; the Shell represents it through model
rotation. Camera angle, turn animation, and harmless visual overhang belong to
presentation; authoritative facing, movement, occupation, and collision belong
to the Kernel. See [Mechanics](Mechanics.md).

## Model proportions and hex footprints

The game now uses a hex grid. The previous square `N×N` unit footprint rule is
superseded. Units must have regular hexagonal footprints at different sizes,
with no oblong footprints. Size `S` consists of a center hex and `S - 1`
complete rings: 1, 7, 19, 37, ... occupied cells. Pets are size 1 (one cell);
ordinary humans are size 2 (seven cells). Buildings may have arbitrary footprints
composed of base hexes and are non-enterable.

Rotation does not use swept collision checks or add intermediate occupied
cells. The Shell may animate between the six facings; visual overlap during
animation is a deferred presentation TODO. The logical footprint retains its
base-cell boundary, regardless of how its outline is drawn.
Larger entities must still be able to occupy more ground and visual space.
The current 2d shell shows the prototype's single-hex forces on pointy-top hexes,
with odd rows shifted right. This does not implement the main 3D Shell.

Oblong model proportions and earlier rectangular-footprint experiments remain
relevant references, but do not define the new hex footprint rules. The main
Shell retains the required orthographic 3/4 miniature-world aesthetic.
