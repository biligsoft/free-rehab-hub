using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Services;
using Godot;

namespace FreeRehabHub.App.Progress;

public partial class ProgressPanelController : Control
{
    private const string EnglishLocale = "en";
    private const string TherapistShellScenePath = "res://scenes/shells/TherapistShell.tscn";

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _errorLabelPath = null!;
    [Export] private NodePath _emptyStateLabelPath = null!;
    [Export] private NodePath _moduleItemListPath = null!;
    [Export] private NodePath _chartPath = null!;
    [Export] private NodePath _recordsContainerPath = null!;
    [Export] private NodePath _reportStatusLabelPath = null!;
    [Export] private NodePath _pdfReportButtonPath = null!;
    [Export] private NodePath _reportFileDialogPath = null!;
    [Export] private NodePath _backButtonPath = null!;

    private Label _titleLabel = null!;
    private Label _errorLabel = null!;
    private Label _emptyStateLabel = null!;
    private ItemList _moduleItemList = null!;
    private ProgressChart _chart = null!;
    private VBoxContainer _recordsContainer = null!;
    private Label _reportStatusLabel = null!;
    private Button _pdfReportButton = null!;
    private FileDialog _reportFileDialog = null!;
    private Button _backButton = null!;

    private SessionContext _sessionContext = null!;
    private AppServices _appServices = null!;
    private ModuleRegistryAutoload _moduleRegistryAutoload = null!;
    private LocalizationAutoload _localization = null!;

    private IReadOnlyList<ProgressRecord> _history = Array.Empty<ProgressRecord>();
    private IReadOnlyList<string> _moduleIds = Array.Empty<string>();

    public override async void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _emptyStateLabel = GetNode<Label>(_emptyStateLabelPath);
        _moduleItemList = GetNode<ItemList>(_moduleItemListPath);
        _chart = GetNode<ProgressChart>(_chartPath);
        _recordsContainer = GetNode<VBoxContainer>(_recordsContainerPath);
        _reportStatusLabel = GetNode<Label>(_reportStatusLabelPath);
        _pdfReportButton = GetNode<Button>(_pdfReportButtonPath);
        _reportFileDialog = GetNode<FileDialog>(_reportFileDialogPath);
        _backButton = GetNode<Button>(_backButtonPath);
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _appServices = GetNode<AppServices>("/root/AppServices");
        _moduleRegistryAutoload = GetNode<ModuleRegistryAutoload>("/root/ModuleRegistryAutoload");
        _localization = GetNode<LocalizationAutoload>("/root/LocalizationAutoload");

        _errorLabel.Text = string.Empty;
        _reportStatusLabel.Text = string.Empty;
        _emptyStateLabel.Visible = false;
        _moduleItemList.ItemSelected += index => ShowModule((int)index);
        _pdfReportButton.Pressed += OnPdfReportButtonPressed;
        _reportFileDialog.FileSelected += OnReportFileSelected;
        _backButton.Pressed += OnBackPressed;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var patient = _sessionContext.ActivePatient;
        if (patient is null)
        {
            _errorLabel.Text = "Aktif hasta bulunamadı.";
            _pdfReportButton.Disabled = true;
            return;
        }

        _titleLabel.Text = $"İlerleme — {patient.FullName}";

        _history = await _appServices.ProgressRecordService!.GetHistoryByPatientIdAsync(patient.Id);
        _moduleIds = _history.Select(record => record.ModuleId).Distinct().ToList();

        _emptyStateLabel.Visible = _history.Count == 0;
        _pdfReportButton.Disabled = _history.Count == 0;

        _moduleItemList.Clear();
        foreach (var moduleId in _moduleIds)
        {
            _moduleItemList.AddItem(ResolveModuleDisplayName(moduleId));
        }

        if (_moduleIds.Count > 0)
        {
            _moduleItemList.Select(0);
            ShowModule(0);
        }
    }

    private void ShowModule(int index)
    {
        if (index < 0 || index >= _moduleIds.Count)
        {
            return;
        }

        var moduleId = _moduleIds[index];
        var records = _history
            .Where(record => record.ModuleId == moduleId)
            .OrderBy(record => record.CompletedAt)
            .ToList();

        _chart.SetValues(records.Select(record => record.NormalizedScore).ToList());

        foreach (var child in _recordsContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var record in records)
        {
            _recordsContainer.AddChild(new Label { Text = FormatRecordLine(record) });
        }
    }

    private string FormatRecordLine(ProgressRecord record)
    {
        var manifest = ResolveManifest(record.ModuleId);
        var metricsText = string.Join(
            ", ",
            record.Metrics.Select(metric =>
                $"{MetricKeyFormatter.Humanize(metric.Key, manifest, _localization.CurrentLocale)}: {metric.Value:0.##}"));
        var scoreText = $"{record.CompletedAt.ToLocalTime():dd.MM.yyyy HH:mm} — {record.NormalizedScore:P0}";
        return metricsText.Length == 0 ? scoreText : $"{scoreText} ({metricsText})";
    }

    private ModuleManifest? ResolveManifest(string moduleId)
    {
        return _moduleRegistryAutoload.Registry.GetAvailableModules()
            .FirstOrDefault(candidate => candidate.Id == moduleId);
    }

    private string ResolveModuleDisplayName(string moduleId)
    {
        var manifest = ResolveManifest(moduleId);
        return manifest is null ? moduleId : Localize(manifest.DisplayName);
    }

    private void OnPdfReportButtonPressed()
    {
        var patient = _sessionContext.ActivePatient;
        if (patient is null)
        {
            return;
        }

        _reportStatusLabel.Text = string.Empty;
        _reportFileDialog.CurrentFile = SuggestReportFileName(patient.FullName);
        _reportFileDialog.PopupCentered(new Vector2I(720, 480));
    }

    private async void OnReportFileSelected(string filePath)
    {
        var patient = _sessionContext.ActivePatient;
        var therapist = _sessionContext.ActiveTherapist;
        if (patient is null || therapist is null)
        {
            _reportStatusLabel.Text = "Aktif hasta veya terapist bulunamadı.";
            return;
        }

        _pdfReportButton.Disabled = true;
        _reportStatusLabel.Text = "Rapor oluşturuluyor...";

        try
        {
            var modules = _moduleIds
                .Select(id => (ModuleId: id, DisplayName: ResolveModuleDisplayName(id), Manifest: ResolveManifest(id)))
                .ToList();
            await _appServices.ProgressReportService!.GeneratePdfAsync(
                patient, therapist, _history, modules, _localization.CurrentLocale, filePath);
            _reportStatusLabel.Text = $"Rapor oluşturuldu: {filePath}";
        }
        catch (Exception exception)
        {
            _reportStatusLabel.Text = $"Rapor oluşturulamadı: {exception.Message}";
        }
        finally
        {
            _pdfReportButton.Disabled = false;
        }
    }

    private static string SuggestReportFileName(string patientFullName)
    {
        var sanitized = new string(patientFullName.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        return $"{sanitized}-ilerleme-raporu.pdf";
    }

    private void OnBackPressed()
    {
        _sessionContext.SetActivePatient(null);
        GetTree().ChangeSceneToFile(TherapistShellScenePath);
    }

    private string Localize(LocalizedText text)
    {
        return _localization.CurrentLocale == EnglishLocale ? text.En : text.Tr;
    }
}
