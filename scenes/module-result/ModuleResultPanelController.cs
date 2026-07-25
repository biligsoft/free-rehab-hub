using FreeRehabHub.App.Autoload;
using FreeRehabHub.App.Shells;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Services;
using Godot;

namespace FreeRehabHub.App.ModuleResultScreen;

public partial class ModuleResultPanelController : Control
{
    private const string EnglishLocale = "en";

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _scoreLabelPath = null!;
    [Export] private NodePath _metricsContainerPath = null!;
    [Export] private NodePath _doneButtonPath = null!;

    private Label _titleLabel = null!;
    private Label _scoreLabel = null!;
    private VBoxContainer _metricsContainer = null!;
    private Button _doneButton = null!;
    private SessionContext _sessionContext = null!;
    private LocalizationAutoload _localization = null!;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _scoreLabel = GetNode<Label>(_scoreLabelPath);
        _metricsContainer = GetNode<VBoxContainer>(_metricsContainerPath);
        _doneButton = GetNode<Button>(_doneButtonPath);
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _localization = GetNode<LocalizationAutoload>("/root/LocalizationAutoload");

        _doneButton.Pressed += OnDonePressed;

        Load();
    }

    private void Load()
    {
        var result = _sessionContext.LastModuleResult;
        if (result is null)
        {
            _titleLabel.Text = "Sonuç bulunamadı.";
            return;
        }

        var manifest = _sessionContext.ActiveModuleManifest;
        var patient = _sessionContext.ActivePatient;
        var moduleDisplayName = manifest is not null ? Localize(manifest.DisplayName) : result.ModuleId;

        _titleLabel.Text = patient is not null
            ? $"Sonuç — {moduleDisplayName} ({patient.FullName})"
            : $"Sonuç — {moduleDisplayName}";
        _scoreLabel.Text = $"Skor: {result.NormalizedScore:P0}";

        foreach (var child in _metricsContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var (key, value) in result.Metrics)
        {
            _metricsContainer.AddChild(new Label { Text = $"{MetricKeyFormatter.Humanize(key)}: {value:0.##}" });
        }
    }

    private void OnDonePressed()
    {
        _sessionContext.SetActiveModuleManifest(null);
        _sessionContext.SetLastModuleResult(null);

        // Kiosk modunda (Role == Child) hasta bağlamı korunuyor — çocuk ChildKioskShell'e
        // dönüp aynı hasta için başka bir modül oynatmaya devam edebilmeli.
        if (_sessionContext.Role != UserRole.Child)
        {
            _sessionContext.SetActivePatient(null);
        }

        GetTree().ChangeSceneToFile(KioskNavigation.ResolveHomeScenePath(_sessionContext.Role));
    }

    private string Localize(LocalizedText text)
    {
        return _localization.CurrentLocale == EnglishLocale ? text.En : text.Tr;
    }
}
