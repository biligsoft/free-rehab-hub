---
name: godot-csharp-standards
description: C# kodlama standartları, SOLID/katman kuralları ve Godot'a özgü sahne/signal/node-referans/autoload kuralları. Herhangi bir .cs veya .tscn dosyası yazmadan/düzenlemeden önce oku.
---

# Godot + C# Kodlama Standartları — FreeRehabHub

Bu proje Godot 4.x (.NET) üzerinde C# ile geliştiriliyor. Aşağıdaki kurallar mimari kararlarla (bkz. CLAUDE.md § Mimari) doğrudan bağlantılıdır — sebep belirtilmeden verilmiş bir kural yoktur.

## 1. Katman bağımlılık kuralı (ihlali kabul edilmez)

`src/FreeRehabHub.Core`, `src/FreeRehabHub.Domain`, `src/FreeRehabHub.Data`, `src/FreeRehabHub.Modules.Contracts`, `src/FreeRehabHub.Services` projelerinin hiçbiri `Godot` namespace'ini **kullanamaz** (`using Godot;` yasak, `GodotObject`/`Node`/`Control` türevi sınıf yasak). Bu katmanlar sade .NET class library'lerdir ve Godot editörü açılmadan xUnit ile test edilir.

Godot'a bağımlı olabilecek TEK yerler:
- `FreeRehabHub.App` (ana Godot projesi)
- `scenes/`, `autoload/` altındaki her şey
- `modules/<modül>/*Controller.cs` (sahneye bağlı script) — ama aynı modül içindeki skorlama sınıfı yine Godot-bağımsız olmalı (bkz. `module-development` skill'i)

Bağımlılık yönü: `UI/Modules → Services → Domain → Core`, `Data → Domain → Core`. Domain asla Data'yı tanımaz (repository arayüzleri Domain'de tanımlanır, Data implemente eder).

Yeni bir dosyaya `using Godot;` eklemeden önce, o dosyanın hangi projede olduğunu kontrol et. Yanlış projede Godot referansı varsa, mantığı Godot-bağımsız bir sınıfa çıkar, Godot tarafında sadece ince bir adaptör bırak.

## 2. İsimlendirme

- Sınıf, arayüz, enum, public metot/property: `PascalCase`. Arayüzler `I` ile başlar (`IExerciseModule`).
- Private/protected alanlar: `_camelCase` (alt çizgi + camelCase).
- Parametreler ve yerel değişkenler: `camelCase`.
- Sabitler: `PascalCase` (`.NET` konvansiyonu — `SCREAMING_CASE` kullanma).
- Godot node/sahne isimleri (`.tscn` içindeki node adları): `PascalCase`, node'un rolünü yansıtsın (`PatientListPanel`, değil `Panel2`).
- Dosya/klasör adları (Godot kaynakları: `.tscn`, `.tres`, sahne script'i olmayan asset'ler): `kebab-case` veya `snake_case` — Godot ekosisteminin konvansiyonu. C# dosyaları (`.cs`) her zaman içerdiği sınıfla birebir aynı `PascalCase` isimde.

## 3. Dosya başına tek sınıf

Her `.cs` dosyası tam olarak bir public sınıf/arayüz/enum içerir, dosya adı o tipin adıyla birebir aynıdır. İç içe yardımcı `private` sınıflar (ör. bir builder'ın private state nesnesi) istisnadır, ama bunlar da nadiren gerekli olmalı. Aynı dosyada birden fazla public tip görürsen, bölme zamanı gelmiştir.

## 4. Magic number yasağı

Kod içinde açıklamasız sayısal/string literal yasak — anlamlı bir `const`, `static readonly`, veya `enum` üyesi olarak adlandırılmalı. Örnek: `if (score > 70)` değil, `if (score > PassingScoreThreshold)`. Bu özellikle skorlama/eşik mantığı içeren modül kodunda (bkz. `IAssessmentModule.Score()`, `IExerciseModule` metrikleri) kritik — klinik eşik değerlerinin nereden geldiği kod okunarak anlaşılabilmeli.

## 5. SOLID uygulaması — proje-özel notlar

- **SRP:** Bir `*Controller.cs` (Node-türevi modül sınıfı) sahne yaşam döngüsünü yönetir, skorlama/iş mantığını yönetmez — bunu ayrı bir Godot-bağımsız sınıfa devret (bkz. `modules/*/Scoring/`).
- **OCP:** Yeni bir modül yeteneği eklerken (`ModuleManifest.RequiredCapabilities`) mevcut enum/switch yapılarını genişletmek yerine string-tabanlı capability listesini kullan — bu zaten OCP için tasarlandı, bozma.
- **LSP:** `IExerciseModule` implementasyonları `Completed` event'ini her zaman tam olarak bir kez tetiklemeli (ne eksik ne fazla) — çağıran kod (`ModuleHost`) bu garantiye güvenir.
- **ISP:** Kamera gerektirmeyen bir `IExerciseModule` asla `IPoseAwareModule`'ü implemente etmemeli — gereksiz arayüz implementasyonu ekleme.
- **DIP:** UI ve Modules katmanı, Services'in somut sınıflarını değil arayüzlerini (`IPoseTrackingService`, `IPatientRepository` vb.) referans alır. Somut implementasyonlar DI ile (Godot autoload üzerinden basit bir service locator/composition root) bağlanır.

## 6. Godot'a özgü kurallar

**Sahne organizasyonu:** Bir ekran/bileşen = bir `.tscn`. "Tanrı sahne" (tek sahnede onlarca sorumluluk) yasak — `scenes/patient/` altında liste, profil, form gibi alt sorumluluklar ayrı sahneler/alt-sahneler olarak kurulur ve gerektiğinde `PackedScene` ile instantiate edilir.

**Signal kullanımı:** Godot signal'leri (`[Signal]`) sadece **aynı sahne ağacı içindeki node-to-node** iletişim için kullanılır (ör. bir buton tıklanınca ebeveyn panel'i haberdar etmek). Katmanlar-arası iletişim (modül → ModuleHost, Service → UI) için C# `event`/arayüz kullan — bunlar Godot-bağımsız katmanlarda da çalışır ve `Modules.Contracts` sözleşmeleriyle tutarlıdır (ör. `IExerciseModule.Completed`). Signal'i asla katman sınırını geçmek için kullanma.

**Node referansları:** `GetNode("../../Some/Path")` gibi kırılgan string yollar yasak. `[Export] private NodePath _fooPath` + `GetNode<T>(_fooPath)` ya da (Godot 4'te tercih edilen) doğrudan `[Export] private Foo _foo;` tip-güvenli export kullan. Referansları `_Ready()` içinde bir kez çöz, döngü içinde tekrar tekrar `GetNode` çağırma.

**Autoload'lar (singleton):** Şu an tanımlı autoload'lar ve tek sorumlulukları:
- `SessionContext` — aktif hasta/terapist/rol (Terapist/Çocuk modu) durumu
- `AppServices` — composition-root/service-locator: `AppServices.Unlock(password)` çağrıldıktan sonra (kilit ekranında, SQLCipher DB açıldığında) kurulan `PatientService`/`TherapistService`/`TherapySessionService` örneklerini tutar ve sahnelere açar. `SessionContext`'ten ayrı tutuluyor çünkü sorumluluğu farklı: biri "kim/hangi hasta aktif" durumunu, diğeri "hangi servis örnekleri kurulu" bağlanmasını taşıyor.
- `LocalizationAutoload` — TR/EN dil değiştirme, Godot `TranslationServer`'ı sarar
- `ThemeManager` — erişilebilirlik temaları (yüksek kontrast, düşük uyaran)
- `ModuleRegistryAutoload` — `IModuleRegistry`'yi sahne ağacına açar, `.tscn` yüklemesini üstlenir (Godot-bağımlı registry sınırı burasıdır)

Yeni bir autoload eklemeden önce sor: bu gerçekten global mi, yoksa bir sahnenin lokal state'i mi? Autoload'lar ince kalmalı — ağır mantığı Services katmanına devret, autoload sadece Godot köprüsü olsun.
