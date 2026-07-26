using System;
using System.Globalization;
using System.Threading.Tasks;
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
    [Export] private NodePath _consentSectionPath = null!;
    [Export] private NodePath _consentGivenByNameInputPath = null!;
    [Export] private NodePath _guardianConsentCheckBoxPath = null!;
    [Export] private NodePath _consentStatusSectionPath = null!;
    [Export] private NodePath _consentStatusLabelPath = null!;
    [Export] private NodePath _withdrawConsentButtonPath = null!;
    [Export] private NodePath _saveButtonPath = null!;
    [Export] private NodePath _cancelButtonPath = null!;
    [Export] private NodePath _errorLabelPath = null!;

    private Label _titleLabel = null!;
    private LineEdit _fullNameInput = null!;
    private LineEdit _dateOfBirthInput = null!;
    private Control _consentSection = null!;
    private LineEdit _consentGivenByNameInput = null!;
    private CheckBox _guardianConsentCheckBox = null!;
    private Control _consentStatusSection = null!;
    private Label _consentStatusLabel = null!;
    private Button _withdrawConsentButton = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Label _errorLabel = null!;
    private AppServices _appServices = null!;
    private SessionContext _sessionContext = null!;
    private Patient? _editingPatient;

    public override async void _Ready()
    {
        _titleLabel = GetNode<Label>(_titleLabelPath);
        _fullNameInput = GetNode<LineEdit>(_fullNameInputPath);
        _dateOfBirthInput = GetNode<LineEdit>(_dateOfBirthInputPath);
        _consentSection = GetNode<Control>(_consentSectionPath);
        _consentGivenByNameInput = GetNode<LineEdit>(_consentGivenByNameInputPath);
        _guardianConsentCheckBox = GetNode<CheckBox>(_guardianConsentCheckBoxPath);
        _consentStatusSection = GetNode<Control>(_consentStatusSectionPath);
        _consentStatusLabel = GetNode<Label>(_consentStatusLabelPath);
        _withdrawConsentButton = GetNode<Button>(_withdrawConsentButtonPath);
        _saveButton = GetNode<Button>(_saveButtonPath);
        _cancelButton = GetNode<Button>(_cancelButtonPath);
        _errorLabel = GetNode<Label>(_errorLabelPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");
        _errorLabel.Text = string.Empty;
        _consentStatusSection.Visible = false;
        _saveButton.Pressed += OnSavePressed;
        _cancelButton.Pressed += OnCancelPressed;
        _withdrawConsentButton.Pressed += OnWithdrawConsentPressed;

        _editingPatient = _sessionContext.ActivePatient;
        if (_editingPatient is not null)
        {
            _titleLabel.Text = "Hastayı Düzenle";
            _fullNameInput.Text = _editingPatient.FullName;
            _dateOfBirthInput.Text = _editingPatient.DateOfBirth.ToString(DateFormat, CultureInfo.InvariantCulture);

            // Rıza sadece hasta oluşturulurken alınıyor — mevcut bir hastayı düzenlerken tekrar
            // istenmiyor (bkz. docs/PROGRESS.md F8.02 kapsam kararı). Düzenleme modunda bunun
            // yerine mevcut rıza durumu gösterilip geri çekme imkanı sunuluyor (bkz. F8.20).
            _consentSection.Visible = false;
            await RefreshConsentStatusAsync(_editingPatient.Id);
        }
    }

    private async Task RefreshConsentStatusAsync(Guid patientId)
    {
        var activeTherapist = _sessionContext.ActiveTherapist;
        if (activeTherapist is null)
        {
            return;
        }

        var consentRecord = await _appServices.ConsentService!.GetByPatientIdAsync(patientId, activeTherapist.Id);
        _consentStatusSection.Visible = consentRecord is not null;
        if (consentRecord is null)
        {
            return;
        }

        _consentStatusLabel.Text = consentRecord.WithdrawnAt is { } withdrawnAt
            ? $"Rıza {withdrawnAt:dd.MM.yyyy} tarihinde geri çekildi."
            : $"Rıza: {consentRecord.ConsentGivenByName} tarafından {consentRecord.ConsentedAt:dd.MM.yyyy} tarihinde verildi.";
        _withdrawConsentButton.Visible = consentRecord.WithdrawnAt is null;
    }

    private async void OnWithdrawConsentPressed()
    {
        if (_editingPatient is null)
        {
            return;
        }

        var activeTherapist = _sessionContext.ActiveTherapist;
        if (activeTherapist is null)
        {
            return;
        }

        try
        {
            await _appServices.ConsentService!.WithdrawAsync(_editingPatient.Id, activeTherapist.Id);
            await RefreshConsentStatusAsync(_editingPatient.Id);
        }
        catch (InvalidOperationException exception)
        {
            _errorLabel.Text = exception.Message;
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

        var consentGivenByName = _consentGivenByNameInput.Text;
        if (_editingPatient is null && string.IsNullOrWhiteSpace(consentGivenByName))
        {
            _errorLabel.Text = "Rıza veren adı boş olamaz.";
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
            await _appServices.ConsentService!.AddAsync(
                new ConsentRecord
                {
                    PatientId = patient.Id,
                    ConsentGivenByName = consentGivenByName,
                    IsGuardianConsent = _guardianConsentCheckBox.ButtonPressed,
                    ConsentedAt = DateTime.UtcNow
                },
                activeTherapist.Id);
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
