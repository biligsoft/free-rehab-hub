using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.ArmRaise.Scoring;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.Modules.ArmRaise;

public partial class ArmRaiseController : Node, IExerciseModule, IPoseAwareModule
{
    private const int TargetReps = 10;
    private const double LowerThresholdDegrees = 30.0;
    private const double UpperThresholdDegrees = 90.0;
    private const float MinLandmarkVisibility = 0.5f;
    private const double AngleGaugeMaxDegrees = 180.0;

    [Export] private NodePath _repLabelPath = null!;
    [Export] private NodePath _angleLabelPath = null!;
    [Export] private NodePath _statusLabelPath = null!;
    [Export] private NodePath _angleProgressBarPath = null!;

    private readonly ArmRaiseScorer _scorer = new();

    private Label _repLabel = null!;
    private Label _angleLabel = null!;
    private Label _statusLabel = null!;
    private ProgressBar _angleProgressBar = null!;

    private ModuleContext _context = null!;
    private bool _isRunning;
    private bool _completed;
    private bool _isArmRaised;
    private int _completedReps;
    private double _sumOfMaxAnglesForCompletedReps;
    private double _maxAngleThisRep;

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.freerehabhub.arm-raise",
        Version = "1.0.0",
        Kind = ModuleKind.Exercise,
        DisplayName = new LocalizedText { Tr = "Kol Kaldırma", En = "Arm Raise" },
        Description = new LocalizedText
        {
            Tr = "Kamera aracılığıyla omuz fleksiyonu (kol kaldırma) hareketini takip eden, tekrar " +
                 "sayısını ve ulaşılan açıyı ölçen bir egzersiz.",
            En = "A camera-based exercise that tracks shoulder flexion (arm raise) movement, " +
                 "measuring repetition count and range of motion achieved."
        },
        Disciplines = new List<Discipline> { Discipline.Physiotherapy },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 2 },
        RequiredCapabilities = new List<string> { "camera" },
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.ArmRaise.ArmRaiseController",
        ScenePath = "res://modules/com.freerehabhub.arm-raise/ArmRaise.tscn",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            ["targetReps"] = new LocalizedText { Tr = "Hedef Tekrar", En = "Target Reps" },
            ["completedReps"] = new LocalizedText { Tr = "Tamamlanan Tekrar", En = "Completed Reps" },
            ["averageMaxAngleDegrees"] = new LocalizedText { Tr = "Ortalama Maksimum Açı (°)", En = "Average Max Angle (°)" }
        }
    };

    public event EventHandler<ModuleResult>? Completed;

    public override void _Ready()
    {
        _repLabel = GetNode<Label>(_repLabelPath);
        _angleLabel = GetNode<Label>(_angleLabelPath);
        _statusLabel = GetNode<Label>(_statusLabelPath);
        _angleProgressBar = GetNode<ProgressBar>(_angleProgressBarPath);
        _angleProgressBar.MinValue = 0;
        _angleProgressBar.MaxValue = AngleGaugeMaxDegrees;
    }

    public Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _completedReps = 0;
        _sumOfMaxAnglesForCompletedReps = 0.0;
        _maxAngleThisRep = 0.0;
        _isArmRaised = false;
        _completed = false;
        _statusLabel.Text = string.Empty;
        UpdateRepLabel();
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
            _maxAngleThisRep = Math.Max(_maxAngleThisRep, angle);
            if (angle < LowerThresholdDegrees)
            {
                _completedReps++;
                _sumOfMaxAnglesForCompletedReps += _maxAngleThisRep;
                _isArmRaised = false;
                _maxAngleThisRep = 0.0;
                UpdateRepLabel();

                if (_completedReps >= TargetReps)
                {
                    RaiseCompletedIfNeeded();
                }
            }
        }
        else if (angle > UpperThresholdDegrees)
        {
            _isArmRaised = true;
            _maxAngleThisRep = angle;
        }
    }

    private void UpdateRepLabel()
    {
        _repLabel.Text = $"Tekrar: {_completedReps} / {TargetReps}";
    }

    private void RaiseCompletedIfNeeded()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        var result = _scorer.Score(ModuleId, TargetReps, _completedReps, _sumOfMaxAnglesForCompletedReps, _context);
        Completed?.Invoke(this, result);
    }
}
