# 64x64 sci-fi unit sprites

This experimental roster contains eight exact 64x64 RGBA sprites derived from
generated concept renders:

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

These sprites are a separate 64x64 art-direction candidate. They do not replace
the deterministic 16x16 production atlas in the Godot shell.
