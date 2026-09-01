# Pixel-perfect sprite converter

`Automapolis.PixelPerfect` turns AI-generated pixel-style renders into deterministic,
game-ready sprite assets. It accepts complete card sheets or individual images and
produces:

- one square RGBA PNG per detected card;
- a transparent, tightly packed sprite sheet;
- a nearest-neighbor checkerboard preview;
- a shared no-dither OKLab palette as PNG and GIMP/Aseprite-compatible GPL;
- a JSON manifest with source panels, extraction bounds, and atlas positions;
- optional panel, foreground-mask, and extraction diagnostics.

The converter detects regularly spaced sheet separators, robustly fits the smooth
background in each card, removes that background and optional cast shadows, normalizes
the silhouettes, performs premultiplied area resampling, thresholds alpha to binary,
and quantizes the whole set to one palette. It does not use generative AI and therefore
does not invent or move details between frames.

The tool targets Windows and .NET 10, matching the repository's supported development
environment.

## Run

From the repository root:

```powershell
dotnet run --project tools/Automapolis.PixelPerfect -- `
  --input Conversations/assets/2026-09-01-convert-faux-pixel-art-sprite-sheets `
  --output docs/art/pixel-perfect-conversion-example `
  --size 64 `
  --palette-colors 32 `
  --diagnostics
```

Input paths are repeatable. A directory input loads its supported images in filename
order. Use `--help` for every option.

## Important controls

- Automatic grid detection works best when card separators span most of the image.
  Supply `--grid 4x3` when an unusual composition defeats it.
- The default eight-percent card inset removes rounded frames and separator bands like
  those in the reference sheets. Reduce `--panel-inset` for tightly framed source art.
- `--scale-mode individual` makes every independently generated card fill its canvas.
  Use `shared` to preserve relative scale across related frames or a deliberately
  scale-consistent source sheet.
- The automatic background threshold is derived independently for every card. Supply
  `--background-threshold` only when a source's background is unusually textured.
- `--names names.txt` assigns stable filenames. Give one name per card in left-to-right,
  top-to-bottom order across each input image.
- Related animation frames should use shared scaling and the same alignment. Bottom
  alignment gives them a stable baseline.

Inspect `preview.png` at native display size. Automated conversion establishes exact
pixels, binary transparency, palette discipline, and consistent framing; a short
Aseprite cluster-cleanup pass can still improve silhouettes produced by an image model.
