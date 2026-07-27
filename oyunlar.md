# Oyun Tasarımları — Taslak

`egzersiz.md`'deki statik egzersiz kartlarının bir kısmını gerçek, etkileşimli `IExerciseModule`
oyunlarına dönüştürmek için tasarım taslakları. Bu dosya **kod değil** — henüz hiçbir modül
implementasyonu yazılmadı, `module-development` skill'indeki adımlara göre (şablon kopyala →
manifest doldur → Controller+Scoring yaz → test yaz) gerçek modüle çevrilmeden önce fikirlerin
gözden geçirilmesi için hazırlandı. Mevcut 2 gerçek Exercise modülü (`arm-raise`: kamera-tabanlı,
`target-tap`: kamerasız) ile aynı iki kategoriye ayrılıyor.

Her tasarım şu alanları taşıyor (gerçek `ModuleManifest`'e kolay çevrilsin diye):
`id` (taslak) / `displayName` (TR-EN) / `description` (TR-EN) / `disciplines` / `requiredCapabilities`
/ `difficultyRange` / **Oyun Mekaniği** (serbest metin) / **Dayandığı egzersiz(ler)** (`egzersiz.md`'deki id).

Kaynak ilham: rehabilitasyon "exergame" literatüründeki yaygın desenler (balon patlatma,
hareket-takipli hedefe ulaşma, denge oyunları) — bkz. dosya sonundaki kaynaklar. Hiçbir oyun
mekaniği belirli bir ticari ürünün (Wii Fit, Kinect oyunları vb.) kopyası değil, bu projenin
kendi `ModuleContext`/`ModuleResult`/`IPoseAwareModule` sözleşmesine göre özgün tasarlandı.

---

## Kamera Tabanlı Oyunlar (`IPoseAwareModule`, MediaPipe pose landmark gerektirir)

### balon-patlat — Balloon Pop
- **displayName:** TR "Balon Patlat" / EN "Balloon Pop"
- **description TR:** Ekranın altından yukarı doğru çıkan balonları, kolunuzu kaldırarak
  (omuz fleksiyonu) patlatın — balonlar gittikçe daha yüksekte beliriyor.
- **description EN:** Pop balloons rising from the bottom of the screen by raising your arm
  (shoulder flexion) — balloons appear progressively higher.
- **disciplines:** physiotherapy
- **requiredCapabilities:** camera
- **difficultyRange:** 1-3 (balon yüksekliği ve çıkış hızı zorlukla artıyor)
- **Oyun Mekaniği:** `arm-raise`'in aynı omuz-açısı hesaplamasını (`ShoulderFlexionCalculator`)
  kullanır, ama tekil "tekrar sayma" yerine ekranda gerçek zamanlı hareket eden hedefler
  (balonlar) vardır — el/bilek landmark'ı bir balonun konumuna yeterince yaklaşınca balon
  "patlar" (görsel + puan). Skor: patlatılan balon sayısı + ortalama erişilen açı.
  Zorluk arttıkça balonun belirdiği yükseklik `arm-raise`'in `FullFlexionAngleDegrees`'ine
  yaklaşır.
- **Dayandığı egzersiz(ler):** `shoulder-flexion-supine`, `duvarda-kayma`

### denge-ustasi — Balance Master
- **displayName:** TR "Denge Ustası" / EN "Balance Master"
- **description TR:** Ekrandaki bir karakter, gerçek vücut ağırlık kaymanıza göre sağa-sola
  yalpalıyor — karakteri düşürmeden bir kirişin üzerinde dengede tutun.
- **description EN:** An on-screen character sways left/right based on your real weight
  shift — keep it balanced on a beam without letting it fall.
- **disciplines:** physiotherapy
- **requiredCapabilities:** camera
- **difficultyRange:** 1-2 (kiriş genişliği daralarak zorlaşır)
- **Oyun Mekaniği:** Kalça/omuz landmark'larının yatay (x ekseni) pozisyonundan anlık bir
  "denge skoru" hesaplanır; karakter bu skora göre gerçek zamanlı yatay konumlanır. Hedef:
  belirlenen süre boyunca (ör. 20 sn) karakteri kirişin sınırları içinde tutmak. Skor:
  toplam süre boyunca merkezden ortalama sapma (düşük = iyi).
- **Dayandığı egzersiz(ler):** `tek-ayak-uzerinde-denge`, `topuk-parmak-yuruyusu`

### meyve-toplama — Fruit Catch
- **displayName:** TR "Meyve Toplama" / EN "Fruit Catch"
- **description TR:** Yukarıdan düşen meyveleri elinizi hareket ettirerek (bilek takibiyle)
  bir sepete toplayın — düşme hızı zamanla artar.
- **description EN:** Move your hand (tracked via wrist landmark) to catch fruits falling
  from the top of the screen into a basket — fall speed increases over time.
- **disciplines:** physiotherapy, occupationalTherapy
- **requiredCapabilities:** camera
- **difficultyRange:** 1-3
- **Oyun Mekaniği:** Bilek landmark'ının x-y konumu ekranda görünmez bir "sepet"i sürükler;
  meyve sepetin bulunduğu alana denk gelince yakalanmış sayılır. Reach/ROM (kolun ekranın
  farklı bölgelerine ulaşması) ve hand-eye coordination'ı birleştirir. Skor: yakalanan/kaçan
  meyve oranı + ortalama reaksiyon süresi (bkz. `TargetTapScorer`'daki desenle tutarlı).
- **Dayandığı egzersiz(ler):** genel ROM/reach egzersizleri (`duvarda-kayma`, `top-yakalama-atma`)

### iki-elle-ulasma — Two-Hand Reach
- **displayName:** TR "İki Elle Ulaşma" / EN "Two-Hand Reach"
- **description TR:** Ekranda aynı anda beliren iki hedefe, HER İKİ elinizi de kullanarak
  aynı anda ulaşmaya çalışın — bilateral koordinasyonu çalıştırır.
- **description EN:** Reach two simultaneously appearing targets using BOTH hands at once —
  trains bilateral coordination.
- **disciplines:** occupationalTherapy, physiotherapy
- **requiredCapabilities:** camera
- **difficultyRange:** 2-3
- **Oyun Mekaniği:** İki bilek landmark'ı aynı anda takip edilir; iki hedef de sadece ikisi
  eş zamanlı (belirlenen bir tolerans penceresi içinde) doğru konuma ulaşınca "başarı" sayılır
  — tek elle ulaşmak yetersiz, bu mekanik özellikle bilateral koordinasyon açığını hedefler.
  Skor: eş-zamanlı başarı oranı + ortalama gecikme farkı (iki el arasındaki zamanlama farkı).
- **Dayandığı egzersiz(ler):** `top-yakalama-atma`, `iki-el-kutu-acma`

---

## Kamerasız Oyunlar (`IExerciseModule`, sadece fare/dokunmatik/klavye — `target-tap` ile aynı kategori)

### hedef-vurma-cift-el — Bilateral Target Tap
- **displayName:** TR "Hedef Vurma — Çift El" / EN "Bilateral Target Tap"
- **description TR:** `target-tap`'in bir varyasyonu: hedefler ekranın sol veya sağ
  yarısında beliriyor, doğru elin (sol hedefe sol el, sağ hedefe sağ el gibi kavramsal
  kural — ama tek fare/dokunmatik girişiyle) doğru tarafa tıklaması isteniyor.
- **description EN:** A variation of `target-tap`: targets appear on the left or right half
  of the screen, requiring the player to tap the correct side promptly.
- **disciplines:** occupationalTherapy, psychology
- **requiredCapabilities:** (yok — kamerasız)
- **difficultyRange:** 1-2
- **Oyun Mekaniği:** `TargetTapScorer`'ın aynısı temel alınabilir (hitCount/missCount/
  averageReactionTimeSeconds), ama hedefin sol/sağ yarısına göre ayrı bir "doğru taraf"
  metriği eklenir — yanlış tarafa (ör. geç/karışık) tıklama "yanlış taraf" olarak ayrıca
  sayılır. Reaksiyon süresi + doğruluk birlikte skorlanır.
- **Dayandığı egzersiz(ler):** dikkat/reaksiyon egzersizleri, `target-tap`'in doğal devamı

### nesne-bul-adlandir — Name That Object
- **displayName:** TR "Nesne Bul ve Adlandır" / EN "Name That Object"
- **description TR:** Ekranda bir resim beliriyor, terapist hastanın söylediği kelimeyi
  "doğru" veya "yardımla doğru" olarak işaretliyor — kategori temalı setler (meyveler,
  hayvanlar, ev eşyaları) arasından seçilebiliyor.
- **description EN:** A picture appears on screen; the therapist marks the patient's spoken
  answer as "correct" or "correct with cue" — picture sets are organized by theme (fruits,
  animals, household items).
- **disciplines:** speechTherapy
- **requiredCapabilities:** (yok — kamerasız, terapist tarafından elle işaretlenen sonuç)
- **difficultyRange:** 1-2
- **Oyun Mekaniği:** Klasik "confrontation naming" testinin gamified hali — art arda gelen
  resimler için terapist bir "Doğru" / "İpucuyla Doğru" / "Yanlış" butonuna basıyor
  (`content-packs/exercise-library`'deki resim setleri kullanılabilir, ayrıca telif-free
  resim kaynağı gerekiyor — bkz. Notlar). Skor: doğru/ipuçlu-doğru/yanlış oranı + ortalama
  yanıt süresi.
- **Dayandığı egzersiz(ler):** `nesne-adlandirma`, `resim-tanimlama`

### nefes-balonu — Breathing Balloon
- **displayName:** TR "Nefes Balonu" / EN "Breathing Balloon"
- **description TR:** Ekrandaki bir balon, kutu nefesi ritmine (4 sn şişme - 4 sn tutma -
  4 sn sönme - 4 sn bekleme) göre büyüyüp küçülüyor — hastadan bu ritme nefesiyle eşlik
  etmesi isteniyor.
- **description EN:** An on-screen balloon inflates/deflates according to a box-breathing
  rhythm (4s inhale - 4s hold - 4s exhale - 4s pause) — the patient follows the rhythm with
  their breath.
- **disciplines:** psychology
- **requiredCapabilities:** (yok — tamamen zamanlayıcı tabanlı görsel, mikrofon gerektirmiyor)
- **difficultyRange:** 1
- **Oyun Mekaniği:** Kamera/mikrofon tabanlı gerçek nefes algılama YOK (kapsam dışı,
  karmaşıklık/güvenilirlik riski) — sadece görsel bir pacer. "Skor" klasik anlamda yok,
  tamamlanan döngü sayısı `ModuleResult.Metrics`'e yazılır (ör. `completedCycles`). Bu,
  projenin "reps/sets" modelinin fiziksel olmayan bir egzersize nasıl uyarlanabileceğine
  iyi bir örnek.
- **Dayandığı egzersiz(ler):** `kutu-nefesi`, `diyafram-nefesi`

### sekil-takibi — Shape Trace
- **displayName:** TR "Şekil Takibi" / EN "Shape Trace"
- **description TR:** Ekranda beliren bir şekli veya harfi fare/dokunmatikle takip edin —
  doğruluk, çizginin şekle ne kadar yakın kaldığına göre puanlanıyor.
- **description EN:** Trace a shape or letter shown on screen using mouse/touch — accuracy
  is scored by how closely the drawn line follows the shape.
- **disciplines:** occupationalTherapy, specialEducation
- **requiredCapabilities:** (yok)
- **difficultyRange:** 1-3 (basit şekillerden karmaşık harflere)
- **Oyun Mekaniği:** Çizilen path ile hedef path arasındaki ortalama piksel mesafesi
  hesaplanıp bir doğruluk yüzdesine çevrilir (`NormalizedScore`). Zorluk, şeklin karmaşıklığı
  (köşe sayısı, eğrilik) ile artar.
- **Dayandığı egzersiz(ler):** `sekil-harf-izleme`, `harf-sayi-izleme`

### renk-kutusu — Color Sort Game
- **displayName:** TR "Renk Kutusu" / EN "Color Sort Game"
- **description TR:** Ekrana gelen renkli/şekilli nesneleri sürükleyip doğru renk/şekil
  kutusuna bırakın — zamanla nesne akış hızı artıyor.
- **description EN:** Drag colored/shaped objects appearing on screen into the matching
  color/shape bin — object flow speed increases over time.
- **disciplines:** specialEducation, occupationalTherapy
- **requiredCapabilities:** (yok)
- **difficultyRange:** 1-2
- **Oyun Mekaniği:** Basit sürükle-bırak; doğru kutuya bırakma = isabet, yanlış kutu veya
  süre aşımı = kaçırma (`TargetTapScorer`'a benzer bir hit/miss modeli). Skor: isabet oranı.
- **Dayandığı egzersiz(ler):** `rege-sekle-gore-siralama`, `blok-istifleme`

---

## Notlar / açık sorular

- **Kamera-tabanlı 4 oyun** mevcut `IPoseAwareModule`/`MediaPipePoseTrackingService`
  altyapısını doğrudan kullanabilir — yeni bir servis gerekmiyor, sadece yeni Controller +
  Scoring sınıfları (bkz. `module-development` skill § 3a).
- **"Nesne Bul ve Adlandır"** resim seti gerektiriyor — `content-packs/`'e telifsiz/serbest
  lisanslı resimler eklenmesi lazım (bkz. `assets/ASSET_MANIFEST.md`'deki Kenney/Lucide
  kürasyon deseni, aynı yaklaşım burada da uygulanabilir).
- **"Nefes Balonu"** bilinçli olarak mikrofon/nefes-algılama içermiyor — bu, yeni bir
  donanım/servis bağımlılığı (ayrı bir "MediaPipe" tarzı risk) getirir, kapsam dışı
  bırakıldı; sadece görsel pacer yeterli kabul edildi.
- Hiçbiri henüz gerçek modül olarak yazılmadı — bu sadece bir tasarım turu. Kullanıcı
  onaylarsa, `module-development` skill'indeki adımlarla (tek adım/onay kuralına uyarak)
  gerçek modüllere çevrilebilir.

## Kaynaklar (ilham için, mekanikler özgün tasarlandı)

- [Gamification in Musculoskeletal Rehabilitation — PMC/NIH](https://pmc.ncbi.nlm.nih.gov/articles/PMC9789284/)
- [Gaming In Physical Therapy & Rehabilitation — Physio Ed.](https://physioed.com/health-advice/treatment/gaming-in-physical-therapy-and-rehabilitation/)
- [Exergames: leveraging the fun of games to support therapy](https://www.medica-tradefair.com/en/media-news/spheres-of-medica-magazine/physio-tech/exergames-leveraging-fun-games-support-therapy)
- [Gamifying Rehabilitation: Motion-Controlled Video Games in Physical Therapy — PLAYWORK](https://www.playwork.me/post/gamifying-rehabilitation-motion-controlled-video-games-in-physical-therapy)
- [10 Creative Naming Therapy Activities for Aphasia — Tactus Therapy](https://tactustherapy.com/aphasia-activities-naming-therapy/)
