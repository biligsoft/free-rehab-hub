#!/usr/bin/env python3
"""FreeRehabHub sürüm paketleme betiği.

Godot'un `--export-release` ile ürettiği build çıktısını, çalışma zamanında ham dosya G/Ç ile
okunan (bkz. CLAUDE.md, autoload/AppContentRoot.cs) loose-file klasörlerle (content-packs/,
assets/fonts/liberation-sans/, modül manifest.json'ları) ve PyInstaller ile paketlenmiş
mediapipe-service binary'siyle (bkz. services/mediapipe-service/build_exe.py) birleştirip
tek bir dağıtılabilir zip üretir.

Ön koşullar (bu betik ikisini de ÇALIŞTIRMAZ, ikisinin de zaten yapılmış olmasını bekler):
  1. Godot export'u (`godot --export-release "<preset>" build/<platform>/...`, bkz.
     export_presets.cfg).
  2. mediapipe-service PyInstaller build'i (`python build_exe.py` services/mediapipe-service/
     içinde) — AYNI işletim sisteminde, PyInstaller cross-compile desteklemiyor.

Kullanım:
    python scripts/package_release.py --platform linux --godot-build-dir build/linux
    python scripts/package_release.py --platform windows --godot-build-dir build/windows
    python scripts/package_release.py --platform macos --godot-build-dir build/macos
"""

from __future__ import annotations

import argparse
import logging
import shutil
import sys
import zipfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
LOOSE_CONTENT_DIRECTORIES = ["content-packs", "assets/fonts/liberation-sans"]

logging.basicConfig(level=logging.INFO, format="%(message)s")
logger = logging.getLogger(__name__)


def copy_loose_content(godot_build_dir: Path) -> None:
    for relative_path in LOOSE_CONTENT_DIRECTORIES:
        source = REPO_ROOT / relative_path
        destination = godot_build_dir / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            shutil.rmtree(destination)
        shutil.copytree(source, destination)
        logger.info("Kopyalandı: %s -> %s", relative_path, destination)


def copy_module_manifests(godot_build_dir: Path) -> None:
    for manifest_path in (REPO_ROOT / "modules").glob("*/manifest.json"):
        module_id = manifest_path.parent.name
        destination = godot_build_dir / "modules" / module_id / "manifest.json"
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(manifest_path, destination)
        logger.info("Kopyalandı: modules/%s/manifest.json -> %s", module_id, destination)


def copy_mediapipe_service(godot_build_dir: Path) -> None:
    source = REPO_ROOT / "services" / "mediapipe-service" / "dist" / "mediapipe-service"
    if not source.exists():
        raise FileNotFoundError(
            f"{source} yok — önce services/mediapipe-service/build_exe.py çalıştırılmalı."
        )

    destination = godot_build_dir / "services" / "mediapipe-service" / "dist" / "mediapipe-service"
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        shutil.rmtree(destination)
    shutil.copytree(source, destination)
    logger.info("Kopyalandı: services/mediapipe-service/dist/mediapipe-service -> %s", destination)


def create_zip(godot_build_dir: Path, platform: str) -> Path:
    zip_path = REPO_ROOT / "build" / f"FreeRehabHub-{platform}.zip"
    zip_path.parent.mkdir(parents=True, exist_ok=True)
    if zip_path.exists():
        zip_path.unlink()

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as archive:
        for file_path in godot_build_dir.rglob("*"):
            if file_path.is_file():
                archive.write(file_path, file_path.relative_to(godot_build_dir))

    logger.info("Zip oluşturuldu: %s", zip_path)
    return zip_path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--platform", required=True, choices=["linux", "windows", "macos"])
    parser.add_argument("--godot-build-dir", required=True, type=Path)
    args = parser.parse_args()

    godot_build_dir = (
        args.godot_build_dir if args.godot_build_dir.is_absolute() else REPO_ROOT / args.godot_build_dir
    )
    if not godot_build_dir.exists():
        logger.error(
            "Godot build klasörü yok: %s (önce `godot --export-release` çalıştırılmalı)", godot_build_dir
        )
        return 1

    copy_loose_content(godot_build_dir)
    copy_module_manifests(godot_build_dir)
    copy_mediapipe_service(godot_build_dir)
    create_zip(godot_build_dir, args.platform)

    return 0


if __name__ == "__main__":
    sys.exit(main())
