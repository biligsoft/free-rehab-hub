using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.ColorSort.Scoring;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.Modules.ColorSort;

public partial class ColorSortController : Node, IExerciseModule
{
    private const int TotalRounds = 8;

    private static readonly Color[] Palette = { Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow };

    [Export] private NodePath _roundLabelPath = null!;
    [Export] private NodePath _targetDisplayPath = null!;
    [Export] private NodePath _redBinButtonPath = null!;
    [Export] private NodePath _blueBinButtonPath = null!;
    [Export] private NodePath _greenBinButtonPath = null!;
    [Export] private NodePath _yellowBinButtonPath = null!;

    private readonly ColorSortScorer _scorer = new();
    private readonly RandomNumberGenerator _random = new();

    private Label _roundLabel = null!;
    private ColorRect _targetDisplay = null!;
    private Button _redBinButton = null!;
    private Button _blueBinButton = null!;
    private Button _greenBinButton = null!;
    private Button _yellowBinButton = null!;

    private ModuleContext _context = null!;
    private int _currentRound;
    private int _correctCount;
    private double _totalResponseTimeSecondsForCorrect;
    private ulong _roundStartTimeMsec;
    private Color _currentTargetColor;
    private bool _isRunning;
    private bool _completed;

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.freerehabhub.color-sort",
        Version = "1.0.0",
        Kind = ModuleKind.Exercise,
        DisplayName = new LocalizedText { Tr = "Renk Kutusu", En = "Color Sort" },
        Description = new LocalizedText
        {
            Tr = "Ekranda beliren rengi doğru kutuya tıklayarak eşleştiren, kamera gerektirmeyen bir " +
                 "sınıflandırma ve tepki hızı egzersizi.",
            En = "A camera-free sorting and reaction-time exercise that matches the color shown on " +
                 "screen to the correct bin."
        },
        Disciplines = new List<Discipline> { Discipline.SpecialEducation, Discipline.OccupationalTherapy },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 1 },
        RequiredCapabilities = new List<string>(),
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.ColorSort.ColorSortController",
        ScenePath = "res://modules/com.freerehabhub.color-sort/ColorSort.tscn",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            ["totalRounds"] = new LocalizedText { Tr = "Toplam Tur", En = "Total Rounds" },
            ["correctCount"] = new LocalizedText { Tr = "Doğru Sayısı", En = "Correct Count" },
            ["incorrectCount"] = new LocalizedText { Tr = "Yanlış Sayısı", En = "Incorrect Count" },
            ["averageResponseTimeSeconds"] = new LocalizedText
            {
                Tr = "Ortalama Yanıt Süresi (sn)",
                En = "Average Response Time (s)"
            }
        }
    };

    public event EventHandler<ModuleResult>? Completed;

    public override void _Ready()
    {
        _roundLabel = GetNode<Label>(_roundLabelPath);
        _targetDisplay = GetNode<ColorRect>(_targetDisplayPath);
        _redBinButton = GetNode<Button>(_redBinButtonPath);
        _blueBinButton = GetNode<Button>(_blueBinButtonPath);
        _greenBinButton = GetNode<Button>(_greenBinButtonPath);
        _yellowBinButton = GetNode<Button>(_yellowBinButtonPath);

        _redBinButton.Pressed += OnRedBinPressed;
        _blueBinButton.Pressed += OnBlueBinPressed;
        _greenBinButton.Pressed += OnGreenBinPressed;
        _yellowBinButton.Pressed += OnYellowBinPressed;
    }

    public Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _currentRound = 0;
        _correctCount = 0;
        _totalResponseTimeSecondsForCorrect = 0.0;
        _completed = false;
        UpdateRoundLabel();
        return Task.CompletedTask;
    }

    public void OnActivated()
    {
        _isRunning = true;
        StartNextRound();
    }

    public void OnPaused()
    {
        _isRunning = false;
    }

    public void OnResumed()
    {
        _isRunning = true;
    }

    public void OnDeactivated()
    {
        // LSP: Completed tam olarak bir kez tetiklenmeli (bkz. godot-csharp-standards § 5) — kullanıcı
        // turları bitirmeden modülden çıkarsa bile ModuleHost'un beklediği garanti burada sağlanıyor.
        _isRunning = false;
        RaiseCompletedIfNeeded();
    }

    private void StartNextRound()
    {
        if (_currentRound >= TotalRounds)
        {
            RaiseCompletedIfNeeded();
            return;
        }

        _currentTargetColor = Palette[_random.RandiRange(0, Palette.Length - 1)];
        _targetDisplay.Color = _currentTargetColor;
        _roundStartTimeMsec = Time.GetTicksMsec();
        UpdateRoundLabel();
    }

    private void OnRedBinPressed() => OnBinPressed(Colors.Red);

    private void OnBlueBinPressed() => OnBinPressed(Colors.Blue);

    private void OnGreenBinPressed() => OnBinPressed(Colors.Green);

    private void OnYellowBinPressed() => OnBinPressed(Colors.Yellow);

    private void OnBinPressed(Color pressedColor)
    {
        if (!_isRunning || _completed)
        {
            return;
        }

        if (pressedColor == _currentTargetColor)
        {
            var elapsedSeconds = (Time.GetTicksMsec() - _roundStartTimeMsec) / 1000.0;
            _correctCount++;
            _totalResponseTimeSecondsForCorrect += elapsedSeconds;
        }

        _currentRound++;
        StartNextRound();
    }

    private void UpdateRoundLabel()
    {
        _roundLabel.Text = $"Tur: {_currentRound} / {TotalRounds}";
    }

    private void RaiseCompletedIfNeeded()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        var result = _scorer.Score(
            ModuleId, _currentRound, _correctCount, _totalResponseTimeSecondsForCorrect, _context);
        Completed?.Invoke(this, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _redBinButton is not null)
        {
            _redBinButton.Pressed -= OnRedBinPressed;
            _blueBinButton.Pressed -= OnBlueBinPressed;
            _greenBinButton.Pressed -= OnGreenBinPressed;
            _yellowBinButton.Pressed -= OnYellowBinPressed;
        }

        base.Dispose(disposing);
    }
}
