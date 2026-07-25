using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.App.ModuleHost;

public partial class ModuleHostController : Control
{
    private const string TherapistShellScenePath = "res://scenes/shells/TherapistShell.tscn";
    private const string ModuleResultPanelScenePath = "res://scenes/module-result/ModuleResultPanel.tscn";

    [Export] private NodePath _statusLabelPath = null!;
    [Export] private NodePath _pauseButtonPath = null!;
    [Export] private NodePath _exitButtonPath = null!;
    [Export] private NodePath _moduleContainerPath = null!;

    private Label _statusLabel = null!;
    private Button _pauseButton = null!;
    private Button _exitButton = null!;
    private Control _moduleContainer = null!;

    private SessionContext _sessionContext = null!;
    private AppServices _appServices = null!;

    // FrameReceived/StatusChanged arka plan thread'inden gelebiliyor (bkz. MediaPipePoseTrackingService) —
    // Godot node'larına (module.OnPoseFrame, _statusLabel) sadece ana thread'den dokunulabildiği için
    // burada kuyruğa alınıp _Process()'te (ana thread) tüketiliyor.
    private readonly ConcurrentQueue<PoseFrame> _pendingFrames = new();
    private readonly ConcurrentQueue<PoseTrackingStatus> _pendingStatusChanges = new();

    private IExerciseModule? _activeModule;
    private IPoseAwareModule? _poseAwareModule;
    private bool _isPaused;
    private bool _isCompleted;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>(_statusLabelPath);
        _pauseButton = GetNode<Button>(_pauseButtonPath);
        _exitButton = GetNode<Button>(_exitButtonPath);
        _moduleContainer = GetNode<Control>(_moduleContainerPath);
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _appServices = GetNode<AppServices>("/root/AppServices");

        _statusLabel.Text = string.Empty;
        _pauseButton.Pressed += OnPausePressed;
        _exitButton.Pressed += OnExitPressed;

        _ = StartModuleAsync();
    }

    public override void _Process(double delta)
    {
        while (_pendingFrames.TryDequeue(out var frame))
        {
            if (!_isPaused)
            {
                _poseAwareModule?.OnPoseFrame(frame);
            }
        }

        while (_pendingStatusChanges.TryDequeue(out var status))
        {
            ApplyPoseTrackingStatus(status);
        }
    }

    private async Task StartModuleAsync()
    {
        var manifest = _sessionContext.ActiveModuleManifest;
        var patient = _sessionContext.ActivePatient;
        if (manifest is null || patient is null || manifest.ScenePath is null)
        {
            _statusLabel.Text = "Başlatılacak modül bulunamadı.";
            _pauseButton.Disabled = true;
            return;
        }

        var scene = GD.Load<PackedScene>(manifest.ScenePath);
        var instance = scene.Instantiate<Node>();

        if (instance is not IExerciseModule module)
        {
            _statusLabel.Text = "Seçilen modül bir egzersiz modülü değil.";
            instance.QueueFree();
            _pauseButton.Disabled = true;
            return;
        }

        _moduleContainer.AddChild(instance);
        _activeModule = module;
        module.Completed += OnModuleCompleted;

        var context = new ModuleContext
        {
            PatientId = patient.Id,
            SessionId = Guid.NewGuid(),
            CompletedAt = DateTime.UtcNow
        };
        await module.InitializeAsync(context);

        if (module is IPoseAwareModule poseAwareModule && _appServices.PoseTrackingService is not null)
        {
            _poseAwareModule = poseAwareModule;
            _appServices.PoseTrackingService.FrameReceived += OnPoseFrameReceived;
            _appServices.PoseTrackingService.StatusChanged += OnPoseTrackingStatusChanged;

            try
            {
                await _appServices.PoseTrackingService.StartAsync();
            }
            catch (Exception exception)
            {
                _statusLabel.Text = $"Kamera başlatılamadı: {exception.Message}";
            }
        }

        module.OnActivated();
    }

    private void OnPoseFrameReceived(object? sender, PoseFrame frame) => _pendingFrames.Enqueue(frame);

    private void OnPoseTrackingStatusChanged(object? sender, PoseTrackingStatus status) =>
        _pendingStatusChanges.Enqueue(status);

    private void ApplyPoseTrackingStatus(PoseTrackingStatus status)
    {
        if (_isCompleted)
        {
            return;
        }

        _statusLabel.Text = status switch
        {
            PoseTrackingStatus.Starting => "Kamera başlatılıyor...",
            PoseTrackingStatus.Error => $"Kamera hatası: {_appServices.PoseTrackingService?.LastError}",
            _ => string.Empty
        };
    }

    private void OnPausePressed()
    {
        if (_activeModule is null || _isCompleted)
        {
            return;
        }

        _isPaused = !_isPaused;
        if (_isPaused)
        {
            _activeModule.OnPaused();
            _pauseButton.Text = "Devam Et";
        }
        else
        {
            _activeModule.OnResumed();
            _pauseButton.Text = "Duraklat";
        }
    }

    private void OnExitPressed()
    {
        if (_activeModule is not null && !_isCompleted)
        {
            // IExerciseModule sözleşmesi: Completed tam olarak bir kez tetiklenmeli — modül henüz
            // tamamlanmadıysa OnDeactivated bunu (varsa kısmi sonuçla) garanti eder, bu da
            // OnModuleCompleted üzerinden sonuç ekranına yönlendirir.
            _activeModule.OnDeactivated();
            return;
        }

        // Modül hiç kurulamadıysa (StartModuleAsync'teki erken guard'lardan biri devreye girdiyse)
        // gösterilecek bir sonuç yok, doğrudan ana ekrana dön.
        ExitToTherapistShell();
    }

    private void OnModuleCompleted(object? sender, ModuleResult result)
    {
        _isCompleted = true;
        CleanUpActiveModule();

        _sessionContext.SetLastModuleResult(result);
        GetTree().ChangeSceneToFile(ModuleResultPanelScenePath);
    }

    private void CleanUpActiveModule()
    {
        if (_poseAwareModule is not null && _appServices.PoseTrackingService is not null)
        {
            _ = _appServices.PoseTrackingService.StopAsync();
            _appServices.PoseTrackingService.FrameReceived -= OnPoseFrameReceived;
            _appServices.PoseTrackingService.StatusChanged -= OnPoseTrackingStatusChanged;
        }

        if (_activeModule is not null)
        {
            _activeModule.Completed -= OnModuleCompleted;
            _activeModule.Dispose();
        }
    }

    private void ExitToTherapistShell()
    {
        CleanUpActiveModule();
        _sessionContext.SetActiveModuleManifest(null);
        GetTree().ChangeSceneToFile(TherapistShellScenePath);
    }
}
