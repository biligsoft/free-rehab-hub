using System;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.App.FormEngine;
using FreeRehabHub.App.Shells;
using FreeRehabHub.Domain;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.App.AssessmentHostScreen;

public partial class AssessmentHostController : Control
{
    private const string ModuleResultPanelScenePath = "res://scenes/module-result/ModuleResultPanel.tscn";
    private const string ResourcePathPrefix = "res://";

    [Export] private NodePath _statusLabelPath = null!;
    [Export] private NodePath _exitButtonPath = null!;
    [Export] private NodePath _formRendererPath = null!;

    private Label _statusLabel = null!;
    private Button _exitButton = null!;
    private FormRendererController _formRenderer = null!;

    private SessionContext _sessionContext = null!;
    private AppServices _appServices = null!;
    private ModuleRegistryAutoload _moduleRegistryAutoload = null!;
    private IAssessmentModule? _activeModule;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>(_statusLabelPath);
        _exitButton = GetNode<Button>(_exitButtonPath);
        _formRenderer = GetNode<FormRendererController>(_formRendererPath);
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _appServices = GetNode<AppServices>("/root/AppServices");
        _moduleRegistryAutoload = GetNode<ModuleRegistryAutoload>("/root/ModuleRegistryAutoload");

        _statusLabel.Text = string.Empty;
        _exitButton.Pressed += OnExitPressed;
        _formRenderer.Submitted += OnFormSubmitted;

        LoadModule();
    }

    private void LoadModule()
    {
        var manifest = _sessionContext.ActiveModuleManifest;
        var patient = _sessionContext.ActivePatient;
        if (manifest is null || patient is null || manifest.FormSchemaPath is null)
        {
            _statusLabel.Text = "Başlatılacak değerlendirme bulunamadı.";
            return;
        }

        IModule instance;
        try
        {
            instance = _moduleRegistryAutoload.Registry.CreateInstance(manifest.Id);
        }
        catch (Exception exception)
        {
            _statusLabel.Text = $"Değerlendirme yüklenemedi: {exception.Message}";
            return;
        }

        if (instance is not IAssessmentModule assessmentModule)
        {
            _statusLabel.Text = "Seçilen modül bir değerlendirme modülü değil.";
            return;
        }

        _activeModule = assessmentModule;

        // FormSchemaPath manifestte "res://content-packs/assessment-forms/....json" olarak
        // saklanıyor — content-packs/ paketlenmiş build'de .pck'e dahil edilmiyor (bkz.
        // export_presets.cfg, AppContentRoot), bu yüzden ham System.IO ile okumadan önce
        // AppContentRoot.Resolve() ile gerçek dosya yoluna çevrilmesi gerekiyor.
        var relativeSchemaPath = manifest.FormSchemaPath.StartsWith(ResourcePathPrefix, StringComparison.Ordinal)
            ? manifest.FormSchemaPath[ResourcePathPrefix.Length..]
            : manifest.FormSchemaPath;
        var schemaFilePath = AppContentRoot.Resolve(relativeSchemaPath);

        try
        {
            var schema = new FormSchemaLoader().LoadFromFile(schemaFilePath);
            _formRenderer.LoadSchema(schema);
        }
        catch (Exception exception)
        {
            _statusLabel.Text = $"Form şeması yüklenemedi: {exception.Message}";
        }
    }

    private async void OnFormSubmitted(object? sender, FormSubmission submission)
    {
        var patient = _sessionContext.ActivePatient;
        if (_activeModule is null || patient is null)
        {
            return;
        }

        var context = new ModuleContext
        {
            PatientId = patient.Id,
            SessionId = Guid.NewGuid(),
            CompletedAt = DateTime.UtcNow
        };
        var result = _activeModule.Score(submission, context);

        var therapist = _sessionContext.ActiveTherapist;
        if (therapist is not null && _appServices.ProgressRecordService is not null)
        {
            var record = new ProgressRecord
            {
                Id = Guid.NewGuid(),
                PatientId = result.PatientId,
                ModuleId = result.ModuleId,
                SessionId = result.SessionId,
                CompletedAt = result.CompletedAt,
                NormalizedScore = result.NormalizedScore,
                Metrics = result.Metrics,
                Notes = result.Notes
            };
            await _appServices.ProgressRecordService.AddAsync(record, therapist.Id);
        }

        _sessionContext.SetLastModuleResult(result);
        GetTree().ChangeSceneToFile(ModuleResultPanelScenePath);
    }

    private void OnExitPressed()
    {
        _sessionContext.SetActiveModuleManifest(null);
        GetTree().ChangeSceneToFile(KioskNavigation.ResolveHomeScenePath(_sessionContext.Role));
    }
}
