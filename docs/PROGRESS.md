## Güncel durum
- Faz: 8 (Sertleştirme, Paketleme, Katkıcı Onboarding) — devam ediyor
- Son tamamlanan adım: F8.13
- Son commit: F8.13 - MediaPipePoseTrackingService.StopAsync: CloseAsync yerine
  CloseOutputAsync kullanılarak ReceiveLoopAsync ile ReceiveAsync çakışması giderildi
- **GitHub Actions çapraz-platform CI matrisi artık tamamen yeşil** — Windows/Ubuntu/macOS
  üçü de gerçekten geçiyor (repo ilk kez origin'e push edildi, F1-F8.13 arası 118 commit).
  SQLCipher'ın Windows/macOS'ta gerçekten çalıştığı ilk kez doğrulandı (Faz 1'den beri açık
  duran risk kapandı). Sıradaki: PyInstaller ile mediapipe-service paketleme.
- Faz-bağımsız: F0.07'de tüm ekranlar gerçek bir temayla (renk/buton/kart) ve
  responsive (anchor tabanlı, ortalanmış kart) yerleşimle güncellendi —
  ayrıntı ve önce/sonra karşılaştırması için bkz. UI inceleme artifact'ı

## Faz geçmişi

### Faz 8 — Sertleştirme, Paketleme, Katkıcı Onboarding: devam ediyor
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
- **GUT (Godot Unit Test) hiç kurulmadı** (F8.06'da fark edildi) — `CLAUDE.md` ve `testing-approach` skill'i sahne/controller testleri için GUT öngörüyor, `tests/gut/` klasör planı var, ama gerçekte hiç oluşturulmadı; CI (`.github/workflows/ci.yml`) sadece xUnit katmanlarını çalıştırıyor, hiç Godot-seviyeli test adımı yok. Şimdiye kadarki tüm sahne/UI doğrulamaları bu oturumlarda geçici, commit edilmeyen Xvfb autopilot script'leriyle yapıldı (bkz. bu dosyadaki F0.07'den beri tekrarlanan doğrulama deseni). Kalıcı bir GUT kurulumu + CI adımı henüz planlanmadı, ayrı bir faz-bağımsız iş olarak ele alınabilir.
- SQLCipher paket kombinasyonu Windows/macOS'ta henüz test edilmedi (Faz 2 bu platformlarda doğrulamadan kapandı — en geç Faz 8'de (paketleme) yapılmalı).
- `localization/strings.csv` editörde import edildi (`.import`/`.translation` dosyaları oluştu, F0.02) ama `project.godot`'un `[internationalization]` bölümüne hâlâ kaydedilmedi — Project Settings → Localization'dan elle eklenmesi gerekiyor (TranslationServer çalışma zamanında CSV'yi otomatik almıyor).
- `SessionContext` F2.11'de, `ModuleRegistryAutoload` F4.03'te eklendi.
- Modül `manifest.json` dosyaları hâlâ her modülün C# sınıfındaki hardcoded `ModuleManifest`'le aynı içeriği taşıyor (bilinçli ikilik — bkz. F4.02: `manifest.json` hafif keşif/katalog için, C# `Manifest` çalışma zamanında otorite). İkisi elle senkron tutulmalı; ileride bir tutarlılık testi (manifest.json ↔ C# Manifest) eklenebilir ama henüz yok.
- **Exercise modülleri kendi `.csproj`'una sahip DEĞİL** (Godot 4'ün "script sadece ana derlemede bulunabilir" motor kısıtlaması yüzünden, bkz. F4.14 ve `module-development` skill § 3a) — yeni bir Exercise modülü eklerken bu farkı unutma: sadece `*Controller.cs`/`Scoring/*.cs` isimlendirme kuralına uy, `.csproj` ekleme. Faz 5'in kamera-tabanlı modülleri de bu kurala tabi olacak.
- Godot editörünün ürettiği `.uid`/`project.godot` değişiklikleri, ben fark etmeden oturumlar arasında birikebiliyor (editör bu ortamın dışında, kullanıcının kendi makinesinde açılıyor) — her adımda `git status` ile kontrol etmeye devam et.
- SQLCipher şifreleme anahtarının kaynağı kullanıcıyla konuşuldu (Faz 8, F8.05 sonrası) — **şimdilik elle giriş kalıyor** (LockScreen'de her açılışta soruluyor, hiçbir yere kaydedilmiyor, F2.12'nin kararı korundu). OS keychain alternatifine Faz 8'in sonunda, paketleme aşamasına yakın tekrar bakılacak — o zamana kadar `SqliteConnectionFactory` anahtarı parametre olarak almaya devam edecek (bkz. `clinical-data-handling` skill).
- `assets/.gdignore` mevcut — bir modül `assets/` altındaki ikon/2D/3D varlıklarından birini gerçekten kullanmaya başladığında bu dosya kaldırılmalı (veya sadece kullanılan alt klasör için daraltılmalı), yoksa Godot editörü o varlığı içe aktarmaz.
- Bu ortamda artık Godot 4.7 mono binary'si (`~/İndirilenler/godot-4.7-mono/godot`) ve Xvfb kurulu — gerçek Godot render'ından ekran görüntüsü almak/UI doğrulamak için kullanılabiliyor (bkz. F0.07'nin doğrulama yöntemi). Kalıcı bir otomasyon script'i repoya eklenmedi, her seferinde geçici bir GDScript autoload ile kurulup iş bitince temizleniyor.
- **Faz 5'ten kalan, henüz bu ortamda doğrulanamayan şey: gerçek kamerayla uçtan uca akış** (`mediapipe-service` + `com.freerehabhub.arm-raise`). Bu geliştirme makinesinde hem kamera erişimi (`video` grubu izni eksik) hem mediapipe'ın kendisi (Fedora araç zinciri uyumsuzluğu, Docker'da doğrulandı) engelli — kullanıcının kendi makinesinde `video` grubu düzeltmesi + `services/mediapipe-service/.venv` kurulumu (`pip install -r requirements.txt`, `python download_model.py`) sonrası gerçek kamerayla test edilmeli.
- Assessment modüllerinin (`general-functional-checkin`) hâlâ gerçek bir oynatma ekranı yok (F5.10'da bilinçli olarak not edildi) — `FormRenderer`'ı barındırıp `IAssessmentModule.Score()`'u çağıracak bir host ekranı gerekiyor, `ModuleLibraryPanel` şu an sadece `Kind == Exercise` modülleri listeliyor. Faz 5'in yarattığı bir eksiklik değil, ayrı faz-bağımsız bir iş.
- `AppServices`'in `services/mediapipe-service`'i çözme şekli (`ProjectSettings.GlobalizePath("res://services/mediapipe-service")`, F5.08) sadece dev modunda çalışır — paketlenmiş build'de `res://` bir `.pck` olur, Python kaynak dosyaları oraya export edilmez/çalıştırılamaz. Faz 8'in "MediaPipe binary gömülü" maddesi bunu çözecek, o zaman bu yol çözümlemesi de gözden geçirilmeli.
- **`ProgressRecord.SessionId`'nin gerçek bir `TherapySessions` kaydına FK'ı yok** (F6.02) — `ModuleHost`, modül başlatırken `TherapySessionService` üzerinden gerçek bir oturum satırı hiç oluşturmuyor, sadece `Guid.NewGuid()` üretiyor (bkz. F5.09/F5.11). İleride gerçek oturum takibi (ör. bir terapi seansında birden fazla modül oynatılması, oturum başlangıç/bitiş zamanı) gerekirse bu bağlantı kurulmalı — Faz 6'nın grafik/rapor ekranları için şimdilik gerekli değil.
- **PdfSharp/`LiberationSansFontResolver` sadece bu Linux geliştirme makinesinde doğrulandı** (F6.05) — font baytları repoya gömülü olduğu için teorik olarak platform-bağımsız olmalı (SQLCipher paket kombinasyonuyla aynı kategori risk), ama Windows/macOS'ta gerçek PDF üretimi henüz test edilmedi. En geç Faz 8'de (paketleme) doğrulanmalı.
- İlerleme/PDF rapor özellikleri (Faz 6) sadece Exercise modüllerini kapsıyor — Assessment modüllerinin (`general-functional-checkin`) hâlâ bir oynatma ekranı olmadığı için (F5.10'dan beri açık) onlardan hiç `ProgressRecord` üretilmiyor. Bu ekran eklendiğinde `ModuleHostController`/`ProgressRecordService` entegrasyonunun Assessment sonuçlarını da kapsayıp kapsamayacağı ayrıca değerlendirilmeli.
