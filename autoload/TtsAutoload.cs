using Godot;

namespace FreeRehabHub.App.Autoload;

// Godot'un yerleşik DisplayServer TTS'ini sarmalıyor (OS-native: Linux'ta speech-dispatcher/
// espeak-ng, Windows'ta SAPI5, macOS'ta NSSpeechSynthesizer — motora yeni bir bağımlılık
// eklenmiyor). TtsGetVoices() (tüm sesleri listeleyen genel çağrı) Linux'ta Godot 4.7'de
// hata veriyor ("Parameter 'synth' is null") — bu yüzden her zaman dil-filtreli
// TtsGetVoicesForLanguage() kullanılıyor.
public partial class TtsAutoload : Node
{
    private const string EnglishLocale = "en";
    private const string TurkishLanguageCode = "tr";
    private const string EnglishLanguageCode = "en";
    private const int DefaultVolume = 100;
    private const float DefaultPitch = 1.0f;
    private const float DefaultRate = 1.0f;

    private LocalizationAutoload _localization = null!;

    public bool IsAvailable { get; private set; }

    public override void _Ready()
    {
        _localization = GetNode<LocalizationAutoload>("/root/LocalizationAutoload");
        IsAvailable = DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech);
    }

    public void Speak(string text)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var languageCode = _localization.CurrentLocale == EnglishLocale ? EnglishLanguageCode : TurkishLanguageCode;
        var voiceIds = DisplayServer.TtsGetVoicesForLanguage(languageCode);
        var voiceId = voiceIds.Length > 0 ? voiceIds[0] : string.Empty;

        DisplayServer.TtsStop();
        DisplayServer.TtsSpeak(text, voiceId, DefaultVolume, DefaultPitch, DefaultRate, utteranceId: 0, interrupt: true);
    }

    public void Stop()
    {
        if (IsAvailable)
        {
            DisplayServer.TtsStop();
        }
    }
}
