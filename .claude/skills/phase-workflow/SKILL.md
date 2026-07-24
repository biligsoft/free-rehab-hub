---
name: phase-workflow
description: Bu projede işin nasıl ilerletileceği — tek adım/onay döngüsü, faz sonu /compact, numaralı commit formatı ve ilerleme takibi. Herhangi bir göreve (kod yazma, dosya oluşturma, faz geçişi) başlamadan önce oku.
---

# Çalışma Akışı — FreeRehabHub

Bu proje tek geliştirici (fizyoterapist, Godot'ta yeni) + Claude Code ile adım adım inşa ediliyor. Bu skill, o sürecin kurallarını tanımlar.

## 1. Tek adım kuralı

- Her seferinde **tek, atomik bir adım** yap (örn. "bir repository sınıfı ekle", "bir sahne oluştur" — "Faz 2'nin tamamını yap" değil).
- Adımı bitirdikten sonra **dur**, ne yaptığını kısaca özetle, kullanıcının onayını bekle.
- Onay gelmeden bir sonraki adıma **geçme**, birden fazla onay gerektiren adımı tek mesajda **birleştirme**.
- "Adım" ölçeği: genelde tek bir dosya/sınıf/sahne veya birbirine sıkı bağlı birkaç dosya (ör. arayüz + tek implementasyonu). Bir faz onlarca adıma bölünebilir — bu normaldir, aceleye getirme.

## 2. Faz sonu

Bir faz (CLAUDE.md § Yol Haritası'ndaki numaralı fazlardan biri) tamamlanıp kullanıcı onayladığında:
1. Faz özetini kısaca çıkar (ne yapıldı, çıktı ne).
2. Kullanıcıya `/compact` çalıştırmasını hatırlat (context'i temizlemek için).
3. İlerleme takip dosyasını güncelle (bkz. § 4).

Fazın ortasında context sıkışırsa da aynı şekilde durup öneri sunulabilir, ama fazı yarıda kesmeden önce kullanıcıya sor.

## 3. Commit mesaj formatı

Her commit şu formatta, numaralandırılmış olmalı:

```
F<faz>.<adım> - <kısa özet, emir kipi değil, ne yapıldığını anlatır>
```

Örnekler:
```
F2.03 - Hasta repository implementasyonu eklendi
F4.11 - IExerciseModule sözleşmesi tanımlandı
F5.02 - MediaPipe WebSocket client iskeleti
```

- `<faz>` ve `<adım>` CLAUDE.md'deki faz numarasına ve o faz içindeki sıradaki adım sayacına karşılık gelir (adım sayacı her fazın başında 1'den başlar).
- Bir commit birden fazla küçük adımı kapsıyorsa en yüksek adım numarasını kullan, ama mümkünse commit'i adım sınırında tut (bire bir commit = adım en temizi).
- Faz/adım kapsamına girmeyen tek seferlik işler (README düzeltmesi, CI ayarı) için `F0.NN` kullan (faz-bağımsız işler).

## 4. İlerleme takibi

`docs/PROGRESS.md` dosyası, oturumlar arası devamlılık için tutulur (context sıfırlansa/compact edilse bile nerede kalındığı kaybolmasın diye):

```markdown
## Güncel durum
- Faz: 2 (Hasta Yönetimi + Veri Katmanı)
- Son tamamlanan adım: F2.03
- Son commit: F2.03 - Hasta repository implementasyonu eklendi

## Faz geçmişi
- Faz 1: tamamlandı (2026-0X-XX)
```

Bu dosya henüz yoksa (proje henüz Faz 1'e başlamadıysa) bir adım tamamlandığında oluştur. Her onaylanmış adımdan sonra güncelle — bu bir "adım" sayılmaz, ilgili koda dokunan adımın parçasıdır, ayrı onay gerektirmez.

## 5. Görev başlamadan önce

Yeni bir göreve başlarken (kod yazma, dosya oluşturma):
1. `docs/PROGRESS.md`'yi oku, hangi fazda/adımda olduğunu doğrula.
2. Göreve uygun diğer skill'i oku (`godot-csharp-standards` her zaman; `module-development` modül işiyse; `testing-approach` test yazarken; `clinical-data-handling` hasta verisine dokunan herhangi bir kod için).
3. Tek adımı uygula, dur.

## 6. Kapsam dışına çıkma

Kullanıcı onaylamadığı sürece: faz sırasını değiştirme, planlanmamış bir refactor'a girişme, "madem buradayım" diyip ilgisiz bir iyileştirme ekleme. Bir sorun/risk fark edersen not et ve sor, sessizce kapsamı genişletme.
