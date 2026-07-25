#!/usr/bin/env python3
"""FreeRehabHub MediaPipe poz-tespit modeli indirme betiği.

Google'ın MediaPipe Pose Landmarker (lite) model paketini indirir.
Lisans: Apache 2.0 (https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker).

Kullanım:
    python3 download_model.py

Yeniden çalıştırma güvenlidir: model zaten indirilmişse atlanır.
"""

import logging
import sys
import urllib.error
import urllib.request
from pathlib import Path

SERVICE_ROOT = Path(__file__).resolve().parent
MODELS_DIR = SERVICE_ROOT / "models"
MODEL_PATH = MODELS_DIR / "pose_landmarker_lite.task"
MODEL_URL = (
    "https://storage.googleapis.com/mediapipe-models/pose_landmarker/"
    "pose_landmarker_lite/float16/latest/pose_landmarker_lite.task"
)
REQUEST_TIMEOUT_SECONDS = 30

logging.basicConfig(level=logging.INFO, format="%(message)s")
logger = logging.getLogger(__name__)


def main() -> int:
    MODELS_DIR.mkdir(parents=True, exist_ok=True)

    if MODEL_PATH.exists():
        logger.info("Model zaten mevcut, atlanıyor: %s", MODEL_PATH)
        return 0

    logger.info("İndiriliyor: %s -> %s", MODEL_URL, MODEL_PATH)
    request = urllib.request.Request(MODEL_URL, headers={"User-Agent": "FreeRehabHub-ModelFetcher/1.0"})
    try:
        with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as response:
            MODEL_PATH.write_bytes(response.read())
    except urllib.error.URLError as exc:
        logger.error("İndirme başarısız: %s", exc)
        return 1

    logger.info("Tamamlandı (%d bytes).", MODEL_PATH.stat().st_size)
    return 0


if __name__ == "__main__":
    sys.exit(main())
