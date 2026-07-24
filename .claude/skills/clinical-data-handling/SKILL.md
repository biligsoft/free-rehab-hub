---
name: clinical-data-handling
description: Hasta/sağlık verisiyle çalışırken uyulması gereken kurallar — şifreleme, audit log, telifli klinik içerik politikası, "tıbbi cihaz değildir" kapsamı. Hasta kaydına, oturum verisine, değerlendirme sonucuna veya loglamaya dokunan herhangi bir kod yazmadan önce oku.
---

# Klinik Veri Kuralları — FreeRehabHub

Bu uygulama gerçek hastaların sağlık verisini tutuyor. Bu skill, veriye "biraz daha rahat" davranmanın kabul edilemez olduğu noktaları listeler.

## 1. Şifreleme

- Tüm hasta verisi diskte SQLCipher ile şifreli tutulur. Şifreleme anahtarı **asla** koda, config dosyasına veya git'e gömülmez.
- Anahtar, OS anahtar zincirinden (Windows Credential Manager / macOS Keychain / libsecret) ya da terapistin ilk kurulumda belirlediği bir parolanın türetilmesinden gelir — bu mekanizma Faz 1/2'de netleşecek, ama "anahtar sabit string" asla kabul edilmez.
- Yedekleme/dışa aktarma dosyaları da şifrelidir; hiçbir export akışı düz metin (plaintext) hasta verisi üretmez.

## 2. Loglama

- Konsol/dosya loglarına **ham hasta verisi yazılmaz** (isim, oturum notu, değerlendirme cevabı, landmark verisi vb.).
- Audit log sadece metadata tutar: kim (terapist id), ne zaman, hangi kayda (hasta id, kayıt türü), hangi işlem (görüntüledi/düzenledi/sildi). İçerik değil, erişim izi.
- Hata ayıklama (debug) logları eklerken "bu satır bir hasta adı/notu içeriyor mu" diye kontrol et — içeriyorsa maskeleme veya kaldırma.

## 3. Veri erişim yolu

Hasta verisine her erişim `Data` katmanındaki repository'ler üzerinden geçer — UI, modül veya servis kodunda doğrudan SQL/dosya erişimi yasak. Bunun sebebi sadece temizlik değil: şifreleme ve audit logging repository seviyesinde merkezi olarak uygulanıyor; bu yolu atlayan kod hem şifrelemeyi hem audit trail'i delebilir.

## 4. İçerik telif politikası (`content-packs/`)

- Standart, isimli, yayıncısı olan klinik değerlendirme ölçekleri (belirli test bataryaları vb.) genelde telifli veya kullanım lisansı gerektirir. Bunların madde metinlerini, puanlama tablolarını `content-packs/` altına **commit etme**.
- `content-packs/` sadece: (a) projeye özel yazılmış/telifsiz örnek içerik, (b) hakları teyit edilmiş ve buna dair bir not/kaynak eklenmiş içerik barındırır.
- Bir katkıcı/kullanıcı gerçek bir telifli ölçeği kullanmak isterse, bu içerik kullanıcının kendi yerel `content-packs/` klasörüne (gitignore'lu) eklenir, repo'ya girmez. Bunu `module-development` skill'indeki modül şablonunda da hatırlat.

## 5. "Tıbbi cihaz değildir" kapsamı

- Uygulamanın hiçbir ekranı/metni skorlama sonucunu bir "tanı" veya "otomatik klinik karar" gibi sunmaz. Sonuçlar her zaman terapistin yorumlayacağı bir veri noktası olarak çerçevelenir.
- Yeni bir sonuç/rapor ekranı yazarken metin taslağını bu çerçeveye göre kur: "X skoru Y oldu" tarzı nötr bilgi, "hasta Z durumundadır" tarzı tanısal ifade değil.
- PDF rapor export'unda (Faz 6) ve genel dokümantasyonda bu kapsamı belirten bir feragatname bulunmalı.

## 6. Çocuk modu / kiosk ek dikkat

Çocuk modunda (Faz 7) ekrana gelen hiçbir yerde ham klinik veri (terapist notları, skor detayları, tanısal bilgi) gösterilmez — çocuk sadece oyunu/egzersizi ve ödül/motivasyon geri bildirimini görür. Klinik detay her zaman terapist moduna hapsedilir.
