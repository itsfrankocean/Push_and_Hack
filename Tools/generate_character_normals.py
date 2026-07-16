from __future__ import annotations

import math
import re
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
CHARACTER_ROOT = ROOT / "Assets" / "Sprites" / "Characters"

SOURCE_IMAGES = [
    CHARACTER_ROOT / "MC(AimDown).png",
    CHARACTER_ROOT / "MC(AimUp).png",
    CHARACTER_ROOT / "MC(Dead).png",
    CHARACTER_ROOT / "MC(Idle2)).png",
    CHARACTER_ROOT / "MC(Jump).png",
    CHARACTER_ROOT / "MC(Push).png",
    CHARACTER_ROOT / "MC(Ready).png",
    CHARACTER_ROOT / "Enemy" / "Enemy.gun.ready.png",
]


def parse_sprite_rects(meta_path: Path, image_height: int) -> list[tuple[int, int, int, int]]:
    text = meta_path.read_text(encoding="utf-8")
    matches = re.finditer(
        r"\n\s+rect:\s*\n"
        r"\s+serializedVersion:\s*2\s*\n"
        r"\s+x:\s*([0-9.]+)\s*\n"
        r"\s+y:\s*([0-9.]+)\s*\n"
        r"\s+width:\s*([0-9.]+)\s*\n"
        r"\s+height:\s*([0-9.]+)",
        text,
    )

    rects: list[tuple[int, int, int, int]] = []
    for match in matches:
        x, y, width, height = (int(round(float(value))) for value in match.groups())
        top = image_height - y - height
        rects.append((x, top, width, height))

    return rects


def chamfer_distance(mask: np.ndarray) -> np.ndarray:
    h, w = mask.shape
    inf = 1_000_000.0
    dist = np.where(mask, inf, 0.0).astype(np.float32)

    sqrt2 = math.sqrt(2.0)
    for y in range(h):
        for x in range(w):
            if not mask[y, x]:
                continue
            best = dist[y, x]
            if x > 0:
                best = min(best, dist[y, x - 1] + 1.0)
            if y > 0:
                best = min(best, dist[y - 1, x] + 1.0)
            if x > 0 and y > 0:
                best = min(best, dist[y - 1, x - 1] + sqrt2)
            if x + 1 < w and y > 0:
                best = min(best, dist[y - 1, x + 1] + sqrt2)
            dist[y, x] = best

    for y in range(h - 1, -1, -1):
        for x in range(w - 1, -1, -1):
            if not mask[y, x]:
                continue
            best = dist[y, x]
            if x + 1 < w:
                best = min(best, dist[y, x + 1] + 1.0)
            if y + 1 < h:
                best = min(best, dist[y + 1, x] + 1.0)
            if x + 1 < w and y + 1 < h:
                best = min(best, dist[y + 1, x + 1] + sqrt2)
            if x > 0 and y + 1 < h:
                best = min(best, dist[y + 1, x - 1] + sqrt2)
            dist[y, x] = best

    dist[~mask] = 0.0
    return dist


def box_blur(values: np.ndarray, passes: int) -> np.ndarray:
    result = values.astype(np.float32)
    for _ in range(passes):
        padded = np.pad(result, 1, mode="edge")
        result = (
            padded[:-2, :-2]
            + padded[:-2, 1:-1]
            + padded[:-2, 2:]
            + padded[1:-1, :-2]
            + padded[1:-1, 1:-1]
            + padded[1:-1, 2:]
            + padded[2:, :-2]
            + padded[2:, 1:-1]
            + padded[2:, 2:]
        ) / 9.0
    return result


def make_rect_normal(src: np.ndarray) -> np.ndarray:
    alpha = src[:, :, 3]
    mask = alpha > 16
    out = np.zeros_like(src, dtype=np.uint8)
    out[:, :, 0] = 128
    out[:, :, 1] = 128
    out[:, :, 2] = 255

    if not np.any(mask):
        return out

    ys, xs = np.nonzero(mask)
    min_x, max_x = int(xs.min()), int(xs.max())
    min_y, max_y = int(ys.min()), int(ys.max())
    cx = (min_x + max_x) * 0.5
    cy = (min_y + max_y) * 0.5
    rx = max((max_x - min_x) * 0.5, 1.0)
    ry = max((max_y - min_y) * 0.5, 1.0)

    dist = chamfer_distance(mask)
    max_dist = max(float(dist.max()), 1.0)
    dome = np.clip(dist / max_dist, 0.0, 1.0) ** 0.72

    rgb = src[:, :, :3].astype(np.float32) / 255.0
    luma = rgb[:, :, 0] * 0.299 + rgb[:, :, 1] * 0.587 + rgb[:, :, 2] * 0.114
    luma_values = luma[mask]
    if luma_values.size > 0:
        luma = (luma - float(luma_values.mean())) / max(float(luma_values.std()), 0.08)
    luma = np.clip(luma, -1.0, 1.0)

    height = box_blur(dome + luma * 0.11, 1)
    gy, gx = np.gradient(height)

    yy, xx = np.indices(mask.shape)
    fill_x = np.clip((xx - cx) / rx, -1.0, 1.0)
    fill_y = np.clip((cy - yy) / ry, -1.0, 1.0)

    # Strong silhouette bevel plus a broad angle-brush style fill gives the
    # pixel character visible 3D response as lights move around it.
    nx = (-gx * 3.8) + (fill_x * dome * 0.66)
    ny = (gy * 3.8) + (fill_y * dome * 0.66)

    # Dark/cool pixels recede a little so clothes, hair, and limbs catch
    # different colors instead of becoming one flat purple plane.
    color_x = (rgb[:, :, 0] - rgb[:, :, 2]) * 0.22
    color_y = (rgb[:, :, 1] - rgb[:, :, 0]) * 0.18
    nx += color_x * mask
    ny += color_y * mask

    nz = np.ones_like(nx) * 0.78
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx /= length
    ny /= length
    nz /= length

    out[:, :, 0] = np.where(mask, np.clip((nx * 0.5 + 0.5) * 255.0, 0, 255), 128).astype(np.uint8)
    out[:, :, 1] = np.where(mask, np.clip((ny * 0.5 + 0.5) * 255.0, 0, 255), 128).astype(np.uint8)
    out[:, :, 2] = np.where(mask, np.clip((nz * 0.5 + 0.5) * 255.0, 0, 255), 255).astype(np.uint8)
    out[:, :, 3] = np.where(mask, 255, 0).astype(np.uint8)
    return out


def normal_path_for(source: Path) -> Path:
    return source.with_name(f"{source.stem}_Normal{source.suffix}")


def generate(source: Path) -> None:
    image = Image.open(source).convert("RGBA")
    src = np.array(image)
    output = np.zeros_like(src, dtype=np.uint8)
    output[:, :, 0] = 128
    output[:, :, 1] = 128
    output[:, :, 2] = 255

    rects = parse_sprite_rects(source.with_suffix(source.suffix + ".meta"), image.height)
    if not rects:
        rects = [(0, 0, image.width, image.height)]

    for x, y, width, height in rects:
        crop = src[y : y + height, x : x + width, :]
        output[y : y + height, x : x + width, :] = make_rect_normal(crop)

    target = normal_path_for(source)
    Image.fromarray(output, "RGBA").save(target)
    print(f"{source.relative_to(ROOT)} -> {target.relative_to(ROOT)} ({len(rects)} frame(s))")


def main() -> None:
    for source in SOURCE_IMAGES:
        generate(source)


if __name__ == "__main__":
    main()
