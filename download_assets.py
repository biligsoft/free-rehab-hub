#!/usr/bin/env python3
"""FreeRehabHub arayüz varlık indirme betiği.

Lucide (ISC/MIT) ikonlarını ve Kenney.nl (CC0) 2D UI paketlerini indirir.
Kaynaklar bu betiğin başındaki sabit listelerde tanımlı; URL'ler önceden
elle doğrulanmıştır (Kenney indirme linkleri her pakette benzersiz bir hash
içerir, tahmin edilerek üretilemez).

Kullanım:
    python3 download_assets.py

Yeniden çalıştırma güvenlidir: zaten indirilmiş dosyalar/paketler atlanır.
"""

import logging
import sys
import time
import urllib.error
import urllib.request
import zipfile
from dataclasses import dataclass, field
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent
ASSETS_ROOT = PROJECT_ROOT / "assets"
UI_ICONS_DIR = ASSETS_ROOT / "ui_icons"
GRAPHICS_2D_DIR = ASSETS_ROOT / "2d_graphics"
MODELS_3D_DIR = ASSETS_ROOT / "3d_models"

MAX_TOTAL_BYTES = 10 * 1024 * 1024 * 1024  # güvenlik üst sınırı (kullanıcı isteği)
REQUEST_DELAY_SECONDS = 1.5
REQUEST_TIMEOUT_SECONDS = 30
USER_AGENT = "FreeRehabHub-AssetFetcher/1.0"

# FreeRehabHub UI'sinde fiilen ihtiyaç duyulan/duyulacak kavramlara göre seçildi:
# kimlik/rol, hasta CRUD, kilit ekranı, erişilebilirlik, egzersiz/ödül, ilerleme
# raporu (Faz 6), durum/doğrulama geri bildirimi. Her isim indirme anında
# doğrulanır (mevcut değilse atlanır, betik çökmez).
LUCIDE_ICON_NAMES = [
    "house", "user", "users", "baby",
    "save", "x", "trash-2", "pencil", "square-pen", "plus", "search",
    "clipboard", "clipboard-list", "file-text",
    "lock", "lock-open", "eye", "eye-off", "shield", "shield-check",
    "settings", "contrast", "languages", "volume-2",
    "play", "pause", "activity", "heart", "heart-pulse", "star", "refresh-cw",
    "check", "circle-check", "circle-x", "circle-alert", "triangle-alert", "info",
    "chevron-left", "chevron-right", "chevrons-left", "chevrons-right",
    "chart-bar", "chart-line", "chart-column", "printer", "download",
    "calendar", "calendar-days", "clock", "log-out", "smile", "frown", "upload",
]
LUCIDE_RAW_BASE = "https://raw.githubusercontent.com/lucide-icons/lucide/main/icons"
LUCIDE_LICENSE_NOTE = (
    "Lucide Icons\n"
    "Kaynak: https://github.com/lucide-icons/lucide\n"
    "Lisans: ISC (ve Feather'dan türetilen ikonlar için MIT) — atıf zorunlu değil.\n"
)

# Kenney.nl indirme linkleri https://kenney.nl/assets/<slug> sayfalarından elle
# doğrulanmıştır (2026-07-24). Format: /media/pages/assets/<slug>/<hash>-<ts>/<dosya>.zip
# category: "2d" -> assets/2d_graphics, "3d" -> assets/3d_models
KENNEY_PACKS = [
    {
        "name": "kenney_ui-pack",
        "url": "https://kenney.nl/media/pages/assets/ui-pack/f651646eab-1718203990/kenney_ui-pack.zip",
        "source_page": "https://kenney.nl/assets/ui-pack",
        "category": "2d",
    },
    {
        "name": "kenney_game-icons",
        "url": "https://kenney.nl/media/pages/assets/game-icons/1ebf9c14af-1677661579/kenney_game-icons.zip",
        "source_page": "https://kenney.nl/assets/game-icons",
        "category": "2d",
    },
    {
        "name": "kenney_mobile-controls",
        "url": "https://kenney.nl/media/pages/assets/mobile-controls/0d047b3be4-1754738457/mobile-controls-1.zip",
        "source_page": "https://kenney.nl/assets/mobile-controls",
        "category": "2d",
    },
    {
        "name": "kenney_generic-items",
        "url": "https://kenney.nl/media/pages/assets/generic-items/96b9087204-1677667000/kenney_generic-items.zip",
        "source_page": "https://kenney.nl/assets/generic-items",
        "category": "2d",
    },
    {
        "name": "kenney_nature-kit",
        "url": "https://kenney.nl/media/pages/assets/nature-kit/37ac38a37b-1677698939/kenney_nature-kit.zip",
        "source_page": "https://kenney.nl/assets/nature-kit",
        "category": "3d",
    },
    {
        "name": "kenney_food-kit",
        "url": "https://kenney.nl/media/pages/assets/food-kit/83086fa91c-1719418518/kenney_food-kit.zip",
        "source_page": "https://kenney.nl/assets/food-kit",
        "category": "3d",
    },
    {
        "name": "kenney_car-kit",
        "url": "https://kenney.nl/media/pages/assets/car-kit/1a312ec241-1775131960/kenney_car-kit.zip",
        "source_page": "https://kenney.nl/assets/car-kit",
        "category": "3d",
    },
    {
        "name": "kenney_animal-pack",
        "url": "https://kenney.nl/media/pages/assets/animal-pack/480cf9f223-1677669996/kenney_animal-pack.zip",
        "source_page": "https://kenney.nl/assets/animal-pack",
        "category": "2d",  # düz PNG/SVG spritesheet - "Redux" adlı 3D varyantla karıştırılmasın
    },
    {
        "name": "kenney_furniture-kit",
        "url": "https://kenney.nl/media/pages/assets/furniture-kit/440e0608a4-1677580847/kenney_furniture-kit.zip",
        "source_page": "https://kenney.nl/assets/furniture-kit",
        "category": "3d",
    },
]
KENNEY_LICENSE = "CC0 1.0 (kamu malı) — Kenney.nl"


class DownloadBudgetExceeded(Exception):
    pass


@dataclass
class DownloadTracker:
    max_bytes: int
    downloaded_bytes: int = 0
    failures: list = field(default_factory=list)

    def register(self, byte_count: int) -> None:
        self.downloaded_bytes += byte_count
        if self.downloaded_bytes > self.max_bytes:
            raise DownloadBudgetExceeded(
                f"Toplam indirme boyutu sınırı aşıldı: "
                f"{self.downloaded_bytes} > {self.max_bytes} bayt"
            )


def setup_logging() -> logging.Logger:
    ASSETS_ROOT.mkdir(parents=True, exist_ok=True)
    log_path = ASSETS_ROOT / "download_assets.log"

    logger = logging.getLogger("download_assets")
    logger.setLevel(logging.INFO)
    formatter = logging.Formatter("%(asctime)s %(levelname)s %(message)s")

    stream_handler = logging.StreamHandler(sys.stdout)
    stream_handler.setFormatter(formatter)
    logger.addHandler(stream_handler)

    file_handler = logging.FileHandler(log_path, encoding="utf-8")
    file_handler.setFormatter(formatter)
    logger.addHandler(file_handler)

    return logger


def fetch_url(url: str) -> bytes:
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as response:
        return response.read()


def download_lucide_icons(tracker: DownloadTracker, logger: logging.Logger) -> None:
    UI_ICONS_DIR.mkdir(parents=True, exist_ok=True)
    logger.info("Lucide ikonları indiriliyor (%d aday ikon)...", len(LUCIDE_ICON_NAMES))

    for icon_name in LUCIDE_ICON_NAMES:
        dest_path = UI_ICONS_DIR / f"{icon_name}.svg"
        if dest_path.exists():
            logger.info("Atlandı (zaten var): %s", icon_name)
            continue

        url = f"{LUCIDE_RAW_BASE}/{icon_name}.svg"
        try:
            content = fetch_url(url)
        except urllib.error.HTTPError as exc:
            logger.warning("İkon indirilemedi (%s): HTTP %s — %s", icon_name, exc.code, url)
            tracker.failures.append(f"lucide:{icon_name}")
            continue
        except (urllib.error.URLError, TimeoutError, OSError) as exc:
            logger.warning("Bağlantı hatası (%s): %s — %s", icon_name, exc, url)
            tracker.failures.append(f"lucide:{icon_name}")
            continue

        dest_path.write_bytes(content)
        tracker.register(len(content))
        logger.info("İndirildi: %s.svg (%d bayt)", icon_name, len(content))
        time.sleep(REQUEST_DELAY_SECONDS)

    (UI_ICONS_DIR / "LICENSE.txt").write_text(LUCIDE_LICENSE_NOTE, encoding="utf-8")


def download_kenney_packs(tracker: DownloadTracker, logger: logging.Logger) -> None:
    GRAPHICS_2D_DIR.mkdir(parents=True, exist_ok=True)
    MODELS_3D_DIR.mkdir(parents=True, exist_ok=True)
    logger.info("Kenney.nl CC0 paketleri indiriliyor (%d paket)...", len(KENNEY_PACKS))

    for pack in KENNEY_PACKS:
        category_dir = GRAPHICS_2D_DIR if pack["category"] == "2d" else MODELS_3D_DIR
        dest_dir = category_dir / pack["name"]
        if dest_dir.exists() and any(dest_dir.iterdir()):
            logger.info("Atlandı (zaten var): %s", pack["name"])
            continue

        zip_path = ASSETS_ROOT / f"_tmp_{pack['name']}.zip"
        try:
            content = fetch_url(pack["url"])
        except urllib.error.HTTPError as exc:
            logger.warning("Paket indirilemedi (%s): HTTP %s — %s", pack["name"], exc.code, pack["url"])
            tracker.failures.append(f"kenney:{pack['name']}")
            continue
        except (urllib.error.URLError, TimeoutError, OSError) as exc:
            logger.warning("Bağlantı hatası (%s): %s — %s", pack["name"], exc, pack["url"])
            tracker.failures.append(f"kenney:{pack['name']}")
            continue

        tracker.register(len(content))
        zip_path.write_bytes(content)
        logger.info("İndirildi: %s.zip (%d bayt)", pack["name"], len(content))

        dest_dir.mkdir(parents=True, exist_ok=True)
        try:
            with zipfile.ZipFile(zip_path) as archive:
                archive.extractall(dest_dir)
            logger.info("Çıkarıldı: %s -> %s", pack["name"], dest_dir)
        except zipfile.BadZipFile as exc:
            logger.warning("Zip açılamadı (%s): %s", pack["name"], exc)
            tracker.failures.append(f"kenney:{pack['name']}:zip")
        finally:
            zip_path.unlink(missing_ok=True)

        (dest_dir / "SOURCE.txt").write_text(
            f"Kaynak sayfa: {pack['source_page']}\n"
            f"İndirme linki: {pack['url']}\n"
            f"Lisans: {KENNEY_LICENSE}\n",
            encoding="utf-8",
        )
        time.sleep(REQUEST_DELAY_SECONDS)


def main() -> int:
    logger = setup_logging()
    tracker = DownloadTracker(max_bytes=MAX_TOTAL_BYTES)

    stale_placeholder = MODELS_3D_DIR / "README.md"
    if stale_placeholder.exists():
        stale_placeholder.unlink()

    try:
        download_lucide_icons(tracker, logger)
        download_kenney_packs(tracker, logger)
    except DownloadBudgetExceeded as exc:
        logger.error(str(exc))
        return 1

    logger.info(
        "Tamamlandı. Toplam indirilen: %.2f MB. Başarısız: %d (%s)",
        tracker.downloaded_bytes / (1024 * 1024),
        len(tracker.failures),
        ", ".join(tracker.failures) if tracker.failures else "yok",
    )
    return 0 if not tracker.failures else 2


if __name__ == "__main__":
    raise SystemExit(main())
