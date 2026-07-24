using System;
using Godot;

namespace FreeRehabHub.App.Autoload;

public partial class LocalizationAutoload : Node
{
    private const string DefaultLocale = "tr";

    private static readonly string[] SupportedLocales = { "tr", "en" };

    public event Action<string>? LocaleChanged;

    public string CurrentLocale { get; private set; } = DefaultLocale;

    public override void _Ready()
    {
        SetLocale(DefaultLocale);
    }

    public void SetLocale(string locale)
    {
        if (Array.IndexOf(SupportedLocales, locale) < 0)
        {
            GD.PushWarning($"Desteklenmeyen dil kodu: {locale}");
            return;
        }

        CurrentLocale = locale;
        TranslationServer.SetLocale(locale);
        LocaleChanged?.Invoke(locale);
    }
}
