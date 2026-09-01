"""Build exact-size, palette-constrained sprite ladders from generated masters."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw


SIZES = (8, 16, 32, 64, 128, 256, 512, 1024)
PALETTE = (
    (11, 15, 21),
    (19, 25, 33),
    (31, 39, 49),
    (47, 57, 69),
    (70, 81, 94),
    (100, 111, 123),
    (91, 68, 30),
    (145, 105, 43),
    (193, 148, 62),
    (6, 92, 105),
    (0, 182, 203),
    (35, 224, 234),
)


def remove_generated_checkerboard(source: Path) -> Image.Image:
    """Turn the bright neutral generated backdrop into real transparency."""
    rgb = Image.open(source).convert("RGB")
    rgba = Image.new("RGBA", rgb.size)
    converted = []
    for red, green, blue in rgb.get_flattened_data():
        is_neutral = max(red, green, blue) - min(red, green, blue) <= 14
        alpha = 0 if is_neutral and min(red, green, blue) >= 205 else 255
        converted.append((red, green, blue, alpha))
    rgba.putdata(converted)

    bounds = rgba.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"No foreground found in {source}")
    return rgba.crop(bounds)


def fixed_palette_image() -> Image.Image:
    palette_image = Image.new("P", (1, 1))
    flat = [channel for color in PALETTE for channel in color]
    flat.extend([0] * (768 - len(flat)))
    palette_image.putpalette(flat)
    return palette_image


def quantize_opaque(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 112 else 0)
    rgb = Image.new("RGB", image.size, PALETTE[0])
    rgb.paste(image.convert("RGB"), mask=alpha)
    indexed = rgb.quantize(
        palette=fixed_palette_image(),
        dither=Image.Dither.NONE,
    )
    result = indexed.convert("RGBA")
    result.putalpha(alpha)
    return result


def fit_to_square(sprite: Image.Image, size: int) -> Image.Image:
    margin = max(1, round(size * 0.055))
    available = size - margin * 2
    scale = min(available / sprite.width, available / sprite.height)
    width = max(1, round(sprite.width * scale))
    height = max(1, round(sprite.height * scale))
    resized = sprite.resize((width, height), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    left = (size - width) // 2
    top = (size - height) // 2
    canvas.alpha_composite(resized, (left, top))
    return quantize_opaque(canvas)


def manual_eight_pixel_sprite(unit: str) -> Image.Image:
    # . transparent, o outline, d dark, m mid, g gold, c cyan, h cyan highlight
    rows = {
        "bastion": (
            "..occho.",
            "ogddddgo",
            "oddcdddo",
            "ooommooo",
            ".oddddo.",
            ".od..do.",
            ".om..mo.",
            ".oo..oo.",
        ),
        "soldier": (
            "...oo...",
            "..och...",
            "..gddg..",
            "..ooooo.",
            "...dd.o.",
            "...d.d..",
            "..om.mo.",
            "..oo.oo.",
        ),
    }[unit]
    colors = {
        ".": (0, 0, 0, 0),
        "o": (*PALETTE[0], 255),
        "d": (*PALETTE[2], 255),
        "m": (*PALETTE[4], 255),
        "g": (*PALETTE[8], 255),
        "c": (*PALETTE[10], 255),
        "h": (*PALETTE[11], 255),
    }
    result = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    result.putdata([colors[pixel] for row in rows for pixel in row])
    return result


def checkerboard(size: tuple[int, int], tile: int = 16) -> Image.Image:
    image = Image.new("RGB", size, (238, 241, 245))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            if (x // tile + y // tile) % 2:
                draw.rectangle((x, y, x + tile - 1, y + tile - 1), fill=(220, 225, 232))
    return image


def build_preview(output: Path) -> None:
    cell_width = 190
    cell_height = 230
    preview = checkerboard((cell_width * len(SIZES), cell_height * 2), tile=16)
    draw = ImageDraw.Draw(preview)
    for row, unit in enumerate(("bastion", "soldier")):
        for column, size in enumerate(SIZES):
            sprite = Image.open(output / f"{unit}-{size}.png").convert("RGBA")
            display_size = min(170, size * max(1, 160 // size))
            display = sprite.resize((display_size, display_size), Image.Resampling.NEAREST)
            x = column * cell_width + (cell_width - display_size) // 2
            y = row * cell_height + 8
            preview.paste(display, (x, y), display)
            draw.text((column * cell_width + 7, row * cell_height + 205), f"{unit} {size}x{size}", fill=(20, 24, 31))
    preview.save(output / "preview.png", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--bastion", type=Path, required=True)
    parser.add_argument("--soldier", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    args.output.mkdir(parents=True, exist_ok=True)
    for unit, source in (("bastion", args.bastion), ("soldier", args.soldier)):
        cleaned = remove_generated_checkerboard(source)
        for size in SIZES:
            sprite = manual_eight_pixel_sprite(unit) if size == 8 else fit_to_square(cleaned, size)
            sprite.save(args.output / f"{unit}-{size}.png", optimize=True)
    build_preview(args.output)


if __name__ == "__main__":
    main()
