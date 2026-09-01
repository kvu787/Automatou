# Art direction

Automapolis targets original 64x64 tactical pixel art with the compact readability
of classic console and handheld turn-based strategy games. The current visual
golden standard for unit sprites is the
[`64x64 sci-fi unit set`](art/sci-fi-units-64/README.md). The Godot shell still
uses an earlier 16x16 palette-indexed atlas compiled from
[`sprites.json`](../Shells/Godot/assets/sprites/sprites.json); treat that atlas as
a transitional runtime implementation, not as the authority for future unit art.

## Visual conflict

The two sides must read before the individual unit does:

- humanity uses squared armor, deliberate symmetry, gunmetal, ward-cyan, and command gold;
- the aliens use low predatory silhouettes, branching limbs, wet reds, bruise-purple, and toxic green;
- human terrain is reinforced and geometric, while alien terrain looks grown and interconnected;
- the Bastion is a bulky, intimidating, roughly human-shaped wall of armor with oversized shoulders, a recessed helmet, a cyan core, and gold command plating;
- the soldier is recognizably human and armored, but slimmer, lighter, and more conventionally equipped than the Bastion.

## Unit sprite visual grammar

- Square 64x64 canvas and nearest-neighbor display.
- Top-down/three-quarter tactical viewpoint with a fixed upper-left light.
- A controlled shared palette with deep navy outlines, cool-gray armor, cyan
  energy, and sharply separated human and alien accents.
- Terrain fills its square. Forces, buildings, and organisms use transparency and layer over terrain.
- Strong silhouettes take priority over internal detail.
- Pixels use hard edges, no dithering, and binary alpha; generated checkerboards,
  soft transparency, and resampling blur are not production-ready.
- Selection, damage, ownership, and targeting remain separate Shell overlays.

## Unit roster expansion

![Sci-fi tactical unit roster design reference](art/sci-fi-unit-roster-reference.png)

This generated design sheet established a broader silhouette vocabulary for compact tactical units: tracked armor, walkers, missile racks, lift fans, rotorcraft, aircraft, drones, arthropods, armored beasts, burrowers, rooted artillery, flyers, and biomass carriers. It is a design reference rather than production art. The exact 16x16 palette grids in `sprites.json` remain the source for the transitional runtime atlas but no longer define the target unit style.

The production roster deliberately reduces each design to one dominant outline and one signature feature. Human machines share squared gunmetal construction, cyan energy, and restrained gold command marks. Alien organisms share red carapace, purple flesh, toxic green organs, asymmetry, and branching limbs.

### 64x64 unit golden standard

![64x64 sci-fi unit sprite roster](art/sci-fi-units-64/preview.png)

The [`64x64 sci-fi unit catalog`](art/sci-fi-units-64/catalog.html) is the current
visual golden standard for all new and redesigned unit sprites. Its eight combat
and support silhouettes establish the required canvas size, three-quarter
tactical viewpoint, upper-left lighting, silhouette density, outline weight,
material treatment, accent discipline, and level of internal detail. Each PNG
has true hard alpha, a controlled palette, and exact 64x64 dimensions.

When this set conflicts with an older 16x16 grid, sprite study, full-color
render, or generated reference sheet, this set takes precedence for unit sprite
art. New units should look as though they belong beside these eight at native
size. The earlier assets remain useful as subject and faction references, but
they must be redrawn to this standard rather than enlarged or reused unchanged.

## Bastion and soldier concept

![Bastion and soldier concept art](art/bastion-soldier-concept.png)

This detailed comparison sheet established the shape language for human forces. The Bastion is a walking fortress whose recessed operator, immense shoulder mass, powered joints, and heavy weapon make it immediately distinct from the conventionally proportioned, mobile Soldier.

### Simplified sprite-translation concept

![Simplified Bastion and Soldier concept art](art/bastion-soldier-concept-simplified.png)

This deliberately low-detail sheet is the authoritative concept reference for future low-resolution sprite work. It preserves the approved proportions and identity while collapsing armor, weapons, and color accents into large continuous masses. Low-resolution translations should begin with these silhouettes and value groups instead of trying to interpret the detailed surface construction of the original concept.

The essential landmarks are:

- Bastion: enormous shoulder domes, recessed helmet and cyan visor, broad chest block with one cyan core, huge gauntlets, a rectangular heavy weapon, and block-like legs and boots;
- Soldier: conventional human proportions, one cyan visor, compact shoulder pads, a simple chest plate, restrained limb armor, and a recognizable service rifle;
- both: dark gunmetal bodies, a few large gold accent groups, and no dependency on tiny panels, cables, lights, pouches, scratches, or other surface texture.

### Simplified-concept pixel sprites

![Bastion and Soldier simplified-concept pixel sprites](art/pixel-sprites/preview.png)

The [`8x8` through `1024x1024` pixel-sprite catalog](art/pixel-sprites/catalog.html) translates the simplified concept into a compact twelve-color palette with true transparency and hard pixel edges. The 8x8 versions are deliberate icon-scale abstractions that preserve only silhouette, visor, core, gold armor grouping, and weapon direction. From 16x16 upward, the designs progressively recover the large forms of the generated pixel-art masters without reintroducing the original concept's surface noise.

### Resolution studies

![Bastion and soldier sprite resolution studies](art/sprite-studies/preview.png)

The [`8x8` through `128x128` pixel-art catalog](art/sprite-studies/catalog.html) records an unsuccessful attempt to translate the concept into a hard-pixel aesthetic. It remains process history, but it is superseded by the full-color render approach below.

### Full-color sprite renders

![Bastion and soldier full-color sprite renders](art/sprite-renders/preview.png)

The [`128x128` through `1024x1024` render catalog](art/sprite-renders/catalog.html) isolates each character from the approved concept and preserves the original painted design. The 1024x1024 cutouts are the masters; 512x512, 256x256, and 128x128 are direct high-quality downscales rather than separate reinterpretations.

## Historical direction reference

![Early sprite direction reference](art/sprite-direction-reference.png)

This image was generated for the earlier general science-fantasy direction. It is retained as process history, but the current Bastion-front sprites and this document supersede its subject matter.
