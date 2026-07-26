namespace FreeRehabHub.Data.Tests;

// Yanlış parolayla başarısız bir SqliteConnection açma denemesinden sonra, native SQLitePCLRaw/
// e_sqlcipher handle'ının serbest bırakılması Windows'ta senkron değil (CI'da F8.09'da
// gözlemlendi) — bu yüzden test temizliğinde dosya/dizin silme birkaç kısa denemeyle yapılıyor.
// Bu sadece test-geçici-dosyaları için; üretim kodunda hiçbir yerde bu desen (başarısız bağlantı
// sonrası aynı dosyayı silme) kullanılmıyor.
internal static class TestFileCleanup
{
    private const int MaxAttempts = 5;
    private const int RetryDelayMilliseconds = 50;

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
            catch (IOException) when (attempt < MaxAttempts)
            {
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
            catch (IOException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(RetryDelayMilliseconds);
            }
        }
    }
}
