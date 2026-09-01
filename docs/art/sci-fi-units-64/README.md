# 64x64 sci-fi unit sprites

This roster is the current visual golden standard for Automapolis unit sprites.
It contains eight exact 64x64 RGBA sprites derived from generated concept
renders:

- scout drone;
- infantry exosuit;
- assault mech;
- hover tank;
- artillery walker;
- shield projector;
- repair drone;
- alien bio-mech.

![Enlarged roster preview](preview.png)

Every sprite uses only fully transparent or fully opaque pixels, a controlled
40-color cross-faction palette, no dithering, and at least three transparent
pixels of canvas padding. The enlarged preview uses nearest-neighbor scaling.
Open [`catalog.html`](catalog.html) to compare native-size and 4x views.

The built-in image generator produced 1254x1254 RGB concept renders with baked
checkerboards. [`build_generated_unit_sprites.py`](../../../tools/build_generated_unit_sprites.py)
turns those renders into game-ready assets by removing only edge-connected
checker pixels, fitting the surviving silhouette to the canvas, quantizing it,
and creating hard alpha. See [`prompts.md`](prompts.md) for the generation spec.

They define the target unit style but do not yet replace the deterministic 16x16
runtime atlas in the Godot shell.

## Current review feedback

The set is strong overall, but it still needs refinement:

- Reduce detail throughout. Favor a clearly legible silhouette, a few large
  value masses, and one or two unmistakable identifying features per unit.
- Do not add rust, wear, damage, scratches, scarring, grime, tiny panel lines,
  or other surface noise.
- Judge every decision at native pixel-perfect display size. A sprite occupies
  only a small portion of the player's field of view, so extra detail muddies
  the image and lowers its apparent quality rather than improving it.
- Preserve the established viewpoint, faction language, palette discipline,
  hard pixel edges, and true transparency while simplifying forms.

The sibling [`sci-fi-units-simplified-v2`](../sci-fi-units-simplified-v2/README.md)
iteration applies this feedback at both 64x64 and 32x32. The current set remains
the golden standard until a later iteration is explicitly approved to replace
it.
