from __future__ import annotations

from pathlib import Path

from generate_sprite_normals import ROOT, configure_normal_meta, ensure_secondary_texture, sobel_normal


TARGETS = [
    ROOT / "Assets/Sprites/Stage/Obstacles/electric.png",
    ROOT / "Assets/Sprites/Stage/Obstacles/electric2.png",
    ROOT / "Assets/Sprites/Stage/Obstacles/electric3-export.png",
    ROOT / "Assets/Sprites/Stage/Obstacles/electric4.png",
    ROOT / "Assets/Sprites/Stage/Obstacles/electric6.png",
    ROOT / "Assets/Sprites/Stage/Obstacles/electric_dst.png",
    ROOT / "Assets/Sprites/Stage/Obstacles/electric_off.png",
    ROOT / "Assets/Sprites/Stage/Obstacles/electric_off3.png",
]


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

    print(f"generated={len(generated)}")
    for item in generated:
        print(item)


if __name__ == "__main__":
    main()
