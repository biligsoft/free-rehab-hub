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
    private const double ThreeStarScoreThreshold = 0.8;
    private const double TwoStarScoreThreshold = 0.5;

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _scoreLabelPath = null!;
    [Export] private NodePath _metricsContainerPath = null!;
    [Export] private NodePath _rewardContainerPath = null!;
    [Export] private NodePath _starsLabelPath = null!;
    [Export] private NodePath _rewardMessageLabelPath = null!;
    [Export] private NodePath _doneButtonPath = null!;

    private Label _titleLabel = null!;
    private Label _scoreLabel = null!;
    private VBoxContainer _metricsContainer = null!;
    private VBoxContainer _rewardContainer = null!;
    private Label _starsLabel = null!;
    private Label _rewardMessageLabel = null!;
    private Button _doneButton = null!;
    private SessionContext _sessionContext = null!;
    private LocalizationAutoload _localization = null!;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _scoreLabel = GetNode<Label>(_scoreLabelPath);
        _metricsContainer = GetNode<VBoxContainer>(_metricsContainerPath);
        _rewardContainer = GetNode<VBoxContainer>(_rewardContainerPath);
        _starsLabel = GetNode<Label>(_starsLabelPath);
        _rewardMessageLabel = GetNode<Label>(_rewardMessageLabelPath);
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
            _scoreLabel.Visible = false;
            _metricsContainer.Visible = false;
            _rewardContainer.Visible = false;
            return;
        }

        var manifest = _sessionContext.ActiveModuleManifest;
        var patient = _sessionContext.ActivePatient;
        var moduleDisplayName = manifest is not null ? Localize(manifest.DisplayName) : result.ModuleId;

        _titleLabel.Text = patient is not null
            ? $"Sonuç — {moduleDisplayName} ({patient.FullName})"
            : $"Sonuç — {moduleDisplayName}";

        var isChildMode = _sessionContext.Role == UserRole.Child;
        _scoreLabel.Visible = !isChildMode;
        _metricsContainer.Visible = !isChildMode;
        _rewardContainer.Visible = isChildMode;

        if (isChildMode)
        {
            LoadReward(result.NormalizedScore);
            return;
        }

        _scoreLabel.Text = $"Skor: {result.NormalizedScore:P0}";

        foreach (var child in _metricsContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var (key, value) in result.Metrics)
        {
            var label = MetricKeyFormatter.Humanize(key, manifest, _localization.CurrentLocale);
            _metricsContainer.AddChild(new Label { Text = $"{label}: {value:0.##}" });
        }
    }

    private void LoadReward(double normalizedScore)
    {
        var starCount = CalculateStarCount(normalizedScore);
        _starsLabel.Text = starCount switch
        {
            3 => "★★★",
            2 => "★★☆",
            _ => "★☆☆"
        };
        _rewardMessageLabel.Text = starCount switch
        {
            3 => "Harika iş çıkardın!",
            2 => "Çok iyi gidiyorsun!",
            _ => "Denemeye devam et, başarıyorsun!"
        };
    }

    private static int CalculateStarCount(double normalizedScore)
    {
        if (normalizedScore >= ThreeStarScoreThreshold)
        {
            return 3;
        }

        if (normalizedScore >= TwoStarScoreThreshold)
        {
            return 2;
        }

        return 1;
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
