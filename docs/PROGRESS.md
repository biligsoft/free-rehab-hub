## Güncel durum
- Faz: 2 (Hasta Yönetimi + Veri Katmanı) — tamamlandı (2026-07-25)
- Son tamamlanan Faz 2 adımı: F2.18
- Son commit: F2.18 - Yedekleme UI tetikleyicisi
- Sıradaki: Faz 3 (Değerlendirme Formu Motoru + İlk Assessment Modülü)
- Faz-bağımsız: F0.07'de tüm ekranlar gerçek bir temayla (renk/buton/kart) ve
  responsive (anchor tabanlı, ortalanmış kart) yerleşimle güncellendi —
  ayrıntı ve önce/sonra karşılaştırması için bkz. UI inceleme artifact'ı

## Faz geçmişi

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
- SQLCipher paket kombinasyonu Windows/macOS'ta henüz test edilmedi (Faz 2 bu platformlarda doğrulamadan kapandı — en geç Faz 8'de (paketleme) yapılmalı).
- `localization/strings.csv` editörde import edildi (`.import`/`.translation` dosyaları oluştu, F0.02) ama `project.godot`'un `[internationalization]` bölümüne hâlâ kaydedilmedi — Project Settings → Localization'dan elle eklenmesi gerekiyor (TranslationServer çalışma zamanında CSV'yi otomatik almıyor).
- `SessionContext` F2.11'de eklendi. `ModuleRegistryAutoload` hâlâ Faz 4'te.
- Godot editörünün ürettiği `.uid`/`project.godot` değişiklikleri, ben fark etmeden oturumlar arasında birikebiliyor (editör bu ortamın dışında, kullanıcının kendi makinesinde açılıyor) — her adımda `git status` ile kontrol etmeye devam et.
- SQLCipher şifreleme anahtarının nereden geleceği (OS keychain / ilk kurulum parolası) hâlâ çözülmedi — `SqliteConnectionFactory` şu an anahtarı parametre olarak alıyor, kaynağı belirlenmedi (bkz. `clinical-data-handling` skill).
- `high-contrast.tres` ve `low-stimulation.tres` hâlâ boş (F0.07 sadece `default.tres`'i doldurdu) — Faz 7'de gerçek bir erişilebilirlik tasarım geçişi gerekiyor.
- `assets/.gdignore` mevcut — bir modül `assets/` altındaki ikon/2D/3D varlıklarından birini gerçekten kullanmaya başladığında bu dosya kaldırılmalı (veya sadece kullanılan alt klasör için daraltılmalı), yoksa Godot editörü o varlığı içe aktarmaz.
- Bu ortamda artık Godot 4.7 mono binary'si (`~/İndirilenler/godot-4.7-mono/godot`) ve Xvfb kurulu — gerçek Godot render'ından ekran görüntüsü almak/UI doğrulamak için kullanılabiliyor (bkz. F0.07'nin doğrulama yöntemi). Kalıcı bir otomasyon script'i repoya eklenmedi, her seferinde geçici bir GDScript autoload ile kurulup iş bitince temizleniyor.
