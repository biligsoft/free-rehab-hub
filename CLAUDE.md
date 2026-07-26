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
│   ├── AppServices.cs               # composition-root: DB açıldıktan sonra kurulan Services katmanı örnekleri
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

Sahne organizasyonu (bir ekran = bir `.tscn`, "tanrı sahne" yasak), signal'ler sadece sahne-içi node-to-node iletişim için (katmanlar arası iletişim C# event/arayüz ile), tip-güvenli node referansları (`[Export]`, string path yasak), autoload listesi (`SessionContext`, `AppServices`, `LocalizationAutoload`, `ThemeManager`, `ModuleRegistryAutoload`). Detay: `.claude/skills/godot-csharp-standards/`.

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

- **SQLCipher + .NET (Faz 1, F1.04 spike'ı ile doğrulandı):** `Microsoft.Data.Sqlite` (8.0.8) + `SQLitePCLRaw.bundle_e_sqlcipher` (2.1.10) kombinasyonu çalışıyor. Başlangıçta `SQLitePCL.Batteries_V2.Init()` çağrılmalı; şifreleme `SqliteConnectionStringBuilder.Password` ile uygulanıyor. Parolasız/yanlış parola ile açma denemeleri beklendiği gibi başarısız oluyor, doğru parola ile veri okunabiliyor — yani şifreleme gerçek, dekoratif değil. **Doğrulandı:** Linux x86_64 (Fedora, .NET 8) — ve **Faz 8'de (F8.08-F8.14, GitHub Actions çapraz-platform CI matrisi ile) Windows ve macOS'ta da gerçekten doğrulandı**, tüm `Data.Tests` (SQLCipher round-trip/yanlış-parola testleri dahil) her iki platformda da geçiyor. `FreeRehabHub.Data` projesine bu paket referansları Faz 2'de eklendi.
- **mediapipe pip paketi Windows/macOS'ta da çalışıyor (Faz 8, F8.16'da GitHub Actions ile doğrulandı):** F5.01'de sadece bu Fedora geliştirme makinesindeki uyumsuzluk doğrulanmıştı (bkz. aşağıdaki madde), Windows/macOS'ta hiç test edilmemişti. F8.16'da CI'a eklenen `mediapipe-package` job'u, `windows-latest`/`macos-latest`/`ubuntu-latest`'te gerçekten `pip install mediapipe` + PyInstaller ile paketleyip üretilen binary'yi çalıştırıp `/health` endpoint'ine istek atıyor — **üçünde de başarılı**. Bu, mediapipe'ın en azından import edilip bir FastAPI/uvicorn sürecini ayağa kaldırabildiğini doğruluyor. **Hâlâ doğrulanmayan:** gerçek kamera görüntüsüyle poz-tespiti (donanım/kamera bu ortamda yok, bkz. §14).
- **Modül glob-keşif (Faz 1, F1.05 spike'ı ile doğrulandı):** `<ProjectReference Include="modules/**/*.csproj" />` MSBuild glob'u çalışıyor — bir modül klasörü `modules/` altına eklendiğinde, ana projenin `.csproj`'una elle dokunmadan derlemeye dahil oluyor. **Ama** çalışma zamanı keşfinde kritik bir bulgu var: `AppDomain.CurrentDomain.GetAssemblies()` ile naif tarama, koddan hiç dokunulmayan (sadece referans verilen) modül assembly'lerini **bulamıyor** — .NET'in "lazy assembly loading" davranışı yüzünden, bir assembly sadece içindeki bir tip fiilen kullanılırsa yükleniyor. **Çözüm:** `IModuleRegistry` implementasyonu, çıktı klasöründeki modül DLL'lerini (`modules/` altından derlenenler) elle `Assembly.LoadFrom` ile yüklemeli, sonra reflection yapmalı — sadece "yüklü assembly'leri tara" yeterli değil. Bu, `module-development` skill'inde de düzeltildi (bkz. o dosya, § Kayıt/keşif).
- **MediaPipe pip paketi + Fedora uyumsuzluğu (Faz 5, F5.01 spike'ı ile doğrulandı):** `mediapipe` pip paketi (0.10.7'den 0.10.35'e kadar test edilen sürümler, hem eski `solutions.pose` hem yeni `tasks.python.vision.PoseLandmarker` API'si) bu projenin geliştirildiği Fedora makinesinde `PoseLandmarker`/`Pose` nesnesini kurarken tutarlı şekilde çöküyor: `ValueError: ... TAG:index:name is invalid` (dahili graph node isminde beklenmeyen büyük harf — bkz. benzer şekil `google/mediapipe#2603`). CPU/GPU delegate seçimi, `MEDIAPIPE_DISABLE_GPU` env var'ı, Python sürümü (3.10/3.14) fark etmiyor — **hem geliştiricinin gerçek makinesinde hem bu ortamda tekrar üretildi**, yani rastgele bir ortam kusuru değil. Aynı test, standart bir Debian tabanlı Docker container'ında (`python:3.10-slim` + `libgl1`/`libegl1`/`libgles2` vb. sistem kütüphaneleri) **sorunsuz çalıştı** — yani sorun mediapipe'ın genelinde değil, özellikle bu Fedora'nın (güncel glibc/libstdc++) prebuilt manylinux wheel'iyle uyumsuzluğunda. **Sonuç:** Üretim mimarisi değişmedi — `mediapipe-service` hedef makinelerde (Windows/macOS/standart Linux dağıtımları) native process olarak çalışacak, bu doğrulanmış bir Fedora-özel araç zinciri sorunudur, mediapipe'ın genel Linux desteğini geçersiz kılmaz. **Ama** bu geliştirme makinesinde (Fedora) `mediapipe-service`'in gerçek poz-tespiti kodunu native çalıştırıp test etmek şu an mümkün değil — Faz 5 boyunca bu makinede mediapipe'a dokunan kısımların geliştirme/test döngüsü `docker-compose.dev.yml` üzerinden (Debian tabanlı bir Python image'ı ile) yapılacak; üretim/paketleme kodu hiçbir yerde Docker'a bağımlı olmayacak. Ayrıca bu ortamda kamera cihazları (`/dev/video0` vb.) var ama kullanıcı `video` grubunda değil — kamera testi için `sudo usermod -aG video $USER` + yeniden oturum açma gerekiyor (henüz yapılmadı).

## 14. Bilinen Riskler / Açık Sorular

Mimari tartışmasında tespit edilip henüz çözülmemiş, ilgili fazda ele alınacak noktalar:

- **KVKK/sağlık verisi uyumluluğu** (rıza kaydı, tam audit log, veri saklama/silme politikası) — Faz 2'de temel atılır, Faz 8'de tam taranır.
- **Hedef donanım/performans baseline'ı** tanımlı değil. F5.01'de bu geliştirme makinesinin (Fedora) MediaPipe'ı native çalıştıramadığı doğrulandı; F8.16'da mediapipe'ın Windows/macOS'ta (GitHub Actions runner'larında) gerçekten import edilip çalıştığı doğrulandı (bkz. §13) — ama bu CI runner'ları gerçek kamera donanımına sahip değil, sadece "mediapipe başlıyor mu" sorusunu yanıtlıyor. Gerçek performans baseline'ı (kare hızı, gecikme) hâlâ gerçek hedef donanımda (kamerası olan bir Windows/macOS/Linux makine) alınmalı.
- **Kamera erişimi bu geliştirme makinesinde hâlâ çalışmıyor — ama nedeni "video grubu" değilmiş.** Faz 8'de kullanıcı `sudo usermod -aG video` yaptı ve yeniden oturum açtı; kontrol edilince `/dev/video0`'da zaten `user:emre:rw-` ACL'i olduğu görüldü (muhtemelen systemd-logind "uaccess", video grubundan bağımsız olarak baştan beri vardı) — yani izin hiç asıl engel değilmiş. Cihaz gerçek ve format sorgularına doğru yanıt veriyor, ama ham V4L2 erişimi (`ffmpeg` ve `cv2.VideoCapture(0)` — `pose_tracker.py`'nin fiilen kullandığı yöntem) sürekli "meşgul" hatası veriyor, kamera uygulaması kapalıyken bile. `pipewire`/`wireplumber` bu makinede çalışıyor — en olası açıklama PipeWire'ın kamerayı kendi üzerinden yönetip ham erişimi engellemesi, ama kesin doğrulanamadı (bu ortamda tam `sudo`/`journalctl` erişimi yok). Detay ve olası çözüm yolu (GStreamer/`pipewiresrc` backend'i) için bkz. `docs/PROGRESS.md` § Açık riskler. Faz 5'in kamera-tabanlı modülü bu makinede hâlâ uçtan uca (gerçek kamera görüntüsüyle) test edilemiyor; sentetik/statik görüntü ile pipeline testi yapılabilir (F5.12'de yapıldığı gibi).
- **Çoklu cihaz/terapist senkronizasyonu yok** — bilinçli varsayım, tek makine/tek kurulum. İleride değişirse mimari yeniden gözden geçirilir.
- **i18n kapsamı** şimdilik sadece UI metinleri; klinik ölçek normlarının yerelleştirilmesi ayrı, çözülmemiş bir problem.
- **TTS'in Türkçe ses kalitesi/varlığı Windows/macOS'ta doğrulanmadı** (Faz 7, F7.08) — `TtsAutoload`, Godot'un yerleşik `DisplayServer` TTS'ine sarılı (sıfır yeni bağımlılık); bu Linux geliştirme makinesinde speech-dispatcher/espeak-ng üzerinden gerçek Türkçe konuşma ürettiği doğrulandı, ama Windows'ta SAPI5'e, macOS'ta NSSpeechSynthesizer/AVSpeechSynthesizer'a sarılıyor — hedef klinik bilgisayarda Türkçe ses paketi kurulu olmayabilir (özellikle Windows dil paketi gerektirebilir). GitHub Actions runner'larında SAPI5/NSSpeechSynthesizer'ın kendisi muhtemelen var, ama Türkçe SES PAKETİNİN kurulu olup olmadığı (ve bu yüzden bu riskin CI ile kapatılıp kapatılamayacağı) henüz denenmedi — SQLCipher/mediapipe'ın aksine (bkz. §13, F8.14/F8.16) bu risk hâlâ açık.
