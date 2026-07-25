from __future__ import annotations

import asyncio
import logging

from fastapi import FastAPI, WebSocket, WebSocketDisconnect

from .pose_tracker import PoseTracker

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="FreeRehabHub MediaPipe Service")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.websocket("/ws/pose")
async def pose_stream(websocket: WebSocket) -> None:
    await websocket.accept()

    tracker = PoseTracker()
    try:
        tracker.open()
    except Exception as exc:
        logger.exception("Poz takibi başlatılamadı")
        await websocket.close(code=1011, reason=str(exc))
        return

    try:
        loop = asyncio.get_event_loop()
        frame_iter = tracker.frames()
        while True:
            # MediaPipe/OpenCV çağrıları senkron ve bloklayıcı — event loop'u kilitlememek
            # için ayrı bir thread'de çalıştırılıyor.
            pose_frame = await loop.run_in_executor(None, next, frame_iter)
            await websocket.send_text(pose_frame.model_dump_json(by_alias=True))
    except WebSocketDisconnect:
        logger.info("İstemci bağlantıyı kapattı.")
    finally:
        tracker.close()
