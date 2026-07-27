using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.BalloonPop.Scoring;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.Modules.BalloonPop;

public partial class BalloonPopController : Node, IExerciseModule, IPoseAwareModule
{
    private const int TotalBalloons = 6;
    private const double BaseRequiredAngleDegrees = 60.0;
    private const double AngleIncrementDegrees = 15.0;
    private const double MovementStartThresholdDegrees = 20.0;
    private const double MovementResetThresholdDegrees = 10.0;
    private const float MinLandmarkVisibility = 0.5f;
    private const double AngleGaugeMaxDegrees = 180.0;

    [Export] private NodePath _balloonLabelPath = null!;
    [Export] private NodePath _angleLabelPath = null!;
    [Export] private NodePath _statusLabelPath = null!;
    [Export] private NodePath _angleProgressBarPath = null!;

    private readonly BalloonPopScorer _scorer = new();

    private Label _balloonLabel = null!;
    private Label _angleLabel = null!;
    private Label _statusLabel = null!;
    private ProgressBar _angleProgressBar = null!;

    private ModuleContext _context = null!;
    private bool _isRunning;
    private bool _completed;
    private bool _isArmRaised;
    private int _currentBalloonIndex;
    private int _poppedCount;
    private double _maxAngleThisAttempt;
    private double _sumOfMaxAnglesForPopped;

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.freerehabhub.balloon-pop",
        Version = "1.0.0",
        Kind = ModuleKind.Exercise,
        DisplayName = new LocalizedText { Tr = "Balon Patlat", En = "Balloon Pop" },
        Description = new LocalizedText
        {
            Tr = "Kamera aracılığıyla omuz fleksiyonu hareketini takip eden, her turda hedef " +
                 "açının biraz daha yükseldiği bir balon patlatma oyunu.",
            En = "A camera-based shoulder flexion exercise framed as a balloon-popping game, " +
                 "where the required angle rises a little with each round."
        },
        Disciplines = new List<Discipline> { Discipline.Physiotherapy },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 2 },
        RequiredCapabilities = new List<string> { "camera" },
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.BalloonPop.BalloonPopController",
        ScenePath = "res://modules/com.freerehabhub.balloon-pop/BalloonPop.tscn",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            ["totalBalloons"] = new LocalizedText { Tr = "Toplam Balon", En = "Total Balloons" },
            ["poppedCount"] = new LocalizedText { Tr = "Patlatılan Balon", En = "Popped Balloons" },
            ["averageMaxAngleDegrees"] = new LocalizedText
            {
                Tr = "Ortalama Maksimum Açı (°)",
                En = "Average Max Angle (°)"
            }
        }
    };

    public event EventHandler<ModuleResult>? Completed;

    public override void _Ready()
    {
        _balloonLabel = GetNode<Label>(_balloonLabelPath);
        _angleLabel = GetNode<Label>(_angleLabelPath);
        _statusLabel = GetNode<Label>(_statusLabelPath);
        _angleProgressBar = GetNode<ProgressBar>(_angleProgressBarPath);
        _angleProgressBar.MinValue = 0;
        _angleProgressBar.MaxValue = AngleGaugeMaxDegrees;
    }

    public Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _currentBalloonIndex = 0;
        _poppedCount = 0;
        _sumOfMaxAnglesForPopped = 0.0;
        _maxAngleThisAttempt = 0.0;
        _isArmRaised = false;
        _completed = false;
        _statusLabel.Text = string.Empty;
        UpdateBalloonLabel();
        return Task.CompletedTask;
    }

    public void OnActivated() => _isRunning = true;

    public void OnPaused() => _isRunning = false;

    public void OnResumed() => _isRunning = true;

    public void OnDeactivated()
    {
        _isRunning = false;
        RaiseCompletedIfNeeded();
    }

    public void OnPoseFrame(PoseFrame frame)
    {
        if (!_isRunning || _completed)
        {
            return;
        }

        var pose = frame.Poses.FirstOrDefault();
        if (pose is null)
        {
            _statusLabel.Text = "Poz algılanamadı — kameraya tam görünür şekilde durun.";
            return;
        }

        var hip = pose.Landmarks.FirstOrDefault(landmark => landmark.Type == PoseLandmarkType.RightHip);
        var shoulder = pose.Landmarks.FirstOrDefault(landmark => landmark.Type == PoseLandmarkType.RightShoulder);
        var elbow = pose.Landmarks.FirstOrDefault(landmark => landmark.Type == PoseLandmarkType.RightElbow);

        if (hip is null || shoulder is null || elbow is null ||
            hip.Visibility < MinLandmarkVisibility ||
            shoulder.Visibility < MinLandmarkVisibility ||
            elbow.Visibility < MinLandmarkVisibility)
        {
            _statusLabel.Text = "Sağ kol/omuz net görünmüyor.";
            return;
        }

        _statusLabel.Text = string.Empty;

        var angle = ShoulderFlexionCalculator.CalculateFlexionAngleDegrees(hip.World, shoulder.World, elbow.World);
        _angleLabel.Text = $"Açı: {angle:F0}°";
        _angleProgressBar.Value = angle;

        if (_isArmRaised)
        {
            _maxAngleThisAttempt = Math.Max(_maxAngleThisAttempt, angle);
            if (angle < MovementResetThresholdDegrees)
            {
                var requiredAngle = RequiredAngleForBalloon(_currentBalloonIndex);
                if (_maxAngleThisAttempt >= requiredAngle)
                {
                    _poppedCount++;
                    _sumOfMaxAnglesForPopped += _maxAngleThisAttempt;
                }

                _currentBalloonIndex++;
                _isArmRaised = false;
                _maxAngleThisAttempt = 0.0;
                UpdateBalloonLabel();

                if (_currentBalloonIndex >= TotalBalloons)
                {
                    RaiseCompletedIfNeeded();
                }
            }
        }
        else if (angle > MovementStartThresholdDegrees)
        {
            _isArmRaised = true;
            _maxAngleThisAttempt = angle;
        }
    }

    private static double RequiredAngleForBalloon(int balloonIndex) =>
        BaseRequiredAngleDegrees + (balloonIndex * AngleIncrementDegrees);

    private void UpdateBalloonLabel()
    {
        var displayedBalloon = Math.Min(_currentBalloonIndex + 1, TotalBalloons);
        var requiredAngle = RequiredAngleForBalloon(Math.Min(_currentBalloonIndex, TotalBalloons - 1));
        _balloonLabel.Text = $"Balon: {displayedBalloon} / {TotalBalloons} (Hedef: {requiredAngle:F0}°)";
    }

    private void RaiseCompletedIfNeeded()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        var result = _scorer.Score(ModuleId, TotalBalloons, _poppedCount, _sumOfMaxAnglesForPopped, _context);
        Completed?.Invoke(this, result);
    }
}
