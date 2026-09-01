# Create sci-fi unit sprite roster

Date: 2026-09-01

## User

> create a variety of sprites for sci-fi units, size 16x16. take inspiration from advance war franchise sprites, which are size 16x16 and very legible.

## Assistant

Created sixteen original 16x16 tactical unit sprites with compact, highly separated silhouettes. The human roster contains a recon bike, striker tank, siege crawler, missile carrier, hover transport, gunship, interceptor, and combat drone. The alien roster contains a skitterling, acid spitter, carapace beast, burrower, living artillery, winged hunter, leviathan, and biomass harvester.

Generated a broader design-reference sheet with the built-in image generator, then translated the designs into exact palette-indexed grids rather than treating the generated pixels as production assets. Added individual true-alpha PNGs, a 4x4 unit sheet, a checkerboard nearest-neighbor preview, atlas and manifest updates, Godot imports, an expanded browser catalog, and art-direction notes.

Validated all twenty-seven project sprites as exact 16x16 palette grids, completed a warning-free .NET 10 Release build, passed all eight behavioral tests, and successfully imported the expanded asset set with Godot 4.7.2.

## User

> the 16x16 sprites are still quite bad.
>
> however, this is excellent: C:\Users\k\Repository\CSharp\Automapolis\docs\art\sci-fi-unit-roster-reference.png
>
> is there correctly pixel perfect version of this?
> C:\Users\k\Repository\CSharp\Automapolis\docs\art\sci-fi-unit-roster-reference.png
>
> because `C:\Users\k\Repository\CSharp\Automapolis\docs\art\sci-fi-unit-roster-reference.png` is definitely not pixel perfect. it's upscaled in some weird way.

## Assistant

Inspected the reference at native resolution and searched the repository for related outputs. Confirmed that no faithful pixel-perfect version exists. The reference is a 1254x1254 RGB image with a baked checkerboard and image-model-simulated pixel texture, not artwork constructed on a consistent logical pixel grid. The 64x64 `units.png` sheet is technically pixel-perfect and transparent, but contains the unsuccessful 16x16 reinterpretations rather than faithful versions of the reference designs.

Recommended reconstructing the successful reference designs as new native-grid sprites at a resolution large enough to retain their silhouettes and signature details, likely 48x48 or 64x64, with true alpha and nearest-neighbor previews.
