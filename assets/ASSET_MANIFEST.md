# Görsel Varlık Kaynakları

Bu klasördeki varlıklar `download_assets.py` betiğiyle indirildi (2026-07-24).
Hepsi ticari kullanıma açık, telifsiz/serbest lisanslı kaynaklardan —
`clinical-data-handling` skill'indeki telif hassasiyeti burada da uygulandı.
Toplam boyut: ~110 MB (10 GB güvenlik sınırının çok altında — Kenney'nin
düşük-poligonlu optimize paketleri gerçekte hiçbir zaman GB mertebesine
çıkmıyor; tüm ilgili kataloğu almak bile bunun katına ulaşmaz).

## ui_icons/ — Lucide Icons

- Kaynak: https://github.com/lucide-icons/lucide
- Lisans: ISC (Feather'dan türetilen bazı ikonlar için MIT) — atıf zorunlu değil
- 53 SVG, tek renk / `currentColor` tabanlı çizgi ikon seti
- Seçim gerekçesi: kilit ekranı, hasta CRUD, terapist seçimi, ilerleme
  raporu (Faz 6), erişilebilirlik (Faz 7) gibi ekranlarda fiilen karşılığı
  olan kavramlara göre kürasyonlu bir alt küme

## 2d_graphics/ — Kenney.nl (CC0), 2D içerik

| Paket | Kaynak sayfa | İçerik / kullanım fikri |
|---|---|---|
| `kenney_ui-pack` | kenney.nl/assets/ui-pack | Panel/buton/kaydırıcı — oyunlaştırılmış egzersiz arayüzü |
| `kenney_game-icons` | kenney.nl/assets/game-icons | Yıldız/kalp/onay — ödül sistemi (Faz 7) |
| `kenney_mobile-controls` | kenney.nl/assets/mobile-controls | Dokunmatik kontrol öğeleri — kiosk modu (Faz 7) |
| `kenney_generic-items` | kenney.nl/assets/generic-items | 160 günlük eşya ikonu — nesne adlandırma/eşleştirme egzersizleri (konuşma terapisi, özel eğitim) |
| `kenney_animal-pack` | kenney.nl/assets/animal-pack | Düz hayvan illüstrasyonları (PNG/SVG spritesheet) — adlandırma kartları, özel eğitim |

## 3d_models/ — Kenney.nl (CC0), 3D içerik

Format: OBJ + FBX + GLB (Godot 4 için GLB önerilir, doğrudan import edilir).

| Paket | Kaynak sayfa | İçerik / kullanım fikri |
|---|---|---|
| `kenney_nature-kit` | kenney.nl/assets/nature-kit | 330 doğa objesi (ağaç, kaya, bitki) |
| `kenney_food-kit` | kenney.nl/assets/food-kit | 200 yiyecek modeli (elma dahil) — özellikle konuşma terapisi nesne adlandırma egzersizleri |
| `kenney_car-kit` | kenney.nl/assets/car-kit | Araba/kamyon/van modelleri |
| `kenney_furniture-kit` | kenney.nl/assets/furniture-kit | Ev eşyası (sandalye, masa, mutfak/banyo) — ergoterapi/günlük yaşam aktiviteleri temalı egzersizler |

Her paketin kendi `SOURCE.txt` dosyası indirme linkini ve lisansı tekrar eder.
Tüm Kenney paketleri CC0 1.0 (kamu malı, atıf gerekmez).

## Kapsam kararı: neden "her şey" değil

Kenney'nin tüm kataloğu (200+ paket, uzay/yarış/kale gibi projeyle
alakasız birçok tema dahil) yerine, projenin fiili ihtiyaçlarına
(terapi/egzersiz UI'si, nesne adlandırma, kiosk/ödül) karşılık gelen
paketler seçildi. 10 GB rakamı bir güvenlik tavanıydı, hedef değil —
Kenney'nin optimize paketleri zaten küçük, o yüzden kürasyonlu seçim
yapmak boyutu düşürmedi, sadece alakasız içeriği eledi.

## Yeniden indirme / güncelleme

```
python3 download_assets.py
```

Zaten var olan dosya/paketleri atlar, sadece eksikleri tamamlar. Yeni bir
Kenney paketi eklemek için `KENNEY_PACKS` listesine bir girdi eklemek
yeterli (indirme linkini kenney.nl/assets/<slug> sayfasından elle
doğrulamayı unutma — linkler tahmin edilemez bir hash içeriyor).
