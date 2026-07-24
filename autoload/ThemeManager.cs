using System;
using System.Collections.Generic;
using Godot;

namespace FreeRehabHub.App.Autoload;

public partial class ThemeManager : Node
{
    private const ThemeVariant DefaultVariant = ThemeVariant.Default;

    private static readonly Dictionary<ThemeVariant, string> ThemePaths = new()
    {
        [ThemeVariant.Default] = "res://themes/default.tres",
        [ThemeVariant.HighContrast] = "res://themes/high-contrast.tres",
        [ThemeVariant.LowStimulation] = "res://themes/low-stimulation.tres",
    };

    public event Action<ThemeVariant>? ThemeChanged;

    public ThemeVariant CurrentVariant { get; private set; } = DefaultVariant;

    public override void _Ready()
    {
        ApplyTheme(DefaultVariant);
    }

    public void ApplyTheme(ThemeVariant variant)
    {
        if (!ThemePaths.TryGetValue(variant, out var path))
        {
            GD.PushWarning($"Tanımsız tema varyantı: {variant}");
            return;
        }

        var theme = GD.Load<Theme>(path);
        if (theme is null)
        {
            GD.PushError($"Tema kaynağı yüklenemedi: {path}");
            return;
        }

        GetTree().Root.Theme = theme;
        CurrentVariant = variant;
        ThemeChanged?.Invoke(variant);
    }
}
