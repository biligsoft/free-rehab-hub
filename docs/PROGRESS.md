## Güncel durum
- Faz: 1 tamamlandı → sıradaki: Faz 2 (Hasta Yönetimi + Veri Katmanı)
- Son tamamlanan adım: F1.06
- Son commit: F1.06 - i18n/tema autoload iskeleti eklendi; ana proje glob/obj çakışması düzeltildi

## Faz geçmişi

### Faz 1 — Temel + Risk Doğrulama (İskelet): tamamlandı (2026-07-24)
- F1.01 - Godot .NET çözüm iskeleti (`FreeRehabHub.csproj`/`.sln`, Godot editörü ile üretildi)
- F1.02 - Katmanlı `src/` class library'leri (Core/Domain/Data/Modules.Contracts/Services), `Directory.Build.props`, composition-root `ProjectReference`'ları
- F1.03 - GitHub Actions CI (`dotnet restore` + `dotnet build`)
- F1.04 - SQLCipher + .NET spike'ı doğrulandı (`Microsoft.Data.Sqlite` 8.0.8 + `SQLitePCLRaw.bundle_e_sqlcipher` 2.1.10, Linux x86_64'te doğrulandı; Windows/macOS doğrulanmadı — bkz. CLAUDE.md §13)
- F1.05 - Modül glob-keşif spike'ı doğrulandı; lazy assembly loading bulgusu (`Assembly.LoadFrom` gerekli) skill/CLAUDE.md'ye işlendi
- F1.06 - i18n/tema autoload iskeleti (`LocalizationAutoload`, `ThemeManager`, TR/EN CSV, tema `.tres` iskeletleri); ana projede gerçek bir yapısal build hatası (obj/ glob çakışması) bulunup düzeltildi

## Açık riskler / bir sonraki fazda hatırlanacaklar
- SQLCipher paket kombinasyonu Windows/macOS'ta henüz test edilmedi (Faz 2 veya Faz 8'de doğrulanmalı).
- `localization/strings.csv` bu ortamda (GUI erişimi yok) Godot editöründen import edilip `project.godot`'un `[internationalization]` bölümüne kaydedilmedi — proje ilk kez editörde açıldığında Project Settings → Localization'dan elle eklenmeli.
- Faz 2'de `SessionContext` ve `ModuleRegistryAutoload` henüz eklenmedi (CLAUDE.md'de tanımlı ama sırası ileriki fazlarda: SessionContext Faz 2, ModuleRegistryAutoload Faz 4).
