---
name: module-development
description: Yeni bir terapi modülü (egzersiz/oyun veya değerlendirme) eklerken izlenecek adım adım rehber ve modül sözleşmesi (IModule/IExerciseModule/IAssessmentModule). Modül eklerken, module-starter şablonunu kullanırken veya mevcut bir modülü değiştirirken oku.
---

# Modül Geliştirme Rehberi — FreeRehabHub

Bu proje açık kaynak ve katkıya açık: bir katkıcı **core dosyalarına dokunmadan** yeni bir terapi modülü ekleyebilmeli. Bu skill hem insan katkıcılar hem de senin (Claude) yeni modül eklerken izleyeceğin adımları tanımlar.

## 0. Önce karar ver: Exercise mi, Assessment mi?

- **`IExerciseModule`**: Etkileşimli, kod+sahne gerektiren bir egzersiz/oyun (kamera-tabanlı olabilir ya da olmayabilir). Kendi `.tscn` sahnesi vardır.
- **`IAssessmentModule`**: Bir değerlendirme formu + skorlama mantığı. Genellikle kendi sahnesi **yoktur** — ortak Form Renderer'ı (`scenes/form-engine/`) şema ile besler.
- **Statik egzersiz kartı (video/talimat, kod gerektirmeyen)** bu sistemin parçası DEĞİL — bu içerik `content-packs/exercise-library/` altına veri olarak eklenir, herhangi bir arayüz implemente etmez. Kod yazmadan önce bunun gerçekten interaktif bir modül mü yoksa statik bir içerik kartı mı olduğuna karar ver.

## 1. `templates/module-starter/` klasörünü kopyala

Hedef: `modules/<modül-id>/` (id formatı: `com.<yazar-veya-organizasyon>.<kısa-ad>`, örn. `com.freerehabhub.balance-hop`). Şablon, ilgili türe (Exercise/Assessment) göre iki alt-varyant içerir; yanlış olanı kopyalama.

## 2. `manifest.json` doldur

Zorunlu alanlar (bkz. `ModuleManifest` sözleşmesi, `FreeRehabHub.Modules.Contracts`):

| Alan | Not |
|---|---|
| `Id` | Global tekil, değiştirilemez (sürümler arası aynı kalır) |
| `Version` | semver |
| `Kind` | `Exercise` \| `Assessment` |
| `DisplayName` / `Description` | **TR ve EN ikisi de zorunlu** — tek dilli manifest kabul edilmez |
| `Disciplines` | En az bir tane: PT/OT/Konuşma/Psikoloji/Özel Eğitim |
| `DifficultyRange` | Min/max zorluk seviyesi |
| `RequiredCapabilities` | Örn. `"pose-tracking"` — gerekmiyorsa boş bırak, fazladan yazma |
| `MinAppVersion` | Bu modülün çalışacağı en düşük app sürümü |
| `EntryPointType` | `IModule` implementasyonunun tam nitelikli tip adı |
| `ScenePath` | Sadece Exercise |
| `FormSchemaPath` | Sadece Assessment |

## 3a. Exercise modülü implementasyonu

- `<Modül>Controller.cs`: `IExerciseModule`'ü implemente eder, `Node`'dan türer, sahne yaşam döngüsünü yönetir (`InitializeAsync`, `OnActivated/OnPaused/OnResumed/OnDeactivated`, `Completed` event'i tam olarak bir kez tetiklenir).
- Kamera/pose verisi gerekiyorsa ayrıca `IPoseAwareModule`'ü implemente et, gerekmiyorsa **implemente etme** (ISP — bkz. `godot-csharp-standards`).
- **Skorlama mantığını Controller'a yazma.** Ayrı, Godot-bağımsız bir sınıfa çıkar (`modules/<id>/Scoring/<Modül>Scorer.cs`, `Godot`'a bağımlı olmayan sade bir .NET sınıfı). Controller sadece bu sınıfı çağırıp sonucu `ModuleResult`'a sarar. Bu ayrım sayesinde skorlama mantığı Godot açılmadan xUnit ile test edilir (bkz. `testing-approach` skill'i).
- Modülün kendi `.csproj`'u `modules/**/*.csproj` glob'u ile ana projeye otomatik dahil olur — elle bir yere referans eklemene gerek yok.

## 3b. Assessment modülü implementasyonu

- `<Modül>Assessment.cs`: `IAssessmentModule`'ü implemente eder. Tek gerçek iş: `ModuleResult Score(FormSubmission submission, ModuleContext context)` — bu **saf bir fonksiyon** olmalı (yan etkisiz, aynı girdi → aynı çıktı). Godot'a hiç dokunmaz.
- Form şeması `schemas/` altındaki meta-şemaya uygun bir JSON/YAML dosyası — `FormSchemaPath` bu dosyayı gösterir.
- **Telifli standart klinik ölçekleri (örn. isimli, yayıncısı olan test bataryaları) şemaya doğrudan gömüp `content-packs/`'e commit etme.** Bkz. `clinical-data-handling` skill'i — bu bir hukuki risk.

## 4. Kayıt / keşif

Elle bir registry dosyasına ekleme **yapma**. `IModuleRegistry`, uygulama açılışında `modules/**/manifest.json` dosyalarını tarar ve çıktı klasöründeki modül DLL'lerini **elle `Assembly.LoadFrom` ile yükleyip** içindeki `IModule` implementasyonlarını bulur — sadece zaten yüklü assembly'leri taramak yeterli **değil** (Faz 1, F1.05 spike'ında doğrulandı: .NET, koddan hiç dokunulmayan referanslı bir assembly'yi otomatik yüklemiyor). Modül görünmüyorsa önce `manifest.json`'daki `EntryPointType`'ın gerçek tip adıyla birebir eştiğini, sonra modül DLL'sinin çıktı klasöründe fiilen var olup olmadığını kontrol et.

## 5. Test

- Scoring sınıfı (`<Modül>Scorer` veya `Score()` metodu) için xUnit testleri **zorunlu** — en az: geçerli girdi, sınır değer, geçersiz/eksik girdi.
- Controller/sahne davranışı için GUT testi opsiyonel ama önerilir (özellikle `Completed` event'inin tam bir kez tetiklendiğini doğrulamak için).

## 6. Yerelleştirme

`DisplayName`, `Description` ve modül içi tüm kullanıcıya görünen metinler TR ve EN için `localization/` sözlüğüne eklenir. Sabit kodlanmış (hardcoded) UI metni modül kodunda yasak.

## 7. PR öncesi kontrol listesi

- [ ] `manifest.json` iki dilde de dolu
- [ ] Scoring mantığı Godot-bağımsız bir sınıfta ve test edilmiş
- [ ] `RequiredCapabilities` doğru beyan edilmiş (özellikle kamera)
- [ ] `content-packs/`'e telifli içerik commit edilmemiş
- [ ] Modül `templates/module-starter/`'daki klasör yapısına uyuyor
- [ ] Core katmanlarında (`Core/Domain/Data/Services`) hiçbir değişiklik yok — sadece `modules/<yeni-modül>/` eklendi
