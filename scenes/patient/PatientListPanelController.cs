using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.App.Shells;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.App.Patients;

public partial class PatientListPanelController : Control
{
    private const string DisplayDateFormat = "dd.MM.yyyy";

    [Export] private NodePath _patientListPath = null!;
    [Export] private NodePath _newPatientButtonPath = null!;
    [Export] private NodePath _editButtonPath = null!;
    [Export] private NodePath _deleteButtonPath = null!;
    [Export] private NodePath _prescriptionButtonPath = null!;
    [Export] private NodePath _modulesButtonPath = null!;
    [Export] private NodePath _progressButtonPath = null!;
    [Export] private NodePath _kioskButtonPath = null!;
    [Export] private NodePath _emptyStateLabelPath = null!;
    [Export] private NodePath _messageLabelPath = null!;
    [Export] private NodePath _confirmDeleteDialogPath = null!;

    private ItemList _patientList = null!;
    private Button _newPatientButton = null!;
    private Button _editButton = null!;
    private Button _deleteButton = null!;
    private Button _prescriptionButton = null!;
    private Button _modulesButton = null!;
    private Button _progressButton = null!;
    private Button _kioskButton = null!;
    private Label _emptyStateLabel = null!;
    private Label _messageLabel = null!;
    private ConfirmationDialog _confirmDeleteDialog = null!;
    private AppServices _appServices = null!;
    private SessionContext _sessionContext = null!;
    private IReadOnlyList<Patient> _patients = Array.Empty<Patient>();

    public override async void _Ready()
    {
        _patientList = GetNode<ItemList>(_patientListPath);
        _newPatientButton = GetNode<Button>(_newPatientButtonPath);
        _editButton = GetNode<Button>(_editButtonPath);
        _deleteButton = GetNode<Button>(_deleteButtonPath);
        _prescriptionButton = GetNode<Button>(_prescriptionButtonPath);
        _modulesButton = GetNode<Button>(_modulesButtonPath);
        _progressButton = GetNode<Button>(_progressButtonPath);
        _kioskButton = GetNode<Button>(_kioskButtonPath);
        _emptyStateLabel = GetNode<Label>(_emptyStateLabelPath);
        _messageLabel = GetNode<Label>(_messageLabelPath);
        _confirmDeleteDialog = GetNode<ConfirmationDialog>(_confirmDeleteDialogPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");

        _editButton.Disabled = true;
        _deleteButton.Disabled = true;
        _prescriptionButton.Disabled = true;
        _modulesButton.Disabled = true;
        _progressButton.Disabled = true;
        _kioskButton.Disabled = true;
        _messageLabel.Text = string.Empty;

        _patientList.ItemSelected += _ => OnPatientSelected();
        _newPatientButton.Pressed += OnNewPatientPressed;
        _editButton.Pressed += OnEditPressed;
        _deleteButton.Pressed += OnDeletePressed;
        _prescriptionButton.Pressed += OnPrescriptionPressed;
        _modulesButton.Pressed += OnModulesPressed;
        _progressButton.Pressed += OnProgressPressed;
        _kioskButton.Pressed += OnKioskPressed;
        _confirmDeleteDialog.Confirmed += OnDeleteConfirmed;

        await ReloadPatientsAsync();
    }

    private async Task ReloadPatientsAsync()
    {
        _patients = await _appServices.PatientService!.GetAllAsync();

        _patientList.Clear();
        foreach (var patient in _patients)
        {
            _patientList.AddItem($"{patient.FullName} ({patient.DateOfBirth.ToString(DisplayDateFormat)})");
        }

        _emptyStateLabel.Visible = _patients.Count == 0;
        _editButton.Disabled = true;
        _deleteButton.Disabled = true;
        _prescriptionButton.Disabled = true;
        _modulesButton.Disabled = true;
        _progressButton.Disabled = true;
        _kioskButton.Disabled = true;
    }

    private void OnPatientSelected()
    {
        _editButton.Disabled = false;
        _deleteButton.Disabled = false;
        _prescriptionButton.Disabled = false;
        _modulesButton.Disabled = false;
        _progressButton.Disabled = false;
        _kioskButton.Disabled = false;
    }

    private void OnNewPatientPressed()
    {
        _sessionContext.SetActivePatient(null);
        GetTree().ChangeSceneToFile("res://scenes/patient/PatientFormPanel.tscn");
    }

    private void OnEditPressed()
    {
        var selectedIndices = _patientList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        _sessionContext.SetActivePatient(_patients[selectedIndices[0]]);
        GetTree().ChangeSceneToFile("res://scenes/patient/PatientFormPanel.tscn");
    }

    private void OnPrescriptionPressed()
    {
        var selectedIndices = _patientList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        _sessionContext.SetActivePatient(_patients[selectedIndices[0]]);
        GetTree().ChangeSceneToFile("res://scenes/prescription/PrescriptionBuilderPanel.tscn");
    }

    private void OnModulesPressed()
    {
        var selectedIndices = _patientList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        _sessionContext.SetActivePatient(_patients[selectedIndices[0]]);
        GetTree().ChangeSceneToFile("res://scenes/module-library/ModuleLibraryPanel.tscn");
    }

    private void OnProgressPressed()
    {
        var selectedIndices = _patientList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        _sessionContext.SetActivePatient(_patients[selectedIndices[0]]);
        GetTree().ChangeSceneToFile("res://scenes/progress/ProgressPanel.tscn");
    }

    private async void OnKioskPressed()
    {
        var selectedIndices = _patientList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        _messageLabel.Text = string.Empty;

        // Fail-closed: PIN kurulu değilse kiosk moduna hiç girilmiyor — aksi halde terapist
        // kiosk kilidinden çıkacak bir yol olmadan içeride kalabilir.
        var isPinConfigured = await _appServices.AccessControlService!.IsPinConfiguredAsync();
        if (!isPinConfigured)
        {
            _messageLabel.Text =
                "Kiosk moduna geçmeden önce \"Kiosk PIN\" ekranından bir çıkış PIN'i belirlemelisiniz.";
            return;
        }

        _sessionContext.SetActivePatient(_patients[selectedIndices[0]]);
        _sessionContext.SetRole(UserRole.Child);
        GetTree().ChangeSceneToFile(KioskNavigation.ChildKioskShellScenePath);
    }

    private void OnDeletePressed()
    {
        var selectedIndices = _patientList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        var patient = _patients[selectedIndices[0]];
        _confirmDeleteDialog.DialogText =
            $"\"{patient.FullName}\" kaydını silmek istediğinize emin misiniz? Bu işlem geri alınamaz.";
        _confirmDeleteDialog.PopupCentered();
    }

    private async void OnDeleteConfirmed()
    {
        var selectedIndices = _patientList.GetSelectedItems();
        if (selectedIndices.Length == 0)
        {
            return;
        }

        var activeTherapist = _sessionContext.ActiveTherapist;
        if (activeTherapist is null)
        {
            return;
        }

        var patient = _patients[selectedIndices[0]];
        _messageLabel.Text = string.Empty;

        try
        {
            await _appServices.PatientService!.DeleteAsync(patient.Id, activeTherapist.Id);
        }
        catch (Exception exception)
        {
            _messageLabel.Text = $"Silme başarısız: {exception.Message}";
            return;
        }

        await ReloadPatientsAsync();
    }
}
