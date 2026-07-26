#!/usr/bin/env python3
"""FreeRehabHub mediapipe-service için PyInstaller giriş noktası.

Geliştirmede servis `python -m uvicorn app.main:app ...` ile başlatılıyor — bu, PyInstaller'ın
donduramayacağı bir CLI modül çağrısı (PyInstaller tek bir betiği bağımlılıklarıyla birlikte
dondurur, genel bir `python -m X` yorumlayıcısı üretmez). Bu betik, `MediaPipePoseTrackingService`
(C#) tarafından hem geliştirme modunda (`.venv`'deki python ile) hem paketlenmiş modda
(`build_exe.py`'nin ürettiği tek çalıştırılabilir ile) aynı `--host`/`--port` argüman
sözleşmesiyle çağrılabilsin diye var.

Kullanım:
    python run_server.py --host 127.0.0.1 --port 8000
"""

from __future__ import annotations

import argparse

import uvicorn

from app.main import app


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()

    uvicorn.run(app, host=args.host, port=args.port)


if __name__ == "__main__":
    main()
