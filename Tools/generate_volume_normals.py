from __future__ import annotations

import math
import re
from pathlib import Path
from uuid import uuid4

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
NORMAL_NAME = "_NormalMap"
NEUTRAL = (128, 128, 255, 0)

CHARACTER_SPRITES = [
    ROOT / "Assets/Sprites/Characters/MC(AimDown).png",
    ROOT / "Assets/Sprites/Characters/MC(AimUp).png",
    ROOT / "Assets/Sprites/Characters/MC(Dead).png",
    ROOT / "Assets/Sprites/Characters/MC(Idle2)).png",
    ROOT / "Assets/Sprites/Characters/MC(Jump).png",
    ROOT / "Assets/Sprites/Characters/MC(Push).png",
    ROOT / "Assets/Sprites/Characters/MC(Ready).png",
]

BASE_DETAIL_NORMALS = [
    (
        ROOT / "Assets/Sprites/Stage/Materials/NewBox.png",
        ROOT / "Assets/Sprites/Stage/Materials/NewBox_n.png",
        ROOT / "Assets/Sprites/Stage/Materials/NewBox_Normal.png",
    ),
]


def read_guid(meta_path: Path) -> str | None:
    if not meta_path.exists():
        return None

    match = re.search(
        r"^guid: ([0-9a-f]{32})$",
        meta_path.read_text(encoding="utf-8", errors="ignore"),
        re.MULTILINE,
    )
    return match.group(1) if match else None


def ensure_normal_meta(meta_path: Path) -> str:
    guid = read_guid(meta_path) or uuid4().hex
    if not meta_path.exists():
        text = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 0
    linearTexture: 0
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureType: 0
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    secondaryTextures: []
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    else:
        text = meta_path.read_text(encoding="utf-8", errors="ignore")

    text = re.sub(r"^guid: [0-9a-f]{32}$", f"guid: {guid}", text, count=1, flags=re.MULTILINE)
    text = re.sub(r"sRGBTexture: \d+", "sRGBTexture: 0", text)
    text = re.sub(r"textureType: \d+", "textureType: 0", text)
    text = re.sub(r"textureCompression: \d+", "textureCompression: 0", text)
    meta_path.write_text(text, encoding="utf-8", newline="\n")
    return guid


def ensure_secondary_texture(source_meta: Path, normal_guid: str) -> None:
    text = source_meta.read_text(encoding="utf-8", errors="ignore")
    top_block = (
        "  secondarySpriteTextures:\n"
        f"  - texture: {{fileID: 2800000, guid: {normal_guid}, type: 3}}\n"
        f"    name: {NORMAL_NAME}\n"
    )

    if re.search(r"\n  secondarySpriteTextures:\n", text):
        text = re.sub(
            r"\n  secondarySpriteTextures:\n(?:  - .*\n(?:    .*\n)*)?",
            "\n" + top_block,
            text,
            count=1,
        )
    else:
        text = text.replace("\n  spriteSheet:\n", "\n" + top_block + "  spriteSheet:\n", 1)

    sheet_block = (
        "    secondaryTextures:\n"
        f"    - texture: {{fileID: 2800000, guid: {normal_guid}, type: 3}}\n"
        f"      name: {NORMAL_NAME}"
    )

    if re.search(r"    secondaryTextures:\s*\[\]", text):
        text = re.sub(r"    secondaryTextures:\s*\[\]", sheet_block, text, count=1)
    elif re.search(r"    secondaryTextures:\n", text):
        text = re.sub(
            r"    secondaryTextures:\n(?:    - .*\n(?:      .*\n)*)?",
            sheet_block + "\n",
            text,
            count=1,
        )
    else:
        text = text.replace("    spriteCustomMetadata:\n", sheet_block + "\n    spriteCustomMetadata:\n", 1)

    source_meta.write_text(text, encoding="utf-8", newline="\n")


def parse_sprite_rects(source_path: Path, image_size: tuple[int, int]) -> list[tuple[int, int, int, int]]:
    meta_path = Path(str(source_path) + ".meta")
    width, height = image_size
    if not meta_path.exists():
        return [(0, 0, width, height)]

    text = meta_path.read_text(encoding="utf-8", errors="ignore")
    rects: list[tuple[int, int, int, int]] = []
    for match in re.finditer(
        r"rect:\s*\n\s*serializedVersion: 2\s*\n\s*x: ([\d.]+)\s*\n\s*y: ([\d.]+)\s*\n\s*width: ([\d.]+)\s*\n\s*height: ([\d.]+)",
        text,
    ):
        x, y, w, h = (int(round(float(value))) for value in match.groups())
        pil_y = height - y - h
        rects.append((x, pil_y, w, h))

    return rects or [(0, 0, width, height)]


def luminance(pixel: tuple[int, int, int, int]) -> float:
    r, g, b, a = pixel
    if a == 0:
        return 0.0
    return ((0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0) * (a / 255.0)


def normalize_to_rgb(nx: float, ny: float, nz: float, alpha: int) -> tuple[int, int, int, int]:
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    nx /= length
    ny /= length
    nz /= length
    return (
        max(0, min(255, round((nx * 0.5 + 0.5) * 255))),
        max(0, min(255, round((ny * 0.5 + 0.5) * 255))),
        max(0, min(255, round((nz * 0.5 + 0.5) * 255))),
        alpha,
    )


def rgb_to_normal(pixel: tuple[int, int, int, int]) -> tuple[float, float, float]:
    r, g, b, _ = pixel
    nx = r / 255.0 * 2.0 - 1.0
    ny = g / 255.0 * 2.0 - 1.0
    nz = b / 255.0 * 2.0 - 1.0
    return nx, ny, nz


def generate_character_volume_normal(source_path: Path) -> Path:
    image = Image.open(source_path).convert("RGBA")
    width, height = image.size
    src = image.load()
    out = Image.new("RGBA", image.size, NEUTRAL)
    dst = out.load()

    rects = parse_sprite_rects(source_path, image.size)
    for rx, ry, rw, rh in rects:
        opaque: list[tuple[int, int]] = []
        for y in range(ry, ry + rh):
            for x in range(rx, rx + rw):
                if 0 <= x < width and 0 <= y < height and src[x, y][3] > 0:
                    opaque.append((x, y))

        if not opaque:
            continue

        min_x = min(x for x, _ in opaque)
        max_x = max(x for x, _ in opaque)
        min_y = min(y for _, y in opaque)
        max_y = max(y for _, y in opaque)
        center_x = (min_x + max_x) * 0.5
        center_y = (min_y + max_y) * 0.5
        radius_x = max((max_x - min_x) * 0.5, 1.0)
        radius_y = max((max_y - min_y) * 0.5, 1.0)

        def h(px: int, py: int) -> float:
            px = min(width - 1, max(0, px))
            py = min(height - 1, max(0, py))
            return luminance(src[px, py])

        for x, y in opaque:
            _, _, _, alpha = src[x, y]
            sx = max(-1.0, min(1.0, (x - center_x) / radius_x))
            sy = max(-1.0, min(1.0, (center_y - y) / radius_y))

            gx = (
                -h(x - 1, y - 1)
                + h(x + 1, y - 1)
                - 2.0 * h(x - 1, y)
                + 2.0 * h(x + 1, y)
                - h(x - 1, y + 1)
                + h(x + 1, y + 1)
            )
            gy = (
                -h(x - 1, y - 1)
                - 2.0 * h(x, y - 1)
                - h(x + 1, y - 1)
                + h(x - 1, y + 1)
                + 2.0 * h(x, y + 1)
                + h(x + 1, y + 1)
            )

            nx = sx * 0.72 - gx * 0.34
            ny = sy * 0.88 - gy * 0.34
            nz = 1.0
            dst[x, y] = normalize_to_rgb(nx, ny, nz, alpha)

    normal_path = source_path.with_name(source_path.stem + "_Normal.png")
    out.save(normal_path)
    return normal_path


def generate_base_detail_normal(source_path: Path, base_path: Path, output_path: Path) -> Path:
    source = Image.open(source_path).convert("RGBA")
    base = Image.open(base_path).convert("RGBA").resize(source.size, Image.Resampling.NEAREST)
    old_detail = Image.open(output_path).convert("RGBA").resize(source.size, Image.Resampling.NEAREST) if output_path.exists() else None

    width, height = source.size
    src = source.load()
    base_px = base.load()
    detail_px = old_detail.load() if old_detail is not None else None
    out = Image.new("RGBA", source.size, (128, 128, 255, 255))
    dst = out.load()

    def h(px: int, py: int) -> float:
        px = min(width - 1, max(0, px))
        py = min(height - 1, max(0, py))
        return luminance(src[px, py])

    for y in range(height):
        for x in range(width):
            alpha = src[x, y][3]
            bx, by, bz = rgb_to_normal(base_px[x, y])
            dx = dy = 0.0
            if detail_px is not None:
                odx, ody, _ = rgb_to_normal(detail_px[x, y])
                dx += odx * 0.28
                dy += ody * 0.28

            gx = (
                -h(x - 1, y - 1)
                + h(x + 1, y - 1)
                - 2.0 * h(x - 1, y)
                + 2.0 * h(x + 1, y)
                - h(x - 1, y + 1)
                + h(x + 1, y + 1)
            )
            gy = (
                -h(x - 1, y - 1)
                - 2.0 * h(x, y - 1)
                - h(x + 1, y - 1)
                + h(x - 1, y + 1)
                + 2.0 * h(x, y + 1)
                + h(x + 1, y + 1)
            )

            nx = bx + dx - gx * 0.18
            ny = by + dy - gy * 0.18
            nz = max(0.35, bz)
            dst[x, y] = normalize_to_rgb(nx, ny, nz, alpha if alpha > 0 else base_px[x, y][3])

    out.save(output_path)
    return output_path


def register_normal(source_path: Path, normal_path: Path) -> None:
    normal_guid = ensure_normal_meta(Path(str(normal_path) + ".meta"))
    source_meta = Path(str(source_path) + ".meta")
    if source_meta.exists():
        ensure_secondary_texture(source_meta, normal_guid)


def main() -> None:
    generated: list[str] = []

    for source_path in CHARACTER_SPRITES:
        if not source_path.exists():
            continue
        normal_path = generate_character_volume_normal(source_path)
        register_normal(source_path, normal_path)
        generated.append(str(normal_path.relative_to(ROOT)))

    for source_path, base_path, output_path in BASE_DETAIL_NORMALS:
        if not source_path.exists() or not base_path.exists():
            continue
        normal_path = generate_base_detail_normal(source_path, base_path, output_path)
        register_normal(source_path, normal_path)
        generated.append(str(normal_path.relative_to(ROOT)))

    print(f"generated={len(generated)}")
    for item in generated:
        print(item)


if __name__ == "__main__":
    main()
