# Oyun Tasarımları — Taslak

`egzersiz.md`'deki statik egzersiz kartlarının bir kısmını gerçek, etkileşimli `IExerciseModule`
oyunlarına dönüştürmek için tasarım taslakları. Bu dosya **kod değil** — henüz hiçbir modül
implementasyonu yazılmadı, `module-development` skill'indeki adımlara göre (şablon kopyala →
manifest doldur → Controller+Scoring yaz → test yaz) gerçek modüle çevrilmeden önce fikirlerin
gözden geçirilmesi için hazırlandı. Mevcut 3 gerçek Exercise modülü (`arm-raise`: kamera-tabanlı,
`target-tap`/`color-sort`: kamerasız) ile aynı kategorilere ayrılıyor; bilişsel egzersiz oyunları
için ayrı bir üçüncü bölüm eklendi (bkz. aşağıda) — bunlar da kamerasız ama farklı bir klinik
hedefi (bellek/dikkat/yürütücü işlevler) var, bu yüzden ayrı gruplandı.

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

## Bilişsel Egzersiz Oyunları (kamerasız, bellek/dikkat/yürütücü işlevler)

Bu grup `egzersiz.md`'deki belirli bir statik karta dayanmıyor — bilişsel rehabilitasyon
literatüründeki klasik nöropsikolojik görev ailelerinin (bellek aralığı, sürdürülen dikkat,
bilişsel esneklik/set-shifting, ikili görev/bölünmüş dikkat) gamified uyarlamaları. Klinik
karşılıkları `Discipline` enum'unda ayrı bir "bilişsel" değeri olmadığı için en yakın disipline
(genelde `occupationalTherapy`/`speechTherapy`/`psychology`) atandı — yeni bir `Discipline` değeri
eklemek ayrı, daha büyük bir mimari karar olurdu, bu tasarım turunda önerilmiyor.

### hafiza-kartlari — Memory Match
- **displayName:** TR "Hafıza Kartları" / EN "Memory Match"
- **description TR:** Ters çevrilmiş kart çiftlerini sırayla açıp eşleşenleri bulun —
  hangi kartın nerede olduğunu hatırlamanız gerekiyor.
- **description EN:** Flip face-down cards two at a time to find matching pairs — you need
  to remember where each card is.
- **disciplines:** occupationalTherapy, speechTherapy, psychology
- **requiredCapabilities:** (yok)
- **difficultyRange:** 1-3 (kart sayısı 8'den 24'e çıkarak artıyor)
- **Oyun Mekaniği:** Klasik "concentration" mekaniği — N çift kart karışık dizilir, oyuncu
  iki kart açar, eşleşmezse belirli bir süre sonra kapanır. Skor: doğru eşleşme sayısı /
  toplam deneme sayısı (verimlilik) + tamamlama süresi. Görsel-mekansal kısa süreli belleği
  hedefler.
- **Dayandığı egzersiz(ler):** yok (yeni bilişsel görev ailesi, doğrudan literatürden)

### farki-bul — Spot the Difference
- **displayName:** TR "Farkı Bul" / EN "Spot the Difference"
- **description TR:** Yan yana duran neredeyse özdeş iki görsel arasındaki farklı noktayı
  süre dolmadan bulup tıklayın.
- **description EN:** Find and click the differing spot between two nearly identical
  side-by-side images before time runs out.
- **disciplines:** occupationalTherapy, psychology
- **requiredCapabilities:** (yok)
- **difficultyRange:** 1-2
- **Oyun Mekaniği:** Basit geometrik şekillerden oluşan iki panel (renk/boyut/pozisyonda tek
  bir fark), tıklama koordinatı farkın bulunduğu bölgeye denk gelirse isabet. Sürdürülen
  dikkat + görsel tarama becerisini hedefler. Skor: doğru bulma oranı + ortalama bulma süresi.
- **Dayandığı egzersiz(ler):** yok

### sirayi-tamamla — Sequence Completion
- **displayName:** TR "Sırayı Tamamla" / EN "Sequence Completion"
- **description TR:** Karışık sırada gösterilen günlük bir aktivitenin adımlarını (ör. "çay
  demleme") doğru sıraya dizin.
- **description EN:** Arrange the scrambled steps of a daily activity (e.g., "making tea")
  into the correct order.
- **disciplines:** occupationalTherapy, speechTherapy, specialEducation
- **requiredCapabilities:** (yok)
- **difficultyRange:** 1-3 (adım sayısı ve benzer-adım tuzakları artarak)
- **Oyun Mekaniği:** Adım kartları karışık sırada listelenir, oyuncu doğru sıraya göre
  tıklayarak seçer (her tıklama bir sonraki "doğru" adımı seçmeli). Yürütücü işlev
  (planlama/sıralama) becerisini hedefler. Skor: doğru sırada seçilen adım oranı.
- **Dayandığı egzersiz(ler):** `gunluk-yasam-becerisi-simulasyonu`

### renk-kelime-uyumu — Color-Word Match (Stroop-Inspired)
- **displayName:** TR "Renk-Kelime Uyumu" / EN "Color-Word Match"
- **description TR:** Ekranda bir renk kelimesi farklı bir renkte yazılı beliriyor (ör.
  kırmızı renkte yazılmış "MAVİ" kelimesi) — yazının rengine göre (kelimenin anlamına değil)
  doğru butona basın.
- **description EN:** A color word appears written in a different ink color (e.g., the word
  "BLUE" written in red) — press the button matching the ink color, not the word's meaning.
- **disciplines:** psychology, speechTherapy
- **requiredCapabilities:** (yok)
- **difficultyRange:** 2-3 (klasik Stroop etkisi — bilinçli olarak zor)
- **Oyun Mekaniği:** `color-sort`'un renk-eşleştirme altyapısı doğrudan yeniden kullanılabilir
  (aynı buton seti), ama hedef artık bir `ColorRect` değil, kelimenin YAZI RENGİ. Seçici
  dikkat/tepki engelleme (inhibition) becerisini hedefler — klasik bir nöropsikolojik ölçüm
  paradigmasının (Stroop) gamified, teşhis amacı taşımayan bir uyarlaması (bkz. Notlar).
  Skor: doğruluk + tepki süresi.
- **Dayandığı egzersiz(ler):** yok (yeni bilişsel görev ailesi)

### cift-gorev-meydan-okumasi — Dual-Task Challenge
- **displayName:** TR "Çift Görev Meydan Okuması" / EN "Dual-Task Challenge"
- **description TR:** `target-tap`'teki gibi hedeflere tıklarken AYNI ANDA arka planda
  duyduğunuz sesler arasında belirli bir sesi (ör. "kaç kere 'bip' duydunuz") zihinden
  sayın — iki görevi birlikte yürütmeniz gerekiyor.
- **description EN:** While tapping targets like in `target-tap`, SIMULTANEOUSLY keep a
  mental count of a specific sound played in the background (e.g., "how many times did you
  hear 'beep'") — you must perform both tasks at once.
- **disciplines:** psychology, physiotherapy
- **requiredCapabilities:** (yok)
- **difficultyRange:** 2-3
- **Oyun Mekaniği:** `TargetTapScorer`'ın motor görevi + ayrı bir "kaç ses duydun" sorusu tur
  sonunda soruluyor, iki ayrı doğruluk metriği (motor isabet oranı + sayma doğruluğu)
  birleştirilerek skorlanıyor. Bölünmüş dikkat/ikili görev performansını hedefler — klinik
  olarak düşme riski değerlendirmesinde kullanılan ikili görev paradigmasının (yürürken
  konuşma vb.) masaüstü/oturarak versiyonu.
- **Dayandığı egzersiz(ler):** `target-tap`'in doğal devamı, ayrıca genel dikkat egzersizleri

### sayi-dizisi-hatirlama — Digit Span Recall
- **displayName:** TR "Sayı/Renk Dizisi Hatırlama" / EN "Digit/Color Span Recall"
- **description TR:** Ekranda kısaca yanıp sönen bir renk/sayı dizisini izleyin, ardından
  aynı diziyi aynı sırayla butonlara tıklayarak tekrar oluşturun — dizi her doğru
  tekrarlamada bir eleman uzuyor.
- **description EN:** Watch a briefly flashing sequence of colors/numbers, then reproduce
  the same sequence in order by pressing buttons — the sequence grows by one element after
  each correct repetition.
- **disciplines:** occupationalTherapy, speechTherapy, psychology
- **requiredCapabilities:** (yok)
- **difficultyRange:** 1-3 (dizi uzunluğu adaptif olarak artıyor/azalıyor)
- **Oyun Mekaniği:** Klasik "Simon" tarzı artan-dizi mekaniği, ama klinik çerçevede "digit
  span" (çalışma belleği kapasitesi) ölçümünün gamified hali. Skor: ulaşılan maksimum dizi
  uzunluğu (klasik digit-span skorlamasına benzer, `NormalizedScore`'a ölçeklenmiş hali).
- **Dayandığı egzersiz(ler):** yok (yeni bilişsel görev ailesi)

### kategori-avcisi — Category Hunter
- **displayName:** TR "Kategori Avcısı" / EN "Category Hunter"
- **description TR:** Ekranda beliren nesnelerden sadece o an geçerli olan kategoriye (ör.
  "sadece meyveler") ait olanlara tıklayın — kategori oyun boyunca birkaç kez değişiyor,
  değiştiğinde eski kurala göre tıklamamaya dikkat edin.
- **description EN:** Tap only the objects belonging to the currently active category
  (e.g., "fruits only") as they appear — the category switches a few times during play, so
  watch out for accidentally following the old rule.
- **disciplines:** occupationalTherapy, specialEducation, speechTherapy
- **requiredCapabilities:** (yok)
- **difficultyRange:** 2-3
- **Oyun Mekaniği:** Basitleştirilmiş bir "set-shifting" (bilişsel esneklik) görevi — kural
  değiştiğinde eski kurala göre doğru olup yeni kurala göre yanlış olan bir tıklama ayrıca
  "perseverasyon hatası" olarak sayılır (klinik olarak anlamlı bir ayrı metrik). Skor:
  genel doğruluk + perseverasyon hata oranı (düşük = iyi).
- **Dayandığı egzersiz(ler):** `renk-sekle-gore-siralama`'nın doğal devamı (kural değişimi eklenmiş hali)

### labirent-planlayici — Maze Planner
- **displayName:** TR "Labirent Planlayıcı" / EN "Maze Planner"
- **description TR:** Labirentte hareket etmeden ÖNCE tüm yolu zihninizde planlayıp sırayla
  yön okları (yukarı/aşağı/sağ/sol) seçerek "programlayın", ardından karakter bu planı
  baştan sona uygular — yanlış planlarsanız karakter duvara çarpar.
- **description EN:** Before moving through the maze, plan the entire path in your head and
  "program" it by selecting direction arrows (up/down/left/right) in order; the character
  then executes the whole plan — a wrong plan makes the character hit a wall.
- **disciplines:** occupationalTherapy, specialEducation
- **requiredCapabilities:** (yok)
- **difficultyRange:** 2-3
- **Oyun Mekaniği:** `sekil-takibi`'nin (anlık iz sürme) aksine, hareket ÖNCEDEN planlanıp
  toplu uygulanıyor — bu, ileriye dönük planlama (forward planning) ve çalışma belleğini,
  sadece motor/görsel-motor beceriyi değil, gerçekten test eden bir yürütücü işlev görevi.
  Skor: ilk denemede tamamlanan labirent oranı + kullanılan toplam hamle/optimal hamle oranı.
- **Dayandığı egzersiz(ler):** `labirent-nokta-birlestirme`'nin planlama-ağırlıklı versiyonu

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
- Hiçbiri henüz gerçek modül olarak yazılmadı — bu sadece bir tasarım turu (`color-sort`
  hariç, bkz. F8.33). Kullanıcı onaylarsa, `module-development` skill'indeki adımlarla (tek
  adım/onay kuralına uyarak) gerçek modüllere çevrilebilir.
- **Bilişsel egzersiz oyunları klinik ölçüm ADI taşımamalı.** "Renk-Kelime Uyumu"/"Sayı
  Dizisi Hatırlama"/"Kategori Avcısı" gibi oyunların altında yatan görev aileleri (Stroop,
  digit span, set-shifting) gerçek, isimli nöropsikolojik testlerin temel paradigmalarından
  esinleniyor — ama bunlar **isimli ölçeklerin kendisi değil**, sadece genel/telifsiz görev
  mantığının gamified bir uyarlaması (bkz. `clinical-data-handling` skill § 4, isimli ölçek
  telif riski). Gerçek modüle çevrilirken `displayName`/`description` bilinçli olarak jenerik
  tutulmalı (ör. "Stroop Testi" değil "Renk-Kelime Uyumu"), ve `clinical-data-handling` § 5
  gereği hiçbir sonuç ekranı bunu bir "bilişsel bozukluk taraması" veya tanısal ölçüm gibi
  sunmamalı — sadece bir alıştırma/oyun skoru olarak çerçevelenmeli.

## Kaynaklar (ilham için, mekanikler özgün tasarlandı)

- [Gamification in Musculoskeletal Rehabilitation — PMC/NIH](https://pmc.ncbi.nlm.nih.gov/articles/PMC9789284/)
- [Gaming In Physical Therapy & Rehabilitation — Physio Ed.](https://physioed.com/health-advice/treatment/gaming-in-physical-therapy-and-rehabilitation/)
- [Exergames: leveraging the fun of games to support therapy](https://www.medica-tradefair.com/en/media-news/spheres-of-medica-magazine/physio-tech/exergames-leveraging-fun-games-support-therapy)
- [Gamifying Rehabilitation: Motion-Controlled Video Games in Physical Therapy — PLAYWORK](https://www.playwork.me/post/gamifying-rehabilitation-motion-controlled-video-games-in-physical-therapy)
- [10 Creative Naming Therapy Activities for Aphasia — Tactus Therapy](https://tactustherapy.com/aphasia-activities-naming-therapy/)
- [Cognitive Rehabilitation Exercises: Effective Strategies for Brain Recovery — Neurolaunch](https://neurolaunch.com/cognitive-rehabilitation-exercises/)
- [21 Effective Exercises For Cognitive Rehabilitation — The Adult Speech Therapy Workbook](https://theadultspeechtherapyworkbook.com/exercises-for-cognitive-rehabilitation/)
- [18 Fun and Engaging Games to Improve Memory and Cognition After Stroke — Flint Rehab](https://www.flintrehab.com/games-to-improve-memory-after-stroke/)
- [A Systematic Review on Serious Games in Attention Rehabilitation and Their Effects — PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC8898139/)
