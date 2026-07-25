# Modül Başlangıç Şablonu

Yeni bir terapi modülü eklerken tam adımlar için `.claude/skills/module-development/SKILL.md`'ye bakın. Bu klasör iki alt-varyant içerir; modülünüzün türüne göre **sadece birini** kopyalayın.

## `exercise/` — `IExerciseModule`

Etkileşimli, kendi sahnesi olan bir egzersiz/oyun. `TemplateExerciseController.cs` sahne yaşam döngüsünü yönetir, skorlama mantığı `Scoring/TemplateExerciseScorer.cs`'de (Godot-bağımsız, xUnit ile test edilir) ayrı tutulur.

**Kendi `.csproj`'u yok, kasıtlı olarak.** Godot 4'ün C# desteğinde bir motor kısıtlaması var: bir `.tscn`'e bağlı script sınıfı sadece ana proje derlemesinde (`FreeRehabHub.dll`) bulunabiliyor, ayrı bir modül DLL'inde değil (bkz. `godotengine/godot#77675`). Bu yüzden `*Controller.cs` ve `Scoring/*.cs` dosyaları, `FreeRehabHub.csproj`'daki isimlendirme-kuralına-dayalı bir `Compile Include` ile doğrudan ana derlemeye dahil oluyor — modül klasörünüze **kendi `.csproj`'unuzu eklemeyin**, sadece isimlendirme kuralına uyun (Controller dosyası `*Controller.cs` ile bitmeli, skorlama sınıfı `Scoring/` alt klasöründe olmalı).

## `assessment/` — `IAssessmentModule`

Bir form şeması + saf bir skorlama fonksiyonu. Kendi sahnesi yok, ortak `scenes/form-engine/FormRenderer.tscn` şemayı okuyarak formu üretir.

## Kopyaladıktan sonra

1. Klasörü `modules/<yeni-modül-id>/` altına taşıyın (id formatı: `com.<yazar-veya-organizasyon>.<kısa-ad>`).
2. Aşağıdaki her yerde `com.yourorg.template-exercise` / `com.yourorg.template-assessment` id'sini ve `TemplateExercise*` / `TemplateAssessment` sınıf/namespace adlarını gerçek modülünüze göre değiştirin: `manifest.json`, `.csproj` (sadece Assessment'ta var), `.cs` dosyaları, `.tscn` (Exercise).
3. `manifest.json`, `.tscn` (`ScenePath`/ext_resource yolları) ve `.cs` (`Manifest.ScenePath`/`FormSchemaPath`) içindeki `res://templates/module-starter/...` yollarını yeni konumunuza (`res://modules/<yeni-modül-id>/...`) göre güncelleyin.
4. **Assessment için:** `form-schema.json`'ı `content-packs/assessment-forms/<yeni-modül-id>.json`'a taşıyın (bkz. CLAUDE.md § Klasör Yapısı — form şemaları `content-packs/`'te veri olarak tutulur, modül klasöründe değil) ve `FormSchemaPath`'i buna göre güncelleyin. Telifli standart klinik ölçekleri buraya **koymayın** (bkz. `clinical-data-handling` skill'i).
5. `manifest.json`'daki `displayName`/`description`'ı TR ve EN olarak doldurun — tek dilli manifest kabul edilmez.
6. `Tests/` altındaki testleri gerçek mantığınıza göre güncelleyin; en az geçerli girdi, sınır değer, eksik/geçersiz girdi senaryoları kalmalı (bkz. `testing-approach` skill'i).
7. Elle bir registry dosyasına ekleme yapmayın — Assessment için `modules/**/*.csproj` glob'u, Exercise için isimlendirme-kuralına-dayalı `Compile Include` glob'u, ve her ikisi için `ModuleRegistry`'nin `manifest.json` taraması modülü otomatik keşfeder.
