# Explore art production options

Date: 2026-09-03

## User

> currently, i am blocked on how to produce art for this game. i've been experimenting with ai generation of pixel art the past few days, and i have gotten decent results, but the consistency is just not quite there yet. help me explore options to get me unblocked.

## User

> for reference, also take a look at "C:\Users\k\Repository\SpriteGeneration"

## Assistant

I think you have exhausted the useful part of the prompt-search phase. The blocker is not that you have not found the right wording; it is that a general image model is being asked to be the art director, model sheet, camera rig, lighting rig, and pixel artist independently for every asset.

`SpriteGeneration` contains 122 dated attempts across September 2 and 3. The evidence is unusually clean:

- The source atlas in attempt 52 and the large sheet in `assets` are coherent because each roster was composed in one generation. The model could see and harmonize the units together.
- Attempt 53's standalone recon drone is technically valid after processing, but it does not belong to the same visual world: its scale, silhouette density, perspective, and simplification are different.
- The conversion scripts successfully guarantee dimensions, hard alpha, palette limits, and padding. Those are file-format properties. They cannot repair a camera angle, proportion system, or shape language that changed upstream.
- The current Godot shell gives a map cell 36x36 screen pixels with two-pixel insets, so a unit is effectively being judged at about 32x32. Producing a highly detailed 64x64 master and then displaying it there explains much of the muddiness.

My recommendation is to stop treating unconstrained AI output as final production art. Use AI for concepts and variations, then put a deterministic stage between it and the game.

## The viable paths

| Path | Speed now | Long-term consistency | Cost / learning | Best use |
| --- | --- | --- | --- | --- |
| Existing AI plus contact sheets | Fast | Low to medium | Low | Prototypes and roster exploration |
| Pixel-specific AI | Fast to test | Medium, if references work | Low recurring cost | A serious one-day experiment |
| Custom generative model | Slow to establish | Medium to high | Dataset and workflow work | Scaling after a style is already approved |
| Fixed 3D render pipeline | Slower start | Very high | Blender/modeling learning or purchased models | Units, vehicles, buildings, animation, rotations |
| Human pixel artist | Moderate | Very high | Cash and collaboration | Final art and a reusable style bible |
| Drop literal pixel art | Fastest | Medium to high | Art-direction change | Shipping with the concept-render approach that already worked |

### 1. Time-box a pixel-specific AI test

PixelLab is the most relevant current tool to benchmark. It specifically supports native small canvases, style references, forced palettes, transparent output, rotation, animation, and top-down tilesets. It currently offers a free trial, so this can be evaluated without committing the project to it. Its generated assets can be used commercially, though its terms prohibit using those outputs to train another model without permission. See the [PixelLab feature and trial page](https://www.pixellab.ai/?tier=0), [API capability list](https://www.pixellab.ai/pixellab-api), and [terms](https://www.pixellab.ai/termsofservice).

I would give this exactly one bounded test: Soldier, Bastion, recon drone, tank, Ravener, and one terrain tile at native 32x32, all using the same style reference and forced palette. Judge them together at 1x in the actual Godot grid. Stop after the free-trial batch if the camera or visual grammar still drifts. Do not enter another hundred-prompt loop.

### 2. Use a deterministic 3D-to-2D pipeline for production

This is my strongest long-term recommendation for Automapolis. Build or acquire low-detail models, then lock one orthographic camera, one down-right facing, one light rig, one set of flat materials, and one scale convention. Render transparent PNGs and reduce them through the converter already in this repository. Every unit will then share actual geometry, projection, lighting, and proportions rather than textual approximations of them. The same source can later produce idle movement, recoil, damage variants, shadows, and alternate facings. Blender supports transparent renders and separate render passes, making the output easy to composite and post-process; see the [Blender film documentation](https://docs.blender.org/manual/en/4.2/render/cycles/render_settings/film.html) and [render-pass documentation](https://docs.blender.org/manual/en/latest/render/layers/passes.html).

The catch is upfront modeling work. A practical compromise is kitbashing simple human machines and sculpting only the large alien masses. At 32x32, the models can be much cruder than they would need to be for a 3D game. The final sprite pass hides a lot.

### 3. Commission the style foundation, not necessarily the whole game

If budget permits, commission a pixel artist for a vertical slice: the same six benchmark assets, a global palette, an Aseprite source template, and a one-page style bible. Ask for construction rules, not only PNGs. You could then use those masters as references for AI, edit derivatives yourself, or commission the remaining roster in batches. This buys the part AI is currently failing at: coherent judgment across assets.

### 4. Consider custom-model AI only after a foundation exists

Scenario offers custom style and character model training, and a local diffusion workflow can combine a style reference through IP-Adapter with structural control such as depth or edges through ControlNet. Those mechanisms are materially stronger than prompt-only consistency. See [Scenario custom-model training](https://www.scenario.com/features/train) and the [Diffusers IP-Adapter documentation](https://huggingface.co/docs/diffusers/using-diffusers/ip_adapter).

I would not start here yet. You do not currently have a sufficiently large, consistently approved dataset; training on the mixed attempts would teach the inconsistency. This becomes attractive after a human artist or 3D pipeline has produced a small gold set.

### 5. Keep the non-pixel pivot available

The high-resolution Bastion and Soldier cutouts already proved that painterly concept-derived tokens can work. A coherent tactical-miniature look at 96-128px, displayed with normal filtering rather than nearest-neighbor, may serve the game better than compromised pixel art. It would require revisiting the grid presentation, terrain, and UI, but it is a valid art direction rather than a failure state.

## What I would do next

1. Declare 32x32 the current gameplay evaluation size because that is approximately what the shell displays today. Do not finalize 64x64 until the map is redesigned to show it natively.
2. Freeze the existing simplified 32x32 set as prototype art. No more general-model prompt tuning for individual production sprites.
3. Run the six-asset PixelLab benchmark as the cheap, fast branch.
4. In parallel conceptually, scope a Blender vertical slice for the same six assets. It needs only crude models, flat materials, the locked camera, and the existing quantization pipeline.
5. Compare both branches inside the game, not in enlarged catalog previews. Score viewpoint, native-size recognition, palette, faction read, editability, and ability to create the next animation or facing.
6. If neither clears the bar, commission the six-asset foundation and use that to unlock the rest of the pipeline.

The important separation is by asset class: use the chosen deterministic pipeline for units and buildings; use a real tileset workflow for terrain; draw UI and overlays in Godot; reserve general image generation for portraits, splash art, marketing, and concept sheets where small inconsistencies are much less damaging. Aseprite supports reusable indexed palettes and tilemaps, which fits the terrain side of this separation: [palette documentation](https://www.aseprite.org/docs/color-bar/) and [tilemap documentation](https://www.aseprite.org/docs/tilemap/).

My vote is **PixelLab benchmark now, 3D-to-sprite as the likely production pipeline, and the existing 32x32 candidates as placeholders so art no longer blocks mechanics**.
