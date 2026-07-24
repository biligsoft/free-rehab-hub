using FreeRehabHub.App.Autoload;
using Godot;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.App.Shells;

public partial class LockScreenController : Control
{
	[Export] private NodePath _passwordInputPath = null!;
	[Export] private NodePath _unlockButtonPath = null!;
	[Export] private NodePath _errorLabelPath = null!;

	private LineEdit _passwordInput = null!;
	private Button _unlockButton = null!;
	private Label _errorLabel = null!;
	private AppServices _appServices = null!;

	public override void _Ready()
	{
		_passwordInput = GetNode<LineEdit>(_passwordInputPath);
		_unlockButton = GetNode<Button>(_unlockButtonPath);
		_errorLabel = GetNode<Label>(_errorLabelPath);
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
			GetTree().ChangeSceneToFile("res://scenes/shells/TherapistSelectionScreen.tscn");
		}
		catch (SqliteException)
		{
			_errorLabel.Text = "Parola yanlış.";
			_passwordInput.Text = string.Empty;
		}
	}
}
