from __future__ import annotations

import logging
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterator, Optional

import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision

from .landmark_types import LANDMARK_NAMES
from .schemas import DetectedPose, PoseFrame, PoseLandmark, PosePoint

logger = logging.getLogger(__name__)

DEFAULT_MODEL_PATH = Path(__file__).resolve().parent.parent / "models" / "pose_landmarker_lite.task"


class PoseTracker:
    def __init__(self, camera_index: int = 0, model_path: Path = DEFAULT_MODEL_PATH) -> None:
        self._camera_index = camera_index
        self._model_path = model_path
        self._capture: Optional[cv2.VideoCapture] = None
        self._landmarker: Optional[vision.PoseLandmarker] = None

    def open(self) -> None:
        if not self._model_path.exists():
            raise FileNotFoundError(
                f"Pose landmarker modeli bulunamadı: {self._model_path} "
                "— önce 'python download_model.py' çalıştırılmalı."
            )

        base_options = mp_python.BaseOptions(model_asset_path=str(self._model_path))
        options = vision.PoseLandmarkerOptions(
            base_options=base_options,
            running_mode=vision.RunningMode.VIDEO,
            num_poses=1,
        )
        self._landmarker = vision.PoseLandmarker.create_from_options(options)

        capture = cv2.VideoCapture(self._camera_index)
        if not capture.isOpened():
            self.close()
            raise RuntimeError(f"Kamera açılamadı (index={self._camera_index}).")
        self._capture = capture

    def close(self) -> None:
        if self._capture is not None:
            self._capture.release()
            self._capture = None
        if self._landmarker is not None:
            self._landmarker.close()
            self._landmarker = None

    def frames(self) -> Iterator[PoseFrame]:
        if self._capture is None or self._landmarker is None:
            raise RuntimeError("PoseTracker.open() çağrılmadan frames() kullanılamaz.")

        while True:
            ok, frame_bgr = self._capture.read()
            if not ok:
                logger.warning("Kameradan frame okunamadı, atlanıyor.")
                continue

            frame_rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
            mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=frame_rgb)
            # detect_for_video zaman damgasının monoton artmasını şart koşuyor.
            timestamp_ms = int(time.monotonic() * 1000)
            result = self._landmarker.detect_for_video(mp_image, timestamp_ms)

            yield _to_pose_frame(result)


def _to_pose_frame(result) -> PoseFrame:
    poses = []
    for normalized_landmarks, world_landmarks in zip(result.pose_landmarks, result.pose_world_landmarks):
        landmarks = [
            PoseLandmark(
                type=name,
                normalized=PosePoint(x=n.x, y=n.y, z=n.z),
                world=PosePoint(x=w.x, y=w.y, z=w.z),
                visibility=n.visibility,
                presence=n.presence,
            )
            for name, n, w in zip(LANDMARK_NAMES, normalized_landmarks, world_landmarks)
        ]
        poses.append(DetectedPose(landmarks=landmarks))

    return PoseFrame(captured_at=datetime.now(timezone.utc), poses=poses)
