using System;
using FreeRehabHub.App.Autoload;
using Godot;

namespace FreeRehabHub.App.Shells;

public partial class TherapistShellController : Control
{
    private const string KioskPinSetupScenePath = "res://scenes/shells/KioskPinSetupPanel.tscn";

    [Export] private NodePath _backupButtonPath = null!;
    [Export] private NodePath _backupStatusLabelPath = null!;
    [Export] private NodePath _backupDirectoryDialogPath = null!;
    [Export] private NodePath _kioskPinButtonPath = null!;
    [Export] private NodePath _themeOptionButtonPath = null!;

    private Button _backupButton = null!;
    private Label _backupStatusLabel = null!;
    private FileDialog _backupDirectoryDialog = null!;
    private Button _kioskPinButton = null!;
    private OptionButton _themeOptionButton = null!;
    private AppServices _appServices = null!;
    private SessionContext _sessionContext = null!;
    private ThemeManager _themeManager = null!;

    public override void _Ready()
    {
        _backupButton = GetNode<Button>(_backupButtonPath);
        _backupStatusLabel = GetNode<Label>(_backupStatusLabelPath);
        _backupDirectoryDialog = GetNode<FileDialog>(_backupDirectoryDialogPath);
        _kioskPinButton = GetNode<Button>(_kioskPinButtonPath);
        _themeOptionButton = GetNode<OptionButton>(_themeOptionButtonPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _themeManager = GetNode<ThemeManager>("/root/ThemeManager");

        _backupStatusLabel.Text = string.Empty;
        _backupButton.Pressed += OnBackupButtonPressed;
        _backupDirectoryDialog.DirSelected += OnBackupDirectorySelected;
        _kioskPinButton.Pressed += OnKioskPinButtonPressed;

        _themeOptionButton.Clear();
        _themeOptionButton.AddItem("Varsayılan");
        _themeOptionButton.AddItem("Yüksek Kontrast");
        _themeOptionButton.AddItem("Düşük Uyaran");
        _themeOptionButton.Selected = (int)_themeManager.CurrentVariant;
        _themeOptionButton.ItemSelected += OnThemeSelected;
    }

    private void OnThemeSelected(long index)
    {
        _themeManager.ApplyTheme((ThemeVariant)index);
    }

    private void OnKioskPinButtonPressed()
    {
        GetTree().ChangeSceneToFile(KioskPinSetupScenePath);
    }

    private void OnBackupButtonPressed()
    {
        _backupStatusLabel.Text = string.Empty;
        _backupDirectoryDialog.PopupCentered(new Vector2I(720, 480));
    }

    private async void OnBackupDirectorySelected(string directory)
    {
        var activeTherapist = _sessionContext.ActiveTherapist;
        if (activeTherapist is null)
        {
            _backupStatusLabel.Text = "Aktif terapist bulunamadı.";
            return;
        }

        _backupButton.Disabled = true;
        _backupStatusLabel.Text = "Yedekleniyor...";

        try
        {
            var backupPath = await _appServices.BackupService!.CreateBackupAsync(directory, activeTherapist.Id);
            _backupStatusLabel.Text = $"Yedek oluşturuldu: {backupPath}";
        }
        catch (Exception exception)
        {
            _backupStatusLabel.Text = $"Yedekleme başarısız: {exception.Message}";
        }
        finally
        {
            _backupButton.Disabled = false;
        }
    }
}
