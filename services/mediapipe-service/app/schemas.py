from __future__ import annotations

from datetime import datetime
from typing import List

from pydantic import BaseModel, ConfigDict


def _to_camel(snake: str) -> str:
    first, *rest = snake.split("_")
    return first + "".join(word.capitalize() for word in rest)


# C# tarafı (Modules.Contracts) PropertyNamingPolicy.CamelCase ile serialize/deserialize ediyor
# (bkz. FormSchemaLoader/ContentPackExerciseLibraryRepository) — burada da aynı sözleşme.
class CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=_to_camel, populate_by_name=True)


class PosePoint(CamelModel):
    x: float
    y: float
    z: float


class PoseLandmark(CamelModel):
    type: str
    normalized: PosePoint
    world: PosePoint
    visibility: float
    presence: float


class DetectedPose(CamelModel):
    landmarks: List[PoseLandmark]


class PoseFrame(CamelModel):
    captured_at: datetime
    poses: List[DetectedPose]
