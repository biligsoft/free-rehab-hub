using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Modules.TargetTap.Scoring;
using Godot;

namespace FreeRehabHub.Modules.TargetTap;

public partial class TargetTapController : Node, IExerciseModule
{
    private const int TotalRounds = 8;
    private const double RoundTimeoutSeconds = 3.0;
    private const float TargetButtonSize = 64f;
    private const float FallbackPlayAreaWidth = 800f;
    private const float FallbackPlayAreaHeight = 400f;

    [Export] private NodePath _roundLabelPath = null!;
    [Export] private NodePath _playAreaPath = null!;

    private readonly TargetTapScorer _scorer = new();
    private readonly RandomNumberGenerator _random = new();

    private Label _roundLabel = null!;
    private Control _playArea = null!;
    private Godot.Timer _roundTimer = null!;
    private Button? _currentTarget;

    private ModuleContext _context = null!;
    private int _currentRound;
    private int _hitCount;
    private double _totalReactionTimeSecondsForHits;
    private ulong _roundStartTimeMsec;
    private bool _isRunning;
    private bool _completed;

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.freerehabhub.target-tap",
        Version = "1.0.0",
        Kind = ModuleKind.Exercise,
        DisplayName = new LocalizedText { Tr = "Hedef Vurma", En = "Target Tap" },
        Description = new LocalizedText
        {
            Tr = "Ekranda rastgele konumlarda beliren hedeflere olabildiğince hızlı ve doğru tıklayarak " +
                 "el-göz koordinasyonu ve reaksiyon süresini çalıştıran, kamera gerektirmeyen bir egzersiz.",
            En = "A camera-free exercise that trains hand-eye coordination and reaction time by tapping " +
                 "targets that appear at random positions on screen, as quickly and accurately as possible."
        },
        Disciplines = new List<Discipline> { Discipline.OccupationalTherapy, Discipline.Psychology },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 1 },
        RequiredCapabilities = new List<string>(),
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.TargetTap.TargetTapController",
        ScenePath = "res://modules/com.freerehabhub.target-tap/TargetTap.tscn",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            ["totalRounds"] = new LocalizedText { Tr = "Toplam Tur", En = "Total Rounds" },
            ["hitCount"] = new LocalizedText { Tr = "İsabet Sayısı", En = "Hit Count" },
            ["missCount"] = new LocalizedText { Tr = "Kaçırma Sayısı", En = "Miss Count" },
            ["averageReactionTimeSeconds"] = new LocalizedText
            {
                Tr = "Ortalama Reaksiyon Süresi (sn)",
                En = "Average Reaction Time (s)"
            }
        }
    };

    public event EventHandler<ModuleResult>? Completed;

    public override void _Ready()
    {
        _roundLabel = GetNode<Label>(_roundLabelPath);
        _playArea = GetNode<Control>(_playAreaPath);

        _roundTimer = new Godot.Timer { OneShot = true, WaitTime = RoundTimeoutSeconds };
        _roundTimer.Timeout += OnRoundTimeout;
        AddChild(_roundTimer);
    }

    public Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _currentRound = 0;
        _hitCount = 0;
        _totalReactionTimeSecondsForHits = 0.0;
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
        _roundTimer.Paused = true;
    }

    public void OnResumed()
    {
        _isRunning = true;
        _roundTimer.Paused = false;
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

        ClearCurrentTarget();

        _currentTarget = new Button
        {
            Text = "●",
            Size = new Vector2(TargetButtonSize, TargetButtonSize),
            Position = RandomTargetPosition()
        };
        _currentTarget.Pressed += OnTargetPressed;
        _playArea.AddChild(_currentTarget);

        _roundStartTimeMsec = Time.GetTicksMsec();
        _roundTimer.Start();
        UpdateRoundLabel();
    }

    private Vector2 RandomTargetPosition()
    {
        var areaSize = _playArea.Size;
        var width = areaSize.X > TargetButtonSize ? areaSize.X : FallbackPlayAreaWidth;
        var height = areaSize.Y > TargetButtonSize ? areaSize.Y : FallbackPlayAreaHeight;

        return new Vector2(
            _random.RandfRange(0f, width - TargetButtonSize),
            _random.RandfRange(0f, height - TargetButtonSize));
    }

    private void OnTargetPressed()
    {
        if (!_isRunning)
        {
            return;
        }

        _roundTimer.Stop();
        var reactionTimeSeconds = (Time.GetTicksMsec() - _roundStartTimeMsec) / 1000.0;
        _hitCount++;
        _totalReactionTimeSecondsForHits += reactionTimeSeconds;
        _currentRound++;
        StartNextRound();
    }

    private void OnRoundTimeout()
    {
        if (!_isRunning)
        {
            return;
        }

        _currentRound++;
        StartNextRound();
    }

    private void ClearCurrentTarget()
    {
        if (_currentTarget is null)
        {
            return;
        }

        // Godot kapanış sırasında çocuk node'ları (Timer, hedef Button) C# Dispose'dan önce native
        // tarafta zaten yok edebiliyor — IsInstanceValid olmadan erişmek ObjectDisposedException fırlatır.
        if (GodotObject.IsInstanceValid(_currentTarget))
        {
            _currentTarget.Pressed -= OnTargetPressed;
            _currentTarget.QueueFree();
        }

        _currentTarget = null;
    }

    private void UpdateRoundLabel()
    {
        var displayedRound = Math.Min(_currentRound + 1, TotalRounds);
        _roundLabel.Text = $"Tur: {displayedRound} / {TotalRounds}";
    }

    private void RaiseCompletedIfNeeded()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        ClearCurrentTarget();
        _roundTimer.Stop();

        var result = _scorer.Score(ModuleId, TotalRounds, _hitCount, _totalReactionTimeSecondsForHits, _context);
        Completed?.Invoke(this, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearCurrentTarget();
            if (GodotObject.IsInstanceValid(_roundTimer))
            {
                _roundTimer.Timeout -= OnRoundTimeout;
            }
        }

        base.Dispose(disposing);
    }
}
