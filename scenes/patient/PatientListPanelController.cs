using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
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
    [Export] private NodePath _emptyStateLabelPath = null!;
    [Export] private NodePath _confirmDeleteDialogPath = null!;

    private ItemList _patientList = null!;
    private Button _newPatientButton = null!;
    private Button _editButton = null!;
    private Button _deleteButton = null!;
    private Button _prescriptionButton = null!;
    private Label _emptyStateLabel = null!;
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
        _emptyStateLabel = GetNode<Label>(_emptyStateLabelPath);
        _confirmDeleteDialog = GetNode<ConfirmationDialog>(_confirmDeleteDialogPath);
        _appServices = GetNode<AppServices>("/root/AppServices");
        _sessionContext = GetNode<SessionContext>("/root/SessionContext");

        _editButton.Disabled = true;
        _deleteButton.Disabled = true;
        _prescriptionButton.Disabled = true;

        _patientList.ItemSelected += _ => OnPatientSelected();
        _newPatientButton.Pressed += OnNewPatientPressed;
        _editButton.Pressed += OnEditPressed;
        _deleteButton.Pressed += OnDeletePressed;
        _prescriptionButton.Pressed += OnPrescriptionPressed;
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
    }

    private void OnPatientSelected()
    {
        _editButton.Disabled = false;
        _deleteButton.Disabled = false;
        _prescriptionButton.Disabled = false;
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
        await _appServices.PatientService!.DeleteAsync(patient.Id, activeTherapist.Id);
        await ReloadPatientsAsync();
    }
}
