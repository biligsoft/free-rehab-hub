using System;
using System.Globalization;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.App.Patients;

public partial class PatientFormPanelController : Control
{
    private const string DateFormat = "yyyy-MM-dd";

    [Export] private NodePath _titleLabelPath = null!;
    [Export] private NodePath _fullNameInputPath = null!;
    [Export] private NodePath _dateOfBirthInputPath = null!;
    [Export] private NodePath _saveButtonPath = null!;
    [Export] private NodePath _cancelButtonPath = null!;
    [Export] private NodePath _errorLabelPath = null!;

    private Label _titleLabel = null!;
    private LineEdit _fullNameInput = null!;
    private LineEdit _dateOfBirthInput = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Label _errorLabel = null!;
    private AppServices _appServices = null!;
    private SessionContext _sessionContext = null!;
    private Patient? _editingPatient;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _fullNameInput = GetNode<LineEdit>(_fullNameInputPath);
        _dateOfBirthInput = GetNode<LineEdit>(_dateOfBirthInputPath);
        _saveButton = GetNode<Button>(_saveButtonPath);
        _cancelButton = GetNode<Button>(_cancelButtonPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _errorLabel.Text = string.Empty;
        _saveButton.Pressed += OnSavePressed;
        _cancelButton.Pressed += OnCancelPressed;

        _editingPatient = _sessionContext.ActivePatient;
        if (_editingPatient is not null)
        {
            _titleLabel.Text = "Hastayı Düzenle";
            _fullNameInput.Text = _editingPatient.FullName;
            _dateOfBirthInput.Text = _editingPatient.DateOfBirth.ToString(DateFormat, CultureInfo.InvariantCulture);
        }
    }

    private async void OnSavePressed()
    {
        var fullName = _fullNameInput.Text;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            _errorLabel.Text = "Ad Soyad boş olamaz.";
            return;
        }

        if (!DateOnly.TryParseExact(
                _dateOfBirthInput.Text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var dateOfBirth))
        {
            _errorLabel.Text = "Doğum tarihi YYYY-AA-GG formatında olmalı.";
            return;
        }

        var activeTherapist = _sessionContext.ActiveTherapist;
        if (activeTherapist is null)
        {
            _errorLabel.Text = "Aktif terapist bulunamadı.";
            return;
        }

        if (_editingPatient is not null)
        {
            _editingPatient.FullName = fullName;
            _editingPatient.DateOfBirth = dateOfBirth;
            await _appServices.PatientService!.UpdateAsync(_editingPatient, activeTherapist.Id);
        }
        else
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                DateOfBirth = dateOfBirth,
                CreatedAt = DateTime.UtcNow
            };
            await _appServices.PatientService!.AddAsync(patient, activeTherapist.Id);
        }

        _sessionContext.SetActivePatient(null);
        GetTree().ChangeSceneToFile("res://scenes/shells/TherapistShell.tscn");
    }

    private void OnCancelPressed()
    {
        _sessionContext.SetActivePatient(null);
        GetTree().ChangeSceneToFile("res://scenes/shells/TherapistShell.tscn");
    }
}
