using System;
using System.Collections.Generic;
using System.Linq;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.App.ModuleLibrary;

public partial class ModuleLibraryPanelController : Control
{
    private const string EnglishLocale = "en";
    private const string ModuleHostScenePath = "res://scenes/module-host/ModuleHost.tscn";
    private const string AssessmentHostScenePath = "res://scenes/assessment-host/AssessmentHost.tscn";
    private const string TherapistShellScenePath = "res://scenes/shells/TherapistShell.tscn";

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _errorLabelPath = null!;
    [Export] private NodePath _moduleItemListPath = null!;
    [Export] private NodePath _startButtonPath = null!;
    [Export] private NodePath _backButtonPath = null!;

    private Label _titleLabel = null!;
    private Label _errorLabel = null!;
    private ItemList _moduleItemList = null!;
    private Button _startButton = null!;
    private Button _backButton = null!;
    private SessionContext _sessionContext = null!;
    private ModuleRegistryAutoload _moduleRegistryAutoload = null!;
    private LocalizationAutoload _localization = null!;

    private IReadOnlyList<ModuleManifest> _availableModules = Array.Empty<ModuleManifest>();

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _moduleItemList = GetNode<ItemList>(_moduleItemListPath);
        _startButton = GetNode<Button>(_startButtonPath);
        _backButton = GetNode<Button>(_backButtonPath);
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _moduleRegistryAutoload = GetNode<ModuleRegistryAutoload>("/root/ModuleRegistryAutoload");
        _localization = GetNode<LocalizationAutoload>("/root/LocalizationAutoload");

        _errorLabel.Text = string.Empty;
        _startButton.Disabled = true;
        _moduleItemList.ItemSelected += _ => _startButton.Disabled = false;
        _startButton.Pressed += OnStartPressed;
        _backButton.Pressed += OnBackPressed;

        Load();
    }

    private void Load()
    {
        var patient = _sessionContext.ActivePatient;
        if (patient is null)
        {
            _errorLabel.Text = "Aktif hasta bulunamadı.";
            _startButton.Disabled = true;
            return;
        }

        _titleLabel.Text = $"Modüller — {patient.FullName}";

        _availableModules = _moduleRegistryAutoload.Registry.GetAvailableModules().ToList();

        _moduleItemList.Clear();
        foreach (var manifest in _availableModules)
        {
            _moduleItemList.AddItem(Localize(manifest.DisplayName));
        }
    }

    private void OnStartPressed()
    {
        var selectedIndices = _moduleItemList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        var manifest = _availableModules[selectedIndices[0]];
        _sessionContext.SetActiveModuleManifest(manifest);
        GetTree().ChangeSceneToFile(manifest.Kind == ModuleKind.Assessment ? AssessmentHostScenePath : ModuleHostScenePath);
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
