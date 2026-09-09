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
import is included in the current Shell.

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

## Current Godot presentation

The Godot Shell uses the Kernel's text symbols for terrain and forces. Color
helps distinguish terrain and factions, while symbols and labels identify them
without depending on color alone.

- Terrain symbols remain visible in occupied cells.
- Force symbols take the central position in occupied cells.
- A count identifies cells with multiple occupants; the inspector lists them all.
- The legend, inspector, and command controls use the same symbol vocabulary.
- Selection and hover remain separate interface treatments.

The symbol presentation allows gameplay work and playtesting to continue while
production models are created. See the [Godot Shell](../Shells/Godot/README.md)
for the symbol key.

## Physical footprint and appearance

Visual composition does not change an entity's logical footprint. Camera angle,
model orientation, and harmless visual overhang belong to presentation; movement,
occupation, and collision belong to the Kernel. See [Mechanics](Mechanics.md).
