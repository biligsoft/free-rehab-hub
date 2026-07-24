# FreeRehabHub — Proje Rehberi

## 1. Proje Özeti

FreeRehabHub, fizyoterapi, ergoterapi, konuşma terapisi, psikoloji ve özel eğitim disiplinlerini kapsayan; açık kaynak, tamamen yerel (offline) çalışan bir terapi/özel eğitim uygulamasıdır. Godot 4.x (.NET/C#) üzerinde geliştirilir.

Temel özellikler: çoklu hasta kaydı, JSON/YAML şema ile tanımlı runtime değerlendirme formu motoru, hazır egzersiz kütüphanesi + hastaya özel reçete, kamera tabanlı hareket takibiyle (MediaPipe) çalışan etkileşimli oyunlar, ilerleme grafikleri + PDF rapor, Terapist/Çocuk (kiosk) rol ayrımı.

Proje açık kaynak olacağı için **modülerlik kritik önceliktir**: katkıcılar core'a dokunmadan yeni terapi modülü ekleyebilmeli.

## 2. Mimari Kararlar (neden böyle seçildi)

| Karar | Gerekçe |
|---|---|
| Godot 4.x .NET, C#, katmanlı mimari | Kullanıcı Python/Java/C# biliyor, OOP/SOLID'e aşina; Godot'ta yeni. Katmanlı yapı, Godot'a özgü karmaşıklığı iş mantığından izole eder. |
| MediaPipe servisi **native process** (Python/FastAPI), Docker **değil** | Docker konteynerinden kameraya erişim Windows/macOS'ta pratikte desteklenmiyor (passthrough sorunu). Klinik bilgisayarları çoğunlukla Windows. Native process bu riski tamamen ortadan kaldırır. Docker Compose sadece Linux/geliştirme için opsiyonel kalır. |
| SQLite/SQLCipher **native dosya**, Docker volume **değil** | SQLite dosya tabanlı bir DB; Godot native çalıştığı sürece Docker'a hiç gerek yok. Gereksiz karmaşıklığı kaldırır. |
| Core/Domain/Data/Modules.Contracts/Services katmanları **Godot-bağımsız** (`using Godot;` yasak) | Godot editörü açılmadan xUnit ile test edilebilir olmaları için. Sadece UI ve modül sahne/controller'ları Godot API'sine dokunur. |
| Modül sistemi: ortak `IModule` + iki alt tip (`IExerciseModule`, `IAssessmentModule`) | Egzersiz/oyun ve değerlendirme formu farklı etkileşim modellerine sahip (biri sahne+yaşam döngüsü, diğeri saf skorlama fonksiyonu), ama ortak keşif/kayıt (registry) mekanizmasını paylaşmalı. |
| Modül keşfi: `modules/**/*.csproj` glob + reflection (derleme-zamanı modüler, çalışma-zamanı keşif) | Gerçek "kod indirip çalıştırma" (hot-load .dll) Godot 4 .NET'te kırılgan/riskli. Bu yaklaşım katkıcının core'a dokunmadan PR açmasını sağlıyor; tam hot-load ileri faz stretch goal. |
| "Hazır egzersiz kütüphanesi" modül sistemi dışında, `content-packs/` altında statik veri | Video/talimat kartları kod gerektirmiyor, generic bir viewer ile gösteriliyor. Modül sistemi sadece gerçekten interaktif/kod gerektiren şeyler için. |
| Değerlendirme içerikleri (`content-packs/assessment-forms/`) telifli ölçek **içermez** | Standart isimli klinik ölçekler genelde telifli; açık kaynak repoya gömülmesi hukuki risk. Bkz. `clinical-data-handling` skill'i. |

## 3. Katman Mimarisi

```
UI  ──────────────┐
                   ├──► Services ──► Domain ──► Core
Modules(impl) ─────┤         ▲
                   ├─────────┘
Modules.Contracts ─┴──► Core
Data ──────────────────► Domain ──► Core
```

Alt katmanlar üst katmanları tanımaz. Domain, Data'yı tanımaz (repository arayüzleri Domain'de, implementasyon Data'da — Dependency Inversion). Detaylı kurallar ve gerekçeler için: **`.claude/skills/godot-csharp-standards/`**.

## 4. Klasör Yapısı

```
free-rehab-hub/
├── project.godot
├── FreeRehabHub.sln
├── CLAUDE.md
├── docker-compose.dev.yml          # opsiyonel, sadece dev/Linux

├── src/                             # Godot-bağımsız class library'ler
│   ├── FreeRehabHub.Core/
│   ├── FreeRehabHub.Domain/
│   ├── FreeRehabHub.Data/           # SQLite/SQLCipher
│   ├── FreeRehabHub.Modules.Contracts/
│   └── FreeRehabHub.Services/

├── FreeRehabHub.App/                # ana Godot .csproj — GodotSharp'a bağımlı tek proje

├── scenes/
│   ├── shells/                      # TherapistShell.tscn, ChildKioskShell.tscn
│   ├── patient/
│   ├── form-engine/                 # şemadan Control ağacı üreten renderer
│   ├── module-host/
│   └── reporting/

├── autoload/
│   ├── SessionContext.cs
│   ├── LocalizationAutoload.cs
│   ├── ThemeManager.cs
│   └── ModuleRegistryAutoload.cs

├── modules/                         # her biri kendi .csproj'u — core'a dokunmadan eklenir
│   └── <modül-id>/
│       ├── manifest.json
│       ├── *.tscn                   # sadece Exercise
│       ├── *Controller.cs           # sadece Exercise
│       ├── Scoring/                 # Godot-bağımsız, test edilebilir
│       └── <modül-id>.csproj

├── templates/module-starter/

├── content-packs/                   # SADECE veri — telif riskini izole eder
│   ├── exercise-library/
│   └── assessment-forms/

├── schemas/                         # form motorunun meta-şeması
├── localization/                    # TR/EN
├── themes/
├── assets/

├── services/
│   └── mediapipe-service/           # native Python/FastAPI süreci
│       ├── app/
│       ├── pyproject.toml / requirements.txt
│       ├── .venv/                   # gitignored
│       ├── build/                   # PyInstaller — tek exe paketleme
│       └── Dockerfile               # opsiyonel dev/Linux yolu

├── tests/
│   ├── FreeRehabHub.Core.Tests/  ...Domain.Tests/  ...Data.Tests/  ...Modules.Contracts.Tests/
│   └── gut/

├── docs/
│   ├── PROGRESS.md                  # güncel faz/adım durumu
│   └── architecture/

└── .claude/skills/
```

## 5. Modül Sözleşmesi (özet)

Tam imzalar için `.claude/skills/module-development/` ve geçmiş tasarım kararları. Temel tipler:

- `ModuleManifest` — Id, Version, Kind (Exercise|Assessment), DisplayName/Description (TR+EN), Disciplines, DifficultyRange, RequiredCapabilities, EntryPointType, ScenePath/FormSchemaPath
- `IModule` — ModuleId, Manifest (ortak taban)
- `IExerciseModule : IModule, IDisposable` — InitializeAsync, OnActivated/OnPaused/OnResumed/OnDeactivated, `event Completed`
- `IPoseAwareModule` — opsiyonel, sadece kamera gerektiren modüller implemente eder
- `IAssessmentModule : IModule` — `ModuleResult Score(FormSubmission, ModuleContext)` (saf fonksiyon)
- `IModuleRegistry` — GetAvailableModules, GetModulesByDiscipline, CreateInstance
- `ModuleResult` — ModuleId, PatientId, SessionId, CompletedAt, NormalizedScore, Metrics, Notes

## 6. Kodlama Standartları (özet)

C# PascalCase/camelCase standart konvansiyonları, dosya başına tek sınıf, magic number yasağı, SOLID'in bu projedeki somut karşılıkları. **Kod yazmadan önce her zaman `.claude/skills/godot-csharp-standards/` oku.**

## 7. Godot'a Özgü Kurallar (özet)

Sahne organizasyonu (bir ekran = bir `.tscn`, "tanrı sahne" yasak), signal'ler sadece sahne-içi node-to-node iletişim için (katmanlar arası iletişim C# event/arayüz ile), tip-güvenli node referansları (`[Export]`, string path yasak), autoload listesi (`SessionContext`, `LocalizationAutoload`, `ThemeManager`, `ModuleRegistryAutoload`). Detay: `.claude/skills/godot-csharp-standards/`.

## 8. Modül Ekleme Rehberi (özet)

`templates/module-starter/` kopyala → `manifest.json` doldur (TR+EN zorunlu) → Exercise ise Controller+Scoring, Assessment ise Score() fonksiyonu yaz → test yaz → PR kontrol listesini geç. Elle kayıt gerekmez, registry otomatik keşfeder. Tam adımlar: `.claude/skills/module-development/`.

## 9. Yol Haritası (Fazlar)

1. **Temel + Risk Doğrulama (İskelet)** — çözüm yapısı, CI; SQLCipher+.NET ve modül-glob-keşif spike'ları; i18n/tema iskeleti.
2. **Hasta Yönetimi + Veri Katmanı** — Patient/Therapist/Session, SQLCipher repository'ler, hasta CRUD UI, şifreli yedekleme, temel audit log.
3. **Değerlendirme Formu Motoru + İlk Assessment Modülü** — form şeması + renderer, `Modules.Contracts` temel sözleşmeler, telif-güvenli örnek değerlendirme modülü.
4. **Modül Sistemi Altyapısı + Egzersiz Kütüphanesi + İlk Kamerasız Egzersiz Modülü** — tam registry/discovery, `module-starter`, `content-packs/exercise-library` + reçete oluşturucu, kamerasız örnek modül.
5. **MediaPipe Entegrasyonu + Kamera Tabanlı Modül** — `mediapipe-service`, `ProcessManagerService`, `IPoseTrackingService`, gerçek kamera-tabanlı modül, hedef donanım doğrulaması.
6. **İlerleme Takibi, Grafikler, PDF Rapor** — `ProgressRecord` toplama, grafik ekranları, PDF export.
7. **Çocuk Modu / Kiosk + Erişilebilirlik** — `AccessControlService`, kiosk kilidi, TTS, yüksek kontrast/düşük uyaran temaları, ödül sistemi.
8. **Sertleştirme, Paketleme, Katkıcı Onboarding** — CONTRIBUTING.md, son kullanıcı installer (MediaPipe binary gömülü), test/güvenlik/KVKK taraması.

Faz içi adımlara ineceğimiz süreç kuralları (tek adım/onay, /compact, ilerleme takibi): `.claude/skills/phase-workflow/`.

## 10. Git Commit Formatı

```
F<faz>.<adım> - <kısa özet>
```
Örn. `F2.03 - Hasta repository implementasyonu eklendi`. Faz-bağımsız işler için `F0.NN`. Detay ve `docs/PROGRESS.md` kullanımı: `.claude/skills/phase-workflow/`.

## 11. Test Yaklaşımı (özet)

`Core/Domain/Data/Modules.Contracts/Services` + modül scoring sınıfları → xUnit (Godot'suz, hızlı). Sahneler/controller'lar → GUT. `IAssessmentModule.Score()` ve her modül scoring sınıfı için test zorunlu. Detay: `.claude/skills/testing-approach/`.

## 12. Skill Dosyaları

Göreve başlamadan önce ilgili skill'i oku:

| Skill | Ne zaman oku |
|---|---|
| `phase-workflow` | Her göreve başlarken — süreç kuralı |
| `godot-csharp-standards` | Herhangi bir `.cs`/`.tscn` yazmadan/düzenlemeden önce |
| `module-development` | Yeni modül eklerken veya mevcut modülü değiştirirken |
| `testing-approach` | Test yazarken veya "nasıl test ederim" sorusunda |
| `clinical-data-handling` | Hasta verisine, loglamaya veya `content-packs/`'e dokunan herhangi bir kod için |

## 13. Doğrulanmış Teknik Kararlar

- **SQLCipher + .NET (Faz 1, F1.04 spike'ı ile doğrulandı):** `Microsoft.Data.Sqlite` (8.0.8) + `SQLitePCLRaw.bundle_e_sqlcipher` (2.1.10) kombinasyonu çalışıyor. Başlangıçta `SQLitePCL.Batteries_V2.Init()` çağrılmalı; şifreleme `SqliteConnectionStringBuilder.Password` ile uygulanıyor. Parolasız/yanlış parola ile açma denemeleri beklendiği gibi başarısız oluyor, doğru parola ile veri okunabiliyor — yani şifreleme gerçek, dekoratif değil. **Doğrulandı:** Linux x86_64 (Fedora, .NET 8). **Doğrulanmadı:** Windows/macOS — bu paket bundle'ı bu platformlar için de native binary içeriyor ama gerçek makinede test edilmedi; Faz 2'de (gerçek repository yazılırken) veya en geç Faz 8'de (paketleme) çapraz platform testi yapılmalı. `FreeRehabHub.Data` projesine bu paket referansları Faz 2'de eklenecek.
- **Modül glob-keşif (Faz 1, F1.05 spike'ı ile doğrulandı):** `<ProjectReference Include="modules/**/*.csproj" />` MSBuild glob'u çalışıyor — bir modül klasörü `modules/` altına eklendiğinde, ana projenin `.csproj`'una elle dokunmadan derlemeye dahil oluyor. **Ama** çalışma zamanı keşfinde kritik bir bulgu var: `AppDomain.CurrentDomain.GetAssemblies()` ile naif tarama, koddan hiç dokunulmayan (sadece referans verilen) modül assembly'lerini **bulamıyor** — .NET'in "lazy assembly loading" davranışı yüzünden, bir assembly sadece içindeki bir tip fiilen kullanılırsa yükleniyor. **Çözüm:** `IModuleRegistry` implementasyonu, çıktı klasöründeki modül DLL'lerini (`modules/` altından derlenenler) elle `Assembly.LoadFrom` ile yüklemeli, sonra reflection yapmalı — sadece "yüklü assembly'leri tara" yeterli değil. Bu, `module-development` skill'inde de düzeltildi (bkz. o dosya, § Kayıt/keşif).

## 14. Bilinen Riskler / Açık Sorular

Mimari tartışmasında tespit edilip henüz çözülmemiş, ilgili fazda ele alınacak noktalar:

- **KVKK/sağlık verisi uyumluluğu** (rıza kaydı, tam audit log, veri saklama/silme politikası) — Faz 2'de temel atılır, Faz 8'de tam taranır.
- **Kiosk kilit mekanizması** platform bazlı zorluk farkları — Faz 7.
- **Hedef donanım/performans baseline'ı** tanımlı değil — Faz 5'te (en ağır işlemsel faz) netleşecek.
- **Çoklu cihaz/terapist senkronizasyonu yok** — bilinçli varsayım, tek makine/tek kurulum. İleride değişirse mimari yeniden gözden geçirilir.
- **i18n kapsamı** şimdilik sadece UI metinleri; klinik ölçek normlarının yerelleştirilmesi ayrı, çözülmemiş bir problem.
