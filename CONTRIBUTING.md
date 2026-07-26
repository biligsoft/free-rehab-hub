# Katkıda Bulunma Rehberi — FreeRehabHub

FreeRehabHub, fizyoterapi, ergoterapi, konuşma terapisi, psikoloji ve özel eğitim disiplinlerini kapsayan, açık kaynak ve tamamen yerel (offline) çalışan bir terapi/özel eğitim uygulamasıdır. Proje, katkıcıların **core'a dokunmadan yeni bir terapi modülü ekleyebilmesi** için modüler olarak tasarlandı — en yaygın katkı yolu budur ve aşağıda ayrı bir başlıkta ele alınıyor.

Bu proje [MIT lisansı](LICENSE) ile yayınlanıyor; gönderdiğiniz her katkı bu lisans altında kabul edilir.

## Davranış

Saygılı, yapıcı ve konu odaklı iletişim bekleniyor. Bu, tıbbi/eğitimsel bağlamda gerçek hastalarla çalışan bir uygulama — tartışmalarda bu hassasiyeti göz önünde bulundurun.

## Geliştirme Ortamı Kurulumu

- **Godot 4.x, .NET/Mono sürümü** (geliştirme 4.7 ile yapılıyor) — [godotengine.org](https://godotengine.org/download) üzerinden ".NET" etiketli sürümü indirin, standart (GDScript-only) sürüm yeterli değil.
- **.NET 8 SDK**.
- Repoyu klonlayıp kökte:
  ```
  dotnet restore FreeRehabHub.sln
  dotnet build FreeRehabHub.sln
  ```
- Godot editöründe açmak için `project.godot`'u Godot'un proje yöneticisinden import edin.
- `services/mediapipe-service/` (kamera tabanlı modüller için) ayrı bir native Python süreci — kurulumu için o klasördeki `requirements.txt`/`pyproject.toml`'a bakın. Bu servise dokunmuyorsanız kurmanıza gerek yok.

## Proje Mimarisini Anlamak

Mimari kararlar, katman kuralları ve klasör yapısı için önce **[`CLAUDE.md`](CLAUDE.md)**'yi okuyun — proje kökündeki bu dosya, Godot'a özgü kısıtlamalar dahil tüm mimari gerekçeleri içeriyor. Daha derin, göreve özel rehberler `.claude/skills/` altında:

| Dosya | Ne zaman okumalı |
|---|---|
| `.claude/skills/godot-csharp-standards/SKILL.md` | Herhangi bir `.cs`/`.tscn` dosyası yazmadan/düzenlemeden önce — kod stili, katman kuralları, sahne organizasyonu |
| `.claude/skills/module-development/SKILL.md` | Yeni bir terapi modülü eklerken veya mevcut birini değiştirirken |
| `.claude/skills/testing-approach/SKILL.md` | Test yazarken |
| `.claude/skills/clinical-data-handling/SKILL.md` | Hasta verisine, loglamaya veya `content-packs/`'e dokunan herhangi bir kod için — **zorunlu okuma**, bkz. aşağıdaki bölüm |

Özet: `Core`/`Domain`/`Data`/`Modules.Contracts`/`Services` katmanları Godot'tan bağımsızdır (`using Godot;` yasak) ve xUnit ile test edilir; sadece UI ve modül sahne/controller'ları Godot API'sine dokunur.

## En Yaygın Katkı Yolu: Yeni Bir Terapi Modülü Eklemek

1. `templates/module-starter/` altındaki `exercise/` (kamera/etkileşimli egzersiz) veya `assessment/` (form tabanlı değerlendirme) varyantını kopyalayın — hangisini seçeceğinize dair rehber o klasördeki `README.md`'de.
2. `templates/module-starter/README.md`'deki adım adım talimatı izleyin (id/isim değiştirme, dosya taşıma, `manifest.json` doldurma).
3. `manifest.json`'daki `displayName`/`description` **hem Türkçe hem İngilizce** doldurulmalı — tek dilli manifest kabul edilmez.
4. Elle bir registry dosyasına eklemeniz gerekmiyor — modül otomatik keşfediliyor.
5. Skorlama mantığınız için en az geçerli girdi, sınır değer ve geçersiz girdi senaryolarını kapsayan testler ekleyin (`testing-approach` skill'i).

### İçerik telif politikası

Standart, isimli, yayıncısı olan klinik değerlendirme ölçekleri genelde telifli veya lisans gerektirir. `content-packs/` altına **sadece** özgün/telifsiz içerik ya da hakları teyit edilmiş içerik (kaynak notuyla) eklenir. Telifli bir ölçek kullanmak istiyorsanız, bunu kendi yerel (gitignore'lu) `content-packs/` klasörünüzde tutun, repoya PR açmayın. Detay: `clinical-data-handling` skill'i § 4.

## Klinik Veri ve Güvenlik Kuralları

Bu uygulama gerçek hastaların sağlık verisini tutuyor. Hasta kaydına, oturum verisine, değerlendirme sonucuna veya loglamaya dokunan **herhangi bir** PR göndermeden önce `.claude/skills/clinical-data-handling/SKILL.md`'yi tam okuyun. Özet kurallar:

- Hasta verisine her erişim `Data` katmanındaki repository'ler üzerinden geçer — UI/servis/modül kodunda doğrudan SQL/dosya erişimi kabul edilmez.
- Konsol/dosya loglarına ham hasta verisi (isim, not, değerlendirme cevabı vb.) yazılmaz.
- Hiçbir ekran/metin sonucu bir "tanı" veya "otomatik klinik karar" gibi sunmaz — bu uygulama bir tıbbi cihaz değildir.
- Çocuk/kiosk moduna gelen hiçbir yerde ham klinik veri (terapist notları, skor detayları) gösterilmez.

## Test Çalıştırma

```
dotnet test FreeRehabHub.sln
```

Bu, `Core`/`Domain`/`Data`/`Modules.Contracts`/`Services` katmanlarını ve her modülün skorlama testlerini çalıştırır (Godot editörü açmaya gerek yok, hızlıdır). Sahne/controller (UI) davranışını değiştiren değişiklikler için henüz otomatik bir Godot test altyapısı (GUT) kurulu değil — bu tür değişiklikleri Godot editöründe veya `godot --headless` ile elle çalıştırıp doğrulayın; PR açıklamasında ne test ettiğinizi belirtin.

PR'ınızın CI'da (`.github/workflows/ci.yml`: restore → build → test) yeşil geçmesi gerekiyor.

## Kod Stili Özeti

Tam kurallar `godot-csharp-standards` skill'inde; en sık karşılaşılan noktalar:

- Dosya başına tek sınıf, standart C# PascalCase/camelCase.
- Magic number yasak — isimlendirilmiş sabit kullanın.
- Node referanslarında string path yasak — tip-güvenli `[Export] NodePath` + `GetNode<T>()` deseni.
- Katmanlar arası iletişim C# `event`/arayüz ile; Godot `signal`'leri sadece sahne-içi node-to-node iletişim için.
- Alt katmanlar üst katmanları tanımaz (`Domain` → `Data`'yı tanımaz, vb. — bağımlılık yönü CLAUDE.md § 3'te).

## Pull Request Süreci

1. Fork/branch açın, tek bir mantıksal değişikliği kapsayan bir PR hazırlayın (birden fazla ilgisiz değişikliği tek PR'da birleştirmeyin).
2. Commit mesajlarınız açıklayıcı olsun (bu projenin kendi geliştirme sürecinde kullandığı `F<faz>.<adım> - ...` numaralı commit formatı içsel bir kayıt tutma yöntemidir, dış katkılarda buna uymanız beklenmiyor).
3. PR açıklamasında şunları belirtin: ne değişti, neden, nasıl test edildi.
4. Bir modül ekliyorsanız `manifest.json`'ın TR+EN dolu olduğunu, testlerin geçtiğini ve (varsa) telifli içerik eklemediğinizi PR açıklamasında teyit edin.

## Sorularınız mı var?

Bir konuyu tartışmak veya netleştirmek isterseniz, koda dalmadan önce bir GitHub issue açın — özellikle mimariyi etkileyecek büyüklükte bir değişiklikse.
