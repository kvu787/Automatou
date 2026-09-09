# Create 64px sci-fi unit sprites

Date: 2026-09-01

## User

> create a variety of pixel art sprites for sci-fi units, size 64x64

## Assistant

Created eight distinct sci-fi units: a scout drone, infantry exosuit, assault
mech, hover tank, artillery walker, shield projector, repair drone, and alien
bio-mech. Generated each unit independently with a shared three-quarter tactical
view, upper-left lighting, gunmetal-and-cyan visual grammar, and a unique accent.

Converted the generated 1254x1254 RGB renders into exact 64x64 RGBA production
candidates. The reproducible converter removes only edge-connected checkerboard
pixels, retains enclosed light armor, fits each silhouette with transparent
padding, quantizes to a controlled 40-color palette without dithering, and emits
binary alpha. Added individual PNGs, a nearest-neighbor preview, a browser
catalog, a manifest, full prompt documentation, and art-direction notes.

Validated every sprite as 64x64 RGBA with only alpha values 0 and 255, between
21 and 28 used RGBA colors, and at least three pixels of transparent padding.

## User

> these are excellent. record in the art direction that these sprites should be treated as the current golden standard

## Assistant

Recorded the 64x64 sci-fi unit set as the current visual golden standard for all
new and redesigned unit sprites. Updated the document's target resolution and
visual grammar, established that this set takes precedence over older 16x16
grids and prior studies when their guidance conflicts, and clarified that the
existing 16x16 atlas is a transitional runtime implementation rather than the
authority for future unit art.

## User

> record this feedback on C:\Users\k\Repository\CSharp\Automapolis\docs\art\sci-fi-units-64:
> - in general, they are quite good, but there are still improvement/adjustments to be made
> - the biggest thing is to reduce detail everywhere. focus on clearly legible and recognizable features, silhouette, etc
> - dont do details like rust, wear/tear, damage, scarring, etc
> - keep in mind that these sprites will be displayed at pixel perfect resolution, so detailing actually reduces the visual quality because it muddies the visuals at pixel perfect resolution since the overall sprite will be pretty small in the user's field of view.
>
> now, let's try to generate 32x32 sprites. incorporate the feedback and generate both 64x64 and 32x32 sprites.

## Assistant

Recorded the feedback in the golden-standard set's README and produced a
non-destructive simplified v2 candidate containing the same eight units at both
exact 64x64 and 32x32 resolutions. Designed the shared masters for 32x32
recognition first, using bold silhouettes, three to five large value masses,
one or two oversized signature features, clean factory-new surfaces, and no
wear, damage, scarring, grime, greebles, or other surface noise.

Extended the reproducible converter with selectable 32x32 or 64x64 output and a
reduced simplified palette. Added hard-alpha PNGs, per-size manifests and
nearest-neighbor previews, a paired native-size comparison catalog, full prompt
documentation, and an art-direction link while leaving the currently approved
golden standard unchanged pending review.
