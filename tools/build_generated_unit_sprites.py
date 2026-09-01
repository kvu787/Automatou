"""Convert generated pixel-art renders into exact, transparent game sprites."""

from __future__ import annotations

import argparse
from collections import deque
import json
from pathlib import Path

from PIL import Image, ImageDraw


DEFAULT_SIZE = 64
STANDARD_PALETTE = (
    (8, 12, 18),
    (17, 24, 35),
    (27, 37, 52),
    (40, 53, 73),
    (62, 76, 96),
    (91, 105, 126),
    (133, 147, 166),
    (185, 195, 207),
    (232, 236, 240),
    (0, 91, 114),
    (0, 156, 188),
    (27, 219, 233),
    (146, 248, 250),
    (110, 74, 10),
    (200, 139, 15),
    (246, 193, 38),
    (139, 48, 0),
    (237, 88, 0),
    (255, 146, 44),
    (104, 17, 24),
    (186, 35, 39),
    (240, 73, 72),
    (13, 72, 77),
    (28, 135, 139),
    (77, 198, 188),
    (52, 34, 74),
    (102, 67, 142),
    (162, 119, 205),
    (21, 54, 109),
    (36, 100, 186),
    (76, 159, 231),
    (53, 85, 9),
    (119, 165, 17),
    (182, 230, 48),
    (85, 24, 73),
    (166, 43, 137),
    (230, 100, 190),
    (104, 94, 78),
    (180, 165, 136),
    (237, 224, 195),
)

# Fewer shade steps keep small sprites organized into large readable masses.
# Each unit uses the shared neutrals and cyan plus only its own accent pair.
SIMPLIFIED_PALETTE = (
    (8, 12, 18),
    (17, 24, 35),
    (32, 43, 59),
    (55, 69, 89),
    (91, 105, 126),
    (151, 163, 180),
    (232, 236, 240),
    (0, 91, 114),
    (0, 174, 204),
    (86, 235, 241),
    (173, 111, 9),
    (246, 193, 38),
    (174, 50, 0),
    (255, 125, 31),
    (119, 19, 26),
    (226, 55, 58),
    (16, 88, 91),
    (60, 177, 169),
    (63, 39, 88),
    (145, 93, 188),
    (20, 64, 132),
    (52, 133, 221),
    (66, 106, 9),
    (153, 211, 27),
    (104, 26, 88),
    (213, 69, 174),
    (119, 107, 88),
    (226, 211, 179),
)

PALETTES = {
    "standard": STANDARD_PALETTE,
    "simplified": SIMPLIFIED_PALETTE,
}


def parse_source(value: str) -> tuple[str, Path]:
    try:
        sprite_id, filename = value.split("=", 1)
    except ValueError as error:
        raise argparse.ArgumentTypeError("sources must use id=path") from error
    if not sprite_id or not filename:
        raise argparse.ArgumentTypeError("sources must use non-empty id=path")
    return sprite_id, Path(filename)


def is_generated_background(pixel: tuple[int, int, int]) -> bool:
    """Recognize the bright, almost-neutral checker used by generated renders."""
    red, green, blue = pixel
    return min(pixel) >= 212 and max(pixel) - min(pixel) <= 34


def remove_edge_connected_checkerboard(source: Path) -> Image.Image:
    """Remove only bright neutral pixels reachable from the image boundary.

    Constraining removal to the edge-connected region preserves enclosed white or
    silver armor panels while clearing the generated checkerboard around and
    between silhouette parts.
    """
    rgb = Image.open(source).convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()
    background = bytearray(width * height)
    pending: deque[tuple[int, int]] = deque()

    def enqueue(x: int, y: int) -> None:
        index = y * width + x
        if background[index] or not is_generated_background(pixels[x, y]):
            return
        background[index] = 1
        pending.append((x, y))

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(height):
        enqueue(0, y)
        enqueue(width - 1, y)

    while pending:
        x, y = pending.popleft()
        if x:
            enqueue(x - 1, y)
        if x + 1 < width:
            enqueue(x + 1, y)
        if y:
            enqueue(x, y - 1)
        if y + 1 < height:
            enqueue(x, y + 1)

    alpha = Image.new("L", rgb.size, 255)
    alpha.putdata([0 if value else 255 for value in background])
    rgba = rgb.convert("RGBA")
    rgba.putalpha(alpha)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"No foreground found in {source}")
    return rgba.crop(bounds)


def fixed_palette_image(palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    palette_image = Image.new("P", (1, 1))
    values = [channel for color in palette for channel in color]
    values.extend([0] * (768 - len(values)))
    palette_image.putpalette(values)
    return palette_image


def quantize_with_hard_alpha(
    image: Image.Image,
    palette: tuple[tuple[int, int, int], ...],
) -> Image.Image:
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 112 else 0)
    rgb = Image.new("RGB", image.size, palette[0])
    rgb.paste(image.convert("RGB"), mask=alpha)
    indexed = rgb.quantize(
        palette=fixed_palette_image(palette),
        dither=Image.Dither.NONE,
    )
    result = indexed.convert("RGBA")
    result.putalpha(alpha)
    return result


def make_sprite(
    source: Path,
    size: int,
    palette: tuple[tuple[int, int, int], ...],
) -> Image.Image:
    cleaned = remove_edge_connected_checkerboard(source)
    margin = max(2, round(size * 0.047))
    available = size - 2 * margin
    scale = min(available / cleaned.width, available / cleaned.height)
    target = (
        max(1, round(cleaned.width * scale)),
        max(1, round(cleaned.height * scale)),
    )
    resized = cleaned.resize(target, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    position = ((size - target[0]) // 2, (size - target[1]) // 2)
    canvas.alpha_composite(resized, position)
    return quantize_with_hard_alpha(canvas, palette)


def checkerboard(size: tuple[int, int], tile: int = 16) -> Image.Image:
    image = Image.new("RGB", size, (238, 241, 245))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            if (x // tile + y // tile) % 2:
                draw.rectangle(
                    (x, y, x + tile - 1, y + tile - 1),
                    fill=(220, 225, 232),
                )
    return image


def build_preview(output: Path, sprite_ids: list[str], size: int) -> None:
    columns = 4
    rows = (len(sprite_ids) + columns - 1) // columns
    cell_width = 272
    cell_height = 292
    preview = checkerboard((columns * cell_width, rows * cell_height))
    draw = ImageDraw.Draw(preview)
    for index, sprite_id in enumerate(sprite_ids):
        sprite = Image.open(output / f"{sprite_id}.png").convert("RGBA")
        enlarged = sprite.resize((256, 256), Image.Resampling.NEAREST)
        column = index % columns
        row = index // columns
        left = column * cell_width + 8
        top = row * cell_height + 8
        preview.paste(enlarged, (left, top), enlarged)
        label = f"{sprite_id.replace('-', ' ')} {size}x{size}"
        draw.text((left, top + 262), label, fill=(17, 24, 35))
    preview.save(output / "preview.png", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", action="append", type=parse_source, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--size", type=int, choices=(32, 64), default=DEFAULT_SIZE)
    parser.add_argument("--palette", choices=tuple(PALETTES), default="standard")
    args = parser.parse_args()

    args.output.mkdir(parents=True, exist_ok=True)
    palette = PALETTES[args.palette]
    sprite_ids = []
    for sprite_id, source in args.source:
        if sprite_id in sprite_ids:
            raise ValueError(f"Duplicate sprite id: {sprite_id}")
        sprite_ids.append(sprite_id)
        sprite = make_sprite(source, args.size, palette)
        sprite.save(args.output / f"{sprite_id}.png", optimize=True)

    build_preview(args.output, sprite_ids, args.size)
    manifest = {
        "size": args.size,
        "format": "RGBA PNG",
        "paletteMode": args.palette,
        "paletteColors": len(palette),
        "sprites": [f"{sprite_id}.png" for sprite_id in sprite_ids],
    }
    (args.output / "manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
