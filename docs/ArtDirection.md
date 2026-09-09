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
