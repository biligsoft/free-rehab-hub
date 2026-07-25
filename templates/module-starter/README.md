# Modül Başlangıç Şablonu

Yeni bir terapi modülü eklerken tam adımlar için `.claude/skills/module-development/SKILL.md`'ye bakın. Bu klasör iki alt-varyant içerir; modülünüzün türüne göre **sadece birini** kopyalayın.

## `exercise/` — `IExerciseModule`

Etkileşimli, kendi sahnesi olan bir egzersiz/oyun. `TemplateExerciseController.cs` sahne yaşam döngüsünü yönetir, skorlama mantığı `Scoring/TemplateExerciseScorer.cs`'de (Godot-bağımsız, xUnit ile test edilir) ayrı tutulur.

## `assessment/` — `IAssessmentModule`

Bir form şeması + saf bir skorlama fonksiyonu. Kendi sahnesi yok, ortak `scenes/form-engine/FormRenderer.tscn` şemayı okuyarak formu üretir.

## Kopyaladıktan sonra

1. Klasörü `modules/<yeni-modül-id>/` altına taşıyın (id formatı: `com.<yazar-veya-organizasyon>.<kısa-ad>`).
2. Aşağıdaki her yerde `com.yourorg.template-exercise` / `com.yourorg.template-assessment` id'sini ve `TemplateExercise*` / `TemplateAssessment` sınıf/namespace adlarını gerçek modülünüze göre değiştirin: `manifest.json`, `.csproj`, `.cs` dosyaları, `.tscn` (Exercise).
3. `manifest.json`, `.tscn` (`ScenePath`/ext_resource yolları) ve `.cs` (`Manifest.ScenePath`/`FormSchemaPath`) içindeki `res://templates/module-starter/...` yollarını yeni konumunuza (`res://modules/<yeni-modül-id>/...`) göre güncelleyin.
4. **Assessment için:** `form-schema.json`'ı `content-packs/assessment-forms/<yeni-modül-id>.json`'a taşıyın (bkz. CLAUDE.md § Klasör Yapısı — form şemaları `content-packs/`'te veri olarak tutulur, modül klasöründe değil) ve `FormSchemaPath`'i buna göre güncelleyin. Telifli standart klinik ölçekleri buraya **koymayın** (bkz. `clinical-data-handling` skill'i).
5. `manifest.json`'daki `displayName`/`description`'ı TR ve EN olarak doldurun — tek dilli manifest kabul edilmez.
6. `Tests/` altındaki testleri gerçek mantığınıza göre güncelleyin; en az geçerli girdi, sınır değer, eksik/geçersiz girdi senaryoları kalmalı (bkz. `testing-approach` skill'i).
7. Elle bir registry dosyasına ekleme yapmayın — `modules/**/*.csproj` glob'u ve `ModuleRegistry`'nin `manifest.json` taraması modülü otomatik keşfeder.
