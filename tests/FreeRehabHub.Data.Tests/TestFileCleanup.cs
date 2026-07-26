namespace FreeRehabHub.Data.Tests;

// Yanlış parolayla başarısız bir SqliteConnection açma denemesinden sonra, Windows'ta native
// SQLitePCLRaw/e_sqlcipher handle'ı bu process içinde hiç serbest bırakılmayabiliyor (F8.10/
// F8.11'de denenen 30x200ms'lik retry bile CI'da yetmedi — bu bir zamanlama sorunu değil,
// muhtemelen handle process ömrü boyunca kalıcı sızıyor). Bu yüzden temizlik en-iyi-çaba: birkaç
// kısa deneme + son çare olarak sessizce vazgeçme. Bu sadece test-geçici-dosyaları için (OS temp
// dizininde, CI runner'ı zaten her çalıştırmadan sonra siliniyor) — üretim kodunda hiçbir yerde
// bu desen (başarısız bağlantı sonrası aynı dosyayı silme) kullanılmıyor, temizlik başarısızlığı
// gerçek bir kaynak sızıntısı ya da davranış hatası anlamına gelmiyor.
internal static class TestFileCleanup
{
    private const int MaxAttempts = 5;
    private const int RetryDelayMilliseconds = 100;

    public static void DeleteFile(string path)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return;
            }
            catch (IOException)
            {
                if (attempt == MaxAttempts)
                {
                    return;
                }

                Thread.Sleep(RetryDelayMilliseconds);
            }
        }
    }

    public static void DeleteDirectory(string path)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }

                return;
            }
            catch (IOException)
            {
                if (attempt == MaxAttempts)
                {
                    return;
                }

                Thread.Sleep(RetryDelayMilliseconds);
            }
        }
    }
}
