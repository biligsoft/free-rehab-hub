---
name: testing-approach
description: Katmana göre test stratejisi — hangi katman xUnit ile (Godot'suz), hangisi GUT ile (Godot içinde) test edilir. Yeni kod yazarken, test eklerken veya "bunu nasıl test ederim" sorusunda oku.
---

# Test Yaklaşımı — FreeRehabHub

## 1. Katman → test aracı eşlemesi

| Katman | Godot'a bağımlı mı? | Test aracı | Nerede çalışır |
|---|---|---|---|
| `Core`, `Domain`, `Data`, `Modules.Contracts`, `Services` | Hayır | xUnit | CI'da saniyeler içinde, Godot editörü gerekmez |
| Modül scoring sınıfları (`modules/*/Scoring/`) | Hayır | xUnit | Aynı şekilde |
| `IAssessmentModule.Score()` implementasyonları | Hayır (saf fonksiyon) | xUnit | Aynı şekilde |
| Sahneler, `*Controller.cs` (Node-türevi), autoload'lar | Evet | GUT (Godot Unit Test addon) | Godot headless modda |
| UI etkileşimi (buton tıklama, form akışı uçtan uca) | Evet | Manuel test (Godot editöründe çalıştırarak) + varsa GUT senaryosu | Godot editörü |

Bu tablo `godot-csharp-standards` skill'indeki katman kuralının doğal sonucu: Godot-bağımsız katmanlar zaten Godot'suz test edilebilir durumda tasarlandığı için, test etmek için Godot açmaya gerek yok.

## 2. Temel kural: test edemiyorsan, mimari yanlış yerde

Bir `*Controller.cs` (Node-türevi) sınıfını doğrudan xUnit ile test etmeye çalıştığını fark edersen, bu bir uyarı işaretidir — o mantık aslında Godot-bağımsız bir sınıfa ait olmalıydı. Controller'ı ince tut, mantığı çıkar (bkz. `module-development` skill'i, § 3a).

## 3. Proje yapısı

`tests/` klasörü `src/`'i birebir yansıtır:
```
tests/FreeRehabHub.Core.Tests/
tests/FreeRehabHub.Domain.Tests/
tests/FreeRehabHub.Data.Tests/
tests/FreeRehabHub.Modules.Contracts.Tests/
tests/gut/                          # Godot-bağımlı sahne testleri
```
Her modülün kendi scoring testleri modül klasörünün içinde kalır (`modules/<id>/Scoring/Tests/` veya `modules/<id>/Tests/`) — modül kendi kendine yeten bir birim olduğu için testleri de yanında taşır, merkezi `tests/` klasörüne dağıtılmaz.

## 4. Zorunlu test kapsamı (minimum bar)

- Her `IAssessmentModule.Score()`: geçerli girdi, sınır değer (min/max skor), eksik/geçersiz form girdisi — en az 3 senaryo.
- Her modül scoring sınıfı: en az bir "normal" ve bir "uç durum" testi.
- `Data` katmanı repository'leri: gerçek SQLite (in-memory veya geçici dosya) üzerinden entegrasyon testi — mock DB kullanma, SQLCipher davranışı mock ile yakalanamaz.
- `IExerciseModule.Completed` event'inin tam bir kez tetiklendiği en az bir GUT senaryosuyla doğrulanmalı (çift tetikleme veya hiç tetiklenmeme, ilerleme kaydının bozulmasına yol açar).

## 5. Ne test edilmez

- Godot'un kendi motor davranışı (render, fizik) test edilmez — bu bizim sorumluluğumuzda değil.
- Üçüncü parti kütüphanelerin (MediaPipe, SQLCipher) doğruluğu test edilmez — sadece bizim onlarla olan entegrasyon kodumuz (`IPoseTrackingService`, repository'ler) test edilir.
- Statik içerik (`content-packs/`) test edilmez — bu veri, davranış değil; şema doğrulaması (JSON schema validasyonu) yeterlidir, bu da Data/Core katmanında bir kez yazılan bir validator ile karşılanır.

## 6. CI

`Core/Domain/Data/Modules.Contracts/Services` + tüm modül scoring testleri her push'ta CI'da xUnit ile koşar (Godot gerekmediği için hızlı). GUT testleri Godot headless kurulumu gerektirdiğinden ayrı, daha yavaş bir CI adımı olarak tasarlanır — bu ayrım Faz 1'de CI iskeleti kurulurken netleşecek.
