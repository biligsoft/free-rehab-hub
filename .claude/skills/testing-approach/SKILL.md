---
name: testing-approach
description: Katmana göre test stratejisi — hangi katman xUnit ile (Godot'suz), hangisi özel sahne-test harness'ıyla (Godot içinde) test edilir. Yeni kod yazarken, test eklerken veya "bunu nasıl test ederim" sorusunda oku.
---

# Test Yaklaşımı — FreeRehabHub

## 1. Katman → test aracı eşlemesi

| Katman | Godot'a bağımlı mı? | Test aracı | Nerede çalışır |
|---|---|---|---|
| `Core`, `Domain`, `Data`, `Modules.Contracts`, `Services` | Hayır | xUnit | CI'da saniyeler içinde, Godot editörü gerekmez |
| Modül scoring sınıfları (`modules/*/Scoring/`) | Hayır | xUnit | Aynı şekilde |
| `IAssessmentModule.Score()` implementasyonları | Hayır (saf fonksiyon) | xUnit | Aynı şekilde |
| Sahneler, `*Controller.cs` (Node-türevi), autoload'lar | Evet | Özel C# sahne-test harness'ı (`tests/scene-tests/`) | Godot headless modda |
| UI etkileşimi (buton tıklama, form akışı uçtan uca) | Evet | Manuel test (Godot editöründe çalıştırarak) + varsa sahne-test senaryosu | Godot editörü |

Bu tablo `godot-csharp-standards` skill'indeki katman kuralının doğal sonucu: Godot-bağımsız katmanlar zaten Godot'suz test edilebilir durumda tasarlandığı için, test etmek için Godot açmaya gerek yok.

**GUT neden kullanılmıyor:** GUT (Godot Unit Test) sadece GDScript içindir — resmi belgelerinde C# desteğinden hiç bahsedilmez. Bu proje tamamen C# olduğu için (bkz. CLAUDE.md §2) GUT mimari olarak uyumsuz; bu yüzden Faz 8 sonrası açık-risk taramasında GUT yerine özel bir C# sahne-test harness'ı yazıldı (bkz. § 3a).

### 3a. Sahne-test harness'ı nasıl çalışır

`tests/scene-tests/` içinde:
- `ISceneTest` — `string Name`, `Task RunAsync(SceneTree sceneTree)`.
- `SceneAssert` — statik `True`/`False`/`Equal<T>`/`NotNull`, başarısızlıkta `SceneAssertionException` fırlatır.
- `SceneTestRunner` — reflection ile `ISceneTest` implementasyonlarını keşfeder (`Assembly.GetExecutingAssembly()`), her birini çalıştırır, `[GEÇTİ]`/`[BAŞARISIZ]` yazdırır, sonunda `GetTree().Quit(...)` ile tüm testler geçtiyse 0, en az biri başarısızsa 1 döner.

**Kritik mimari kural:** `SceneTestRunner` `project.godot`'ta **kalıcı bir autoload**'dır (`SceneTestRunner="*res://tests/scene-tests/SceneTestRunner.cs"`), ana sahne DEĞİLDİR. Normal uygulama çalışırken tamamen etkisizdir — sadece `FREEREHABHUB_RUN_SCENE_TESTS` ortam değişkeni set edilmişse devreye girer. Bunun nedeni: eğer runner ana sahne olsaydı, bir testin kendi `ChangeSceneToFile` çağrısı ana sahneyi (yani runner'ın kendisini) yok ederdi — bu gerçekten yaşanmış bir bug, ilk denemede standalone-sahne yaklaşımıyla keşfedildi (`ObjectDisposedException`, test kendi assertion'larını geçtikten SONRA runner çökerken).

Her sahne testi, `AppServices.Unlock(password, databasePathOverride)` ile izole bir geçici SQLite dosyası açar (`Path.GetTempPath()` altında, `finally` bloğunda silinir) — gerçek `user://freerehabhub.db` dosyasına hiç dokunulmaz, elle yedek/geri yükleme dansına gerek kalmaz.

**Çalıştırma:**
```bash
FREEREHABHUB_RUN_SCENE_TESTS=1 godot --headless --path .
```
Linux geliştirme makinesinde ekran sunucusu yoksa `xvfb-run -a` ile sarılmalı (bkz. CI job'u, `.github/workflows/ci.yml` → `scene-tests`).

Yeni bir sahne testi eklemek için `tests/scene-tests/` altına `ISceneTest` implemente eden yeni bir `.cs` dosyası eklemek yeterli — elle kayıt gerekmez, `SceneTestRunner` reflection'la otomatik keşfeder (bkz. `AssessmentHostSceneTest.cs` örneği).

## 2. Temel kural: test edemiyorsan, mimari yanlış yerde

Bir `*Controller.cs` (Node-türevi) sınıfını doğrudan xUnit ile test etmeye çalıştığını fark edersen, bu bir uyarı işaretidir — o mantık aslında Godot-bağımsız bir sınıfa ait olmalıydı. Controller'ı ince tut, mantığı çıkar (bkz. `module-development` skill'i, § 3a).

## 3. Proje yapısı

`tests/` klasörü `src/`'i birebir yansıtır:
```
tests/FreeRehabHub.Core.Tests/
tests/FreeRehabHub.Domain.Tests/
tests/FreeRehabHub.Data.Tests/
tests/FreeRehabHub.Modules.Contracts.Tests/
tests/scene-tests/                  # Godot-bağımlı sahne testleri (özel harness, bkz. § 3a)
```
Her modülün kendi scoring testleri modül klasörünün içinde kalır (`modules/<id>/Scoring/Tests/` veya `modules/<id>/Tests/`) — modül kendi kendine yeten bir birim olduğu için testleri de yanında taşır, merkezi `tests/` klasörüne dağıtılmaz.

## 4. Zorunlu test kapsamı (minimum bar)

- Her `IAssessmentModule.Score()`: geçerli girdi, sınır değer (min/max skor), eksik/geçersiz form girdisi — en az 3 senaryo.
- Her modül scoring sınıfı: en az bir "normal" ve bir "uç durum" testi.
- `Data` katmanı repository'leri: gerçek SQLite (in-memory veya geçici dosya) üzerinden entegrasyon testi — mock DB kullanma, SQLCipher davranışı mock ile yakalanamaz.
- `IExerciseModule.Completed` event'inin tam bir kez tetiklendiği en az bir sahne-test senaryosuyla doğrulanmalı (çift tetikleme veya hiç tetiklenmeme, ilerleme kaydının bozulmasına yol açar).
- Yeni bir modül eklerken `manifest.json` ↔ C# `Manifest` tutarlılık kontrolüne yeni modülü de dahil et (bkz. `tests/FreeRehabHub.Modules.Contracts.Tests/ManifestConsistencyTests.cs` Assessment için, `tests/scene-tests/ModuleManifestConsistencySceneTest.cs` Exercise için) — ikisi elle senkron tutulduğu için (bkz. `module-development` skill'i), yeni modül bu kontrole eklenmezse divergence sessizce fark edilmez.

## 5. Ne test edilmez

- Godot'un kendi motor davranışı (render, fizik) test edilmez — bu bizim sorumluluğumuzda değil.
- Üçüncü parti kütüphanelerin (MediaPipe, SQLCipher) doğruluğu test edilmez — sadece bizim onlarla olan entegrasyon kodumuz (`IPoseTrackingService`, repository'ler) test edilir.
- Statik içerik (`content-packs/`) test edilmez — bu veri, davranış değil; şema doğrulaması (JSON schema validasyonu) yeterlidir, bu da Data/Core katmanında bir kez yazılan bir validator ile karşılanır.

## 6. CI

`Core/Domain/Data/Modules.Contracts/Services` + tüm modül scoring testleri her push'ta CI'da (`build` job) xUnit ile koşar (Godot gerekmediği için hızlı). Sahne testleri ayrı bir job'da (`scene-tests`, şimdilik sadece `ubuntu-latest`) koşar: Godot .NET/Mono binary'si indirilir, proje kaynakları içe aktarılır (`--import`), sonra `FREEREHABHUB_RUN_SCENE_TESTS=1` ile Xvfb altında headless çalıştırılır. Xvfb Linux'a özgü olduğu için Windows/macOS'a henüz eklenmedi (headless çalıştırma o platformlarda zaten sanal framebuffer gerektirmiyor, ama şimdilik doğrulanmadı).

**Not:** `DisplayServer` özelliklerinden bazıları (ör. TTS) `--headless` modda hiç çalışmıyor — `DisplayServer.HasFeature(...)` platform fark etmeksizin `--headless`'ta `false` dönüyor. Bu tür OS-entegrasyonu doğrulamaları `scene-tests`'e (headless) dahil edilemez; bunun yerine `SceneTestRunner` ile aynı desende (kalıcı, env-var-gated autoload) ama PENCERELİ çalıştırılan ayrı bir mekanizma gerekir — örnek: `autoload/TtsDiagnosticRunner.cs` (`FREEREHABHUB_RUN_TTS_CHECK=1 godot --path .`, dikkat: `--headless` OLMADAN), CI'da ayrı bir `tts-check` job'unda çalışıyor.
