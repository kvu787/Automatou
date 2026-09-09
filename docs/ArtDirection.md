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
course of play. The chosen approach is to give units four directional facings
and rotate their models accordingly. Keeping model rotations static and relying
on a rotatable camera to expose other sides is not the desired approach.
The standard orthographic 3/4 overhead camera aesthetic remains required.

Four facings expose different sides of a unit; this does not imply unrestricted
inspection from every elevation. Building orientation and other model-viewing
controls remain undecided. See [Mechanics](Mechanics.md) for the facing decision
and its unresolved gameplay effects.

## Current 2d shell presentation

The 2d shell uses the Kernel's text symbols for terrain and forces. Color
helps distinguish terrain and factions, while symbols and labels identify them
without depending on color alone.

- Terrain symbols remain visible in occupied cells.
- Force symbols take the central position in occupied cells.
- A count identifies cells with multiple occupants; the inspector lists them all.
- The legend, inspector, and command controls use the same symbol vocabulary.
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

## Open visual question: oblong models

Square-ish models fit square cell footprints naturally. For oblong models, the
square-footprint requirement is under reconsideration: a square footprint can
leave substantial empty ground around the model, while a non-square footprint
can fit its proportions more closely but introduces additional mechanical
complexity. No replacement footprint rule has been selected.

Resolve the desired visual fit first. Compare empty space, model proportions,
and composition in the required orthographic 3/4 view across all four facings;
defer discussion of the resulting mechanics until the visual questions have
been addressed.
