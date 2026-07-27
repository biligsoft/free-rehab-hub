# Egzersiz Kütüphanesi — Taslak

Bu dosya, `content-packs/exercise-library/` altındaki hazır egzersiz kütüphanesine eklenmek üzere
tasarlanmış bir **taslak**tır — henüz JSON'a çevrilip repoya commit edilmedi, önce gözden
geçirilmesi için buraya yazıldı. Mevcut 3 örnek (`ankle-pumps`, `shoulder-flexion-supine`,
`fine-motor-pegboard`) sadece fizyoterapi (2) ve ergoterapi (1) kapsıyordu; konuşma terapisi,
psikoloji ve özel eğitim için hiç kart yoktu. Aşağıdaki liste tüm 5 disiplini kapsayacak şekilde
genel/telifsiz (isimli bir ölçek veya markaya ait olmayan), yaygın bilinen egzersiz türlerinden
oluşuyor — `clinical-data-handling` skill'inin § 4'ündeki telif kuralına uygun: hiçbir metin
belirli bir kaynaktan birebir kopyalanmadı, her açıklama bu proje için özgün yazıldı.

Her madde `content-packs/exercise-library/*.json` şemasıyla birebir aynı alanları taşıyor
(`id`/`displayName`/`instructions`/`disciplines`/`difficultyLevel`/`suggestedRepetitions`/
`suggestedSets`/`tags`) — onaylanınca doğrudan JSON'a çevrilebilir.

**Not (Psikoloji kartları için):** `suggestedRepetitions`/`suggestedSets` alanları PT/OT
egzersizleri için tasarlanmış (tekrar/set) — nefes/gevşeme/farkındalık egzersizlerinde bunları
"tekrar = döngü/nefes sayısı" olarak yorumladım (ör. "10 tekrar" = 10 nefes döngüsü). Bu alanların
psikoloji/konuşma/özel eğitim kartlarında ne kadar anlamlı olduğu kullanıcıyla ayrıca konuşulabilir.

---

## Fizyoterapi (Physiotherapy)

### diz-ekstansiyonu-oturarak — Seated Knee Extension
- **TR:** Diz Ekstansiyonu (Oturarak)
- **EN:** Seated Knee Extension
- **Talimat TR:** Sandalyede dik oturun, bir bacağınızı dizden yavaşça düz olana kadar
  kaldırın, 2-3 saniye tutun, ardından kontrollü şekilde indirin.
- **Talimat EN:** Sit upright in a chair, slowly straighten one leg at the knee until fully
  extended, hold for 2-3 seconds, then lower it back down with control.
- **Zorluk:** 1 | **Tekrar:** 12 | **Set:** 3
- **Etiketler:** knee, strengthening, lower-extremity

### kalca-abduksiyonu-yan-yatarak — Side-Lying Hip Abduction
- **TR:** Kalça Abdüksiyonu (Yan Yatarak)
- **EN:** Side-Lying Hip Abduction
- **Talimat TR:** Yan yatın, alttaki bacağınızı hafifçe bükün, üstteki bacağınızı düz tutarak
  yavaşça yukarı kaldırın, ardından kontrollü şekilde indirin.
- **Talimat EN:** Lie on your side, slightly bend the bottom leg, keep the top leg straight
  and slowly raise it upward, then lower it back down with control.
- **Zorluk:** 2 | **Tekrar:** 12 | **Set:** 2
- **Etiketler:** hip, strengthening, lower-extremity

### boyun-rotasyonu — Neck Rotation
- **TR:** Boyun Rotasyonu
- **EN:** Neck Rotation
- **Talimat TR:** Dik oturun, başınızı yavaşça bir omzunuza doğru çevirin, kısa bir süre
  tutun, ardından yavaşça karşı yöne çevirin.
- **Talimat EN:** Sit upright, slowly turn your head toward one shoulder, hold briefly, then
  slowly turn toward the opposite side.
- **Zorluk:** 1 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** neck, range-of-motion

### duvarda-kayma — Wall Slides
- **TR:** Duvarda Kayma
- **EN:** Wall Slides
- **Talimat TR:** Sırtınızı duvara yaslayın, kollarınızı duvara değecek şekilde yukarı doğru
  kaydırın, ardından yavaşça başlangıç pozisyonuna indirin.
- **Talimat EN:** Lean your back against a wall, slide your arms upward while keeping them
  in contact with the wall, then slowly lower them back to the starting position.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 3
- **Etiketler:** shoulder, range-of-motion, upper-extremity

### kopru-egzersizi — Bridging
- **TR:** Köprü Egzersizi
- **EN:** Bridging
- **Talimat TR:** Sırtüstü yatın, dizlerinizi bükün, ayaklarınız yere basarken kalçanızı
  yavaşça yukarı kaldırın, kısa bir süre tutun, ardından kontrollü şekilde indirin.
- **Talimat EN:** Lie on your back with knees bent and feet flat on the floor, slowly lift
  your hips upward, hold briefly, then lower back down with control.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 3
- **Etiketler:** hip, core, strengthening

### el-bilegi-fleksiyon-ekstansiyon — Wrist Flexion/Extension
- **TR:** El Bileği Fleksiyon/Ekstansiyonu
- **EN:** Wrist Flexion/Extension
- **Talimat TR:** Kolunuzu masaya dayayıp el bileğinizi masadan sarkıtın, bileğinizi yavaşça
  yukarı ve aşağı hareket ettirin.
- **Talimat EN:** Rest your forearm on a table with your wrist hanging off the edge, slowly
  move your wrist up and down.
- **Zorluk:** 1 | **Tekrar:** 15 | **Set:** 2
- **Etiketler:** wrist, range-of-motion, upper-extremity

### tek-ayak-uzerinde-denge — Single-Leg Balance
- **TR:** Tek Ayak Üzerinde Denge
- **EN:** Single-Leg Balance
- **Talimat TR:** Sağlam bir yüzeye (sandalye, tezgah) yakın durun, bir ayağınız üzerinde
  dengenizi korumaya çalışın, gerekirse desteğe hafifçe tutunun.
- **Talimat EN:** Stand near a stable surface (chair, counter), try to balance on one leg,
  lightly holding on for support if needed.
- **Zorluk:** 2 | **Tekrar:** 5 (her ayak) | **Set:** 2
- **Etiketler:** balance, lower-extremity, fall-prevention

### topuk-parmak-yuruyusu — Heel-to-Toe Walk
- **TR:** Topuk-Parmak Yürüyüşü
- **EN:** Heel-to-Toe Walk (Tandem Gait)
- **Talimat TR:** Bir ayağınızın topuğunu diğer ayağınızın parmak uçlarına değecek şekilde
  düz bir çizgi üzerinde ilerleyerek yürüyün, gerekirse bir desteğe yakın durun.
- **Talimat EN:** Walk along a straight line placing the heel of one foot directly in front
  of the toes of the other, staying near a support surface if needed.
- **Zorluk:** 2 | **Tekrar:** 10 adım | **Set:** 2
- **Etiketler:** balance, gait, fall-prevention

### otur-kalk-egzersizi — Sit-to-Stand
- **TR:** Otur-Kalk Egzersizi
- **EN:** Sit-to-Stand
- **Talimat TR:** Sandalyede oturur pozisyondan, mümkünse kollarınızı kullanmadan, kontrollü
  şekilde ayağa kalkın, ardından yavaşça tekrar oturun.
- **Talimat EN:** From a seated position in a chair, stand up with control (avoiding using
  your arms if possible), then slowly sit back down.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 3
- **Etiketler:** lower-extremity, functional, strengthening

### govde-rotasyonu-oturarak — Seated Trunk Rotation
- **TR:** Gövde Rotasyonu (Oturarak)
- **EN:** Seated Trunk Rotation
- **Talimat TR:** Sandalyede dik oturun, kollarınızı göğsünüzde çaprazlayın, gövdenizi yavaşça
  bir yöne çevirin, başlangıca dönün, ardından karşı yöne çevirin.
- **Talimat EN:** Sit upright in a chair, cross your arms over your chest, slowly rotate your
  trunk to one side, return to center, then rotate to the other side.
- **Zorluk:** 1 | **Tekrar:** 10 (her yön) | **Set:** 2
- **Etiketler:** trunk, core, range-of-motion

### kalca-ekstansiyonu-ayakta — Standing Hip Extension
- **TR:** Kalça Ekstansiyonu (Ayakta)
- **EN:** Standing Hip Extension
- **Talimat TR:** Bir sandalye veya tezgaha hafifçe tutunarak dik durun, bir bacağınızı düz
  tutarak yavaşça arkaya doğru kaldırın, ardından kontrollü şekilde indirin.
- **Talimat EN:** Stand upright while lightly holding onto a chair or counter, keep one leg
  straight and slowly lift it backward, then lower it back down with control.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** hip, strengthening, lower-extremity

### skapular-retraksiyon — Scapular Retraction
- **TR:** Skapular Retraksiyon (Kürek Kemiği Sıkma)
- **EN:** Scapular Retraction
- **Talimat TR:** Dik oturun veya durun, omuzlarınızı gevşek bırakıp kürek kemiklerinizi
  birbirine yaklaştıracak şekilde geriye ve aşağıya doğru sıkın, birkaç saniye tutun.
- **Talimat EN:** Sit or stand upright, keep shoulders relaxed, squeeze your shoulder blades
  back and down toward each other, hold for a few seconds.
- **Zorluk:** 1 | **Tekrar:** 12 | **Set:** 3
- **Etiketler:** shoulder, posture, upper-extremity

### servikal-germe — Cervical Stretch
- **TR:** Servikal (Boyun) Germe
- **EN:** Cervical Stretch
- **Talimat TR:** Dik oturun, başınızı sırtınızı düz tutarak yavaşça bir omzunuza doğru
  eğin, hafif bir gerilme hissedene kadar 20-30 saniye bu pozisyonda kalın.
- **Talimat EN:** Sit upright, keep your back straight, gently tilt your head toward one
  shoulder, hold for 20-30 seconds until you feel a light stretch.
- **Zorluk:** 1 | **Tekrar:** 3 (her yön) | **Set:** 1
- **Etiketler:** neck, stretching, range-of-motion

### pelvik-tilt — Pelvic Tilt
- **TR:** Pelvik Tilt
- **EN:** Pelvic Tilt
- **Talimat TR:** Sırtüstü yatın, dizlerinizi bükün, karın kaslarınızı sıkarak belinizi yere
  doğru bastırın, birkaç saniye tutun, ardından gevşetin.
- **Talimat EN:** Lie on your back with knees bent, tighten your abdominal muscles to press
  your lower back flat against the floor, hold for a few seconds, then relax.
- **Zorluk:** 1 | **Tekrar:** 12 | **Set:** 2
- **Etiketler:** core, lower-back, stability

### kuadriseps-izometrik — Quad Sets
- **TR:** Kuadriseps İzometrik Egzersizi (Quad Sets)
- **EN:** Quad Sets
- **Talimat TR:** Bacağınızı düz uzatarak oturun veya yatın, dizinizin arkasını yere/yatağa
  doğru bastırarak uyluk kasınızı sıkın, birkaç saniye tutun, ardından gevşetin.
- **Talimat EN:** Sit or lie with your leg straight, press the back of your knee down toward
  the floor/bed while tightening your thigh muscle, hold for a few seconds, then relax.
- **Zorluk:** 1 | **Tekrar:** 12 | **Set:** 3
- **Etiketler:** knee, strengthening, isometric

### baldir-kaldirma — Calf Raises
- **TR:** Baldır Kaldırma
- **EN:** Calf Raises
- **Talimat TR:** Ayakta durun (gerekirse bir yüzeye hafifçe tutunun), topuklarınızı
  yerden kaldırarak parmak uçlarınızda yükselin, ardından yavaşça indirin.
- **Talimat EN:** Stand up (lightly holding onto a surface if needed), rise onto your toes
  by lifting your heels off the floor, then lower back down slowly.
- **Zorluk:** 1 | **Tekrar:** 15 | **Set:** 2
- **Etiketler:** ankle, strengthening, lower-extremity

---

## Ergoterapi (Occupational Therapy)

### oyun-hamuru-sikma — Therapy Putty Squeeze
- **TR:** Oyun Hamuru Sıkma
- **EN:** Therapy Putty Squeeze
- **Talimat TR:** Elinizdeki terapi hamurunu avucunuzda sıkıca sıkın, birkaç saniye tutun,
  ardından yavaşça bırakın.
- **Talimat EN:** Squeeze the therapy putty firmly in your palm, hold for a few seconds,
  then slowly release.
- **Zorluk:** 1 | **Tekrar:** 15 | **Set:** 2
- **Etiketler:** hand, grip-strength, fine-motor

### boncuk-dizme — Bead Stringing
- **TR:** Boncuk Dizme
- **EN:** Bead Stringing
- **Talimat TR:** Küçük boncukları baş parmak ve işaret parmağınızla tutup bir ipe tek tek
  dizin, hareketi yavaş ve kontrollü yapmaya özen gösterin.
- **Talimat EN:** Pick up small beads using your thumb and index finger and string them onto
  a cord one at a time, moving slowly and with control.
- **Zorluk:** 1 | **Tekrar:** 20 | **Set:** 1
- **Etiketler:** fine-motor, hand, eye-hand-coordination

### dugme-iliklemek — Buttoning Practice
- **TR:** Düğme İlikleme Pratiği
- **EN:** Buttoning Practice
- **Talimat TR:** Bir düğme tahtası veya eski bir gömlek üzerinde düğmeleri tek tek iliklemeye
  ve açmaya çalışın.
- **Talimat EN:** Practice fastening and unfastening buttons one at a time on a button board
  or an old shirt.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** fine-motor, dressing, adl

### cimbizla-nesne-tasima — Tweezer Transfer
- **TR:** Cımbızla Küçük Nesne Taşıma
- **EN:** Tweezer Transfer
- **Talimat TR:** Cımbız kullanarak küçük nesneleri (pom-pom, boncuk vb.) bir kaptan diğerine
  tek tek taşıyın.
- **Talimat EN:** Using tweezers, transfer small objects (pom-poms, beads, etc.) one at a
  time from one container to another.
- **Zorluk:** 2 | **Tekrar:** 15 | **Set:** 1
- **Etiketler:** fine-motor, pincer-grasp, hand

### bardaktan-bardaga-aktarma — Pouring Practice
- **TR:** Bardaktan Bardağa Aktarma
- **EN:** Pouring Practice
- **Talimat TR:** Bir bardaktaki su veya taneli malzemeyi (pirinç, nohut vb.) dökmeden diğer
  bardağa aktarmaya çalışın.
- **Talimat EN:** Try to pour water or granular material (rice, chickpeas, etc.) from one
  cup into another without spilling.
- **Zorluk:** 1 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** adl, hand-coordination, bilateral-coordination

### top-yakalama-atma — Ball Catch and Throw
- **TR:** Top Yakalama/Atma
- **EN:** Ball Catch and Throw
- **Talimat TR:** Bir terapist veya yardımcıyla karşılıklı olarak yumuşak bir topu yakalayıp
  geri atın, iki elinizi birlikte kullanmaya özen gösterin.
- **Talimat EN:** Catch and throw a soft ball back and forth with a therapist or helper,
  focusing on using both hands together.
- **Zorluk:** 1 | **Tekrar:** 15 | **Set:** 2
- **Etiketler:** bilateral-coordination, hand-eye-coordination, gross-motor

### makasla-cizgi-kesme — Cutting Along a Line
- **TR:** Makasla Çizgi Kesme
- **EN:** Cutting Along a Line
- **Talimat TR:** Kağıt üzerine çizilmiş düz veya kıvrımlı bir çizgiyi makasla takip ederek
  kesin.
- **Talimat EN:** Using scissors, cut along a straight or curved line drawn on paper.
- **Zorluk:** 2 | **Tekrar:** 5 (çizgi) | **Set:** 1
- **Etiketler:** fine-motor, bilateral-coordination, hand

### sekil-harf-izleme — Shape/Letter Tracing
- **TR:** Şekil/Harf İzleme
- **EN:** Shape/Letter Tracing
- **Talimat TR:** Kağıt üzerindeki noktalı bir şekli veya harfi kalemle takip ederek çizin.
- **Talimat EN:** Trace over a dotted shape or letter on paper using a pencil.
- **Zorluk:** 1 | **Tekrar:** 5 (şekil) | **Set:** 2
- **Etiketler:** fine-motor, visual-motor, handwriting

### iki-el-kutu-acma — Bimanual Container Opening
- **TR:** İki El Koordinasyonu — Kutu Açma
- **EN:** Bimanual Container Opening
- **Talimat TR:** Bir elinizle kavanoz veya kutuyu sabit tutarken diğer eliniz kapağı açıp
  kapatın, ardından elleri değiştirerek tekrarlayın.
- **Talimat EN:** Hold a jar or container steady with one hand while the other hand opens
  and closes the lid, then repeat with hands switched.
- **Zorluk:** 2 | **Tekrar:** 8 | **Set:** 2
- **Etiketler:** bilateral-coordination, adl, hand

### fermuar-cekme-pratigi — Zipper Pull Practice
- **TR:** Fermuar Çekme Pratiği
- **EN:** Zipper Pull Practice
- **Talimat TR:** Bir giyim eşyası veya alıştırma tahtasındaki fermuarı bir elinizle sabit
  tutup diğer elinizle yavaşça açıp kapatın.
- **Talimat EN:** Hold the fabric steady with one hand while slowly pulling a zipper (on a
  garment or practice board) up and down with the other hand.
- **Zorluk:** 2 | **Tekrar:** 8 | **Set:** 2
- **Etiketler:** fine-motor, dressing, adl

### ayakkabi-baglama-pratigi — Shoe Tying Practice
- **TR:** Ayakkabı Bağlama Pratiği
- **EN:** Shoe Tying Practice
- **Talimat TR:** Bir ayakkabı bağlama tahtası veya gerçek ayakkabı üzerinde bağcıkları
  adım adım (çapraz geçirme, düğüm atma) bağlamayı deneyin.
- **Talimat EN:** Practice tying shoelaces step by step (crossing, looping, knotting) using
  a lacing board or a real shoe.
- **Zorluk:** 3 | **Tekrar:** 3 (deneme) | **Set:** 1
- **Etiketler:** fine-motor, dressing, sequencing

### mandal-sikma — Clothespin Pinch
- **TR:** Mandal Sıkma
- **EN:** Clothespin Pinch
- **Talimat TR:** Baş ve işaret parmağınızla bir çamaşır mandalını sıkıp bir ipe veya kart
  kenarına tek tek takın.
- **Talimat EN:** Using your thumb and index finger, squeeze open a clothespin and clip it
  onto a line or the edge of a card, one at a time.
- **Zorluk:** 1 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** fine-motor, pincer-grasp, grip-strength

### kendi-kendine-yemek-yeme-pratigi — Self-Feeding Practice
- **TR:** Kendi Kendine Yemek Yeme Pratiği
- **EN:** Self-Feeding Practice
- **Talimat TR:** Bir kaşık veya çatal kullanarak tabaktaki lokmaları alıp ağzınıza
  götürmeyi, dökmeden ve kontrollü şekilde tekrarlayın.
- **Talimat EN:** Using a spoon or fork, practice picking up bites from a plate and bringing
  them to your mouth in a controlled manner without spilling.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 1
- **Etiketler:** adl, self-feeding, hand-eye-coordination

---

## Konuşma Terapisi (Speech Therapy)

### dudak-gulumseme-tutma — Lip Smile Hold
- **TR:** Dudak Gülümseme Tutma
- **EN:** Lip Smile Hold
- **Talimat TR:** Geniş bir gülümseme yapıp dudak kaslarınızı birkaç saniye gergin tutun,
  ardından yavaşça gevşetin.
- **Talimat EN:** Make a wide smile and hold your lip muscles tense for a few seconds, then
  slowly relax them.
- **Zorluk:** 1 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** oral-motor, lips, articulation

### pipetle-uefleme — Straw Blowing
- **TR:** Pipetle Üfleme
- **EN:** Straw Blowing
- **Talimat TR:** Bir pipet yardımıyla bardaktaki suya üfleyerek köpük oluşturun veya masadaki
  hafif bir nesneyi (pamuk topu vb.) üfleyerek hareket ettirin.
- **Talimat EN:** Use a straw to blow bubbles into a cup of water, or blow a light object
  (like a cotton ball) across the table.
- **Zorluk:** 1 | **Tekrar:** 8 | **Set:** 2
- **Etiketler:** oral-motor, breath-control, lips

### dil-sinavi — Tongue Push-Ups
- **TR:** Dil Şınavı
- **EN:** Tongue Push-Ups
- **Talimat TR:** Dilinizi bir tahta spatulaya veya temiz bir kaşığa doğru itip birkaç saniye
  kuvvet uygulayın, ardından gevşetin.
- **Talimat EN:** Push your tongue against a tongue depressor or clean spoon and apply
  pressure for a few seconds, then relax.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** oral-motor, tongue, articulation

### ayna-karsisinda-agiz-hareketleri — Mirror Oral-Motor Imitation
- **TR:** Ayna Karşısında Ağız Hareketleri
- **EN:** Mirror Oral-Motor Imitation
- **Talimat TR:** Ayna karşısında dudak/dil/çene hareketlerini (gülümseme, dudak yuvarlama,
  dili sağa-sola hareket ettirme vb.) taklit ederek tekrarlayın.
- **Talimat EN:** In front of a mirror, imitate and repeat lip/tongue/jaw movements (smiling,
  lip rounding, moving the tongue side to side, etc.).
- **Zorluk:** 1 | **Tekrar:** 8 (hareket başına) | **Set:** 1
- **Etiketler:** oral-motor, imitation, articulation

### tekerleme-tekrar-pratigi — Tongue-Twister Repetition
- **TR:** Tekerleme/Ses Tekrarı Pratiği
- **EN:** Tongue-Twister/Sound Repetition Practice
- **Talimat TR:** Terapistin belirlediği bir sesi veya kısa tekerlemeyi yavaş ve net bir
  şekilde art arda tekrarlayın.
- **Talimat EN:** Slowly and clearly repeat a target sound or short tongue-twister
  determined by the therapist, several times in a row.
- **Zorluk:** 2 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** articulation, phonology, repetition

### nesne-adlandirma — Confrontation Naming
- **TR:** Nesne Adlandırma
- **EN:** Confrontation Naming
- **Talimat TR:** Gösterilen bir nesne veya resmin adını olabildiğince hızlı ve net söylemeye
  çalışın; sözcük gelmezse tanımlayarak (ör. "içinden su içilen şey") yaklaşmayı deneyin.
- **Talimat EN:** Try to name a shown object or picture as quickly and clearly as possible;
  if the word doesn't come, try approximating it by describing it (e.g., "the thing you
  drink water from").
- **Zorluk:** 2 | **Tekrar:** 15 (nesne) | **Set:** 1
- **Etiketler:** word-finding, naming, aphasia

### resim-tanimlama — Picture Description
- **TR:** Resim Tanımlama
- **EN:** Picture Description
- **Talimat TR:** Gösterilen bir sahne resmini mümkün olduğunca ayrıntılı cümlelerle
  anlatmaya çalışın (kim, ne yapıyor, nerede).
- **Talimat EN:** Try to describe a shown scene picture in as much detail as possible (who,
  what they are doing, where).
- **Zorluk:** 2 | **Tekrar:** 3 (resim) | **Set:** 1
- **Etiketler:** language, description, aphasia

### kategori-kelime-bulma — Category Word-Finding
- **TR:** Kategori Sıralama / Kelime Bulma
- **EN:** Category Word-Finding
- **Talimat TR:** Verilen bir kategoriye (ör. "meyveler", "hayvanlar") ait olabildiğince çok
  kelime sayın.
- **Talimat EN:** Name as many words as you can that belong to a given category (e.g.,
  "fruits", "animals").
- **Zorluk:** 2 | **Tekrar:** 1 (kategori) | **Set:** 3
- **Etiketler:** word-finding, category-fluency, cognition

### sesli-okuma-pratigi — Reading Aloud Practice
- **TR:** Sesli Okuma Pratiği
- **EN:** Reading Aloud Practice
- **Talimat TR:** Kısa bir paragrafı yüksek sesle, net telaffuzla ve uygun hızda okumaya
  çalışın.
- **Talimat EN:** Try to read a short paragraph aloud with clear pronunciation and an
  appropriate pace.
- **Zorluk:** 1 | **Tekrar:** 1 (paragraf) | **Set:** 2
- **Etiketler:** fluency, articulation, reading

### minimal-cift-ayirt-etme — Minimal Pairs Discrimination
- **TR:** Minimal Çift Ayırt Etme
- **EN:** Minimal Pairs Discrimination
- **Talimat TR:** Sadece tek bir sesle farklılaşan iki kelimeden ("kar" / "kâr" gibi)
  birini duyup doğru resmi veya kartı işaret edin.
- **Talimat EN:** After hearing one of two words that differ by a single sound (e.g., "cat"
  / "cap"), point to the correct picture or card.
- **Zorluk:** 2 | **Tekrar:** 12 (çift) | **Set:** 1
- **Etiketler:** phonology, auditory-discrimination, articulation

### yonergeleri-takip-etme — Following Directions
- **TR:** Yönergeleri Takip Etme
- **EN:** Following Directions
- **Talimat TR:** Terapistin verdiği, giderek uzayan sözel yönergeleri (ör. "kırmızı kartı
  al ve mavi kutuya koy") dinleyip sırasıyla uygulayın.
- **Talimat EN:** Listen to increasingly complex verbal instructions given by the therapist
  (e.g., "pick up the red card and put it in the blue box") and carry them out in order.
- **Zorluk:** 2 | **Tekrar:** 8 (yönerge) | **Set:** 1
- **Etiketler:** auditory-comprehension, following-directions, cognition

### evet-hayir-sorulari — Yes/No Questions
- **TR:** Evet/Hayır Soruları
- **EN:** Yes/No Questions
- **Talimat TR:** Sorulan basit evet/hayır sorularına (ör. "Bu bir hayvan mı?") baş sallama,
  konuşma veya bir sembol/kartla yanıt verin.
- **Talimat EN:** Respond to simple yes/no questions (e.g., "Is this an animal?") using a
  head nod, speech, or a symbol/card.
- **Zorluk:** 1 | **Tekrar:** 10 (soru) | **Set:** 1
- **Etiketler:** comprehension, communication, aphasia

### cumle-tamamlama — Sentence Completion
- **TR:** Cümle Tamamlama
- **EN:** Sentence Completion
- **Talimat TR:** Yarım bırakılan bir cümleyi (ör. "Sabah kalkınca dişlerimi ...") uygun
  kelimeyle tamamlayın.
- **Talimat EN:** Complete a sentence that is left unfinished (e.g., "In the morning I brush
  my ...") with an appropriate word.
- **Zorluk:** 1 | **Tekrar:** 10 (cümle) | **Set:** 1
- **Etiketler:** language, word-finding, syntax

### melodik-tonlama — Melodic Intonation Practice
- **TR:** Melodik Tonlama Pratiği
- **EN:** Melodic Intonation Practice
- **Talimat TR:** Kısa bir cümleyi konuşmak yerine basit bir ezgiyle "söyleyerek" ifade
  etmeyi deneyin — melodinin ritmi kelimelerin akıcılığına yardımcı olur.
- **Talimat EN:** Instead of speaking a short phrase, try "singing" it with a simple melody
  — the rhythm of the tune helps support fluent word production.
- **Zorluk:** 3 | **Tekrar:** 5 (cümle) | **Set:** 1
- **Etiketler:** fluency, apraxia, melodic-intonation-therapy

---

## Psikoloji (Psychology)

### diyafram-nefesi — Diaphragmatic Breathing
- **TR:** Diyaframatik Nefes Egzersizi
- **EN:** Diaphragmatic Breathing
- **Talimat TR:** Rahat bir pozisyonda oturun veya uzanın, bir elinizi göğsünüze diğerini
  karnınıza koyun, burnunuzdan derin bir nefes alırken karnınızın şiştiğini hissedin, ağzınızdan
  yavaşça verin.
- **Talimat EN:** Sit or lie in a comfortable position, place one hand on your chest and the
  other on your belly, breathe in deeply through your nose feeling your belly rise, then
  exhale slowly through your mouth.
- **Zorluk:** 1 | **Tekrar:** 10 (nefes döngüsü) | **Set:** 2
- **Etiketler:** breathing, relaxation, anxiety

### kutu-nefesi — Box Breathing
- **TR:** Kutu Nefesi
- **EN:** Box Breathing
- **Talimat TR:** 4 saniye nefes alın, 4 saniye tutun, 4 saniye verin, 4 saniye bekleyin —
  bu döngüyü tekrarlayın.
- **Talimat EN:** Inhale for 4 seconds, hold for 4 seconds, exhale for 4 seconds, pause for
  4 seconds — repeat this cycle.
- **Zorluk:** 1 | **Tekrar:** 8 (döngü) | **Set:** 1
- **Etiketler:** breathing, relaxation, stress-management

### progresif-kas-gevsetme — Progressive Muscle Relaxation
- **TR:** Progresif Kas Gevşetme
- **EN:** Progressive Muscle Relaxation
- **Talimat TR:** Sırasıyla her bir kas grubunu (eller, kollar, omuzlar, bacaklar vb.) birkaç
  saniye gerin, ardından yavaşça gevşetip aradaki farkı hissetmeye çalışın.
- **Talimat EN:** In sequence, tense each muscle group (hands, arms, shoulders, legs, etc.)
  for a few seconds, then slowly release and notice the difference.
- **Zorluk:** 2 | **Tekrar:** 8 (kas grubu) | **Set:** 1
- **Etiketler:** relaxation, body-awareness, stress-management

### bes-duyu-farkindaligi — Five Senses Grounding
- **TR:** 5-4-3-2-1 Duyusal Farkındalık
- **EN:** Five Senses Grounding
- **Talimat TR:** Sırasıyla görebildiğiniz 5, dokunabildiğiniz 4, duyabildiğiniz 3,
  koklayabildiğiniz 2 ve tadabildiğiniz 1 şeyi sessizce (veya yüksek sesle) sayın.
- **Talimat EN:** In order, silently (or out loud) name 5 things you can see, 4 you can
  touch, 3 you can hear, 2 you can smell, and 1 you can taste.
- **Zorluk:** 1 | **Tekrar:** 1 (tam döngü) | **Set:** 1
- **Etiketler:** grounding, anxiety, mindfulness

### dokunsal-nesne-odaklanma — Tactile Object Focus
- **TR:** Dokunsal Nesneyle Şimdiki Ana Odaklanma
- **EN:** Tactile Object Focus
- **Talimat TR:** Elinize dokusu belirgin bir nesne (stres topu, kumaş parçası vb.) alın,
  dokusuna, ağırlığına ve sıcaklığına birkaç dakika tam dikkatle odaklanın.
- **Talimat EN:** Hold an object with a distinct texture (stress ball, fabric piece, etc.)
  and focus your full attention on its texture, weight, and temperature for a few minutes.
- **Zorluk:** 1 | **Tekrar:** 1 (oturum) | **Set:** 1
- **Etiketler:** grounding, mindfulness, sensory

### dusunce-kaydi — Thought Record
- **TR:** Düşünce Kaydı / Düşünceyi Sorgulama
- **EN:** Thought Record
- **Talimat TR:** Sizi rahatsız eden bir durumu, o an aklınızdan geçen düşünceyi ve
  hissettiğiniz duyguyu yazın; ardından bu düşünceyi destekleyen ve çürüten kanıtları
  listeleyip daha dengeli bir düşünce oluşturmaya çalışın.
- **Talimat EN:** Write down a distressing situation, the thought that went through your
  mind, and the emotion you felt; then list evidence for and against that thought and try
  to form a more balanced thought.
- **Zorluk:** 2 | **Tekrar:** 1 (kayıt) | **Set:** 1
- **Etiketler:** cognitive-restructuring, cbt, journaling

### davranissal-aktivasyon-plani — Behavioral Activation Planning
- **TR:** Davranışsal Aktivasyon Planı
- **EN:** Behavioral Activation Planning
- **Talimat TR:** Önümüzdeki hafta için keyif veya başarı hissi verebilecek küçük, somut bir
  aktivite seçin, ne zaman yapacağınızı planlayın ve sonrasında ne hissettiğinizi not edin.
- **Talimat EN:** Choose one small, concrete activity for the coming week that might bring
  enjoyment or a sense of accomplishment, plan when you'll do it, and note how you felt
  afterward.
- **Zorluk:** 2 | **Tekrar:** 1 (aktivite) | **Set:** 1
- **Etiketler:** behavioral-activation, cbt, mood

### sukran-gunlugu — Gratitude Journaling
- **TR:** Şükran Günlüğü
- **EN:** Gratitude Journaling
- **Talimat TR:** Bugün minnettar olduğunuz üç şeyi kısaca yazın ve neden önemli olduklarını
  bir cümleyle belirtin.
- **Talimat EN:** Briefly write down three things you are grateful for today and note in one
  sentence why each one matters.
- **Zorluk:** 1 | **Tekrar:** 3 (madde) | **Set:** 1
- **Etiketler:** journaling, positive-psychology, mood

### duygu-durumu-izleme — Mood Tracking
- **TR:** Duygu Durumu İzleme
- **EN:** Mood Tracking
- **Talimat TR:** Günün belirli saatlerinde ruh halinizi 0-10 arası bir ölçekte
  değerlendirip kısaca hangi olayın etkili olduğunu not edin.
- **Talimat EN:** At set times during the day, rate your mood on a 0-10 scale and briefly
  note which event may have influenced it.
- **Zorluk:** 1 | **Tekrar:** 3 (gün içi) | **Set:** 1
- **Etiketler:** mood, self-monitoring, journaling

### deger-netlestirme — Values Clarification
- **TR:** Değer Netleştirme
- **EN:** Values Clarification
- **Talimat TR:** Sizin için gerçekten önemli olan 3-5 yaşam değerini (ör. aile, sağlık,
  yaratıcılık) yazın ve bugünkü davranışlarınızın bu değerlerle ne kadar uyumlu olduğunu
  değerlendirin.
- **Talimat EN:** Write down 3-5 life values that truly matter to you (e.g., family, health,
  creativity) and reflect on how well today's actions aligned with them.
- **Zorluk:** 2 | **Tekrar:** 1 (oturum) | **Set:** 1
- **Etiketler:** values, act, self-reflection

### endise-zamani-planlama — Worry Time Scheduling
- **TR:** Endişe Zamanı Planlama
- **EN:** Worry Time Scheduling
- **Talimat TR:** Gün içinde aklınıza gelen endişeleri hemen üzerinde durmadan not edin,
  belirlediğiniz sabit bir "endişe zamanı"na (ör. akşam 15 dakika) erteleyip o an üzerine
  düşünün.
- **Talimat EN:** Jot down worries as they arise during the day without dwelling on them,
  then postpone thinking about them until a fixed "worry time" (e.g., 15 minutes in the
  evening).
- **Zorluk:** 2 | **Tekrar:** 1 (gün) | **Set:** 1
- **Etiketler:** anxiety, worry-management, cbt

### sefkatli-beden-taramasi — Compassionate Body Scan
- **TR:** Şefkatli Beden Taraması
- **EN:** Compassionate Body Scan
- **Talimat TR:** Rahat bir pozisyonda uzanın, dikkatinizi yavaşça ayak parmaklarınızdan
  başınıza doğru her bölgeye yargılamadan, nazik bir farkındalıkla yönlendirin.
- **Talimat EN:** Lie in a comfortable position and slowly guide your attention from your
  toes up to your head, region by region, with gentle, non-judgmental awareness.
- **Zorluk:** 1 | **Tekrar:** 1 (oturum) | **Set:** 1
- **Etiketler:** mindfulness, self-compassion, relaxation

### kendine-sefkat-molasi — Self-Compassion Break
- **TR:** Kendine Şefkat Molası
- **EN:** Self-Compassion Break
- **Talimat TR:** Zor bir anda kendinize sessizce "bu zor bir an", "böyle hissetmek insani",
  "kendime nazik davranabilirim" cümlelerini söyleyin.
- **Talimat EN:** During a difficult moment, silently say to yourself: "this is a moment of
  difficulty", "feeling this way is human", "I can be kind to myself".
- **Zorluk:** 1 | **Tekrar:** 3 (cümle) | **Set:** 1
- **Etiketler:** self-compassion, mindfulness, emotion-regulation

### problem-cozme-calisma-sayfasi — Problem-Solving Worksheet
- **TR:** Problem Çözme Çalışma Sayfası
- **EN:** Problem-Solving Worksheet
- **Talimat TR:** Bir sorunu net bir şekilde tanımlayın, mümkün olduğunca çok çözüm
  seçeneği listeleyin, her birinin artı/eksisini değerlendirip bir tanesini seçip uygulama
  planı yapın.
- **Talimat EN:** Clearly define a problem, list as many possible solutions as you can,
  weigh the pros/cons of each, then choose one and make an action plan.
- **Zorluk:** 2 | **Tekrar:** 1 (problem) | **Set:** 1
- **Etiketler:** problem-solving, cbt, planning

---

## Özel Eğitim (Special Education)

### rege-sekle-gore-siralama — Sorting by Color/Shape
- **TR:** Renge/Şekle Göre Sıralama
- **EN:** Sorting by Color/Shape
- **Talimat TR:** Karışık nesneleri (bloklar, düğmeler vb.) renk veya şekillerine göre ayrı
  gruplara ayırın.
- **Talimat EN:** Sort a mixed set of objects (blocks, buttons, etc.) into separate groups
  by color or shape.
- **Zorluk:** 1 | **Tekrar:** 1 (tam sıralama) | **Set:** 2
- **Etiketler:** cognitive, sorting, categorization

### parmak-boyama — Finger Painting
- **TR:** Parmak Boyama
- **EN:** Finger Painting
- **Talimat TR:** Parmak boyalarını kullanarak kağıt üzerinde serbestçe veya belirli bir
  şekli takip ederek boyama yapın.
- **Talimat EN:** Use finger paints to create marks on paper, either freely or by following
  a specific shape.
- **Zorluk:** 1 | **Tekrar:** 1 (oturum) | **Set:** 1
- **Etiketler:** sensory, fine-motor, creative

### blok-istifleme — Block Stacking
- **TR:** Blok İstifleme
- **EN:** Block Stacking
- **Talimat TR:** Blokları devirmeden mümkün olduğunca yüksek (veya belirli bir modele göre)
  istifleyin.
- **Talimat EN:** Stack blocks as high as possible without toppling them (or according to a
  given pattern).
- **Zorluk:** 1 | **Tekrar:** 3 (deneme) | **Set:** 2
- **Etiketler:** fine-motor, cognitive, hand-eye-coordination

### koni-yerlestirme — Cone Placing
- **TR:** Koni Yerleştirme
- **EN:** Cone Placing
- **Talimat TR:** Küçük konileri her iki elinizi de kullanarak belirlenen noktalara tek tek
  yerleştirin.
- **Talimat EN:** Using both hands, place small cones one at a time onto designated target
  points.
- **Zorluk:** 1 | **Tekrar:** 10 | **Set:** 2
- **Etiketler:** bilateral-coordination, fine-motor, gross-motor

### dokusal-eslestirme — Texture Matching
- **TR:** Dokusal Eşleştirme
- **EN:** Texture Matching
- **Talimat TR:** Farklı dokulara sahip nesne çiftlerini gözünüzü kapatarak (veya açık) dokunma
  yoluyla eşleştirin.
- **Talimat EN:** Match pairs of objects with different textures by touch, with eyes closed
  (or open).
- **Zorluk:** 1 | **Tekrar:** 5 (çift) | **Set:** 1
- **Etiketler:** sensory, tactile, matching

### vucut-farkindaligi-oyunu — Body Awareness Game (Simon Says)
- **TR:** Vücut Farkındalığı Oyunu (Simon Diyor ki)
- **EN:** Body Awareness Game (Simon Says)
- **Talimat TR:** "Simon diyor ki" formatında verilen vücut hareketi talimatlarını (ör.
  "burnuna dokun", "kollarını aç") sadece "Simon diyor ki" denildiğinde uygulayın.
- **Talimat EN:** Follow "Simon Says"-style body movement instructions (e.g., "touch your
  nose", "spread your arms") only when preceded by "Simon says".
- **Zorluk:** 1 | **Tekrar:** 10 (komut) | **Set:** 1
- **Etiketler:** body-awareness, gross-motor, listening

### labirent-nokta-birlestirme — Maze / Dot-to-Dot
- **TR:** Labirent / Nokta Birleştirme
- **EN:** Maze / Dot-to-Dot
- **Talimat TR:** Kağıt üzerindeki bir labirentte başlangıçtan bitişe çizgiyi taşırmadan
  ilerleyin veya numaralı noktaları sırayla birleştirin.
- **Talimat EN:** Navigate a paper maze from start to finish without crossing the lines, or
  connect numbered dots in sequence.
- **Zorluk:** 1 | **Tekrar:** 3 (labirent) | **Set:** 1
- **Etiketler:** visual-motor, fine-motor, sequencing

### harf-sayi-izleme — Letter/Number Tracing
- **TR:** Harf/Sayı İzleme
- **EN:** Letter/Number Tracing
- **Talimat TR:** Noktalı çizgilerle gösterilen harf veya sayıları kalemle üzerinden geçerek
  yazın.
- **Talimat EN:** Trace over letters or numbers shown with dotted outlines using a pencil.
- **Zorluk:** 1 | **Tekrar:** 5 (harf/sayı) | **Set:** 2
- **Etiketler:** fine-motor, pre-writing, cognitive

### sosyal-beceri-rol-oynama — Social Skills Role-Play
- **TR:** Sosyal Beceri Rol Oynama (Sıra Alma)
- **EN:** Social Skills Role-Play (Turn-Taking)
- **Talimat TR:** Terapist veya bir akranla basit bir sıra-alma oyunu (ör. top yuvarlama,
  kart çekme) oynayarak "sıramı bekliyorum" davranışını pratik edin.
- **Talimat EN:** Play a simple turn-taking game with the therapist or a peer (e.g., rolling
  a ball, drawing cards) to practice "waiting for my turn" behavior.
- **Zorluk:** 1 | **Tekrar:** 5 (tur) | **Set:** 1
- **Etiketler:** social-skills, turn-taking, communication

### duygu-tanima-kartlari — Emotion Recognition Cards
- **TR:** Duygu Tanıma Kartları
- **EN:** Emotion Recognition Cards
- **Talimat TR:** Farklı yüz ifadelerini gösteren kartlara bakıp hangi duyguyu (mutlu,
  üzgün, kızgın vb.) ifade ettiğini söyleyin veya işaret edin.
- **Talimat EN:** Look at cards showing different facial expressions and name or point to
  which emotion (happy, sad, angry, etc.) each one expresses.
- **Zorluk:** 1 | **Tekrar:** 10 (kart) | **Set:** 1
- **Etiketler:** emotion-recognition, social-skills, communication

### gorsel-program-takibi — Visual Schedule Following
- **TR:** Görsel Program Takibi
- **EN:** Visual Schedule Following
- **Talimat TR:** Resimli bir günlük program üzerindeki adımları (ör. "el yıka" → "otur" →
  "çalış") sırasıyla takip edip her adımı tamamlayınca işaretleyin.
- **Talimat EN:** Follow the steps on a picture-based daily schedule (e.g., "wash hands" →
  "sit down" → "work") in order, checking off each step as it's completed.
- **Zorluk:** 1 | **Tekrar:** 1 (program) | **Set:** 1
- **Etiketler:** visual-schedule, sequencing, independence

### sayi-eslestirme — Number Matching
- **TR:** Sayı Eşleştirme
- **EN:** Number Matching
- **Talimat TR:** Aynı sayıyı gösteren kart çiftlerini bulup eşleştirin veya bir sayıyı o
  kadar nesneyle (ör. 3 rakamı — 3 blok) eşleştirin.
- **Talimat EN:** Find and match pairs of cards showing the same number, or match a number
  to that many objects (e.g., the numeral 3 — 3 blocks).
- **Zorluk:** 1 | **Tekrar:** 10 (çift) | **Set:** 1
- **Etiketler:** cognitive, number-sense, matching

### ses-harf-eslestirme — Sound-Letter Matching
- **TR:** Ses-Harf Eşleştirme
- **EN:** Sound-Letter Matching
- **Talimat TR:** Söylenen bir sesi (ör. "mmm") duyup o sesle başlayan harfi veya resmi
  gösteren kartı seçin.
- **Talimat EN:** Listen to a spoken sound (e.g., "mmm") and select the card showing the
  letter or picture that begins with that sound.
- **Zorluk:** 2 | **Tekrar:** 10 (çift) | **Set:** 1
- **Etiketler:** phonics, pre-literacy, cognitive

### gunluk-yasam-becerisi-simulasyonu — Functional Life Skill Simulation
- **TR:** Günlük Yaşam Becerisi Simülasyonu (Market Alışverişi)
- **EN:** Functional Life Skill Simulation (Grocery Shopping)
- **Talimat TR:** Basit bir alışveriş listesindeki ürünleri oyuncak/kart raflardan bulup
  bir sepete koyma ve "ödeme yapma" adımlarını sırasıyla canlandırın.
- **Talimat EN:** Role-play finding items from a simple shopping list on toy/card shelves,
  placing them in a basket, and "paying" — following the steps in order.
- **Zorluk:** 2 | **Tekrar:** 1 (senaryo) | **Set:** 1
- **Etiketler:** functional-skills, sequencing, community-skills

---

## Kaynaklar (araştırma için ilham, hiçbir metin birebir alınmadı)

- [A Guide to Common Physical Therapy Exercises — Biomotion PT](https://www.biomotionpt.com/unlocking-wellness-a-guide-to-common-physical-therapy-exercises/)
- [15 Physical Therapy Exercises for Everyday Comfort — Hinge Health](https://www.hingehealth.com/resources/articles/physical-therapy-exercises/)
- [Range of Motion — Physiopedia](https://www.physio-pedia.com/Range_of_Motion)
- [What Are Balance and Gait Training Exercises? — RehabSelect](https://blog.rehabselect.net/what-are-balance-and-gait-training-exercises)
- [Gait Training Exercises for Stroke Patients — Flint Rehab](https://www.flintrehab.com/gait-training-exercises/)
- [50 Fine Motor Occupational Therapy Activities at Home — Your Therapy Source](https://www.yourtherapysource.com/blog1/2020/03/18/occupational-therapy-activities-at-home/)
- [35 Fine Motor Activities: Therapists' Ultimate List — NAPA Center](https://napacenter.org/fine-motor-activities/)
- [The Best Bilateral Coordination Activities for Kids — The OT Toolbox](https://www.theottoolbox.com/bilateral-coordination-activities/)
- [Bilateral Coordination: 15 Exercises & Activities For Children](https://hes-extraordinary.com/bilateral-coordination-challenges-and-strategies-to-improve-development)
- [4 Kids' Oral Motor Exercises for Muscle Weakness — Speech Blubs](https://speechblubs.com/blog/kids-oral-motor-exercises)
- [Oral Motor Exercises — The OT Toolbox](https://www.theottoolbox.com/oral-motor-exercises/)
- [5 Best Aphasia Exercises and Activities — Constant Therapy Health](https://constanttherapyhealth.com/brainwire/best-aphasia-exercises-and-activities/)
- [10 Creative Naming Therapy Activities for Aphasia — Tactus Therapy](https://tactustherapy.com/aphasia-activities-naming-therapy/)
- [10+ Mindful Grounding Techniques — Positive Psychology](https://positivepsychology.com/grounding-techniques/)
- [Grounding Techniques — Therapist Aid](https://www.therapistaid.com/therapy-article/grounding-techniques-article)
- [Breathing Exercises handout — UW](https://depts.washington.edu/uwhatc/wp-content/uploads/2023/03/Breathing-Exercises-handout.pdf)
- [35+ Powerful CBT Exercises & Techniques for Therapists — Positive Psychology](https://positivepsychology.com/cbt-cognitive-behavioral-therapy-techniques-worksheets/)
- [Behavioral Activation & Exposure: CBT Exercises — Dialectical Behavior Therapy](https://dialecticalbehaviortherapy.com/cbt/behavioral-activation-exposure/)
- [Top 10 Sensory Activities for SPED Teachers — Roylco](https://roylco.com/top-10-sensory-activities-for-sped-teachers/)
- [Enhancing Sensory Integration: Movement Activities for Kids](https://itsasensoryworld.org/enhancing-sensory-integration-movement-activities-for-kids/)
- [Gamification in Musculoskeletal Rehabilitation — PMC/NIH](https://pmc.ncbi.nlm.nih.gov/articles/PMC9789284/)
- [Gaming In Physical Therapy & Rehabilitation — Physio Ed.](https://physioed.com/health-advice/treatment/gaming-in-physical-therapy-and-rehabilitation/)
- [Gamifying Rehabilitation: Motion-Controlled Video Games in Physical Therapy — PLAYWORK](https://www.playwork.me/post/gamifying-rehabilitation-motion-controlled-video-games-in-physical-therapy)
- [Exercises for Scapula Protraction and Retraction — Physical Therapy](https://physical-therapy.us/exercises-for-scapula-protraction-retraction/)
- [Scapular Retraction Exercises — Posture Direct](https://www.posturedirect.com/scapular-retraction-exercises/)
- [Dressing Independently: OT-Approved Tricks for Zippers, Buttons, and Shoes](https://carolinabehaviorandbeyond.com/dressing-independently-ot-approved-tricks-for-zippers-buttons-and-shoes/)
- [The Scoop on Zippers and Buttons: OT Strategies — Focus Therapy](https://focusflorida.com/occupational-therapy/the-scoop-on-zippers-and-buttons-ot-strategies-for-mastering-daily-dressing-skills/)
- [29+ Minimal Pairs Activities Speech Therapy — Speech Therapy Store](https://www.speechtherapystore.com/minimal-pairs-speech-therapy/)
- [Do You Hear What I Hear? Using Minimal Pairs in Speech Therapy — Bjorem Speech](https://www.bjoremspeech.com/blogs/bjorem-speech-blog/do-you-hear-what-i-hear-using-minimal-pairs-in-speech-therapy)
- [14 Therapy Exercises for Building Self-Compassion — Hilltop Hope Counseling](https://www.hilltophopecounseling.com/14-therapy-exercises-for-building-self-compassion/)
- [16 Compassion Focused Therapy Techniques & Exercises — Positive Psychology](https://positivepsychology.com/compassion-focused-therapy-training-exercises-worksheets/)
- [How Visual Supports Can Enhance Communication in Autism](https://www.mastermindbehavior.com/post/how-visual-supports-can-enhance-communication-in-autism)
- [How to teach emotion recognition and labelling to children with autism — LuxAI](https://luxai.com/blog/emotion-recognition-for-autism/)

## Sonraki adım (öneri)

Bu taslak onaylanırsa, kabul edilen maddeler `content-packs/exercise-library/<id>.json` olarak
(mevcut 3 dosyayla birebir aynı şemada) tek tek eklenebilir — istersen hepsini birden veya
disiplin disiplin gözden geçirip onaylayabiliriz (bu projenin "tek adım, onay" kuralına uygun
şekilde). `clinical-data-handling` skill'i gereği bunlar sadece statik veri, herhangi bir modül
koduna dokunmuyor.
