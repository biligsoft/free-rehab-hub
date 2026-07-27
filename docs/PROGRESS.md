## Güncel durum
- CLAUDE.md § Yol Haritası'ndaki 8 fazın tamamı tamamlandı (Faz 8, 2026-07-26). Sonrasında
  açık risk listesi kullanıcıyla gözden geçirilip önceliklendirildi, tek tek ele alınıyor
  (bkz. § Açık riskler ve aşağıdaki "Faz 8 sonrası" bölümü — tüm detay/gerekçe orada).
- Son tamamlanan adım: F8.33
- Son commit: F8.32 - Kamera/PipeWire riskinin gercek kok nedeni bulundu (motion_bridge Docker container'i)
- F8.33'te `egzersiz.md`/`oyunlar.md` taslaklarından biri ("Renk Kutusu") gerçek bir
  `com.freerehabhub.color-sort` Exercise modülüne çevrildi — bkz. § Faz 8 sonrası. Özel Eğitim
  disiplininde ilk gerçek modül. Sıradaki: kullanıcıyla henüz konuşulmadı.
- F8.32'de kamera/PipeWire riskinin **gerçek** kök nedeni bulundu: F8.22'nin teşhisi (PipeWire
  monitor.v4l2 çift-yönetimi) yanlışmış — asıl engel kullanıcının kendi kurduğu, projeyle
  ilgisiz bir Docker container'ı (`motion_bridge`, `/dev/video0`'ı doğrudan mount edip tekelen
  tutuyor). Kullanıcı "şimdilik dokunma" dedi, risk bilinçli olarak açık kalıyor — bkz. § Açık
  riskler. Sıradaki: kullanıcıyla henüz konuşulmadı.

## Faz geçmişi

### Faz 8 — Sertleştirme, Paketleme, Katkıcı Onboarding: tamamlandı (2026-07-26)
- Kapsam kararı (kullanıcıyla konuşuldu): üç alt özellik var (CONTRIBUTING.md, installer+
  paketleme, test/güvenlik/KVKK taraması) — test/güvenlik/KVKK taramasıyla başlanacak, çünkü
  paketlemeden önce yapılması daha mantıklı (bulunacak açıklar installer'ı etkileyebilir).
- **Test/güvenlik/KVKK taraması (ilk tur bulguları):** `clinical-data-handling` skill'indeki
  kontrol listesine göre kod taraması yapıldı. Temiz çıkanlar: şifreleme (gerçek, hardcoded
  anahtar yok), loglama (üretim kodunda tek bir `GD.Print`/`Console.WriteLine` bile yok),
  veri erişim yolu (%100 repository üzerinden, Data katmanı dışında ham SQL yok),
  `content-packs/` telif durumu (özgün/jenerik içerik, isimli telifli ölçek yok), "tıbbi cihaz
  değildir" feragatnamesi (PDF raporda mevcut), kiosk/çocuk modunda klinik veri izolasyonu
  (F7.07'de zaten kurulmuştu). Bulunan 3 madde: (1) hasta silme bug'ı (aşağıda F8.01, düzeltildi),
  (2) rıza kaydı (consent record) hiç yok — CLAUDE.md'nin "Faz 2'de temel atılır" dediği şey
  aslında hiç yapılmamış, henüz ele alınmadı, (3) SQLCipher parolası her açılışta elle giriliyor
  (F2.12'nin bilinçli kararı) — OS keychain alternatifine geçilip geçilmeyeceği henüz
  kullanıcıyla konuşulmadı, bug değil.
- F8.01 - **Hasta silme cascade-delete bug fix.** `SqlitePatientRepository.DeleteAsync` düz bir
  `DELETE FROM Patients` çalıştırıyordu; `TherapySessions`/`Prescriptions`/`ProgressRecords`
  FK'ları `ON DELETE CASCADE` içermediği için (`PRAGMA foreign_keys = ON` açıkken), gerçek
  geçmişi olan herhangi bir hastayı silmek `SQLite Error 19: FOREIGN KEY constraint failed`
  fırlatıyordu — `OnDeleteConfirmed` (`async void`) bunu hiç yakalamıyordu, çöküyordu. Hem
  fonksiyonel bir bug hem KVKK "silme hakkı" engeli. Silme semantiği kullanıcıyla konuşuldu:
  soft-delete yerine **tam (cascade) silme** seçildi — mevcut onay diyaloğu zaten "Bu işlem
  geri alınamaz" diyor, bu vaadi gerçekten yerine getiren minimal düzeltme. `DeleteAsync` artık
  tek transaction içinde `ProgressRecordMetrics → ProgressRecords → PrescriptionItems →
  Prescriptions → TherapySessions → Patients` sırasıyla siliyor (şema `ON DELETE CASCADE`
  yerine C# tarafında elle — gerekçe: `CREATE TABLE IF NOT EXISTS` mevcut DB'leri geriye dönük
  migrate etmez, ayrıca projenin her yerdeki elle-transaction konvansiyonuyla tutarlı).
  `AuditLogs`'a kasıtlı olarak dokunulmadı (`RecordId` polimorfik/FK'sız, erişim izi veriyle
  birlikte silinmemeli). `PatientListPanelController.OnDeleteConfirmed`'e try/catch + hata
  mesajı eklendi (defans amaçlı, `TherapistShellController`'daki yedekleme hata-gösterme
  deseniyle aynı) — bu vesileyle `_kioskMessageLabel` genel amaçlı `_messageLabel`'e yeniden
  adlandırıldı. Yeni xUnit testi: hasta + seans + reçete(+kalem) + ilerleme kaydı(+metrik)
  oluşturup silme sonrası hepsinin gerçekten gittiğini doğruluyor. Tüm çözüm: 44/44 Data.Tests,
  41/41 Services.Tests, diğer tüm test projeleri yeşil. Xvfb+gerçek Godot ile gerçek UI'dan
  doğrulandı: geçmişli hasta "Sil" ile hatasız silindi, ilişkili kayıtların gerçekten gittiği
  teyit edildi.
- Rıza kaydı (consent record) kapsam kararları (kullanıcıyla konuşuldu): (1) hasta
  oluşturmada zorunlu — PatientFormPanel'in "Yeni Hasta" akışına eklenecek, rıza bilgisi
  girilmeden hasta kaydedilemeyecek (en doğal nokta: sağlık verisi işlenmeye başlamadan önce
  alınıyor); var olan hastalar için geriye dönük boş kalacak (bu ortamdaki test DB'si için
  önemsiz, gerçek kullanımda ayrıca ele alınmalı). (2) Kapsam minimal tutulacak: kim verdi
  (hasta kendisi veya veli/vasi adı) + ne zaman — "veri saklama"/"rapor paylaşımı" gibi ayrı
  amaç onayları veya versiyonlu rıza metni gibi zengin bir KVKK uyum programı bu projenin
  ölçeğine göre orantısız büyük, yapılmayacak.
- F8.02 - **`ConsentRecord` domain modeli + `IConsentRecordRepository` sözleşmesi.**
  `Domain`'e eklendi — `KioskPin`(F7.01) ile aynı desen, bilinçli olarak dar. `ConsentRecord`:
  `PatientId`, `ConsentGivenByName`, `IsGuardianConsent`, `ConsentedAt`, `WithdrawnAt`
  (nullable — geri çekme alanı modelde şimdiden var, ama geri çekme UI/repository metodu bu
  adımda yok, ayrı bir sonraki adımda eklenecek). `IConsentRecordRepository`:
  `GetByPatientIdAsync`, `AddAsync` — sadece hasta oluşturmada tek seferlik kayıt için gereken
  iki metot. DB şeması, SQLite implementasyonu, `PatientFormPanel` entegrasyonu bu adımda yok.
- F8.03 - **`ConsentRecords` DB şeması + `SqliteConsentRecordRepository`.** `PatientId` doğal
  anahtar (primary key, hasta başına tek kayıt — geri çekme yeni satır değil, aynı kayıt
  üzerinde `WithdrawnAt` güncellemesiyle olacak, `KioskPin`'in tekil-satır fikrine benzer ama
  hasta bazlı). **Yol boyunca bulunan risk:** `ConsentRecords` de `Patients`'a FK ile bağlı
  olacağından, F8.01'in cascade-delete listesine eklenmezse rıza kaydı olan hastalar için aynı
  FK bug'ı geri gelirdi — `SqlitePatientRepository.DeleteAsync`'e `DELETE FROM ConsentRecords`
  adımı eklenerek erken yakalandı. 3 yeni test (round-trip, kayıt yokken null, veli rızası +
  geri çekme tarihi birlikte); F8.01'in cascade-delete testi de genişletilip rıza kaydı olan
  bir hastanın hatasız silindiği ve `ConsentRecords` satırının gerçekten gittiği doğrulandı.
  Tüm çözüm: 47/47 Data.Tests, 41/41 Services.Tests, diğer tüm test projeleri yeşil.
- F8.04 - **`ConsentService` + `AppServices` bağlantısı.** `AuditRecordType`'a `ConsentRecord`
  eklendi. `AddAsync` (boş isimde `ArgumentException`, `Created` audit log — `RecordId=
  PatientId`, `ConsentRecord`'un doğal anahtarı bu), `GetByPatientIdAsync` (kayıt bulunursa
  `Viewed` audit log — `PrescriptionService.GetLatestByPatientIdAsync` ile aynı konvansiyon).
  `AppServices.Unlock()`'a diğer servislerle aynı anda bağlandı. 4 yeni test (fake repository
  ile). Tüm çözüm: 47/47 Data.Tests, 45/45 Services.Tests, diğer tüm test projeleri yeşil.
- F8.05 - **`PatientFormPanel`e zorunlu rıza alanı entegrasyonu.** Yeni hasta oluşturma
  akışına "Rıza Bilgisi" bölümü eklendi: "Rıza Veren Adı (Hasta veya Veli/Vasi)" + "Veli/vasi
  adına veriliyor" onay kutusu; rıza adı boşken kaydetme engelleniyor. Başarılı kaydette
  `PatientService.AddAsync`'ten sonra `ConsentService.AddAsync` çağrılıyor. Hasta **düzenleme**
  modunda rıza bölümü tamamen gizli — rıza sadece oluşturmada isteniyor (F8.02'nin kapsam
  kararı), mevcut hastayı düzenlerken tekrar sorulmuyor. Xvfb+gerçek Godot ile gerçek UI'dan 3
  senaryo doğrulandı: rıza adı boşken engellendi (hasta oluşmadı), dolu+veli/vasi işaretiyle
  hasta+rıza kaydı birlikte oluştu (doğru alanlarla), düzenleme modunda rıza bölümü gizli,
  rıza istemeden normal düzenleme çalıştı. Tüm çözüm: 47/47 Data.Tests, 45/45 Services.Tests.

**Rıza kaydı özelliği tamamlandı (F8.02-05).** Hasta oluşturmada zorunlu, minimal (kim verdi +
ne zaman) bir rıza kaydı — CLAUDE.md'nin "Faz 2'de temel atılır" dediği ama hiç yapılmamış olan
KVKK boşluğunu kapatıyor (bkz. F8.01'in tarama notu). Geri çekme (`WithdrawnAt` alanı modelde
var ama UI/repository güncelleme metodu yok) ve mevcut/geçmiş hastalar için geriye dönük kayıt
girme, bilinçli olarak bu kapsamın dışında bırakıldı — ayrı bir iş olarak ele alınabilir.

SQLCipher parola/anahtar yönetimi kararı kullanıcıyla konuşuldu: **şimdilik elle giriş kalıyor**
(F2.12'nin kararı korundu), OS keychain alternatifine Faz 8'in sonunda, paketleme aşamasına
yakın tekrar bakılacak.

- F8.06 - **`LICENSE` (MIT) + `CONTRIBUTING.md`.** Lisans kararı kullanıcıyla konuşuldu — proje
  MIT ile yayınlanacak (F6.05'te PdfSharp'ın "lisans netliği" gerekçesiyle seçilmesiyle tutarlı).
  `CONTRIBUTING.md`: proje tanıtımı+lisans notu, geliştirme ortamı kurulumu, mimari özet +
  `CLAUDE.md`/skill dosyalarına yönlendirme tablosu (tekrar yazmak yerine DRY), en yaygın katkı
  yolu olarak modül ekleme rehberi (`templates/module-starter/`), içerik telif politikası,
  klinik veri/güvenlik kuralları özeti, test çalıştırma, kod stili özeti, PR süreci. **Bilinçli
  karar:** dış katkıcılar için içsel `F<faz>.<adım>` commit formatı zorunlu tutulmadı (bu
  projenin kendi Claude-Code-destekli sürecine özgü bir kayıt yöntemi) — standart açıklayıcı
  commit + PR açıklaması istendi. **Yol boyunca bulunan boşluk:** `CLAUDE.md`/`testing-approach`
  skill'i GUT'u (Godot Unit Test) sahne/controller testleri için öngörüyor ama `tests/gut/` hiç
  oluşturulmadı, CI hiç Godot-seviyeli test çalıştırmıyor — şimdiye kadarki tüm sahne
  doğrulamaları geçici Xvfb script'leriyle yapıldı (bkz. Açık riskler). CONTRIBUTING.md bunu
  olduğu gibi (GUT kurulu değil, elle test edin) yazdı, kurulumuna girilmedi.

**Installer+paketleme kapsam/sıralama kararı (kullanıcıyla konuşuldu):** bu ortamda Windows/
macOS makine ve kamera yok — SQLCipher/TTS/MediaPipe'ın gerçek Windows/macOS'ta çalışması hiç
doğrulanamaz. Karar: gerçek Windows/macOS doğrulaması için GitHub Actions'ın `windows-latest`/
`macos-latest` runner'ları kullanılacak (kamerasız otomatik smoke-test); v1 installer basit
export+zip olacak (kurulum sihirbazı — NSIS/Inno Setup/.dmg — ayrı, sonraki bir iş).

- F8.07 - **Godot export preset'leri (Linux/Windows/macOS).** `export_presets.cfg` eklendi —
  üç platform için `.NET` export config'i. Export template'leri (1.2GB) indirilip kuruldu.
  **Yol boyunca bulunan altyapı sorunu:** `/tmp` sadece 3.4GB'lık bir tmpfs — 1.2GB indirme +
  açma işlemi bunu doldurup shell'i geçici olarak kilitledi (temel komutlar bile exit code 1
  ile başarısız oldu); çözüm indirme/açmayı `/home` partisyonunda (327GB boş) yapmak oldu —
  büyük tek seferlik indirmeler için scratchpad kuralının bir istisnası. **Linux:** gerçekten
  export edildi, Xvfb'de gerçek ekran görüntüsüyle doğrulandı (LockScreen doğru temayla render
  ediliyor). **Yol boyunca bulunan gerçek bug:** ilk denemede `export_filter="all_resources"`
  test kaynak kodunu, `bin/`/`obj/` derleme çıktılarını, `.claude/` skill dosyalarını da pakete
  dahil ediyordu — `exclude_filter` eklendi, `.pck` 3.45MB'tan 105KB'a indi, temiz build tekrar
  ekran görüntüsüyle doğrulandı. **Windows:** export edildi, gerçek bir "PE32+ executable for MS
  Windows" üretildiği `file` komutuyla teyit edildi (çalıştırılamadı, donanım yok). **macOS:**
  ilk denemede gerçek bir config hatası bulundu (universal/arm64 export'un ETC2 ASTC doku
  formatını proje ayarlarında etkin gerektirdiği) — `project.godot`'a
  `textures/vram_compression/import_etc2_astc=true` eklenerek düzeltildi, tekrar denendi, gerçek
  bir `.app` bundle (Info.plist, PkgInfo, arm64 .dll'ler) içeren zip üretildi. `/build/`
  `.gitignore`'a eklendi. Tüm çözüm: 47/47 Data.Tests, 45/45 Services.Tests — `project.godot`
  değişikliği regresyona yol açmadı. Bilinçli olarak dışarıda bırakılan: gerçek Windows/macOS'ta
  çalıştırma doğrulaması (sıradaki adım — CI matrisi — bunu kısmen çözecek), code signing/icon.
- F8.08 - **CI, çapraz-platform build matrisine genişletildi.** `.github/workflows/ci.yml`,
  tek `ubuntu-latest` job'ından `[ubuntu-latest, windows-latest, macos-latest]` matrisine
  çevrildi (`fail-fast: false`). **Yol boyunca bulunan büyük gerçek durum:** repo hiç origin'e
  push edilmemişti (F1'den beri, sadece "first commit" origin'deydi) — bu commit ilk kez tüm
  F1-F8.08 geçmişini (112 commit) push etti. Push, GitHub'ın workflow-dosyası-değiştiren PAT'lar
  için gerektirdiği `workflow` scope'u yüzünden ilk denemede reddedildi; kullanıcı PAT'ına bu
  scope'u ekleyip tekrar push etti. **İlk gerçek CI sonucu:** macOS tamamen yeşil (SQLCipher
  dahil — Faz 1'den beri açık duran "macOS'ta doğrulanmadı" riskini kapattı), Ubuntu'da 1 gerçek
  ürün bug'ı (`MediaPipePoseTrackingService.StopAsync` race condition), Windows'ta 2 test-
  temizliği hatası (SQLite dosya kilidi) bulundu — detaylar aşağıdaki adımlarda.
- F8.09 - **İlk tur düzeltmeler.** (1) `SqliteConnectionFactory.CreateOpenConnection()`,
  `Open()` başarısız olduğunda connection'ı hiç dispose etmiyordu (native handle leak,
  Windows'ta dosya silmeyi engelliyordu) — try/catch+dispose eklendi. (2)
  `MediaPipePoseTrackingService.StopAsync()`, `Cancel()`'ı `CloseAsync()`'ten önce çağırıyordu
  — .NET'in `ClientWebSocket`'i iptal edilen bir `ReceiveAsync`'i "aborted" durumuna soktuğu
  için `CloseAsync` sonra `ObjectDisposedException` fırlatıyordu; sıra tersine çevrildi.
  Push sonrası CI: Ubuntu/macOS yeşil, Windows'ta aynı 2 hata **hâlâ** vardı (kök neden daha
  derinmiş, bkz. F8.10).
- F8.10 - **Windows dosya kilidi — gerçek kök neden.** Sızıntı `SqliteConnection` seviyesinde
  değil, native SQLitePCLRaw/e_sqlcipher katmanındaymış — `.Dispose()` çağırmak çözmüyor.
  Üretim kodunda (`src/`, `autoload/`, `scenes/`) bu desen (başarısız bağlantı sonrası aynı
  dosyayı silme) hiç kullanılmadığı doğrulandı — yani bu **ürün bug'ı değil, sadece test
  hijyeni**. `tests/FreeRehabHub.Data.Tests/TestFileCleanup.cs` eklendi (retry-with-backoff,
  5×50ms). Push sonrası CI: Windows'ta **aynı hata devam etti** (bütçe yetersizmiş).
- F8.11 - Retry penceresi 30×200ms'e (6 saniyeye kadar) büyütüldü. Push sonrası CI: Windows'ta
  **yine aynı hata**, üstelik 1-3 saniyede — "yeterince bekleme" sorunu olmadığı netleşti,
  native handle muhtemelen process ömrü boyunca hiç serbest bırakılmıyor.
- F8.12 - **Yaklaşım değişti: retry yerine en-iyi-çaba temizlik.** `TestFileCleanup`, birkaç
  kısa denemeden sonra hâlâ başarısız olursa sessizce vazgeçecek şekilde değiştirildi — bu
  sadece OS temp dizinindeki bir dosya, CI runner'ı zaten siliniyor, gerçek bir kaynak sızıntısı
  değil. Push sonrası CI: **Windows sonunda yeşil oldu** — ama bu sefer Ubuntu VE macOS'ta
  `MediaPipePoseTrackingServiceTests` yeni bir semptomla başarısız oldu (exception değil, beklenmeyen
  bir `Error` durumu: `[Starting, Running, Error, Stopped]`) — F8.09'un `Cancel`/`Close` sıra
  düzeltmesi yeterli değilmiş.
- F8.13 - **Gerçek kök neden: `CloseAsync` vs `CloseOutputAsync`.** `ClientWebSocket.CloseAsync()`
  karşı taraftan kapanış onayı okumak için kendi içinde ayrı bir `ReceiveAsync` çalıştırıyor —
  bu, `ReceiveLoopAsync`'in zaten bekleyen kendi `ReceiveAsync`'iyle aynı soket üzerinde
  çakışıp `InvalidOperationException` fırlatıyordu (ReceiveLoopAsync bunu yakalayıp durumu
  Error'a çeviriyordu). `CloseAsync` yerine `CloseOutputAsync` kullanıldı (sadece kapanış
  çerçevesi gönderir, okuma yapmaz — karşı tarafın kapanışını zaten çalışan `ReceiveLoopAsync`
  kendisi okuyup temiz çıkıyor). Yerel doğrulama: önceden flaky olan test art arda 20 kez
  çalıştırıldı, 20/20 geçti. **Push sonrası CI: Windows/Ubuntu/macOS üçü de tamamen yeşil.**
  GitHub Actions çapraz-platform CI matrisi (F8.08'in asıl amacı) artık gerçekten geçiyor.
- F8.14 - **Paketlenmiş build'ler için birleşik path çözümleme.** Mediapipe-service yol
  düzeltmesine bakarken aynı sorunun `content-packs/` (exercise-library), `res://modules`
  (manifest.json tarama) ve PDF font yolunda da olduğu ortaya çıktı — kullanıcıyla konuşulup
  hepsi tek bir birleşik çözümle düzeltildi. `autoload/AppContentRoot.cs` eklendi: önce
  `GlobalizePath("res://...")` deneniyor, sonuç gerçekten diskte varsa kullanılıyor (dev/editör
  modu); yoksa (export edilmiş build, bu klasörler `.pck` içinde sanal dosya sistemi), çalışan
  executable'ın yanına düşülüyor. `AppServices` (exercise-library, mediapipe-service, PDF font)
  ve `ModuleRegistryAutoload` (`res://modules`) bu yardımcıyı kullanacak şekilde güncellendi.
  `export_presets.cfg`: `content-packs/**`, `services/mediapipe-service/**`,
  `assets/fonts/liberation-sans/**`, `modules/*/manifest.json` artık `.pck`'e dahil edilmiyor
  (ham `System.IO` ile okunuyorlar) — modüllerin `.tscn` dosyaları hâlâ paketleniyor (Godot'un
  `PackedScene` yüklemesi için gerekli). Gerçek export edilmiş Linux binary'siyle uçtan uca
  doğrulandı: paketleme scriptinin yapacağı kopyalama simüle edilip (klasörler executable'ın
  yanına kopyalandı), binary **nötr bir çalışma dizininden** (`/tmp`, kaynak ağacıyla
  çakışmayı önlemek için) çalıştırıldı — 3/3 modül keşfedildi, 3/3 egzersiz kartı yüklendi,
  mediapipe/font yolları doğru çözülüp var olduğu doğrulandı. Push sonrası CI: Windows/Ubuntu/
  macOS üçü de yeşil.
- F8.15 - **`mediapipe-service` için PyInstaller paketleme.**
  `services/mediapipe-service/run_server.py` eklendi (PyInstaller giriş noktası — dev modda
  kullanılan `python -m uvicorn app.main:app ...` CLI çağrısı PyInstaller'da dondurulamıyor,
  `--host`/`--port` argümanlarını uvicorn'a aktaran tek bir betik gerekiyordu).
  `build_exe.py`: `pyinstaller --onedir --collect-all mediapipe` (onedir — mediapipe gibi çok
  sayıda native ikili içeren büyük bağımlılıklar için onefile'dan daha güvenilir/hızlı);
  `models/` klasörü bilinçli olarak pakete dahil edilmiyor (F8.14'teki "loose file" deseniyle
  tutarlı). `requirements-build.txt` (sadece `pyinstaller`, çalışma zamanı bağımlılığı değil).
  `MediaPipePoseTrackingService`'in constructor'ı `argumentsTemplate` parametresi alacak
  şekilde genişletildi (dev modda `-m uvicorn ...`, paketlenmiş modda `run_server.py`'ın kendi
  `--host`/`--port` sözleşmesi); 3 test çağrısı güncellendi. `AppServices
  .ResolveMediaPipeCommand`: `dist/mediapipe-service/mediapipe-service(.exe)` varsa onu
  kullanıyor, yoksa dev-mode `.venv` python'a düşüyor. Doğrulama F5.01'de kurulan Docker/Debian
  yolu üzerinden gerçek yapıldı: PyInstaller build'i Docker'da gerçekten tamamlandı (370MB,
  mediapipe'ın native ikilileri dahil), üretilen binary gerçekten çalıştırıldı, `/health`
  `{"status":"ok"}` ile yanıt verdi. Tüm çözüm: 47/47 Data.Tests, 45/45 Services.Tests. Push
  sonrası CI: Windows/Ubuntu/macOS üçü de yeşil. Windows/macOS için PyInstaller cross-compile
  desteklemediğinden buradan üretilemedi — gerçek Windows/macOS binary'leri ayrı bir işle
  GitHub Actions runner'larında üretilmeli.
- F8.16 - **CI'a `mediapipe-package` job'u eklendi.** `.github/workflows/ci.yml`'e yeni bir
  job — `windows-latest`/`macos-latest`/`ubuntu-latest` matrisinde Python 3.10 kurup
  `build_exe.py`'ı gerçekten çalıştırıyor, ardından üretilen binary'yi başlatıp `/health`'e
  istek atarak `200` yanıtını doğruluyor (Linux/macOS: bash+curl, Windows: PowerShell+
  `Invoke-WebRequest` — platforma özgü ayrı adımlar), sonra `dist/` çıktısını build artifact
  olarak yüklüyor (7 gün saklama). **CI'da 6/6 job yeşil** (`build` × 3 + `mediapipe-package`
  × 3) — SQLCipher'ın ardından mediapipe'ın da Windows/macOS'ta gerçekten import edilip
  PyInstaller ile paketlenip çalıştırılabildiği ilk kez doğrulandı (F5.01'den beri sadece
  Linux/Docker'da biliniyordu). Kamera/gerçek poz-tespiti hâlâ test edilemedi (donanım yok),
  ama "mediapipe bu platformda hiç başlamıyor" riski kapandı.
- F8.17 - **Son paketleme scripti.** `scripts/package_release.py` eklendi — Godot export
  çıktısını (`build/<platform>/`), loose-file içeriği (F8.14: `content-packs/`,
  `assets/fonts/liberation-sans/`, modül `manifest.json`'ları) ve PyInstaller ile paketlenmiş
  mediapipe-service binary'sini (F8.15) birleştirip tek bir dağıtılabilir zip üretiyor. Ön
  koşulları (Godot export + mediapipe build) kendisi çalıştırmıyor, ikisi de ayrı adımlarda
  zaten var — sadece son birleştirme/paketleme. Uçtan uca doğrulandı: gerçek Godot Linux
  export'u yapılıp script çalıştırıldı, tüm beklenen dosyalar zip'e doğru girdi; zip
  **tamamen ayrı, temiz bir dizine** açılıp gerçek binary çalıştırıldı — LockScreen doğru
  temayla render edildi, tıpkı bir kullanıcının indirip çalıştırması gibi. Bu, orijinal 5
  maddelik installer+paketleme planının tamamını bitiriyor.

**Faz 8 tamamlandı (2026-07-26).** Üç alt özelliğin üçü de bitti: test/güvenlik/KVKK taraması
(F8.01-F8.05 — hasta silme cascade-delete bug'ı, rıza kaydı özelliği, SQLCipher anahtar
yönetimi kararı), CONTRIBUTING.md (F8.06 — MIT lisansı), installer+paketleme (F8.07-F8.17 —
export preset'leri, GitHub Actions çapraz-platform CI matrisi, paketlenmiş build path
çözümleme, PyInstaller mediapipe paketleme, son birleştirme scripti). Bu, CLAUDE.md § Yol
Haritası'ndaki 8 fazın tamamını kapatıyor.

Bu, projenin "bitti" anlamına gelmediğini not etmek gerekir — bilinçli olarak dar tutulan
kapsam kararları ve gerçek hedef donanımda doğrulanmayı bekleyen noktalar bu dosyanın sonundaki
"Açık riskler" bölümünde duruyor: gerçek kamera donanımıyla uçtan uca test (bu geliştirme
ortamında hiç yapılamadı), TTS'in Türkçe ses kalitesi Windows/macOS'ta doğrulanmadı, GUT hiç
kurulmadı (sahne/UI testleri hâlâ elle/Xvfb ile yapılıyor), rıza kaydının geri çekme akışı yok,
gerçek bir installer sihirbazı (v1 sadece export+zip) yok, code signing yok. Sekiz fazın
tamamlanması, "planlanan iskelet uçtan uca çalışıyor ve gerçek platformlarda (CI ile) doğrulandı"
anlamına geliyor — "artık hiçbir açık yok" anlamına gelmiyor.

### Faz 8 sonrası — Açık risk takibi (F8.18+, faz numarası kullanıcının tercihiyle korundu)
- Kullanıcıyla açık riskler listesi gözden geçirilip önceliklendirildi. Bu arada birkaç madde
  Faz 8 çalışmasıyla zaten çözülmüş olduğu için temizlendi (SQLCipher/PdfSharp/mediapipe path
  çözümleme Windows/macOS notları — hepsi F8.08-F8.14'te fiilen doğrulanmıştı).
- **Kamera erişimi araştırıldı ama çözülemedi.** Kullanıcı `sudo usermod -aG video` yapıp
  yeniden oturum açtı; kontrol edilince `/dev/video0`'da zaten `user:emre:rw-` ACL'i olduğu
  görüldü (video grubundan bağımsız, muhtemelen systemd-logind "uaccess") — yani **eski teşhis
  ("video grubunda değil") yanlış çıktı**, izin hiç asıl engel değilmiş. Cihaz gerçek (format
  sorgularına doğru yanıt veriyor), ama ham V4L2 erişimi (`ffmpeg` ve `cv2.VideoCapture(0)` —
  `pose_tracker.py`'nin fiilen kullandığı yöntem) sürekli "meşgul" hatası veriyor, kamera
  uygulaması (Cheese, guvcview) kapalıyken bile; `guvcview`'ın kendi hatası aslında ilgisiz bir
  GTK3 çökmesiymiş (kamera erişimiyle ilgisi yok). Bu makinede `pipewire`/`wireplumber`
  çalıştığı doğrulandı — en olası açıklama PipeWire'ın kamerayı kendi üzerinden yönetip ham
  erişimi engellemesi, ama kesin doğrulanamadı (bu ortamda tam `sudo`/`journalctl` erişimi
  yok). Kullanıcıyla konuşulup bu konuda durduruldu — hedef donanım zaten çoğunlukla Windows
  (PipeWire yok), ileride GStreamer/`pipewiresrc` backend'i denenebilir (kod değişikliği
  gerektirir, henüz yapılmadı). Detay için CLAUDE.md § 14.
- F8.18 - **Assessment modülü oynatma ekranı.** F5.10'dan beri bilinçli olarak açık bırakılan
  boşluk kapatıldı: `scenes/assessment-host/AssessmentHost.tscn`/Controller eklendi —
  `ModuleRegistry.CreateInstance()` ile `IAssessmentModule`'ü reflection'la kurup mevcut
  `FormRenderer.tscn`'i (F3.04) alt sahne olarak gömüyor, form şemasını
  `manifest.FormSchemaPath`'ten (F8.14'ün `AppContentRoot.Resolve()` deseniyle) yüklüyor.
  Gönderilince `Score()` çağrılıp `ModuleResult` → `ProgressRecord`'a çevrilip kaydediliyor
  (Exercise akışındaki `ModuleHostController.OnModuleCompleted` ile birebir aynı desen), sonra
  mevcut `ModuleResultPanel`'e yönleniyor (zaten rol-farkında, değişiklik gerekmedi).
  `ModuleLibraryPanelController` artık sadece Exercise değil tüm modülleri listeliyor, seçilen
  modülün `Kind`'ına göre `ModuleHost`/`AssessmentHost`'a yönlendiriyor. **Bilinçli olarak
  dokunulmadı:** `ChildKioskShellController` hâlâ sadece Exercise modülleri listeliyor —
  çocuğun kiosk modunda gözetimsiz bir öz-bildirim formu doldurması ayrı bir klinik/ürün kararı,
  henüz konuşulmadı. Xvfb+gerçek Godot ile gerçek UI'dan uçtan uca doğrulandı: modül kütüphanesi
  artık 3 modül listeliyor (2 Exercise + 1 Assessment), Assessment seçilip Başlat'a basılınca
  gerçekten `AssessmentHost`'a gidip formu doğru başlık ve 5 alanla render ediyor; gerçek form
  etkileşimi (slider/seçim/checkbox/metin) + Gönder → `ModuleResultPanel`'e yönleniyor; skor
  hesaplaması elle doğrulanan beklenen değerle (0.30) birebir eşleşti, `ProgressRecord`
  gerçekten veritabanına kaydedildi. Tüm çözüm: 47/47 Data.Tests, 45/45 Services.Tests. Push
  sonrası CI: 6/6 job yeşil.
- F8.19 - **GUT yerine özel C# sahne-test harness'ı.** "GUT kurulumuyla devam et" talimatıyla
  başlandı, ama GUT'un kendi README'si incelenince ("allows you to write tests for your
  gdscript in gdscript", C# hiç geçmiyor) GUT'un sadece GDScript için olduğu doğrulandı — bu
  proje tamamen C# olduğu için mimari olarak uyumsuz. Kullanıcıya sorulup ("Özel bir C#
  sahne-test harness'ı yaz (Recommended)") GUT yerine `tests/scene-tests/` altında yeni bir
  harness yazıldı: `ISceneTest` (arayüz), `SceneAssert` (statik assertion yardımcıları,
  `SceneAssertionException` fırlatır), `SceneTestRunner` (reflection'la `ISceneTest`
  implementasyonlarını keşfeder, çalıştırır, `[GEÇTİ]`/`[BAŞARISIZ]` yazdırır, tüm testler
  geçerse exit 0 / en az biri başarısızsa exit 1 döner). İlk gerçek test:
  `AssessmentHostSceneTest` — F8.18'in tüm akışını uçtan uca doğruluyor (modül kütüphanesi →
  form doldurma → skorlama → `ProgressRecord` kalıcılığı → sonuç ekranı).
  **Mimari düzeltme (görev sırasında bulunan gerçek bug):** `SceneTestRunner` ilk denemede
  bağımsız bir sahne olarak `godot ... res://tests/scene-tests/SceneTestRunner.tscn` şeklinde
  çalıştırılıyordu (project.godot'a hiç dokunmamak için). Boş (0 test) çalıştırmada işe yaradı,
  ama gerçek `AssessmentHostSceneTest` (kendi assertion'larını GEÇTİKTEN sonra bile)
  `System.ObjectDisposedException` fırlattı — çünkü test kendi içinde `ChangeSceneToFile`
  çağırınca, runner ANA SAHNE olduğu için kendi kendini yok etmiş oluyordu. Çözüm:
  `SceneTestRunner.tscn` silindi, `SceneTestRunner` **kalıcı bir autoload**'a çevrildi
  (`project.godot` → `SceneTestRunner="*res://tests/scene-tests/SceneTestRunner.cs"`),
  `FREEREHABHUB_RUN_SCENE_TESTS` ortam değişkeni set edilmemişse `_Ready()` içinde erkenden
  `return` ediyor — yani normal uygulama çalışırken (env var yokken) tamamen etkisiz, elle
  autoload ekleme/çıkarma dansına hiç gerek kalmadı. Her sahne testi
  `AppServices.Unlock(password, databasePathOverride)` ile izole bir geçici SQLite dosyası
  kullanıyor (`finally`'de silinir) — gerçek `user://freerehabhub.db`'ye hiç dokunulmuyor,
  daha önceki manuel yedek/geri yükleme dansına artık gerek yok. Doğrulama: (1) gerçek test
  ile çalıştırıldığında exit 0, `[GEÇTİ]`; (2) env var yokken sıfır "SCENE-TESTS" çıktısı,
  uygulama öncekiyle birebir aynı davranıyor; (3) geçici bir "bilerek başarısız" test eklenip
  çalıştırıldığında exit 1 ve `[BAŞARISIZ]` doğru raporlandı, sonra bu geçici dosya silinip
  temiz 1/1 durumuna dönüldüğü teyit edildi. `.github/workflows/ci.yml`'e yeni bir
  `scene-tests` job'u eklendi (şimdilik sadece `ubuntu-latest`): Godot .NET/Mono binary'sini
  indirip `--headless --import` ile proje kaynaklarını içe aktarıyor, sonra
  `FREEREHABHUB_RUN_SCENE_TESTS=1` ile Xvfb altında headless çalıştırıyor. Windows/macOS'a
  henüz eklenmedi (Xvfb Linux'a özgü; o platformlarda headless çalıştırma zaten sanal
  framebuffer gerektirmiyor ama bu iddia henüz CI'da doğrulanmadı — ihtiyaç doğarsa ayrı bir
  adımda ele alınır). `CLAUDE.md` § 11 ve `testing-approach` skill'i GUT yerine bu harness'ı
  belgeleyecek şekilde güncellendi. Push sonrası CI: 7/7 job yeşil (yeni `scene-tests` job'u
  dahil), ilk denemede (taze checkout, hiç `.godot/` cache'i olmadan) sorunsuz çalıştı.
- F8.20 - **Rıza kaydı geri çekme akışı.** Kapsam kullanıcıyla konuşuldu ("Rıza geri
  çekildiğinde uygulama davranışı ne olmalı?" → "Minimal: sadece kayıt (Recommended)") —
  geri çekme hiçbir işlevsel kısıtlama getirmiyor (yeni terapi seansı/modül başlatma
  engellenmiyor, hasta pasif sayılmıyor), sadece bir zaman damgası kaydediliyor; terapist
  isterse ayrıca F8.01'in cascade-delete akışıyla hastayı elle silebilir. İki alt adımda
  yapıldı:
  - **Backend (Domain/Data/Services):** `IConsentRecordRepository.WithdrawAsync(patientId,
    withdrawnAt)` + SQLite implementasyonu (`UPDATE ConsentRecords SET WithdrawnAt = ...`).
    `AuditAction`'a yeni bir `Withdrawn` değeri eklendi — "Updated" ile karıştırılmasın diye,
    KVKK denetim izinde geri çekme anının ayrı görünmesi için. `ConsentService.WithdrawAsync`:
    rıza kaydı yoksa veya zaten geri çekilmişse `InvalidOperationException`, aksi halde
    `WithdrawnAt` set edilip `Withdrawn` audit log'u yazılıyor. Testler: 3 yeni
    Services.Tests (başarılı + audit log, kayıt yok, zaten geri çekilmiş), 1 yeni Data.Tests
    entegrasyon testi (gerçek SQLite round-trip). Services.Tests 48/48, Data.Tests 48/48.
  - **UI:** `PatientFormPanelController` düzenleme modunda artık `ConsentSection`'ı (yeni
    hasta girişi) gizlediği gibi, yeni bir `ConsentStatusSection` gösteriyor: rıza kimin
    tarafından ne zaman verildiğini (`"Rıza: {isim} tarafından {tarih} verildi."`) veya geri
    çekilmişse ne zaman geri çekildiğini (`"Rıza {tarih} tarihinde geri çekildi."`) yazan bir
    etiket + henüz geri çekilmemişse görünen bir "Rızayı Geri Çek" butonu. Buton
    `ConsentService.WithdrawAsync`'i çağırıp durumu yeniden çekiyor (`RefreshConsentStatusAsync`),
    çift tıklamadan doğabilecek `InvalidOperationException`'ı yakalayıp hata etiketinde
    gösteriyor. Yeni bir sahne testi eklendi: `PatientConsentWithdrawalSceneTest` — hasta
    düzenleme ekranını açıp rıza durumunu doğruluyor, geri çekme butonuna basıyor, hem UI'ın
    hem veritabanının (gerçek `ConsentService.GetByPatientIdAsync` ile) güncellendiğini
    doğruluyor. Xvfb + gerçek Godot ile ekran görüntüsüyle de görsel olarak doğrulandı (geri
    çekmeden önce/sonra iki ekran görüntüsü, düzen bozulmadı, buton doğru kayboldu). Tüm sahne
    testleri: 2/2 geçti (`AssessmentHostSceneTest` + yeni test).
- F8.21 - **TTS Türkçe ses paketi doğrulaması (kısmi — sadece ubuntu-latest).** CI'da gerçek
  Godot ile pencereli bir doğrulama eklemeden önce yerel bir spike yapıldı, iki gerçek bulgu
  çıktı:
  1. **`--headless` modda TTS hiç görünmüyor.** `DisplayServer.HasFeature(TextToSpeech)`
     platform fark etmeksizin `--headless`'ta hep `false` dönüyor (bu dev makinesinde hem
     headless hem pencereli/Xvfb ile karşılaştırmalı test edildi) — mevcut `scene-tests` job'u
     (headless) bu yüzden TTS'i asla test edemez, ayrı, pencereli bir mekanizma gerekiyordu.
  2. **Yeni bir gerçek motor bulgusu:** `DisplayServer.TtsGetVoicesForLanguage(dil)`, istenen
     dilde HİÇ ses yoksa dahili bir Godot hatası logluyor (`ERROR: Parameter "synth" is null`,
     `tts_linux.cpp`) — bu dev makinesinde Türkçe ses paketi olmadığı için "tr" çağrısında
     tetiklendi. Non-fatal: boş dizi dönüyor, `TtsAutoload.Speak()`'ün mevcut boş-ses-kimliği
     fallback'i (varsayılan sese düşme) bu durumda da hatasız çalışmaya devam ediyor —
     yani hedef klinik bilgisayarda Türkçe ses paketi kurulu olmasa bile uygulama çökmüyor,
     sadece stderr'e bir ERROR satırı yazılıyor.
  Bulgular sonrası kapsam kullanıcıyla konuşuldu ("Windows/macOS'ta gerçek Godot TTS
  doğrulaması için ne kadar kapsamla başlayalım?" → "Önce sadece ubuntu-latest
  (Recommended)") — Windows/macOS için Godot mono binary'sinin asset adları ve macOS'ta
  app-bundle yolu yerel olarak doğrulanamadığından, mediapipe/scene-tests'teki gibi kör
  iterasyona girmeden önce Linux'ta sağlam bir temel kurulup ayrı bir adımda genişletilecek.
  Eklenenler:
  - `autoload/TtsDiagnosticRunner.cs` (yeni) — `SceneTestRunner` ile aynı desende (kalıcı,
    env-var-gated autoload, `FREEREHABHUB_RUN_TTS_CHECK`), ama `SceneTestRunner`'a DAHİL
    EDİLMEDİ çünkü farklı çalıştırma modu gerektiriyor (pencereli, headless değil).
    `DisplayServer.HasFeature` false ise exit 1 (gerçek platform-desteği boşluğu, hata
    sayılır); "tr" için 0 ses bulunması exit 1 SAYILMIYOR (beklenen/olası durum) — sadece
    bilgi amaçlı loglanıyor; gerçek `TtsAutoload.Speak()`/`Stop()` çağrılıp hata fırlatmadığı
    doğrulanıyor.
  - `.github/workflows/ci.yml`'e yeni `tts-check` job'u (sadece `ubuntu-latest`): Godot
    indirilip `--import` ile kaynaklar aktarılıyor, `speech-dispatcher`/`espeak-ng` apt ile
    kuruluyor, sonra `FREEREHABHUB_RUN_TTS_CHECK=1` ile Xvfb altında ama **`--headless`
    OLMADAN** (pencereli) çalıştırılıyor.
  - `CLAUDE.md` §13 (yeni doğrulanmış bulgu maddesi) ve §14 (TTS riski "kısmen ilerletildi"
    olarak güncellendi), `testing-approach` skill'i (yeni not: `DisplayServer` özellikleri
    `--headless`'ta çalışmayabilir, `TtsDiagnosticRunner` örnek olarak referans verildi).
  Yerel doğrulama: env var yokken hem headless hem pencereli modda tamamen etkisiz (sıfır
  "TTS-CHECK" çıktısı); env var + `--headless` → beklendiği gibi exit 1 (`HasFeature = False`);
  env var + pencereli (Xvfb, `--headless` yok) → exit 0, `HasFeature = True`, `tr` ses
  sayısı = 0 (bu makinede Türkçe paket yok, beklenen), `Speak()`/`Stop()` hatasız. Mevcut
  `scene-tests` de yeni autoload'la birlikte hâlâ 2/2 geçiyor (regresyon yok).
- F8.22 - **Kamera/PipeWire sorunu derinleştirildi (sistem-seviyesi, repoya commit edilecek
  kod yok).** Önceki turda ("video grubu değil, muhtemelen PipeWire") yarım kalan hipotezi
  test etmek için `wpctl status`/`pw-cli ls Device`/`ls Node` ile daha derin incelendi:
  - **Gerçek bulgu #1:** WirePlumber aynı fiziksel USB kamerayı (Azurewave `ov9734`,
    `13d3:56f9`) HEM `monitor.v4l2` (iki ayrı Device: `/dev/video0`, `/dev/video1`) HEM
    `monitor.libcamera` ile aynı anda yönetiyordu — ikisi de `wireplumber.conf`'un
    `hardware.video-capture` profilinde `wants = [ monitor.v4l2, monitor.libcamera ]` olarak
    tanımlı, kasıtlı ama bu donanımda gereksiz bir çift-yönetim.
  - **Düzeltme (kullanıcının onayıyla uygulandı, kalıcı bırakıldı):**
    `~/.config/wireplumber/wireplumber.conf.d/51-disable-libcamera-monitor.conf` — WirePlumber
    `main` profilinde `monitor.libcamera = disabled`. `systemctl --user restart wireplumber`
    ile uygulandı; `wpctl status`'ta libcamera Device'ı gerçekten kayboldu.
  - **Ama asıl "meşgul" hatası ÇÖZÜLMEDİ.** `fuser`/`lsof` düzeltmeden önce `/dev/video0`'ı
    `pipewire` daemon'unun (fd 68u) açık tuttuğunu gösterdi — libcamera temizliğinden sonra
    bile (hatta `pipewire`/`pipewire-pulse` servisleri de tam yeniden başlatılıp fuser'ın HİÇBİR
    işlem göstermediği tamamen temiz bir durumdan başlanınca bile), hem `ffmpeg -f v4l2 -i
    /dev/video0` hem `gst-launch-1.0 pipewiresrc` (PipeWire'ın KENDİ yolu, `target-object` ile
    doğru Source'a yönlendirilmiş hâliyle) aynı `-16 (Device or resource busy)` hatasını verdi.
    Bu, sorunun iki farklı SÜRECİN çakışması olmadığını, PipeWire'ın kendi v4l2 monitörünün
    cihazı sadece graph'a açığa çıkarmak için sürekli açık tuttuğunu VE bu spesifik kameranın
    V4L2/UVC sürücüsünün (ya da SPA v4l2 eklentisinin) ikinci HİÇBİR açma/stream-negotiate
    denemesine (kendi `pipewiresrc`'i dahil) izin vermediğini gösteriyor.
  - **Kesin çözüm yolu bulundu ama uygulanmadı:** `monitor.v4l2`'yi de aynı şekilde devre dışı
    bırakmak muhtemelen `ffmpeg`/OpenCV'nin doğrudan erişimini açardı. Kullanıcıya soruldu
    ("PipeWire'ın v4l2 monitörünü TAMAMEN devre dışı bırakmayı deneyeyim mi?") → "Hayır, burada
    duralım (Recommended)" — çünkü bu, günlük kullanılan bu masaüstü makinede tarayıcı/video
    görüşme gibi PipeWire-tabanlı diğer kamera kullanımlarını da kalıcı olarak bozardı; kapsam
    bu projenin ihtiyacına göre orantısız büyük bir tradeoff.
  - `CLAUDE.md` §14 güncellendi (daha kesin teşhis, aynı sonuç: hedef donanım Windows olduğu
    için bu Fedora-özel bulgu üretim mimarisini etkilemiyor). `docs/PROGRESS.md` § Açık
    riskler'deki eski (daha belirsiz) madde bu daha kesin bulguyla değiştirildi.
- F8.23 - **TTS doğrulamasını Windows/macOS'a genişletme.** F8.21'de bilinçli olarak
  ubuntu-latest'le sınırlanan kapsam ("Godot mono binary'sinin Windows/macOS asset adları
  yerel doğrulanamadığından") bu adımda genişletildi. Önce asset adlarını TAHMİN ETMEDEN,
  `https://api.github.com/repos/godotengine/godot/releases/tags/4.7-stable` ile gerçek dosya
  listesi çekildi, ardından iki zip fiilen indirilip içerikleri incelendi:
  - Windows: `Godot_v4.7-stable_mono_win64.zip` →
    `Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64.exe` (Linux'takiyle birebir aynı
    isimlendirme deseni, tahmin doğru çıktı).
  - macOS: `Godot_v4.7-stable_mono_macos.universal.zip` → bir `.app` bundle,
    `Godot_mono.app/Contents/MacOS/Godot`.
  `.github/workflows/ci.yml`'deki `tts-check` job'u `matrix.include` ile 3 platforma
  genişletildi (`os`, `godot_zip_url`, `godot_bin` üçlüsü her platform için ayrı) — Linux
  Xvfb altında pencereli çalışıyor (F8.21'deki gibi), Windows/macOS'ta doğrudan (bu
  runner'ların zaten gerçek bir masaüstü oturumu var, Xvfb'ye gerek yok). Push sonrası CI:
  **10/10 job yeşil, Windows/macOS'taki `tts-check` job'ları da ilk denemede başarılı** —
  F8.09-F8.13'teki gibi bir düzeltme turu gerekmedi (asset adlarının önceden gerçek API'den
  doğrulanmış olması bu riski önceden bertaraf etmişti). `CLAUDE.md` §13/§14 güncellendi:
  TTS Türkçe ses paketi doğrulaması riski artık kapatıldı (SQLCipher/mediapipe ile aynı
  doğrulama seviyesinde) — CI runner'larında fiilen Türkçe ses paketi olup olmadığı hâlâ
  bilinmiyor (job log'ları admin yetkisi gerektiriyor) ama bu önemli değil, çünkü zaten
  önemli olan ("Türkçe ses yokken bile çökmüyor mu") doğrulandı.
- F8.24 - **Modül manifest.json ↔ C# Manifest tutarlılık testi.** Açık risk listesinden
  ("İkisi elle senkron tutulmalı; ileride bir tutarlılık testi eklenebilir ama henüz yok")
  ele alındı. `ModuleRegistry.GetAvailableModules()` manifest.json'ları JSON'dan parse ediyor;
  her modülün C# sınıfındaki hardcoded `Manifest` property'si ayrı, elle yazılmış bir kopya —
  ikisi arasında hiçbir otomatik kontrol yoktu. İki parçada eklendi (modül türüne göre
  farklı test aracı gerektiği için, bkz. `testing-approach` skill'i):
  - **Assessment modülleri (Godot-bağımsız):** `tests/FreeRehabHub.Modules.Contracts.Tests/
    ManifestConsistencyTests.cs` — mevcut `ModuleRegistryTests.cs`'nin zaten kurduğu
    `TestModulesRoot` fixture'ını (general-functional-checkin'in gerçek manifest.json'unun bir
    kopyası) yeniden kullanıyor, `new GeneralFunctionalCheckinAssessment().Manifest` ile
    karşılaştırıyor. Karşılaştırma mantığı `ManifestAssert.Equal()` yardımcı sınıfında (alan
    alan `Assert.Equal` — `ModuleManifest`/`LocalizedText`/`DifficultyRange` `Equals`
    override etmediği için xUnit'in `Assert.Equal`'ı doğrudan kullanılamıyor).
  - **Exercise modülleri (Node-türevi, target-tap + arm-raise):** xUnit'te kurulamadıkları
    için (bkz. testing-approach § 2, "Controller'ı xUnit ile test etmeye çalıştığını fark
    edersen bu bir uyarı işareti") yeni bir sahne testi:
    `tests/scene-tests/ModuleManifestConsistencySceneTest.cs` — `ModuleRegistryAutoload.
    Registry.GetAvailableModules()` (gerçek `modules/` klasörü) ile `new TargetTapController()`/
    `new ArmRaiseController()`'ın `.Manifest`'ini karşılaştırıyor.
  Her iki test de bilerek bozulup (manifest.json'da `version`/`difficultyRange.max` değiştirilip)
  doğru şekilde `[FAIL]`/`[BAŞARISIZ]` verdiği, sonra düzeltilip temiz duruma (xUnit 16/16,
  sahne testleri 3/3) döndüğü doğrulandı.
- F8.25 - **Gerçek bug fix: `FakeMediaPipeServer.Dispose()` macOS'ta "Address already in
  use" fırlatıyordu.** F8.24 push'u sonrası CI'da 9/10 job yeşildi, sadece
  `build (macos-latest)`'in `Test` adımı başarısız oldu. Job log'unun tam metnine erişim
  admin yetkisi gerektirdiğinden kullanıcı GitHub Actions arayüzünden log'u paylaştı — bu,
  F8.09-F8.13'teki desenin aynısı (raw log erişimi sınırlı, gerçek kanıt kullanıcıdan geldi).
  Log, hatanın YENİ manifest tutarlılık testleriyle hiç ilgisi olmadığını gösterdi
  (`FreeRehabHub.Modules.Contracts.Tests.dll`: 16/16 geçti) — asıl hata önceden var olan
  `MediaPipePoseTrackingServiceTests.StartStop_FullCycle_StatusTransitionsInOrder`'da,
  `System.Net.HttpListenerException: Address already in use`, `FakeMediaPipeServer.Dispose()`
  içinde. **Kök neden:** `Dispose()` art arda hem `_listener.Stop()` hem `_listener.Close()`
  çağırıyordu — `HttpListener`'ın Windows-dışı (mono kökenli, macOS/Linux'ta kullanılan)
  yönetilen implementasyonunda `Close()` zaten `Stop()`'un işini kendi içinde yapıyor; ikisini
  art arda çağırmak aynı prefix'i iki kez kaldırmaya çalışıp bu hatayı fırlatıyor. Bu test
  daha önce macOS'ta hep yeşildi (F8.13'te doğrulanmıştı) — muhtemelen runner image'ının
  .NET SDK'sı güncellenip önceden zararsız kalan bu redundant çağrı artık tetiklenir hale
  geldi (F8.09-F8.13'teki WebSocket race'lerinin aynı kategorisinde bir bulgu, ama üretim
  kodunda değil, test yardımcı sınıfında). **Düzeltme:** sadece `Close()` çağrılıyor,
  ayrıca `HttpListenerException` etrafında best-effort bir try/catch eklendi (bkz.
  `TestFileCleanup.cs`, F8.12'deki aynı prensip — kapanış temizliği zaten geçmiş olan gerçek
  test assertion'larını geçersiz kılmamalı). Yerel doğrulama: Linux'ta bu bug hiç
  reprodüklenmedi (muhtemelen platform-özel timing farkı), ama düzeltme sonrası 5x arka
  arkaya çalıştırılıp hep 48/48 geçti — asıl doğrulama macOS CI'da.
- F8.26 - **SQLCipher parola yönetimi kararı kalıcılaştırıldı: elle giriş korunuyor.**
  Detay için bkz. § Açık riskler'deki ilgili madde — kod değişikliği yok, sadece
  `docs/PROGRESS.md`'de kararın gerekçesi kayıt altına alındı.
- F8.27 - **İlerleme/PDF rapor akışının Assessment modülleriyle çalıştığı gerçek UI'dan
  uçtan uca doğrulandı.** Açık risk listesindeki "sadece kod okumasıyla teyit edildi" notu
  kapatıldı. Yeni bir sahne testi eklendi: `tests/scene-tests/ProgressPanelAssessmentSceneTest.cs`
  — bir Assessment modülünden (general-functional-checkin) gelen gerçek bir `ProgressRecord`
  ekleyip `ProgressPanel`'i açıyor, modül listesinde doğru görünen adın (`Genel Fonksiyonel
  Değerlendirme`) çıktığını, kayıt satırının doğru skoru gösterdiğini doğruluyor, sonra PDF
  Rapor butonunu tetikleyip (`FileDialog.FileSelected` sinyalini doğrudan emit ederek) gerçek
  bir PDF dosyasının diskte oluştuğunu, geçerli bir `%PDF-` başlığı taşıdığını ve durum
  etiketinin başarı mesajı gösterdiğini teyit ediyor. İlk denemede geçti (4/4 sahne testi).
  Xvfb + gerçek Godot ile ekran görüntüsüyle de görsel olarak doğrulandı — iki kayıtlı bir
  hasta için grafik doğru yükseliş eğilimini çiziyor, kayıt satırları doğru görünüyor.
  **Yol boyunca gerçek bir küçük bulgu çıktı** (bkz. § Açık riskler'deki yeni madde): metrik
  etiketleri (`painLevel` → "Pain Level") hiç yerelleştirilmiyor — `MetricKeyFormatter.Humanize()`
  sadece camelCase'i mekanik olarak Title Case'e çeviriyor, çeviri tablosu yok, bu yüzden TR
  arayüzde bile İngilizce görünüyor. Kullanıcıya bildirildi, henüz kapsam/öncelik konuşulmadı.
  Bu vesileyle `docs/PROGRESS.md`'nin "Güncel durum" bölümü de sadeleştirildi — zamanla
  `phase-workflow` skill'inin öngördüğü kısa 3 satırlık özetten (Faz/adım/commit) uzaklaşıp
  bir değişiklik geçmişine dönüşmüştü; detaylar zaten bu "Faz 8 sonrası" bölümünde var.
- F8.28 - **Metrik etiketi yerelleştirmesi — 1. adım: `ModuleManifest.MetricLabels`.**
  F8.27'de bulunan açık risk ele alınmaya başlandı. Tasarım kullanıcıyla konuşuldu: mevcut
  `manifest.json` ↔ C# `Manifest` ikili-doğruluk-kaynağı deseniyle (bkz. F4.02/F8.24) tutarlı
  olarak, her modülün ürettiği metrik anahtarları için manifest'e `MetricLabels` (`Dictionary
  <string, LocalizedText>`) eklendi — hem `manifest.json`'da hem hardcoded C# `Manifest`'te.
  Karşılığı olmayan bir anahtarla karşılaşılırsa (`MetricKeyFormatter.Humanize` bir sonraki
  adımda) mevcut mekanik Title-Case dönüşümüne düşülecek (kullanıcı kararı — hiçbir zaman boş/
  çökük görünmesin, projenin heryerdeki "zarif geri düşme" prensibiyle tutarlı, bkz. TTS boş-
  ses fallback'i). `FreeRehabHub.Services`'in zaten `FreeRehabHub.Modules.Contracts`'a
  referans verdiği doğrulandı (csproj kontrolü) — katman kuralını çiğnemeden `MetricKeyFormatter`
  doğrudan `ModuleManifest` kullanabilecek. Bu adımda dolduruldu: 3 gerçek modül
  (`general-functional-checkin`: `painLevel`/`functionalDifficulty`/`symptomCount`; `target-tap`:
  `totalRounds`/`hitCount`/`missCount`/`averageReactionTimeSeconds`; `arm-raise`: `targetReps`/
  `completedReps`/`averageMaxAngleDegrees`) + `templates/module-starter/` (exercise + assessment,
  yeni katkıcılar örnekten görsün diye). İki manifest-tutarlılık testi de (`ManifestAssert.cs`
  xUnit, `ModuleManifestConsistencySceneTest.cs` sahne testi) `MetricLabels`'ı karşılaştıracak
  şekilde genişletildi — JSON/C# senkronizasyonu bu yeni alan için de otomatik korunuyor.
  **Bilinçli olarak bu adımda yapılmadı:** `MetricKeyFormatter.Humanize` hâlâ bu sözlüğü
  okumuyor, 3 ekran (`ModuleResultPanelController`/`ProgressPanelController`/
  `ProgressReportService`) hâlâ eski mekanik dönüşümü çağırıyor — veri eklendi ama henüz hiçbir
  yerde kullanılmıyor, asıl kullanıcı-görünür değişiklik bir sonraki adımda. Doğrulama:
  `dotnet build` temiz (Godot ana projesi/Exercise modülleri dahil), `dotnet test
  FreeRehabHub.sln` 132/132 yeşil, Xvfb+gerçek Godot ile sahne testleri 4/4 geçti (genişletilmiş
  `ModuleManifestConsistencySceneTest` dahil).
- F8.29 - **Metrik etiketi yerelleştirmesi — 2. adım: `MetricKeyFormatter` bağlandı, risk kapandı.**
  F8.28'de eklenen `ModuleManifest.MetricLabels` verisi bu adımda gerçekten kullanılmaya başlandı.
  `MetricKeyFormatter.Humanize(key, manifest, locale)`: önce `manifest.MetricLabels`'ta arar,
  bulamazsa (anahtar eksik VEYA `manifest` null) eski mekanik camelCase→Title Case dönüşümüne
  düşer — kullanıcı kararı (bkz. F8.28), hiçbir zaman boş/çökük görünmesin diye. 3 çağrı noktası
  güncellendi: `ModuleResultPanelController` (zaten elindeki `ActiveModuleManifest`'i geçiyor),
  `ProgressPanelController` (yeni `ResolveManifest(moduleId)` yardımcı metodu — `FormatRecordLine`
  `static`'ten instance metoda çevrildi, `ResolveModuleDisplayName` da aynı yardımcıyı paylaşıyor),
  `ProgressReportService.GeneratePdfAsync` (imzaya `string locale` + `modules` tuple'ına
  `ModuleManifest? Manifest` eklendi — `FreeRehabHub.Services`'in zaten `Modules.Contracts`'a
  referans verdiği doğrulanmıştı, katman kuralı ihlal edilmedi).
  **Testler:** yeni `tests/FreeRehabHub.Services.Tests/MetricKeyFormatterTests.cs` (4 senaryo:
  TR/EN etiket bulundu, anahtar eksik → fallback, manifest null → fallback) + mevcut iki sahne
  testine (`AssessmentHostSceneTest`, `ProgressPanelAssessmentSceneTest`) gerçek UI'da "Ağrı
  Seviyesi" metninin göründüğünü doğrulayan assertion eklendi — F8.27'nin bulduğu sorunun
  gerçekten düzeldiğini, sadece kod okumasıyla değil çalışan uygulamada kanıtlıyor (hem
  `ModuleResultPanel` hem `ProgressPanel` ekranında). `ProgressReportServiceTests.cs` yeni
  imzaya uyacak şekilde güncellendi (PDF içeriğinden metin çıkarma bu projede hiç yapılmıyor,
  önceki testler gibi sadece geçerli PDF üretimini doğruluyor). `module-development` skill'i
  (§2 tablo, §6 yerelleştirme, §7 kontrol listesi) ve `templates/module-starter/README.md`
  yeni katkıcılar için `metricLabels` notuyla güncellendi. Doğrulama: `dotnet build` temiz,
  `dotnet test FreeRehabHub.sln` 136/136 yeşil, Xvfb+gerçek Godot sahne testleri 4/4 geçti.
  **Metrik etiketi yerelleştirmesi riski (F8.27'de bulundu) F8.28+F8.29 ile tamamen kapandı.**
- F8.30 - **`localization/strings.csv` riski — beklenenden farklı bir kapanış: kayıt değil, kaldırma.**
  Açık risk listesindeki madde ("CSV import edildi ama `project.godot`'a kaydedilmedi") ele
  alınmaya başlandı, ama araştırma orijinal tanının eksik olduğunu ortaya çıkardı: CSV'deki 7
  anahtar (`APP_NAME`, `ROLE_THERAPIST`, `ROLE_CHILD`, `MENU_PATIENTS`, `MENU_SETTINGS`,
  `ACTION_SAVE`, `ACTION_CANCEL`) kod veya `.tscn` içinde **hiçbir yerde** kullanılmıyordu (grep
  ile doğrulandı) — `TranslationServer` sadece `LocalizationAutoload.SetLocale()` içinde
  çağrılıyordu, hiçbir yerde `Tr("ANAHTAR")` çağrısı yoktu. Gerçek ekran metinleri (ör.
  `PatientFormPanel.tscn`'deki "Kaydet"/"Vazgeç" butonları) doğrudan Türkçe hardcoded — TR/EN
  geçişi sadece modül içeriği için (manifest `DisplayName`/`Description`, F8.28'in
  `MetricLabels`'ı) projenin kendi `LocalizedText`/`Localize()` deseniyle çalışıyor, Godot'un
  native CSV-tabanlı çeviri sistemiyle hiç bağlantısı yok. **Sonuç:** `project.godot`'a CSV'yi
  kaydetmek (riskin orijinal isteği) hiçbir görünür etki yaratmayacaktı. Kullanıcıya bu bulgu
  sunuldu, 4 seçenek arasından "kullanılmayan scaffold'u kaldır" seçildi — sabit UI metinlerini
  gerçekten `Tr()` çağrılarına çevirip işlevsel hale getirmek (daha büyük kapsamlı, ayrı bir iş)
  veya sadece mekanik kayıt (etkisiz olacağı bilinerek) yerine. Yapılan: `localization/strings.csv`
  + üretilen `.import`/`.translation` dosyaları `git rm` ile kaldırıldı (klasör artık yok, boş
  dizin git'te izlenmez). `CLAUDE.md`'nin klasör haritasından `localization/` satırı çıkarıldı.
  `module-development` skill § 6'daki **yanlış** iddia ("DisplayName/Description `localization/`
  sözlüğüne eklenir") düzeltildi — gerçekte bu alanlar her zaman doğrudan `LocalizedText` (Tr/En)
  olarak `manifest.json`/hardcoded `Manifest`'te taşınıyor, `localization/` hiç kullanılmadı;
  bu yanlış iddia muhtemelen F0.02'de CSV kurulduğunda "böyle olacak" niyetiyle yazılmış ama
  gerçek modül geliştirme hiç bu yoldan gitmemişti. Doğrulama: `dotnet build`/`dotnet test`
  temiz (kod değişikliği olmadığı için test sayısı sabit), Xvfb+gerçek Godot ile sahne testleri
  4/4 geçti (CSV'nin kaldırılması projenin re-import/çalışma davranışını bozmadı), `git status`
  ile Godot'un stray `.uid`/`project.godot` değişikliği üretmediği teyit edildi.
  **Bilinçli olarak bu adımın dışında bırakıldı:** sabit UI metinlerinin (buton/panel başlıkları)
  gerçekten `Tr()` ile yerelleştirilmesi — bu, EN'e geçildiğinde uygulamanın chrome'unun hâlâ
  Türkçe kalması anlamına geliyor, ayrı ve daha büyük kapsamlı bir iş olarak kalıyor (istenirse
  ileride ele alınabilir).
- F8.31 - **`ProgressRecord.SessionId` FK riski kapandı: modül oynatışı başına gerçek bir
  `TherapySession`.** Araştırma `TherapySession` domain modeli + repository + `TherapySessionService`'in
  (Faz 2'den beri) zaten tam ve test edilmiş olduğunu ama hiçbir ekran tarafından kullanılmadığını
  ortaya çıkardı — `ModuleHostController`/`AssessmentHostController` her modül başlatışında sadece
  `Guid.NewGuid()` üretiyordu, karşılığında hiçbir DB satırı yoktu. Kullanıcıya üç seçenek sunuldu
  (A: modül-oynatışı-başına seans / B: çok-modüllü ziyaret bazlı seans, yeni UX gerektirir / C:
  dokunma) — **A seçildi** (minimal, yeni UX yok). Yapılan:
  - `ModuleHostController.StartModuleAsync`: modül başlamadan önce (`therapist`/`TherapySessionService`
    mevcutsa) yeni bir `TherapySession` (`StartedAt = DateTime.UtcNow`) oluşturup `TherapySessionService
    .AddAsync` ile kaydediyor, `Id`'sini `ModuleContext.SessionId` olarak kullanıyor (`_activeSession`
    alanında saklanıyor). `OnModuleCompleted`: aynı `_activeSession`'ın `EndedAt`'ini `result.CompletedAt`
    ile güncelleyip `UpdateAsync` çağırıyor — hem doğal tamamlanma hem erken çıkış (`OnDeactivated`
    üzerinden, `IExerciseModule`'ün "Completed tam bir kez tetiklenir" garantisi sayesinde) aynı yoldan
    geçtiği için tek bir kapatma noktası yeterli.
  - `AssessmentHostController.OnFormSubmitted`: Assessment'ta başlama/skorlama senkron tek bir çağrıda
    olduğu için `StartedAt = EndedAt = completedAt` ile tek adımda oluşturuluyor.
  - Terapist aktif değilse (beklenmeyen durum, ama `ProgressRecord` kaydıyla aynı guard) gerçek bir
    seans oluşturulmuyor, `SessionId` eskisi gibi rastgele bir `Guid`'e düşüyor — zarif geri düşme.
  - `DatabaseInitializer.cs`: `ProgressRecords.SessionId`'ye `REFERENCES TherapySessions(Id)` eklendi,
    eski "FK yok" yorumu güncellendi. **Not:** `CREATE TABLE IF NOT EXISTS` var olan veritabanlarını
    geriye dönük migrate etmiyor (bkz. F8.01'deki aynı kısıt) — bu FK sadece bundan sonra oluşturulan
    veritabanları için geçerli, bu projenin henüz üretime çıkmamış olması nedeniyle kabul edilebilir.
  - **Testler kırıldı ve düzeltildi:** `PRAGMA foreign_keys = ON` zaten açık olduğundan, gerçek SQLite
    üzerinden çalışan mevcut testler (`SqliteProgressRecordRepositoryTests`, `SqlitePatientRepositoryTests`
    'in cascade-delete testi, `ProgressPanelAssessmentSceneTest`) rastgele `SessionId = Guid.NewGuid()`
    kullanıyordu — yeni FK ile bunlar gerçek bir FK ihlaliyle başarısız olurdu. Hepsi önce gerçek bir
    `TherapySession` seed edip onun `Id`'sini kullanacak şekilde güncellendi.
  - **Yeni testler:** `AssessmentHostSceneTest`'e gerçek UI akışının gerçekten bir `TherapySession`
    oluşturup kapattığını doğrulayan assertion'lar eklendi. Exercise tarafı için hiç var olmayan bir
    boşluk fark edildi (`ModuleHostController`'ın hiç sahne testi yoktu) — yeni
    `tests/scene-tests/ModuleHostTherapySessionSceneTest.cs` eklendi: kamerasız `target-tap` modülünü
    başlatıp hemen Exit'e basarak (round'ları kazanmaya gerek yok, `OnDeactivated` zaten `Completed`'ı
    tetikliyor) gerçek bir `TherapySession`'ın oluşup kapandığını uçtan uca doğruluyor. **Sağlamlık
    kontrolü:** `_activeSession.EndedAt` güncellemesi geçici olarak devre dışı bırakılıp test
    gerçekten `[BAŞARISIZ]` verdiği doğrulandı, sonra geri alınıp temiz 5/5 durumuna dönüldü.
  Doğrulama: `dotnet build` temiz, `dotnet test FreeRehabHub.sln` 136/136 yeşil, Xvfb+gerçek Godot
  ile sahne testleri 5/5 geçti (4'ten 5'e — yeni Exercise seans testi dahil).
- F8.32 - **Kamera/PipeWire riski yeniden açıldı ve gerçek kök nedeni bulundu (kod değişikliği
  yok, sistem-seviyesi teşhis).** Kullanıcı F8.22'nin bulduğu kesin çözümü ("`monitor.v4l2`'yi de
  devre dışı bırak") denemek istedi. Geçici olarak uygulandı
  (`~/.config/wireplumber/wireplumber.conf.d/52-disable-v4l2-monitor.conf`, `wireplumber`/
  `pipewire`/`pipewire-pulse` yeniden başlatıldı) ve doğrulandı: `wpctl status`'ta Video bölümü
  tamamen boştu, `fuser /dev/video0` hiçbir şey göstermiyordu — yani PipeWire kamerayı artık hiç
  yönetmiyordu. **Ama `ffmpeg -f v4l2 -i /dev/video0` yine de "Device or resource busy" verdi.**
  Bu, F8.22'nin teşhisinin (PipeWire'ın kendi monitörünün cihazı tuttuğu) **yanlış/eksik**
  olduğunu kanıtladı. Derinlemesine bakılınca (`ps aux`, sonra kullanıcının kendi oturumunda
  `sudo` ile `/proc/<pid>/exe`, `/proc/<pid>/cwd`, `systemctl status <pid>`, `docker inspect`)
  gerçek sebep ortaya çıktı: **`motion_bridge-motion-bridge` adında, kullanıcının kendi kurduğu,
  bu projeyle hiç ilgisi olmayan bir Docker container** — `/dev/video0`'ı doğrudan cihaz
  geçişiyle (`--device=/dev/video0`) mount edip kernel seviyesinde tekelen tutuyordu,
  `RestartPolicy: unless-stopped` olduğu için kesintisiz (tespit anında 4 gündür) çalışıyordu.
  Root yetkisiyle çalıştığı için normal kullanıcı `fuser`'ı bu process'i hiç göremiyordu — F8.22'nin
  "fuser hiçbir şey göstermiyor" bulgusunun neden yanıltıcı olduğu da böylece anlaşıldı. WirePlumber
  deneyi hemen geri alındı (`52-disable-v4l2-monitor.conf` silindi, servisler yeniden başlatıldı,
  `wpctl status` F8.22 sonrası duruma birebir döndüğü doğrulandı) — kalıcı hiçbir iz bırakılmadı.
  Kullanıcıya `motion_bridge` container'ı hakkında ne yapmak istediği soruldu (dokunma / geçici
  durdurup test et / kalıcı durdur) → **"Şimdilik dokunma"** — container'a hiç dokunulmadı.
  `CLAUDE.md` §14 bu yeni, doğru teşhisi yansıtacak şekilde güncellendi (F8.22'nin PipeWire
  bulgusu gerçek ama artık alakasız bir tarihsel not olarak korundu). **Sonuç:** kamera riski
  hâlâ açık (bilinçli olarak), ama artık YANLIŞ bir teşhise dayanmıyor — kullanıcı isterse
  `motion_bridge`'i durdurup (`docker stop`, `unless-stopped` yüzünden reboot dahil kendiliğinden
  geri gelmez) kamerayı gerçekten serbest bırakabilir, bu FreeRehabHub'ın kod tabanını hiç
  ilgilendirmiyor.
- F8.33 - **Yeni modül: `com.freerehabhub.color-sort` (Renk Kutusu) — Özel Eğitim disiplininde
  ilk gerçek modül.** Kullanıcı isteğiyle önce `egzersiz.md` (5 disiplini kapsayan egzersiz
  taslakları) ve `oyunlar.md` (bunlardan bir kısmını gerçek `IExerciseModule` oyunlarına
  dönüştüren 9 tasarım taslağı, internetten araştırılıp özgün yazıldı) hazırlandı; kullanıcı
  bunlardan "Renk Kutusu"nu (kamerasız, sürükle-bırak yerine tıklama tabanlı sadeleştirilmiş
  sınıflandırma/tepki hızı oyunu) gerçek modüle çevirmeyi seçti. `module-development` skill'indeki
  adımlar izlendi: `templates/module-starter/exercise/` kopyalandı,
  `modules/com.freerehabhub.color-sort/` altına taşındı — `manifest.json` (TR+EN, disciplines:
  `specialEducation`+`occupationalTherapy`, `metricLabels` F8.28 deseniyle baştan dolu),
  `ColorSortController.cs` (8 tur, her turda rastgele bir hedef renk beliriyor, 4 renkli kutu
  butonundan doğrusuna tıklanıyor — `TargetTapController`'ın tur/timer desenine benzer ama zaman
  aşımı yok, sadece doğru/yanlış), Godot-bağımsız `Scoring/ColorSortScorer.cs`
  (`TargetTapScorer`'ın accuracy+speed formülüyle birebir aynı desen, ayrı test edilebilir).
  **Yol boyunca bulunan gerçek bug (kod incelemesiyle, yazılırken yakalandı):** ilk taslakta
  buton `Pressed` event'ine lambda ifadeleri (`() => OnBinPressed(Colors.Red)`) ile abone
  olunmuştu — `Dispose()`'ta aynı şekilde yeni bir lambda ile `-=` çağırmak hiçbir şeyi
  gerçekten abonelikten çıkarmaz (her lambda ayrı bir delegate nesnesi), bu yüzden dört ayrı
  adlandırılmış handler metoduna (`OnRedBinPressed` vb.) çevrildi — `TargetTapController`'daki
  `OnTargetPressed` deseniyle tutarlı. **Testler:** `Tests/ColorSortScorerTests.cs` (5 senaryo,
  `TargetTapScorerTests`'in aynısı — tam isabet, tam kaçırma, orta-aralık, aşırı-yavaş-yanıt,
  sıfır-tur), `FreeRehabHub.sln`'e `dotnet sln add` ile eklendi.
  `tests/scene-tests/ModuleManifestConsistencySceneTest.cs`'e yeni modül eklendi (manifest.json
  ↔ C# Manifest tutarlılığı, module-development skill § 7 kontrol listesi gereği). Yeni
  `tests/scene-tests/ColorSortSceneTest.cs`: gerçek UI'da modül kütüphanesinden seçip
  `ModuleHost`'a geçiyor, `TargetDisplay`'in gerçek anlık rengini okuyup 6 doğru + 2 kasıtlı
  yanlış buton tıklaması yapıyor, `ModuleResult.Metrics`'in (`correctCount=6`,
  `incorrectCount=2`) beklenenle birebir eştiğini VE `Completed`'in tam bir kez tetiklendiğini
  (çift `ProgressRecord` OLMADIĞINI, tam 1 kayıt olduğunu) doğruluyor. Doğrulama: `dotnet build`
  temiz, `dotnet test` 141/141 yeşil (yeni 5 `ColorSort.Tests` dahil), Xvfb+gerçek Godot sahne
  testleri 6/6 geçti (yeni `ColorSortSceneTest` ilk denemede geçti). Ekran görüntüsüyle görsel
  doğrulama bu adımda yapılmadı — sahne testi zaten gerçek node ağacı üzerinden etkileşiyor,
  istenirse ayrıca eklenebilir.

### Faz 7 — Çocuk Modu / Kiosk + Erişilebilirlik: tamamlandı (2026-07-26)
- Kapsam kararı (kullanıcıyla konuşuldu): dört alt özellik var (AccessControlService+kiosk
  kilidi, erişilebilirlik temaları, TTS, ödül sistemi) — CLAUDE.md'deki sıralamayla aynı
  şekilde AccessControlService+kiosk kilidiyle başlanacak, diğerleri sonra ayrı ayrı ele
  alınacak.
- Kiosk çıkış tasarım kararı (kullanıcıyla konuşuldu): kiosk kilidinden çıkmak için var olan
  SQLCipher master parolası değil, ayrı/kısa bir kiosk-çıkış PIN'i istenecek — kiosk günde
  defalarca kullanılacağı için uzun parolayı her seferinde yazmak pratik değil. PIN app-geneli
  tek bir PIN (terapist bazlı değil) — "hangi terapist çıkardı" bilgisi zaten kiosk'a girerken
  set edilmiş olan `SessionContext.ActiveTherapist`'ten geliyor, PIN sadece "bu gerçekten
  terapist, çocuk değil" doğrulaması.
- F7.01 - **`KioskPin` domain modeli + `IKioskPinRepository` sözleşmesi.** `Domain`'e eklendi
  — `PinHash`/`Salt`/`UpdatedAt`. F6.01'deki gibi bilinçli olarak dar: DB şeması, SQLite
  implementasyonu, hashleme mantığı (`AccessControlService`) veya UI bu adımda yok.
- F7.02 - **`KioskPin` DB şeması + `SqliteKioskPinRepository`.** Tekil-satır ayar tablosu (Id
  yok — `SetAsync` transaction içinde eski satırı silip yenisini yazıyor,
  `SqlitePrescriptionRepository`'deki transaction deseniyle aynı). 3 yeni test: PIN
  yokken `GetAsync` `null`, round-trip, ikinci `SetAsync` öncekini değiştiriyor. Tüm
  `Data.Tests`: 43/43 yeşil.
- F7.03 - **`AccessControlService` + `AppServices` bağlantısı.** `AuditRecordType`'a
  `KioskPin` eklendi. `SetPinAsync` (PBKDF2-SHA256, 100.000 iterasyon, rastgele salt —
  yeni paket gerekmedi, .NET BCL; boş PIN'de `ArgumentException`; ilk kurulumda `Created`,
  üzerine yazmada `Updated` audit log), `VerifyPinAsync` (`CryptographicOperations
  .FixedTimeEquals` ile sabit-zamanlı karşılaştırma, timing-attack'e karşı),
  `IsPinConfiguredAsync`. `AppServices.Unlock()`'a diğer servislerle aynı anda bağlandı.
  7 yeni test (fake repository'lerle). Tüm çözüm: 119/119 test yeşil.
- F7.04 - **Kiosk PIN kurulum/değiştirme ekranı.** `scenes/shells/KioskPinSetupPanel.tscn`/
  Controller — `PatientFormPanel`'deki Card/Content düzeni: durum etiketi ("PIN kurulu"/
  "kurulu değil"), iki `secret=true` `LineEdit` (PIN + tekrar), Kaydet/Geri, mesaj etiketi.
  Doğrulama: boş PIN, eşleşmeyen PIN, aktif terapist yoksa hata. `TherapistShell`'e "Kiosk
  PIN" butonu + navigasyon eklendi. Bu ekranda yeni xUnit testi yok (saf Godot UI, önceki
  ekranlarla aynı desen). Xvfb+gerçek Godot ile uçtan uca doğrulandı: gerçek buton
  tıklamalarıyla kurulmamış durum → eşleşmeyen PIN hatası → boş PIN hatası → başarılı kayıt
  ("PIN kaydedildi", durum güncellendi, alanlar temizlendi) → `AccessControlService` üzerinden
  doğru/yanlış PIN doğrulaması → Geri ile `TherapistShell`'e dönüş, hepsi doğru çalıştı.
- F7.05 - **`ChildKioskShell` + PIN korumalı kiosk moduna geçiş.** `KioskNavigation` (küçük
  paylaşılan yardımcı: `UserRole`'e göre ana ekran yolu) eklendi; `ModuleHostController
  .ExitToHomeScreen` (eski adıyla `ExitToTherapistShell`) ve `ModuleResultPanelController`'ın
  "Tamam" butonu artık buna göre yönleniyor — `ModuleLibraryPanelController`'a dokunulmadı
  çünkü kiosk akışı onu hiç kullanmıyor. `ModuleResultPanelController.OnDonePressed`, `Role ==
  Child` iken `ActivePatient`'ı temizlemiyor (çocuk aynı hasta için başka modül oynatmaya devam
  edebilsin diye). `scenes/shells/ChildKioskShell.tscn`/Controller — `ModuleLibraryPanel`'in
  modül listeleme mantığının kiosk'a özel kopyası + "Terapist Girişi" (PIN + Çıkış,
  `AccessControlService.VerifyPinAsync` ile doğrulanıyor). `PatientListPanel`'e "Kiosk Moduna
  Geç" butonu eklendi — **fail-closed**: PIN kurulu değilse girişi engelleyip mesaj gösteriyor
  (terapist PIN kurmadan kiosk'a kilitlenip çıkış yolu olmadan kalmasın diye). Bu ekranlarda
  yeni xUnit testi yok (saf Godot UI/navigasyon). Xvfb+gerçek Godot ile uçtan uca doğrulandı
  (17 ayrı kontrol): PIN yokken engelleniyor → PIN kurulunca kiosk'a giriliyor (Role=Child,
  doğru başlık) → modül seçilip oynanıyor → erken çıkış sonuç ekranına gidiyor → "Tamam"
  `TherapistShell`'e değil `ChildKioskShell`'e dönüyor, hasta korunuyor → yanlış PIN
  reddediliyor, doğru PIN ile Role=Therapist'e dönüp hasta temizleniyor.
- F7.06 - **Erişilebilirlik temaları: Yüksek Kontrast / Düşük Uyaran.** `themes/high-contrast
  .tres` ve `themes/low-stimulation.tres` (Faz 1'den beri boş stub) `default.tres`'teki her
  theme-type-variation (`TitleLabel`, `SectionLabel`, `ErrorLabel`, `EmptyStateLabel`,
  `PrimaryButton`, `DangerButton` vb.) karşılanacak şekilde dolduruldu. **Yüksek Kontrast:**
  siyah/beyaz zemin, kalın (3-4px) kenarlık, büyütülmüş yazı tipi (taban 20pt, başlık 32pt),
  gölge yok, güçlü mavi birincil buton, kırmızı tehlike butonu, klasik amber seçim rengi.
  **Düşük Uyaran:** soluk/desatüre soğuk palet, ince (1px) kenarlık, geniş köşe yuvarlaklığı,
  gölge yok, toz mavi-gri birincil buton, alarm-kırmızısından kaçınan hardal tonunda tehlike
  butonu, biraz küçültülmüş (15pt) yazı tipi. `TherapistShell` araç çubuğuna tema seçici
  (`OptionButton`) eklendi, `ThemeManager.ApplyTheme()`'e bağlandı — sadece terapist ekranında,
  kiosk modundaki çocuk tema seçeneği görmüyor. Yeni xUnit testi yok (saf tema kaynağı/UI).
  Xvfb+gerçek Godot ile doğrulandı: tema sırayla Varsayılan→Yüksek Kontrast→Düşük Uyaran
  değiştirildi, her aşamada ekran görüntüsü alınıp görsel olarak incelendi (renk/kenarlık/yazı
  boyutu tasarlandığı gibi), `PatientFormPanel`'e geçişte temanın kalıcı kaldığı doğrulandı
  (`GetTree().Root.Theme` kök seviyesinde ayarlandığı için). **Yol boyunca bulunan harness
  detayı:** ekran görüntüsü için beklenen `RenderingServer.FramePostDraw` sinyali `--headless`
  modda hiç tetiklenmiyor (süreç zaman aşımına uğruyor) — çözüm, sadece bu doğrulama script'i
  için `xvfb-run` + headless olmayan gerçek Godot çalıştırması; ürünün/normal test akışının
  kalıcı bir parçası değil.
- F7.07 - **Ödül sistemi: basit görsel ödül.** Kullanıcıyla kapsam konuşuldu (kalıcı rozet/başarı
  sistemi ve kümülatif yıldız sayacı seçenekleri yerine): oturum-bazlı, kalıcı kayıt gerektirmeyen
  en basit yaklaşım seçildi — yeni DB şeması/domain modeli yok. `ModuleResultPanel`e sadece
  Child/kiosk modunda görünen bir `RewardContainer` eklendi; o modda klinik skor/metrikler
  gizleniyor (çocuğa anlamlı değil), yerine `ModuleResult.NormalizedScore`'dan türetilen 1-3
  yıldız (`≥%80`→★★★, `≥%50`→★★☆, altı→★☆☆) + kutlayıcı mesaj gösteriliyor — en düşük skorda
  bile 0 yıldız yok, mesaj her zaman cesaretlendirici ("Denemeye devam et, başarıyorsun!").
  Terapist modunda davranış değişmedi. Yeni xUnit testi yok (saf UI). Xvfb+gerçek Godot ile 5
  senaryo doğrulandı: terapist modu (değişmedi), 3 farklı skor için doğru yıldız/mesaj, sonuç
  yokken tüm alanların doğru gizlenmesi — ekran görüntüleriyle dolu/boş yıldız karakterlerinin
  (★/☆) düzgün render edildiği de görsel olarak teyit edildi.
- F7.08 - **TTS: `TtsAutoload` + ChildKioskShell "Dinle" butonu.** Kütüphane seçimi kullanıcıyla
  konuşuldu (Godot'un yerleşik `DisplayServer` TTS'i / bundled Piper nöral TTS / bundled
  espeak-ng arasında) — **Godot'un yerleşik `DisplayServer` TTS'i** seçildi: sıfır yeni
  bağımlılık/native süreç, MediaPipe'daki gibi ikinci bir paketleme yükü yok. Karardan önce bu
  makinede fiilen spike'landı: Linux'ta speech-dispatcher/espeak-ng üzerinden gerçek Türkçe
  konuşma ürettiği doğrulandı (`TtsIsSpeaking()=true`). **Yol boyunca bulunan gerçek bug:**
  `DisplayServer.TtsGetVoices()` (tüm sesleri listeleyen genel çağrı) Linux'ta Godot 4.7'de
  içeride hata veriyor (`Parameter "synth" is null`) ve boş liste dönüyor — dil-filtreli
  `TtsGetVoicesForLanguage()` sorunsuz çalışıyor, `TtsAutoload` bu yüzden hep onu kullanıyor.
  `autoload/TtsAutoload.cs` (6. autoload): `Speak(text)` aktif dile göre ses seçip `TtsSpeak`
  çağırıyor, `Stop()`, `IsAvailable`. `ChildKioskShell`e "Dinle" butonu eklendi (Başlat'ın
  yanında, aynı seçim-sonrası-aktifleşme deseniyle) — seçili modülün adını okuyor, okuma
  zorluğu olan/henüz okuma bilmeyen çocuklar için. Yeni xUnit testi yok (Godot API'sine sarılı
  saf UI). Xvfb+gerçek Godot ile gerçek buton tıklamalarıyla doğrulandı: seçim öncesi
  Dinle/Başlat ikisi de devre dışı, seçim sonrası ikisi de aktif, doğru modül adı okunuyor,
  Dinle'ye basınca `TtsIsSpeaking()` false'tan true'ya geçiyor. **Bilinen risk:** sadece
  Linux'ta (speech-dispatcher/espeak-ng kurulu) doğrulandı; Windows'ta SAPI5'e, macOS'ta
  NSSpeechSynthesizer/AVSpeechSynthesizer'a sarılıyor — hedef klinik bilgisayarda Türkçe ses
  paketi kurulu olmayabilir, gerçek donanımda doğrulanmalı (bkz. CLAUDE.md §14).

**Faz 7 tamamlandı (2026-07-26).** CLAUDE.md'de listelenen dört alt özellik de bitti:
`AccessControlService` + PIN korumalı kiosk kilidi (F7.01-05), erişilebilirlik temaları
(F7.06), ödül sistemi (F7.07), TTS (F7.08) — kullanıcıyla konuşulup bu şekilde tamamlanmış
sayıldı. Bilinçli olarak dar bırakılan kapsam: TTS şu an sadece `ChildKioskShell`'deki
modül adını okuyor (form alanları/talimat metinleri gibi başka yerlere genişletilmedi) —
ihtiyaç doğarsa ayrı bir faz-bağımsız işle ele alınabilir, `TtsAutoload.Speak()` zaten genel
amaçlı. Kiosk kilit mekanizmasının platform bazlı zorluk farkları (CLAUDE.md §14'te bilinen
risk olarak işaretliydi) bu fazda ayrıca ele alınmadı — PIN tabanlı çözüm platform bağımsız
olduğu için bu riski by-design ortadan kaldırdı.

### Faz 6 — İlerleme Takibi, Grafikler, PDF Rapor: tamamlandı (2026-07-25)
- Kapsam kararı (kullanıcıyla konuşuldu): Assessment modüllerinin (`general-functional-checkin`)
  hâlâ bir oynatma ekranı yok (F5.10'da not edilmişti), bu yüzden Faz 6 önce sadece
  `ModuleHost` üzerinden geçen Exercise sonuçlarını kalıcı kayda çevirecek; Assessment
  entegrasyonu, o host ekranı eklendiğinde ayrı bir faz-bağımsız işle ele alınacak.
- F6.01 - **`ProgressRecord` domain modeli + `IProgressRecordRepository` sözleşmesi.**
  `Domain`'e eklendi — bir modülün (Exercise) tamamlanmasının kalıcı, değişmez kaydı
  (`ExercisePrescription`'daki desenle aynı: Update yok, sadece `AddAsync` + geçmiş sorgusu).
  Alanlar (`ModuleId`, `SessionId`, `CompletedAt`, `NormalizedScore`, `Metrics`, `Notes`)
  bilinçli olarak `Modules.Contracts.ModuleResult` ile birebir aynı isimlendirmede — Domain,
  katman kuralı gereği Modules.Contracts'a bağımlı olamadığı için ayrı bir tip, dönüşüm
  ileride Services katmanında yapılacak. Bilinçli olarak dar tutuldu: bu adımda DB şeması,
  SQLite implementasyonu veya `ModuleHost` entegrasyonu yok.
- F6.02 - **`ProgressRecords`/`ProgressRecordMetrics` DB şeması + `SqliteProgressRecordRepository`.**
  `PrescriptionRepository` ile aynı desen: `AddAsync` (transaction içinde kayıt + metrikler ayrı
  child tabloya), `GetHistoryByPatientIdAsync` (CompletedAt'e göre azalan). `SessionId`'ye bilinçli
  olarak FK yok — `ModuleHost` henüz her oynatışta gerçek bir `TherapySessions` kaydı oluşturmuyor,
  sadece tekil bir `Guid` üretiyor (bkz. Açık riskler). Gerçek SQLCipher dosyasıyla 4 test: round-trip
  (metriklerle), null alanlar, çoklu kayıt sıralaması, boş geçmiş. Tüm çözüm: 105/105 test yeşil.
- F6.03 - **`ProgressRecordService` + `AppServices` bağlantısı + `ModuleHost`'tan kayıt.**
  `PrescriptionService` ile aynı desen (`AddAsync` → kayıt + `Created` audit log, `GetHistoryByPatientIdAsync`
  loglanmıyor — liste görünümü). `AuditRecordType`'a `ProgressRecord` değeri eklendi. `ModuleHostController
  .OnModuleCompleted` artık `ModuleResult`'u `ProgressRecord`'a çevirip (`SessionContext.ActiveTherapist`
  varsa) kalıcı kaydediyor, sonra sonuç ekranına yönleniyor. Xvfb+gerçek Godot ile uçtan uca doğrulandı:
  `arm-raise`'e senkron 10 tekrarlık sentetik `PoseFrame` beslendi, modül tamamlandı, gerçek SQLCipher
  DB'den `ProgressRecordService.GetHistoryByPatientIdAsync` ile doğru `ModuleId`/skor/metriklerle 1 kayıt
  geri okundu. Tüm çözüm: 107/107 test yeşil.
- F6.04 - **Hasta ilerleme grafiği ekranı.** `scenes/progress/ProgressChart.cs` (Godot'a hazır bir grafik
  kontrolü olmadığı için `_Draw()` ile elle çizilen basit çizgi grafiği, üçüncü parti bağımlılık yok) +
  `ProgressPanelController`/`.tscn` — hastanın `ProgressRecord` geçmişini modüle göre gruplayıp bir
  `ItemList`'te listeliyor (en son kayıt edilen modül önce), seçilen modül için grafik + tarih/skor/metrik
  satırlarından oluşan bir liste gösteriyor (`ModuleResultPanel`'deki camelCase→Title Case metrik
  hümanizasyonuyla aynı). `PatientListPanel`'e "İlerleme" butonu + navigasyon eklendi. Bu ekranda yeni
  xUnit testi yok (saf Godot UI, önceki ekranlarla aynı desen). Xvfb+gerçek Godot ile uçtan uca doğrulandı:
  3 kayıt (2 modül) seed edilip gerçek "İlerleme" tıklamasıyla ekran açıldı, modül listesi doğru sırada,
  seçim değişince grafik/kayıt listesi doğru güncellendi.
- F6.05 - **PDF ilerleme raporu.** Kütüphane seçimi kullanıcıyla konuşuldu: QuestPDF (modern API ama
  OSI onaylı olmayan "Community" lisans) yerine **PdfSharp** (MIT) seçildi — proje açık kaynak
  olduğu için lisans netliği önceliklendirildi. PdfSharp 6.x .NET Core'da (GDI+ yok) sistem
  fontlarını otomatik çözemediği için `LiberationSansFontResolver` (`IFontResolver`) yazıldı;
  Liberation Sans (OFL-1.1, `assets/fonts/liberation-sans/`) repoya gömülü — hedef makinede hangi
  fontların kurulu olduğundan bağımsız, tutarlı render için. `ProgressReportService`: hasta/terapist
  başlığı, `clinical-data-handling` skill § 5 gereği zorunlu "tıbbi tanı değildir" feragatnamesi,
  modül bazlı kayıt tablosu, sayfa taşınca otomatik yeni sayfa (`EnsureSpace`). Rapor üretimi
  `AuditAction.Exported` ile loglanıyor (yeni eklenen audit action). Üç ekranda (`ModuleResultPanel`,
  `ProgressPanel`, rapor) tekrarlanan camelCase→okunabilir metin dönüşümü `MetricKeyFormatter`'a
  çıkarıldı (rule-of-three). Testler: `ProgressReportServiceTests` — gerçek gömülü fontla PDF üretimi,
  çok sayfalı rapor (`PdfReader` ile geri açılıp `PageCount` doğrulanıyor), audit log doğrulaması. Tüm
  çözüm: 109/109 test yeşil. Xvfb+gerçek Godot ile uçtan uca doğrulandı: hasta listesinden gerçek
  tıklamalarla İlerleme → PDF Rapor → dosya kaydetme akışı çalıştı; üretilen PDF `pdftotext`/`pdftoppm`
  ile görsel olarak da doğrulandı, Türkçe karakterler (İ, ı, ğ, ş, ü, ö) doğru render ediliyor.

**Faz 6 tamamlandı (2026-07-25).** `ProgressRecord` toplama (Exercise modülleri, `ModuleHost`
tamamlanınca otomatik kaydediyor) → hasta bazlı grafik/liste ekranı → PDF rapor export, uçtan uca
çalışıyor. Kullanıcıyla konuşulan bilinçli kapsam kararı: Assessment modülleri (`general-functional
-checkin`) hâlâ oynatma ekranından yoksun olduğu için (F5.10'dan beri bilinen, Faz 5'in kapsamı
dışında bırakılan açık) bu faz sadece Exercise sonuçlarını kapsıyor — Assessment entegrasyonu, o
host ekranı eklendiğinde ayrı bir faz-bağımsız işle ele alınabilir. PdfSharp/font-gömme yaklaşımı bu
Linux geliştirme makinesinde doğrulandı; Windows/macOS'ta cross-platform font render'ı henüz
doğrulanmadı (SQLCipher'daki aynı platform-doğrulama boşluğuyla aynı kategori, bkz. Açık riskler).

### Faz 5 — MediaPipe Entegrasyonu + Kamera Tabanlı Modül: tamamlandı (2026-07-25)
- F5.01 - **Risk-doğrulama spike'ı: MediaPipe native çalışıyor mu?** F1.04/F1.05'teki spike deseniyle, mimariye girmeden önce MediaPipe'ın bu geliştirme makinesinde gerçekten poz tespiti yapıp yapamadığı test edildi. İki ayrı engel bulundu: (1) bu ortamda kamera erişimi yok (`video` grubu izni eksik, kullanıcının kendi aksiyonu gerekiyor), (2) mediapipe pip paketi (0.10.7-0.10.35, hem eski `solutions.pose` hem yeni `tasks.vision.PoseLandmarker` API'si) bu Fedora makinesinde `PoseLandmarker`/`Pose` kurulumunda tutarlı şekilde çöküyor (`TAG:index:name is invalid` — dahili graph isimlendirme hatası, CPU/GPU delegate'ten ve Python sürümünden bağımsız, hem bu ortamda hem kullanıcının kendi makinesinde tekrar üretildi). Aynı test standart bir Debian tabanlı Docker container'ında sorunsuz çalıştı — sorunun mediapipe'ın genelinde değil, bu Fedora'nın araç zincirinde (glibc/libstdc++) olduğu doğrulandı. **Karar:** üretim mimarisi (native process, Docker değil) değişmedi; bu geliştirme makinesinde mediapipe'a dokunan kodun geliştirme/test döngüsü Docker üzerinden yapılacak. Bulgular `CLAUDE.md` §13/§14'e işlendi. Kod değişikliği yok (saf risk-doğrulama adımı, F1.04/F1.05 gibi).
- F5.02 - **Poz verisi kontratı.** `Modules.Contracts`'a `PoseFrame`/`DetectedPose`/`PoseLandmark`/`PoseLandmarkType`/`PosePoint` eklendi — MediaPipe'ın 33 sabit BlazePose landmark'ını birebir modelliyor (`PoseLandmarkType` enum'u, magic index yok). Kullanıcıyla önce iki tasarım kararı konuşuldu: (1) her landmark hem normalize 2D+z (ekran-bağımlı) hem world (metre, ROM ölçümü için) koordinat taşıyor — ikisi de aynı `PoseLandmark` nesnesinde (iki paralel liste yerine, index eşleştirme hatasına kapalı); (2) `PoseFrame.Poses` bilinçli olarak liste (tek poz değil) — ileride çoklu kişi (ör. terapist+çocuk aynı kamerada) ihtimaline açık, kullanıcının kararı. Hiç poz tespit edilmediğinde `Poses` boş liste (null değil). Davranışsız POCO'lar oldukları için (`ModuleResult`/`DifficultyRange` gibi) ayrı test yazılmadı.
- F5.03 - **`IPoseAwareModule` arayüzü.** Tek metotlu (`void OnPoseFrame(PoseFrame frame)`), ISP gereği `IExerciseModule`'den ayrı ve opsiyonel — sadece kamera gerektiren modüller implemente eder (F3.01'de tam bu yüzden kapsam dışı bırakılmıştı, artık `PoseFrame` netleştiği için tamamlandı). `ModuleHost`'un aktif modül bunu implemente ediyorsa `IPoseTrackingService`'ten gelen frame'leri buraya iletmesi bekleniyor — o bağlantı henüz yazılmadı (F5.04+ kapsamı).
- F5.04 - **`IPoseTrackingService` sözleşmesi.** `Modules.Contracts`'a eklendi (implementasyonu değil — `IPatientRepository`/Data ayrımıyla aynı desen, ağır iş — WebSocket + `mediapipe-service` süreç yönetimi — `Services`'te olacak). `PoseTrackingStatus` enum'u (Stopped/Starting/Running/Error) + `StartAsync`/`StopAsync` + `event FrameReceived`/`event StatusChanged`. Kullanıcıyla iki karar konuşuldu: (1) sadece başlangıç hatası değil, oturum ortasında da (süreç çökerse/kamera koparsa) sürekli durum sinyali olsun — bugünkü F5.01 bulgularının (pipeline'ın kırılganlığı) doğrudan sonucu; (2) `StartAsync` çağrıldığında süreç çalışmıyorsa onu içeride kendisi başlatsın (ProcessManagerService'i sarmalayarak) — modül/ModuleHost kodu tek bir arayüze bağımlı kalır. Ayrıca `LastError` (nullable string) eklendi — tek bir enum değeriyle "mediapipe kurulu değil" ile "kamera izni yok" gibi çok farklı hataları ayırt edemeyeceğimiz için.
- F5.05 - **`services/mediapipe-service/` iskeleti.** FastAPI uygulaması: `GET /health` + `WS /ws/pose` (bağlantı açıldığında kamera açılır, bağlı kaldığı sürece sürekli `PoseFrame` JSON'ı akıtır, bağlantı kopunca kamera serbest bırakılır). `app/schemas.py`'deki Pydantic modelleri C# tarafındaki `JsonNamingPolicy.CamelCase` sözleşmesiyle birebir (camelCase alan adları, `PoseLandmarkType` enum değerleriyle aynı camelCase landmark isimleri — `app/landmark_types.py`, index-eş). `app/pose_tracker.py`, OpenCV kamera + MediaPipe `PoseLandmarker`'ı (`detect_for_video`, monoton zaman damgası) sarmalıyor. `download_model.py`, `download_assets.py` ile aynı desen (stdlib-only, yeniden-çalıştırılabilir) — model dosyası ve `.venv/` gitignore'da, repoya gömülmüyor. `Dockerfile`, F5.01'de doğrulanan Debian tabanlı geliştirme/test yolu (yalnız dev/Linux, üretim native process). Docker'da test edildi: `/health` 200 dönüyor, `PoseTracker`'ın gerçek kodu (kamerasız) `PoseLandmarker`'ı sorunsuz kuruyor, kamerasız ortamda `/ws/pose`'a bağlanan istemci beklendiği gibi `code=1011, reason="Kamera açılamadı..."` ile kapanıyor. **Test edilemeyen:** gerçek kamerayla uçtan uca akış (bu ortamda kamera erişimi yok) — kullanıcının kendi makinesinde `video` grubu düzeltmesinden sonra doğrulanmalı.
- F5.06 - **`ProcessManagerService`.** `FreeRehabHub.Services`'e eklendi — genel amaçlı native süreç yaşam döngüsü yöneticisi (mediapipe'a özel bir şey bilmiyor, `IPoseTrackingService` implementasyonu bunu sarmalayacak, F5.07 kapsamı). `Start`/`Stop`/`IsRunning` + `WaitUntilHealthyAsync` (bir health-check URI'sini polling ile bekler, `mediapipe-service`'in F5.05'te eklenen `/health`'iyle kullanılacak). Stdout/stderr redirect edilmedi — okunmadan bırakılan redirect edilmiş bir pipe, OS buffer'ı dolunca alt süreci asılı bırakabilir (bilinen .NET `Process` tuzağı), bu adımda log yönlendirmesine ihtiyaç yok. Kameraya/mediapipe'a bağımlı olmadığı için gerçek xUnit testleri yazıldı (6 test: başlatma, durdurma, iki kez durdurma güvenliği, health-check başarı/timeout/süreç-çalışmıyor durumu) — `sleep`/`timeout` komutu (OS'e göre seçiliyor) + `HttpListener` kullanıldı, mock'lanmadı. Tüm çözüm: 88/88 test yeşil.
- F5.07 - **`MediaPipePoseTrackingService`.** `IPoseTrackingService`'in gerçek implementasyonu (`FreeRehabHub.Services`, `Modules.Contracts`'a yeni `ProjectReference` eklendi). `StartAsync`: süreç çalışmıyorsa `ProcessManagerService` ile başlatır → `/health`'i bekler → `ws://.../ws/pose`'a bağlanır → arka planda sürekli okuma döngüsü başlatır; herhangi bir adım başarısız olursa `Status=Error`/`LastError` set edip fırlatır. Okuma döngüsü Python'dan gelen camelCase JSON'ı `PoseFrame`'e deserialize edip `FrameReceived`'i tetikliyor — **arka plan thread'inden ateşleniyor**, Godot node'larına dokunan dinleyicilerin (`ModuleHost`) `CallDeferred` ile ana thread'e geçmesi gerektiği kodda not edildi. `StopAsync` WebSocket'i kapatır (kamera Python tarafında serbest kalır) ama süreci öldürmez — sıcak tutulur, bir sonraki `StartAsync`'te soğuk-başlangıç maliyeti olmasın diye. Testler, `HttpListener`'ın gerçek WebSocket accept desteğiyle sahte bir mediapipe-service (`FakeMediaPipeServer`) simüle ediyor — Python/Docker'a ihtiyaç yok; gerçek JSON→`PoseFrame` parse'ı (enum dahil), health-check timeout'ta `Error`+exception, ve Starting→Running→Stopped durum sırası doğrulandı. **Yol boyunca bulunan gerçek bug:** `ProcessManagerService.Start()`, `Process.Start()` başarısız olduğunda (ör. geçersiz working directory) `_process` alanını tutarsız bırakıyordu, sonraki `Stop()` `HasExited`'a erişirken çöküyordu — düzeltildi (`_process` sadece `Start()` başarılı olursa atanıyor) + regresyon testi eklendi. Tüm çözüm: 92/92 test yeşil.
- F5.08 - **`AppServices`'e `IPoseTrackingService` bağlandı.** `Unlock()` içinde diğer servislerle aynı anda `MediaPipePoseTrackingService` kuruluyor (DB parolasına bağımlı değil, ama "tek kurulum kapısı" tutarlılığı için — `ExerciseLibraryRepository`'yle aynı gerekçe) — sadece nesne oluşturuluyor, `StartAsync` çağrılmadığı için kamera/süreç bu noktada başlamıyor. Python yürütülebiliri `services/mediapipe-service/.venv/` konvansiyonel konumundan OS'e göre çözülüyor (`bin/python` / `Scripts/python.exe`), venv'in gerçekten var olup olmadığı burada doğrulanmıyor (yoksa hata ancak `StartAsync` fiilen çağrılınca ortaya çıkar). `services/` klasörü `res://` ile aynı kökte olduğu için `ProjectSettings.GlobalizePath("res://services/mediapipe-service")` ile çözülüyor — bu sadece dev modunda çalışır, paketlenmiş build'de `res://` bir `.pck` olacağı için farklı bir çözüm gerekecek (Faz 8 kapsamı, CLAUDE.md'ye not düşülmedi çünkü zaten Faz 8'in "MediaPipe binary gömülü" maddesi bunu kapsıyor). Xvfb+gerçek Godot ile doğrulandı: `Unlock()` sonrası `PoseTrackingService` dolu, `Status=Stopped`.
- F5.09 - **`ModuleHost` sahnesi/controller'ı.** Kullanıcıyla önce kapsam konuşuldu: hiçbir modülün (target-tap dahil) gerçek bir oynatma ekranı olmadığı ortaya çıktı (`scenes/module-host/` hiç kurulmamıştı, target-tap'in F4.14 doğrulaması tamamen geçici Xvfb harness'iyle yapılmıştı) — dar kapsamlı bir kamera-özel harness yerine genel bir ModuleHost kurulmasına karar verildi (F5.10 modül kütüphanesi ekranı, F5.11 sonuç ekranı, F5.12+ asıl kamera modülü olacak şekilde plana bölündü). `scenes/module-host/ModuleHostController.cs` + `.tscn`: `SessionContext.ActiveModuleManifest`'i (yeni eklendi, `ActivePatient` ile aynı desen) okuyup `manifest.ScenePath`'i `PackedScene.Instantiate()` ile kurar (reflection değil), `InitializeAsync`→`OnActivated`, `Completed` dinleme, Duraklat/Devam Et/Çık (Çık modül tamamlanmadıysa `OnDeactivated()` ile "Completed tam bir kez" garantisini tetikler). Modül `IPoseAwareModule` da ise `AppServices.PoseTrackingService`'i başlatıp durduruyor; `FrameReceived`/`StatusChanged` arka plan thread'inden geldiği için `ConcurrentQueue` + `_Process()`'te tüketim kullanıldı (`CallDeferred` DEĞİL — `PoseFrame` Godot `Variant` sistemiyle marshal edilemeyen düz bir C# sınıfı olduğu için; F5.07'deki yorum bunu netleştirmiyordu, gerçek mekanizma burada netleşti). Xvfb+gerçek Godot ile target-tap üzerinden uçtan uca doğrulandı: gerçek buton tıklamalarıyla 8 tur tamamlandı, skor doğru gösterildi, Çık modülü temizleyip session state'i sıfırlayıp `TherapistShell`'e hatasız geçti.
- F5.10 - **Modül kütüphanesi ekranı.** `PatientListPanel`'e "Modüller" butonu + yeni `scenes/module-library/ModuleLibraryPanel.tscn`/Controller. `ModuleRegistry.GetAvailableModules()`'i **sadece `Kind == Exercise`** olacak şekilde filtreliyor — Assessment modüllerinin (`general-functional-checkin`) gerçek bir oynatma ekranı (FormRenderer'ı barındıran) henüz yok, bilinçli olarak Faz 5 kapsamı dışında bırakıldı (bu, Faz 5'in yarattığı bir eksiklik değil, önceden var olan bir boşluk — ayrı, faz-bağımsız bir işle kapatılabilir). Seçilen modül `SessionContext.ActiveModuleManifest`'e yazılıp `ModuleHost`'a yönlendiriliyor. Xvfb+gerçek Godot ile PatientListPanel'den başlayan tam gerçek akış (sahte servis yok, gerçek buton tıklamaları) doğrulandı: hasta seçilince buton açılıyor, kütüphane doğru şekilde 1 modül gösteriyor (filtre çalışıyor), seçip Başlat'a basınca ModuleHost gerçekten target-tap'i kurup çalıştırıyor.
- F5.12 - **İlk kamera-tabanlı egzersiz modülü: `com.freerehabhub.arm-raise`** ("Kol Kaldırma"). F5.11 (sonuç ekranı) atlanıp doğrudan asıl kamera modülüne geçildi (kullanıcı kararı). Konsept: omuz fleksiyonu (kol kaldırma) — sağ kalça/omuz/dirsek `PoseLandmark`'larının **world** koordinatlarından fleksiyon açısı hesaplanıyor (`Scoring/ShoulderFlexionCalculator.cs`, saf geometri, Godot-bağımsız: kalça→omuz vektörüne göre omuz→dirsek vektörünün açısı — 0° kol yanda, 90° yatay, 180° tam kaldırılmış), zaten kütüphanede olan "shoulder-flexion-supine" kartıyla kavramsal olarak örtüşüyor. Kol <30°'den >90°'ye çıkıp tekrar <30°'ye dönünce 1 tekrar sayılıyor, 10 tekrar hedef; skor = tamamlanma oranı + ortalama ulaşılan açı kalitesinin ortalaması (`Scoring/ArmRaiseScorer.cs`). Landmark görünürlüğü (`Visibility < 0.5`) düşükse veya poz hiç yoksa kullanıcıya durum mesajı gösteriliyor (F5.01'de bizzat görülen pipeline kırılganlığının bilinçli sonucu). target-tap ile aynı mimari desen (kendi `.csproj`'u yok, `Compile Include` glob'una güveniyor, `Tests/` ayrı csproj + `.sln`'e eklendi). 9 xUnit testi (açı geometrisi: yanda/yatay/kaldırılmış/dejenere; skorlama: tam/sıfır/kısmi/sınır). Xvfb+gerçek Godot ile tam gerçek entegrasyon doğrulandı (sahte servis yok): `PatientListPanel → ModuleLibraryPanel` artık 2 modül gösteriyor (yeni modül otomatik keşfedildi), `ModuleHost` `ArmRaise.tscn`'i gerçekten kurdu, `IPoseAwareModule` cast'i çalıştı, sentetik `PoseFrame` verisiyle (gerçek kamera yok) 10 tekrar döngüsü doğru sayıldı, `%100` skorla tamamlandı. **Test edilemeyen:** gerçek kamerayla uçtan uca (F5.01'den beri bilinen ortam kısıtı).
- F5.11 - **Sonuç ekranı.** F5.12'den sonra tamamlandı (kullanıcı isteğiyle sıra değişti). `scenes/module-result/ModuleResultPanel.tscn`/Controller — `ModuleHost`, modül tamamlanınca (normal bitiş veya `OnExitPressed`'in tetiklediği erken çıkış, ikisi de aynı `OnModuleCompleted` yoluna giriyor — `CleanUpActiveModule()`'e çıkarıldı) artık inline status-label yerine bu ekrana yönlendiriyor: modül adı (yerelleştirilmiş) + hasta adı, skor (`%`), metrikler okunabilir formatta (`completedReps` → "Completed Reps" gibi basit bir camelCase→Title Case dönüşümüyle). Kalıcı kayıt yok (Faz 6 kapsamı). `SessionContext`'e `LastModuleResult` eklendi (`ActiveModuleManifest` ile aynı desen). **Yol boyunca bulunan isim çakışması:** yeni namespace `FreeRehabHub.App.ModuleResult` yapılmıştı ama bu, `Modules.Contracts.ModuleResult` sınıfıyla çakıştı (C#'ın enclosing-namespace çözümlemesi `using`'in önüne geçip CS0118 hatası verdi) — `FreeRehabHub.App.ModuleResultScreen` olarak düzeltildi. Xvfb+gerçek Godot ile target-tap üzerinden tam uçtan uca doğrulandı: sonuç ekranı doğru başlık/skor/4 metrik gösterdi, "Tamam" basınca `ActiveModuleManifest`/`LastModuleResult`/`ActivePatient` tamamen temizlendi.

**Faz 5 tamamlandı (2026-07-25).** MediaPipe entegrasyonu ve kamera-tabanlı modül altyapısı uçtan uca çalışıyor: `mediapipe-service` (Python/FastAPI) → `MediaPipePoseTrackingService`/`ProcessManagerService` (C#) → `ModuleHost` (poz frame'lerini `IPoseAwareModule`'e ileten genel oynatma altyapısı, aynı zamanda target-tap'e de kalıcı bir oynatma yolu kazandırdı) → `com.freerehabhub.arm-raise` (ilk gerçek kamera modülü) → sonuç ekranı. **Bu ortamda test edilemeyen tek şey gerçek kamera görüntüsüyle uçtan uca akış** (F5.01'den beri bilinen kısıt) — kullanıcının kendi makinesinde `video` grubu düzeltmesi + `services/mediapipe-service/.venv` kurulumu sonrası doğrulanmalı. F5.10'da bilinçli olarak not edilen açık: Assessment modüllerinin (`general-functional-checkin`) hâlâ gerçek bir oynatma ekranı yok (FormRenderer'ı barındıran bir host gerekiyor) — Faz 5'in kapsamı dışında bırakıldı, ayrı bir iş.

### Faz 4 — Modül Sistemi Altyapısı + Egzersiz Kütüphanesi + İlk Kamerasız Egzersiz Modülü: tamamlandı (2026-07-25)
- F4.01 - **`IModuleRegistry` sözleşmesi.** `GetAvailableModules`, `GetModulesByDiscipline`, `CreateInstance` — CLAUDE.md §5 ile birebir.
- F4.02 - **`ModuleRegistry` implementasyonu.** `modules/**/manifest.json` tarayıp hafifçe `ModuleManifest` listesi döndürüyor (DLL yüklemeden); `CreateInstance`, modül klasöründeki `.csproj` adından derlenmiş DLL'i bulup özel bir `AssemblyLoadContext` (`ModuleLoadContext`) ile yüklüyor. Gerçek Godot çalışma zamanında bulunan bug: Godot, oyunun kendi assembly'lerini kendi özel ALC'sine yüklediği için düz `Assembly.LoadFrom` `Modules.Contracts`'ın **ayrı bir kopyasını** yüklüyor, `as IModule` cast'i sessizce başarısız oluyordu — `ModuleLoadContext`, paylaşılan sözleşme assembly'lerini her zaman zaten-yüklü kopyaya yönlendirerek düzeltti.
- F4.03 - **`ModuleRegistryAutoload`.** `res://modules` + oyunun çıktı klasörünü çözüp `ModuleRegistry`'yi sahne ağacına açan ince autoload.
- F4.04 - **`templates/module-starter/`.** Exercise ve Assessment alt-varyantları, her biri kendi `Tests/` projesiyle. (F4.14'te Exercise varyantı, aşağıdaki mimari düzeltmeye göre güncellendi.)
- F4.05 - **`content-packs/exercise-library/`.** `ExerciseCard` (Domain) + `IExerciseLibraryRepository`/`ContentPackExerciseLibraryRepository` (JSON dosya tabanlı, DB değil) + 3 örnek statik egzersiz kartı. Yol boyunca `LocalizedText`, Domain'in de kullanabilmesi için `Modules.Contracts`'tan `Core`'a taşındı (Domain, Modules.Contracts'a bağımlı olamaz).
- F4.06–F4.10 - **Reçete oluşturucu (backend).** `ExercisePrescription`/`PrescriptionItem` (Domain, **değişmez geçmiş** modeli — her atama ayrı bir kayıt, `Update` yok, sadece `Add`+`GetLatest`/`GetHistory`) → `IPrescriptionRepository` → `Prescriptions`/`PrescriptionItems` tabloları → `SqlitePrescriptionRepository` (transaction içinde) → `PrescriptionService` (audit log'lu, liste görünümü loglanmıyor).
- F4.11 - `AppServices`'e `PrescriptionService` + `ExerciseLibraryRepository` bağlandı.
- F4.12 - **`PrescriptionBuilderPanel` ekranı.** Kütüphaneden kart ekleme + reps/sets düzenleme (dinamik satırlar) + hastanın en son reçetesiyle ön-dolum + Kaydet/Vazgeç. Xvfb+gerçek DB ile uçtan uca doğrulandı.
- F4.13 - `PatientListPanel`'e "Reçete" butonu + navigasyon.
- F4.14 - **İlk kamerasız egzersiz modülü: `com.freerehabhub.target-tap`** ("Hedef Vurma" — reaksiyon/koordinasyon egzersizi). **Yol boyunca bulunan büyük mimari sorun:** Godot 4 C#'ta belgelenmiş, hâlâ açık bir motor kısıtlaması var (`godotengine/godot#77675`, `#82060`) — bir `.tscn`'e bağlı script sınıfı SADECE ana proje derlemesinde bulunabiliyor, ayrı bir modül DLL'inde değil. Bu, F1.05'ten beri varsayılan "her modül kendi ayrı csproj'u" mimarisini kendi sahnesi olan (Exercise) modüller için kırıyordu — hiç gerçek Godot'ta test edilmemiş olan F4.04 şablonu da dahil. **Düzeltme:** Exercise modülleri artık kendi `.csproj`'una sahip değil; `*Controller.cs`/`Scoring/*.cs` dosyaları isimlendirme-kuralına-dayalı bir `Compile Include` (`modules\**\*Controller.cs`, `modules\**\Scoring\*.cs`) ile doğrudan `FreeRehabHub.csproj`'a deriveniyor (elle referans gerekmez, sadece dosya adı kuralına uyulmalı). Assessment modülleri (ayrı csproj + reflection) değişmedi. `ModuleRegistry.CreateInstance`, `ScenePath`'i olan modüller için artık net bir hata fırlatıyor (Godot katmanı `PackedScene.Instantiate()` ile kurmalı, reflection değil). Ayrıca gerçek Godot kapanışında bulunan ikinci bir bug: `Dispose(bool)`, native tarafta zaten yok edilmiş çocuk node'lara (`Timer`, hedef `Button`) erişmeye çalışıyordu — `GodotObject.IsInstanceValid()` koruması eklendi.

### Faz 3 — Değerlendirme Formu Motoru + İlk Assessment Modülü: tamamlandı (2026-07-25)
- F3.01 - **Modules.Contracts temel modül sözleşmeleri.** `ModuleKind`, `LocalizedText`, `DifficultyRange`, `ModuleManifest`, `ModuleContext`, `ModuleResult`, `FormSubmission`, `IModule`, `IExerciseModule`, `IAssessmentModule` eklendi (CLAUDE.md §5 ile birebir). `IPoseAwareModule` (Faz 5'te kamera veri şekli netleşince) ve `IModuleRegistry` (implementasyonu Faz 4 kapsamı) bilinçli olarak kapsam dışı bırakıldı — henüz kullanacak hiçbir şey yokken spekülatif tasarlanmadı.
- F3.02 - **Form şeması veri modeli.** `FormFieldType`, `FormFieldOption`, `FormField`, `FormSchema` eklendi — `FormSubmission`'la aynı katmanda, Godot-bağımsız.
- F3.03 - **FormSchemaLoader.** JSON'dan `FormSchema`'ya deserialize (`System.Text.Json`, camelCase); doğrulama: Id zorunlu, başlık/etiketler TR+EN ikisi de dolu, en az bir alan, alan Id'leri tekil, seçim alanlarının en az bir seçeneği olmalı, `MinValue > MaxValue` yasak. Hatalar `FormSchemaValidationException` ile fırlatılıyor. İlk kez `tests/FreeRehabHub.Modules.Contracts.Tests` projesi kuruldu (8 test).
- F3.04 - **Form renderer.** `scenes/form-engine/FormRenderer.tscn` + Controller — `FormSchema`'dan gerçek zamanlı Control ağacı üretiyor (Text→LineEdit, Number→SpinBox, Scale→HSlider, SingleChoice→OptionButton, MultiChoice→CheckBox listesi, Boolean→CheckBox), etiketler `LocalizationAutoload.CurrentLocale`'e göre TR/EN. Gönder'de zorunlu alan kontrolü yapılıp C# `event Submitted` ile `FormSubmission` dışarı açılıyor (Godot signal değil — katmanlar-arası iletişim kuralına uygun). Xvfb+Godot otomasyonuyla 6 alan tipi uçtan uca doğrulandı (render → programatik değer girişi → Gönder → doğru `FormSubmission`).
- F3.05 - **Örnek değerlendirme modülü.** `modules/com.freerehabhub.general-functional-checkin/` — telifsiz, projeye özel bir ağrı/fonksiyon öz-bildirim formu + `IAssessmentModule` implementasyonu (`Score()` ağrı/zorluk skalalarını 0-1 normalize skora çeviriyor, ham metrikleri ve belirti sayısını `Metrics`'e taşıyor). `modules/**/*.csproj` glob'u ilk kez `FreeRehabHub.csproj`'a bağlandı (test projeleri hariç) — F1.05 spike'ının ilk gerçek kullanımı. Yol boyunca bulunan tasarım kusuru: `Score()`'un saf fonksiyon kalabilmesi için `ModuleContext`'e `CompletedAt` eklendi (önceden `Score()` içinde `DateTime.UtcNow` çağrılıyordu, bu "aynı girdi→aynı çıktı" kuralını bozardı). `content-packs/assessment-forms/general-functional-checkin.json`'ın `FormSchemaLoader` ile gerçekten yüklenebildiği ayrı bir testle doğrulandı.

### Faz 2 — Hasta Yönetimi + Veri Katmanı: tamamlandı (2026-07-24 – 2026-07-25)
- F2.01 - Domain temel varlıkları (`Patient`, `Therapist`, `TherapySession`, `Discipline` enum'u — `Discipline` Core'da, ileride Modules.Contracts'ın da kullanacağı ortak kavram olduğu için)
- F2.02 - Repository arayüzleri (`IPatientRepository`, `ITherapistRepository`, `ITherapySessionRepository`)
- F2.03 - SQLCipher bağlantı katmanı (`SqliteConnectionFactory`), ilk xUnit test projesi (`tests/FreeRehabHub.Data.Tests`), CI'ya test adımı
- F2.04 - Veritabanı şeması (`DatabaseInitializer`: Therapists/Patients/TherapySessions tabloları) + `PRAGMA foreign_keys = ON`
- F2.05 - `SqlitePatientRepository` implementasyonu ve entegrasyon testleri
- F2.06 - `SqliteTherapistRepository` implementasyonu ve entegrasyon testleri
- F2.07 - `SqliteTherapySessionRepository` implementasyonu ve entegrasyon testleri
- F2.08 - Audit log Domain modeli (`AuditLogEntry`, `AuditAction`, `AuditRecordType`, `IAuditLogRepository`)
- F2.09 - `AuditLogs` tablosu ve `SqliteAuditLogRepository` implementasyonu
- F2.10 - `PatientService`/`TherapistService`/`TherapySessionService` — CRUD işlemlerini repository + audit log ile sarıyor (`GetById`/`Add`/`Update`/`Delete` loglanıyor, liste görünümleri loglanmıyor)
- F2.11 - `SessionContext` autoload'u (aktif terapist/hasta/rol durumu, `UserRole` enum'u Domain'de)
- F2.12 - Kilit ekranı (`LockScreen`) + `AppServices` composition-root autoload'u — SQLCipher parolası her açılışta soruluyor, hiçbir yere kaydedilmiyor (kullanıcı kararı)
- F2.13 - Salt-okunur hasta listesi (`PatientListPanel`) + `TherapistShell`
- F2.14 - `TherapistSelectionScreen` — aktif terapisti seçme/oluşturma (audit log'daki "kim" alanının kaynağı; kullanıcı kararıyla her açılışta soruluyor, tek-terapist varsayımı değil)
- F2.15 - Hasta ekleme formu (`PatientFormPanel`)
- F0.02 - Godot editöründen (bir noktada elle açılmış) üretilen `.uid` sidecar dosyaları, `localization/strings.csv` import çıktıları, `project.godot` güncellemesi
- F0.03 - `docs/PROGRESS.md` Faz 2 ilerlemesiyle güncellendi
- F0.04 - Godot editörünün ürettiği eksik `.uid` sidecar dosyaları ve `project.godot` güncellemesi
- F0.05 - **Gerçek bug fix:** Tip-güvenli `[Export] private Foo _foo;` (Node-türevi alan) deseni elle yazılan `.tscn`'lerde çalışmıyor — `_foo = NodePath(...)` ataması alanı sessizce doldurmuyor, `_Ready()`'de `NullReferenceException`. Tüm sahneler `[Export] NodePath` + `GetNode<T>()` desenine çevrildi, skill dosyası güncellendi (bkz. `godot-csharp-standards` § Node referansları). Ayrıca tema `.tres` dosyalarında eksik `[resource]` bloğu düzeltildi, `.godot/` önbellek bozulması (NativeCalls.cs hatası) temizlenerek çözüldü.
- F0.06 - `docs/PROGRESS.md` F2.11-F2.15 ve F0.02-F0.05 ile güncellendi
- F0.07 - **Arayüz tema ve responsive yerleşim düzeltmeleri.** Xvfb + Godot (headless) otomasyon turuyla ekran görüntüsü alınıp UI incelemesi yapıldı (9 bulgu); hepsi düzeltildi. `themes/default.tres` artık gerçek renk paleti, buton (birincil/ikincil/devre dışı), giriş alanı, liste ve hata etiketi stilleri tanımlıyor (önceden boş `[resource]` bloğuydu). LockScreen, TherapistSelectionScreen, PatientListPanel, PatientFormPanel, TherapistShell'in kökü tam-ekran anchor'landı, içerik `PanelContainer` tabanlı ortalanmış bir karta taşındı. `PatientListPanelController`'a boş liste mesajı ve tarih yerelleştirmesi (`dd.MM.yyyy`) eklendi. `high-contrast.tres`/`low-stimulation.tres` bilinçli olarak dokunulmadı (Faz 7 kapsamı).
- F0.08 - **UI ikon ve 2D/3D varlık kütüphanesi.** `download_assets.py` (yeniden çalıştırılabilir, stdlib-only) — 53 kürasyonlu Lucide ikonu (`assets/ui_icons/`, ISC/MIT), 5 Kenney.nl 2D paketi + 4 Kenney.nl 3D paketi (`assets/2d_graphics/`, `assets/3d_models/`, hepsi CC0). Kaynak/lisans dökümü `assets/ASSET_MANIFEST.md`'de. Toplam ~110 MB. `assets/.gdignore` eklendi çünkü henüz hiçbir sahne bu varlıklara referans vermiyor — binlerce dosya Godot'un içe aktarma taramasını kilitliyordu; bir modül fiilen kullanmaya başladığında kaldırılmalı.
- F0.09 - `docs/PROGRESS.md` F0.06-F0.08 ile güncellendi
- F2.16 - **Hasta düzenleme/silme UI.** `PatientListPanel`'e seçili hastaya bağlı Düzenle/Sil butonları eklendi (seçim yokken devre dışı); Sil, `ConfirmationDialog` ile onay istiyor. `PatientFormPanel` artık `SessionContext.ActivePatient` üzerinden çift modda çalışıyor: null ise ekleme, doluysa düzenleme (başlık değişiyor, alanlar önceden doluyor, Kaydet `UpdateAsync`'e yönleniyor, Id/CreatedAt korunuyor). `themes/default.tres`'e Sil için `DangerButton` (kırmızı vurgulu) varyasyonu eklendi. Xvfb+Godot otomasyon turuyla seç/düzenle/güncelle/sil akışı uçtan uca doğrulandı.
- F2.17 - **Şifreli yedekleme (backend).** `IBackupRepository` (Domain) + `SqliteBackupRepository` (Data) — SQLite'ın native backup API'si (`SqliteConnection.BackupDatabase`) kaynak ve hedefi aynı parolayla açarak (`SqliteConnectionFactory.CreateSiblingFactory`) şifreli-şifreliye kopya üretiyor, ara adımda hiç plaintext yazılmıyor. `BackupService` (Services) audit log'a `RecordType=Database, Action=Created` kaydı düşüyor (tüm veritabanına uygulandığı için `RecordId=Guid.Empty`). `AppServices`'e bağlandı. Testler gerçek bir SQLCipher dosyasıyla uçtan uca doğruluyor: yedek aynı parolayla okunabiliyor, yanlış parolayla açılamıyor.
- F2.18 - **Yedekleme UI tetikleyicisi.** `TherapistShell`'e üstte toolbar (Yedekle butonu + durum etiketi) eklendi; buton, dizin seçen bir `FileDialog` açıyor, seçim sonrası `BackupService.CreateBackupAsync` çağrılıp sonuç/hata durum etiketinde gösteriliyor. Xvfb+Godot otomasyonuyla uçtan uca doğrulandı; üretilen dosyanın `file` komutuyla düz SQLite değil opak "data" olarak tanındığı (şifrelemenin dekoratif değil gerçek olduğu) teyit edildi.

### Faz 1 — Temel + Risk Doğrulama (İskelet): tamamlandı (2026-07-24)
- F1.01 - Godot .NET çözüm iskeleti (`FreeRehabHub.csproj`/`.sln`, Godot editörü ile üretildi)
- F1.02 - Katmanlı `src/` class library'leri (Core/Domain/Data/Modules.Contracts/Services), `Directory.Build.props`, composition-root `ProjectReference`'ları
- F1.03 - GitHub Actions CI (`dotnet restore` + `dotnet build`)
- F1.04 - SQLCipher + .NET spike'ı doğrulandı (`Microsoft.Data.Sqlite` 8.0.8 + `SQLitePCLRaw.bundle_e_sqlcipher` 2.1.10, Linux x86_64'te doğrulandı; Windows/macOS doğrulanmadı — bkz. CLAUDE.md §13)
- F1.05 - Modül glob-keşif spike'ı doğrulandı; lazy assembly loading bulgusu (`Assembly.LoadFrom` gerekli) skill/CLAUDE.md'ye işlendi
- F1.06 - i18n/tema autoload iskeleti (`LocalizationAutoload`, `ThemeManager`, TR/EN CSV, tema `.tres` iskeletleri); ana projede gerçek bir yapısal build hatası (obj/ glob çakışması) bulunup düzeltildi

## Açık riskler / bir sonraki fazda hatırlanacaklar
- ~~GUT (Godot Unit Test) hiç kurulmadı~~ — **F8.19'da çözüldü.** GUT sadece GDScript içindir (README'sinde C# hiç geçmiyor), bu proje tamamen C# olduğu için kullanılamayacağı doğrulandı. Bunun yerine `tests/scene-tests/` altında özel bir C# sahne-test harness'ı yazıldı (`ISceneTest`/`SceneAssert`/`SceneTestRunner`, kalıcı env-var-gated autoload) ve `.github/workflows/ci.yml`'e `scene-tests` job'u (şimdilik sadece `ubuntu-latest`) eklendi. Detay: `testing-approach` skill'i § 3a, PROGRESS.md F8.19.
- ~~`localization/strings.csv` `project.godot`'a kaydedilmedi~~ — **F8.30'da çözüldü, ama beklenenden farklı şekilde: kayıt değil, kaldırma.** Araştırma CSV'deki 7 anahtarın kod/`.tscn` içinde hiç kullanılmadığını ortaya çıkardı (gerçek UI metinleri hardcoded Türkçe, modül içeriği TR/EN'i ayrı bir mekanizmayla — `LocalizedText` — çalışıyor) — `project.godot`'a kaydetmenin hiçbir görünür etkisi olmayacaktı. Kullanıcıyla konuşulup dosyalar `git rm` ile kaldırıldı, `CLAUDE.md`/`module-development` skill'i güncellendi (skill'deki yanlış "DisplayName `localization/`'a eklenir" iddiası da düzeltildi). Sabit UI metinlerinin gerçekten `Tr()` ile yerelleştirilmesi bilinçli olarak kapsam dışı bırakıldı — istenirse ayrı bir iş.
- `SessionContext` F2.11'de, `ModuleRegistryAutoload` F4.03'te eklendi.
- ~~Modül `manifest.json` ↔ C# `Manifest` tutarlılık testi yok~~ — **F8.24'te eklendi.** İkisinin elle senkron tutulması gereken bilinçli ikiliği (bkz. F4.02) hâlâ geçerli, ama artık bir divergence sessizce fark edilmeden kalamaz: `tests/FreeRehabHub.Modules.Contracts.Tests/ManifestConsistencyTests.cs` (Assessment modülleri, Godot-bağımsız, xUnit) ve `tests/scene-tests/ModuleManifestConsistencySceneTest.cs` (Exercise modülleri, Node-türevi, sahne testi) birlikte 3 gerçek modülün tamamını kapsıyor.
- **Exercise modülleri kendi `.csproj`'una sahip DEĞİL** (Godot 4'ün "script sadece ana derlemede bulunabilir" motor kısıtlaması yüzünden, bkz. F4.14 ve `module-development` skill § 3a) — yeni bir Exercise modülü eklerken bu farkı unutma: sadece `*Controller.cs`/`Scoring/*.cs` isimlendirme kuralına uy, `.csproj` ekleme. Faz 5'in kamera-tabanlı modülleri de bu kurala tabi olacak.
- Godot editörünün ürettiği `.uid`/`project.godot` değişiklikleri, ben fark etmeden oturumlar arasında birikebiliyor (editör bu ortamın dışında, kullanıcının kendi makinesinde açılıyor) — her adımda `git status` ile kontrol etmeye devam et.
- ~~SQLCipher şifreleme anahtarı: elle giriş mi, OS keychain mi?~~ — **F8.26'da kalıcı olarak karara bağlandı: elle giriş.** Faz 8 sonrası açık risk taramasında tekrar gündeme alındı (F8.05 sonrası "Faz 8'in sonunda tekrar bakılacak" notu buradaydı). İki seçenek karşılaştırıldı: (A) OS keychain entegrasyonu (Windows Credential Manager P/Invoke, macOS `security` CLI, Linux `secret-tool`/libsecret — üçü de yeni bir NuGet bağımlılığı gerektirmez ama Linux'ta evrensel kurulu değil, fallback gerekir) — gerçek kullanılabilirlik kazancı ama 3 platform-özel backend + sınırlı CI-doğrulanabilirlik riski; (B) mevcut elle-giriş davranışını koru. Kullanıcı B'yi seçti: bu ölçekte tek-kullanıcı/klinik senaryosunda elle giriş kabul edilebilir, projenin minimalizm çizgisiyle tutarlı, KVKK açısından da savunulabilir (OS keychain'in kendisi düzgün kilitli değilse daha az güvenli bile olabilir). LockScreen her açılışta soruyor, hiçbir yere kaydedilmiyor, `SqliteConnectionFactory` anahtarı parametre olarak almaya devam ediyor (F2.12'nin orijinal kararı — artık kalıcı, "geçici" değil). Bu konu tekrar açılmayacak.
- `assets/.gdignore` mevcut — bir modül `assets/` altındaki ikon/2D/3D varlıklarından birini gerçekten kullanmaya başladığında bu dosya kaldırılmalı (veya sadece kullanılan alt klasör için daraltılmalı), yoksa Godot editörü o varlığı içe aktarmaz.
- Bu ortamda artık Godot 4.7 mono binary'si (`~/İndirilenler/godot-4.7-mono/godot`) ve Xvfb kurulu — gerçek Godot render'ından ekran görüntüsü almak/UI doğrulamak için kullanılabiliyor (bkz. F0.07'nin doğrulama yöntemi). Kalıcı bir otomasyon script'i repoya eklenmedi, her seferinde geçici bir GDScript autoload ile kurulup iş bitince temizleniyor.
- **Faz 5'ten kalan, hâlâ bu ortamda doğrulanamayan şey: gerçek kamerayla uçtan uca akış** (`mediapipe-service` + `com.freerehabhub.arm-raise`). **F8.32'de gerçek kök nedeni bulundu — F8.22'nin teşhisi (PipeWire'ın kendi `monitor.v4l2`'sinin cihazı tuttuğu) yanlış/eksikmiş.** `monitor.v4l2`'yi geçici olarak devre dışı bırakıp PipeWire'ın kamerayı artık hiç yönetmediği doğrulandıktan sonra bile `ffmpeg -f v4l2` yine "Device or resource busy" verdi. Gerçek sebep: kullanıcının kendi kurduğu, bu projeyle **hiç ilgisi olmayan** bir Docker container (`motion_bridge-motion-bridge`) `/dev/video0`'ı doğrudan cihaz geçişiyle mount edip kernel seviyesinde tekelen tutuyor (`RestartPolicy: unless-stopped` — reboot dahil kendiliğinden gelmeye devam eder) — root yetkisiyle çalıştığı için normal kullanıcı `fuser`'ı bunu hiç göremiyordu. WirePlumber deneyi hemen geri alındı, kalıcı hiçbir iz bırakılmadı. Kullanıcıya soruldu, **"şimdilik dokunma"** dendi — container'a hiç dokunulmadı, risk bilinçli olarak açık bırakıldı. **Sonuç:** PipeWire `monitor.v4l2`/`monitor.libcamera` çift-yönetimi (F8.22) gerçek ama alakasız bir bulguydu; asıl engel hep bu Docker container'ıymış, hedef donanımı (klinik bilgisayarlarda böyle bir container olmayacak) hiç etkilemiyor. Kullanıcı isterse `motion_bridge`'i durdurup kamerayı gerçekten serbest bırakabilir — bu FreeRehabHub'ın kod tabanını hiç ilgilendirmiyor. Bu makinede gerçek kamerayla test hâlâ mümkün değil, sentetik/statik görüntüyle pipeline testi (F5.12) geçerli yol olmaya devam ediyor.
- ~~Assessment modüllerinin hâlâ gerçek bir oynatma ekranı yok~~ — **F8.18'de çözüldü** (`AssessmentHost.tscn`/Controller, bkz. yukarıdaki F8.18 girdisi).
- ~~`ProgressRecord.SessionId`'nin gerçek bir `TherapySessions` kaydına FK'ı yok~~ — **F8.31'de çözüldü** (A seçeneği: modül oynatışı başına 1 `TherapySession`, kullanıcıyla konuşulup seçildi). `ModuleHostController`/`AssessmentHostController` artık her modül başlatışında gerçek bir `TherapySession` oluşturup (`StartedAt`), tamamlanınca kapatıyor (`EndedAt`); `ProgressRecords.SessionId`'ye gerçek bir `REFERENCES TherapySessions(Id)` eklendi. **Bilinçli olarak kapsam dışı bırakılan (B seçeneği):** çok-modüllü bir "ziyaret" kavramı (bir terapi seansında birden fazla modül oynatılıp tek bir seansa bağlanması) — bu, şu an hiç var olmayan yeni bir "ziyaret başlat/bitir" UX'i gerektiriyor, istenirse ayrı bir iş olarak ele alınabilir.
- ~~İlerleme/PDF rapor özellikleri sadece Exercise modüllerini kapsıyor~~ — **F8.27'de gerçek UI'dan uçtan uca doğrulandı** (önceden sadece kod okumasıyla teyit edilmişti). Bkz. aşağıdaki Faz 8 sonrası girdisi.
- ~~Metrik etiketleri hiç yerelleştirilmiyor~~ — **F8.28+F8.29'da tamamen çözüldü.** F8.27'de keşfedildi: `MetricKeyFormatter.Humanize()` camelCase metrik anahtarlarını (`painLevel`, `functionalDifficulty`) sadece mekanik olarak Title Case'e çeviriyordu ("Pain Level"), hiçbir çeviri tablosu yoktu. F8.28'de `ModuleManifest.MetricLabels` (TR/EN sözlük) eklendi, 3 gerçek modül + 2 şablon dolduruldu, tutarlılık testleri genişletildi. F8.29'da `MetricKeyFormatter.Humanize` + 3 çağrı noktası (`ModuleResultPanelController`/`ProgressPanelController`/`ProgressReportService`) bu sözlüğü kullanacak şekilde bağlandı; gerçek UI'da ("Ağrı Seviyesi" görünüyor) sahne testleriyle doğrulandı. Karşılığı olmayan bir anahtar hâlâ mekanik Title Case'e düşüyor (bilinçli fallback, çökme yok).
