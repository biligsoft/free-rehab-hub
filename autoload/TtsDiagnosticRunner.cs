using System;
using FreeRehabHub.App.Autoload;
using Godot;

namespace FreeRehabHub.App.Diagnostics;

// Kalıcı bir autoload — ama SceneTestRunner (tests/scene-tests/) ile aynı desende, normal
// uygulama çalışırken tamamen etkisiz: sadece FREEREHABHUB_RUN_TTS_CHECK ortam değişkeni set
// edilmişse devreye giriyor.
//
// SceneTestRunner'a dahil EDİLMEDİ çünkü farklı bir çalıştırma modu gerektiriyor: sahne testleri
// --headless'ta çalışıyor, ama DisplayServer.HasFeature(TextToSpeech) --headless modda HER
// PLATFORMDA hep false dönüyor (bu spike'la doğrulandı) — yani TTS'i gerçekten test etmek için
// PENCERELİ (Linux'ta Xvfb altında, Windows/macOS'ta doğrudan) bir çalıştırma şart.
//
// Çalıştırma: FREEREHABHUB_RUN_TTS_CHECK=1 godot --path .  (DİKKAT: --headless OLMADAN)
public partial class TtsDiagnosticRunner : Node
{
    private const string RunCheckEnvironmentVariable = "FREEREHABHUB_RUN_TTS_CHECK";
    private const string TurkishLanguageCode = "tr";

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(RunCheckEnvironmentVariable)))
        {
            return;
        }

        try
        {
            var hasFeature = DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech);
            GD.Print($"TTS-CHECK: DisplayServer.HasFeature(TextToSpeech) = {hasFeature}");

            if (!hasFeature)
            {
                GD.PrintErr("TTS-CHECK: Bu platformda TTS özelliği hiç yok — beklenmeyen bir platform desteği boşluğu.");
                GetTree().Quit(1);
                return;
            }

            // Not: bir dil için hiç ses bulunamazsa Godot 4.7'nin Linux TTS binding'i dahili bir
            // ERROR logluyor ("Parameter 'synth' is null", tts_linux.cpp) — non-fatal, boş dizi
            // dönüyor. Bu beklenen/zararsız — hedef makinede Türkçe ses paketi kurulu olmayabilir,
            // bu durumda bu ERROR satırı görülecek ama uygulama çökmüyor (bkz. aşağıdaki
            // TtsAutoload.Speak() çağrısı, boş sesle varsayılan sese düşüyor).
            var turkishVoices = DisplayServer.TtsGetVoicesForLanguage(TurkishLanguageCode);
            GD.Print($"TTS-CHECK: 'tr' için bulunan ses sayısı = {turkishVoices.Length}");
            if (turkishVoices.Length == 0)
            {
                GD.Print("TTS-CHECK: Bu makinede Türkçe ses paketi kurulu değil (beklenen/olası bir durum, hata değil).");
            }
            else
            {
                GD.Print($"TTS-CHECK: Örnek Türkçe ses kimliği: {turkishVoices[0]}");
            }

            var ttsAutoload = GetNode<TtsAutoload>("/root/TtsAutoload");
            GD.Print($"TTS-CHECK: TtsAutoload.IsAvailable = {ttsAutoload.IsAvailable}");
            ttsAutoload.Speak("Test");
            ttsAutoload.Stop();
            GD.Print("TTS-CHECK: TtsAutoload.Speak()/Stop() hatasız tamamlandı.");

            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"TTS-CHECK: Beklenmeyen hata: {exception}");
            GetTree().Quit(1);
        }
    }
}
