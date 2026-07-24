## Güncel durum
- Faz: 2 (Hasta Yönetimi + Veri Katmanı) — devam ediyor
- Son tamamlanan adım: F2.10
- Son commit: F0.02 - Godot editörünün ürettiği .uid sidecar dosyaları, i18n import çıktıları ve project.godot güncellemesi eklendi
- Kalan Faz 2 kapsamı: hasta CRUD UI, şifreli yedekleme

## Faz geçmişi

### Faz 2 — Hasta Yönetimi + Veri Katmanı: devam ediyor (başlangıç 2026-07-24)
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
- F0.02 - Godot editöründen (bir noktada elle açılmış) üretilen `.uid` sidecar dosyaları, `localization/strings.csv` import çıktıları, `project.godot` güncellemesi

### Faz 1 — Temel + Risk Doğrulama (İskelet): tamamlandı (2026-07-24)
- F1.01 - Godot .NET çözüm iskeleti (`FreeRehabHub.csproj`/`.sln`, Godot editörü ile üretildi)
- F1.02 - Katmanlı `src/` class library'leri (Core/Domain/Data/Modules.Contracts/Services), `Directory.Build.props`, composition-root `ProjectReference`'ları
- F1.03 - GitHub Actions CI (`dotnet restore` + `dotnet build`)
- F1.04 - SQLCipher + .NET spike'ı doğrulandı (`Microsoft.Data.Sqlite` 8.0.8 + `SQLitePCLRaw.bundle_e_sqlcipher` 2.1.10, Linux x86_64'te doğrulandı; Windows/macOS doğrulanmadı — bkz. CLAUDE.md §13)
- F1.05 - Modül glob-keşif spike'ı doğrulandı; lazy assembly loading bulgusu (`Assembly.LoadFrom` gerekli) skill/CLAUDE.md'ye işlendi
- F1.06 - i18n/tema autoload iskeleti (`LocalizationAutoload`, `ThemeManager`, TR/EN CSV, tema `.tres` iskeletleri); ana projede gerçek bir yapısal build hatası (obj/ glob çakışması) bulunup düzeltildi

## Açık riskler / bir sonraki fazda hatırlanacaklar
- SQLCipher paket kombinasyonu Windows/macOS'ta henüz test edilmedi (Faz 2 veya Faz 8'de doğrulanmalı).
- `localization/strings.csv` editörde import edildi (`.import`/`.translation` dosyaları oluştu, F0.02) ama `project.godot`'un `[internationalization]` bölümüne hâlâ kaydedilmedi — Project Settings → Localization'dan elle eklenmesi gerekiyor (TranslationServer çalışma zamanında CSV'yi otomatik almıyor).
- Faz 2'de `SessionContext` henüz eklenmedi — hasta CRUD UI'si için gerekecek (aktif terapist kimliği, `PatientService` vb.'ye `actingTherapistId` olarak geçilecek). `ModuleRegistryAutoload` Faz 4'te.
- SQLCipher şifreleme anahtarının nereden geleceği (OS keychain / ilk kurulum parolası) hâlâ çözülmedi — `SqliteConnectionFactory` şu an anahtarı parametre olarak alıyor, kaynağı belirlenmedi (bkz. `clinical-data-handling` skill).
