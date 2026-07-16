from __future__ import annotations

import math
import re
from pathlib import Path
from uuid import uuid4

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
NORMAL_NAME = "_NormalMap"
STRENGTH = 3.0

TARGETS = [
    ROOT / "Assets/Sprites/Characters/MC(AimDown).png",
    ROOT / "Assets/Sprites/Characters/MC(AimUp).png",
    ROOT / "Assets/Sprites/Characters/MC(Dead).png",
    ROOT / "Assets/Sprites/Characters/MC(Idle2)).png",
    ROOT / "Assets/Sprites/Characters/MC(Jump).png",
    ROOT / "Assets/Sprites/Characters/MC(Push).png",
    ROOT / "Assets/Sprites/Characters/MC(Ready).png",
    ROOT / "Assets/Sprites/Characters/Enemy/Enemy.gun.ready.png",
    ROOT / "Assets/Sprites/Stage/Maps/Elevator.png",
    ROOT / "Assets/Sprites/Stage/Maps/Map.png",
    ROOT / "Assets/Sprites/Stage/Maps/단색타일.png",
    ROOT / "Assets/Sprites/Stage/Maps/배경제거.png",
    ROOT / "Assets/Sprites/Stage/Maps/배경채운벽.png",
    ROOT / "Assets/Sprites/Stage/Maps/배경채운벽2.png",
    ROOT / "Assets/Sprites/Stage/Maps/파이프 제거.png",
    ROOT / "Assets/Sprites/Stage/Maps/파이프 제거2.png",
]


def read_guid(meta_path: Path) -> str | None:
    if not meta_path.exists():
        return None
    match = re.search(r"^guid: ([0-9a-f]{32})$", meta_path.read_text(encoding="utf-8", errors="ignore"), re.MULTILINE)
    return match.group(1) if match else None


def normal_meta_template(guid: str) -> str:
    return f"""fileFormatVersion: 2
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
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 1
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
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
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def configure_normal_meta(meta_path: Path) -> str:
    guid = read_guid(meta_path) or uuid4().hex
    if meta_path.exists():
        text = meta_path.read_text(encoding="utf-8", errors="ignore")
        text = re.sub(r"^guid: [0-9a-f]{32}$", f"guid: {guid}", text, count=1, flags=re.MULTILINE)
        text = re.sub(r"sRGBTexture: \\d+", "sRGBTexture: 0", text)
        text = re.sub(r"textureType: \\d+", "textureType: 0", text)
        text = re.sub(r"textureCompression: \\d+", "textureCompression: 0", text)
    else:
        text = normal_meta_template(guid)
    meta_path.write_text(text, encoding="utf-8", newline="\n")
    return guid


def ensure_secondary_texture(source_meta: Path, normal_guid: str) -> None:
    text = source_meta.read_text(encoding="utf-8", errors="ignore")
    top_block = (
        "  secondarySpriteTextures:\n"
        f"  - texture: {{fileID: 2800000, guid: {normal_guid}, type: 3}}\n"
        f"    name: {NORMAL_NAME}\n"
    )

    if "  secondarySpriteTextures:" in text:
        text = re.sub(r"\n  secondarySpriteTextures:\n(?:  - .*\n(?:    .*\n)*)?", "\n" + top_block, text, count=1)
    else:
        text = text.replace("\n  spriteSheet:\n", "\n" + top_block + "  spriteSheet:\n", 1)

    sheet_block = (
        "    secondaryTextures:\n"
        f"    - texture: {{fileID: 2800000, guid: {normal_guid}, type: 3}}\n"
        f"      name: {NORMAL_NAME}"
    )
    if re.search(r"    secondaryTextures:\s*\[\]", text):
        text = re.sub(r"    secondaryTextures:\s*\[\]", sheet_block, text, count=1)
    elif "    secondaryTextures:" in text:
        text = re.sub(
            r"    secondaryTextures:\n    - texture: \{fileID: 2800000, guid: [0-9a-f]{32}, type: 3\}\n      name: _NormalMap",
            sheet_block,
            text,
            count=1,
        )
    else:
        text = text.replace("    spriteCustomMetadata:\n", sheet_block + "\n    spriteCustomMetadata:\n", 1)

    source_meta.write_text(text, encoding="utf-8", newline="\n")


def build_height(pixels: list[tuple[int, int, int, int]], width: int, height: int) -> list[list[float]]:
    alpha_values = [a for _, _, _, a in pixels]
    alpha_varies = min(alpha_values) != max(alpha_values)
    result = [[0.0] * width for _ in range(height)]

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[y * width + x]
            if a == 0:
                continue
            lum = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0
            alpha = a / 255.0
            result[y][x] = alpha if alpha_varies else lum
    return result


def sobel_normal(source_path: Path, output_path: Path) -> None:
    image = Image.open(source_path).convert("RGBA")
    width, height = image.size
    pixels = list(image.getdata())
    height_map = build_height(pixels, width, height)

    out = Image.new("RGBA", (width, height))
    out_pixels = out.load()

    for y in range(height):
        for x in range(width):
            _, _, _, alpha = pixels[y * width + x]
            if alpha == 0:
                out_pixels[x, y] = (128, 128, 255, 0)
                continue

            def h(xx: int, yy: int) -> float:
                return height_map[min(height - 1, max(0, yy))][min(width - 1, max(0, xx))]

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

            nx = -gx * STRENGTH
            ny = -gy * STRENGTH
            nz = 1.0
            length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
            nx /= length
            ny /= length
            nz /= length
            out_pixels[x, y] = (
                max(0, min(255, round((nx * 0.5 + 0.5) * 255))),
                max(0, min(255, round((ny * 0.5 + 0.5) * 255))),
                max(0, min(255, round((nz * 0.5 + 0.5) * 255))),
                alpha,
            )

    out.save(output_path)


def main() -> None:
    generated: list[str] = []
    for source_path in TARGETS:
        if not source_path.exists():
            continue
        normal_path = source_path.with_name(source_path.stem + "_Normal.png")
        sobel_normal(source_path, normal_path)
        normal_guid = configure_normal_meta(Path(str(normal_path) + ".meta"))
        source_meta = Path(str(source_path) + ".meta")
        if source_meta.exists():
            ensure_secondary_texture(source_meta, normal_guid)
        generated.append(str(normal_path.relative_to(ROOT)))

    print(f"strength={STRENGTH}")
    print(f"generated={len(generated)}")
    for item in generated:
        print(item)


if __name__ == "__main__":
    main()
