using FreeRehabHub.App.Autoload;
using Godot;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.App.Shells;

public partial class LockScreenController : Control
{
    [Export] private LineEdit _passwordInput = null!;
    [Export] private Button _unlockButton = null!;
    [Export] private Label _errorLabel = null!;

    private AppServices _appServices = null!;

    public override void _Ready()
    {
        _appServices = GetNode<AppServices>("/root/AppServices");
        _errorLabel.Text = string.Empty;
        _unlockButton.Pressed += OnUnlockPressed;
        _passwordInput.TextSubmitted += _ => OnUnlockPressed();
    }

    private void OnUnlockPressed()
    {
        var password = _passwordInput.Text;
        if (string.IsNullOrEmpty(password))
        {
            _errorLabel.Text = "Parola boş olamaz.";
            return;
        }

        try
        {
            _appServices.Unlock(password);
            GetTree().ChangeSceneToFile("res://scenes/shells/TherapistShell.tscn");
        }
        catch (SqliteException)
        {
            _errorLabel.Text = "Parola yanlış.";
            _passwordInput.Text = string.Empty;
        }
    }
}
