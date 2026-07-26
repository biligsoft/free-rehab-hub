#!/usr/bin/env python3
"""mediapipe-service'i PyInstaller ile tek bir çalıştırılabilir dizine paketler.

Kullanım:
    pip install -r requirements.txt -r requirements-build.txt
    python build_exe.py

Çıktı: dist/mediapipe-service/ (onedir modu — mediapipe gibi çok sayıda native ikili
içeren büyük bağımlılıklar için onefile modundan daha güvenilir ve daha hızlı başlıyor,
onefile her başlangıçta tüm içeriği geçici bir dizine açar).

PyInstaller cross-compile desteklemez — bu betik SADECE çalıştırıldığı işletim sistemi
için bir çıktı üretir (Windows'ta çalıştırılırsa Windows .exe, macOS'ta macOS binary'si vb.
üretilir). Bu yüzden Windows/macOS çıktıları gerçek o platformlarda (bu projede GitHub
Actions windows-latest/macos-latest runner'larında) üretilmeli.

`models/` klasörü (download_model.py'nin indirdiği .task dosyası) kasıtlı olarak pakete
dahil edilmiyor — çalışma zamanında dosya yolu olarak okunuyor, PyInstaller'ın statik
paketlemesine gerek yok; `dist/mediapipe-service/` yanına ayrıca kopyalanmalı.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

SERVICE_ROOT = Path(__file__).resolve().parent
ENTRY_POINT = SERVICE_ROOT / "run_server.py"


def main() -> int:
    command = [
        sys.executable,
        "-m",
        "PyInstaller",
        "--name",
        "mediapipe-service",
        "--onedir",
        "--noconfirm",
        "--collect-all",
        "mediapipe",
        str(ENTRY_POINT),
    ]
    result = subprocess.run(command, cwd=SERVICE_ROOT, check=False)
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
