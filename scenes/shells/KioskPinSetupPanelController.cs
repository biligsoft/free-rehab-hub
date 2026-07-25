using System;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using Godot;

namespace FreeRehabHub.App.Shells;

public partial class KioskPinSetupPanelController : Control
{
    private const string TherapistShellScenePath = "res://scenes/shells/TherapistShell.tscn";

    [Export] private NodePath _statusLabelPath = null!;
    [Export] private NodePath _pinInputPath = null!;
    [Export] private NodePath _confirmPinInputPath = null!;
    [Export] private NodePath _saveButtonPath = null!;
    [Export] private NodePath _backButtonPath = null!;
    [Export] private NodePath _messageLabelPath = null!;

    private Label _statusLabel = null!;
    private LineEdit _pinInput = null!;
    private LineEdit _confirmPinInput = null!;
    private Button _saveButton = null!;
    private Button _backButton = null!;
    private Label _messageLabel = null!;
    private AppServices _appServices = null!;
    private SessionContext _sessionContext = null!;

    public override async void _Ready()
    {
        _statusLabel = GetNode<Label>(_statusLabelPath);
        _pinInput = GetNode<LineEdit>(_pinInputPath);
        _confirmPinInput = GetNode<LineEdit>(_confirmPinInputPath);
        _saveButton = GetNode<Button>(_saveButtonPath);
        _backButton = GetNode<Button>(_backButtonPath);
        _messageLabel = GetNode<Label>(_messageLabelPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");

        _messageLabel.Text = string.Empty;
        _saveButton.Pressed += OnSavePressed;
        _backButton.Pressed += OnBackPressed;

        await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        var isConfigured = await _appServices.AccessControlService!.IsPinConfiguredAsync();
        _statusLabel.Text = isConfigured
            ? "Kiosk PIN'i kurulu. Değiştirmek için yeni bir PIN girip kaydedin."
            : "Kiosk PIN'i henüz kurulmadı.";
    }

    private async void OnSavePressed()
    {
        _messageLabel.Text = string.Empty;
        var pin = _pinInput.Text;
        var confirmPin = _confirmPinInput.Text;

        if (string.IsNullOrWhiteSpace(pin))
        {
            _messageLabel.Text = "PIN boş olamaz.";
            return;
        }

        if (pin != confirmPin)
        {
            _messageLabel.Text = "Girilen PIN'ler eşleşmiyor.";
            return;
        }

        var activeTherapist = _sessionContext.ActiveTherapist;
        if (activeTherapist is null)
        {
            _messageLabel.Text = "Aktif terapist bulunamadı.";
            return;
        }

        _saveButton.Disabled = true;
        try
        {
            await _appServices.AccessControlService!.SetPinAsync(pin, activeTherapist.Id);
            _pinInput.Text = string.Empty;
            _confirmPinInput.Text = string.Empty;
            await RefreshStatusAsync();
            _messageLabel.Text = "PIN kaydedildi.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = $"PIN kaydedilemedi: {exception.Message}";
        }
        finally
        {
            _saveButton.Disabled = false;
        }
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile(TherapistShellScenePath);
    }
}
