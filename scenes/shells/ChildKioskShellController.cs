using System;
using System.Collections.Generic;
using System.Linq;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.App.Shells;

public partial class ChildKioskShellController : Control
{
    private const string EnglishLocale = "en";
    private const string ModuleHostScenePath = "res://scenes/module-host/ModuleHost.tscn";

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _errorLabelPath = null!;
    [Export] private NodePath _moduleItemListPath = null!;
    [Export] private NodePath _startButtonPath = null!;
    [Export] private NodePath _therapistPinInputPath = null!;
    [Export] private NodePath _therapistExitButtonPath = null!;
    [Export] private NodePath _exitMessageLabelPath = null!;

    private Label _titleLabel = null!;
    private Label _errorLabel = null!;
    private ItemList _moduleItemList = null!;
    private Button _startButton = null!;
    private LineEdit _therapistPinInput = null!;
    private Button _therapistExitButton = null!;
    private Label _exitMessageLabel = null!;
    private SessionContext _sessionContext = null!;
    private AppServices _appServices = null!;
    private ModuleRegistryAutoload _moduleRegistryAutoload = null!;
    private LocalizationAutoload _localization = null!;

    private IReadOnlyList<ModuleManifest> _exerciseModules = Array.Empty<ModuleManifest>();

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _moduleItemList = GetNode<ItemList>(_moduleItemListPath);
        _startButton = GetNode<Button>(_startButtonPath);
        _therapistPinInput = GetNode<LineEdit>(_therapistPinInputPath);
        _therapistExitButton = GetNode<Button>(_therapistExitButtonPath);
        _exitMessageLabel = GetNode<Label>(_exitMessageLabelPath);
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _appServices = GetNode<AppServices>("/root/AppServices");
        _moduleRegistryAutoload = GetNode<ModuleRegistryAutoload>("/root/ModuleRegistryAutoload");
        _localization = GetNode<LocalizationAutoload>("/root/LocalizationAutoload");

        _errorLabel.Text = string.Empty;
        _exitMessageLabel.Text = string.Empty;
        _startButton.Disabled = true;
        _moduleItemList.ItemSelected += _ => _startButton.Disabled = false;
        _startButton.Pressed += OnStartPressed;
        _therapistExitButton.Pressed += OnTherapistExitPressed;
        _therapistPinInput.TextSubmitted += _ => OnTherapistExitPressed();

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

        _titleLabel.Text = $"Egzersizler — {patient.FullName}";

        // Şimdilik sadece Exercise modülleri — Assessment modüllerinin gerçek bir oynatma
        // ekranı henüz yok (bkz. ModuleLibraryPanelController'daki aynı not).
        _exerciseModules = _moduleRegistryAutoload.Registry.GetAvailableModules()
            .Where(manifest => manifest.Kind == ModuleKind.Exercise)
            .ToList();

        _moduleItemList.Clear();
        foreach (var manifest in _exerciseModules)
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

        _sessionContext.SetActiveModuleManifest(_exerciseModules[selectedIndices[0]]);
        GetTree().ChangeSceneToFile(ModuleHostScenePath);
    }

    private async void OnTherapistExitPressed()
    {
        _exitMessageLabel.Text = string.Empty;
        var pin = _therapistPinInput.Text;
        if (string.IsNullOrEmpty(pin))
        {
            return;
        }

        var isPinValid = await _appServices.AccessControlService!.VerifyPinAsync(pin);
        _therapistPinInput.Text = string.Empty;

        if (!isPinValid)
        {
            _exitMessageLabel.Text = "Yanlış PIN.";
            return;
        }

        _sessionContext.SetRole(UserRole.Therapist);
        _sessionContext.SetActivePatient(null);
        GetTree().ChangeSceneToFile(KioskNavigation.TherapistShellScenePath);
    }

    private string Localize(LocalizedText text)
    {
        return _localization.CurrentLocale == EnglishLocale ? text.En : text.Tr;
    }
}
