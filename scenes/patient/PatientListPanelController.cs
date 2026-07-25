using FreeRehabHub.App.Autoload;
using Godot;

namespace FreeRehabHub.App.Patients;

public partial class PatientListPanelController : Control
{
    private const string DisplayDateFormat = "dd.MM.yyyy";

    [Export] private NodePath _patientListPath = null!;
    [Export] private NodePath _newPatientButtonPath = null!;
    [Export] private NodePath _emptyStateLabelPath = null!;

    private ItemList _patientList = null!;
    private Button _newPatientButton = null!;
    private Label _emptyStateLabel = null!;

    public override async void _Ready()
    {
        _patientList = GetNode<ItemList>(_patientListPath);
        _newPatientButton = GetNode<Button>(_newPatientButtonPath);
        _emptyStateLabel = GetNode<Label>(_emptyStateLabelPath);
        _newPatientButton.Pressed += OnNewPatientPressed;

        var appServices = GetNode<AppServices>("/root/AppServices");
        var patients = await appServices.PatientService!.GetAllAsync();

        _patientList.Clear();
        foreach (var patient in patients)
        {
            _patientList.AddItem($"{patient.FullName} ({patient.DateOfBirth.ToString(DisplayDateFormat)})");
        }

        _emptyStateLabel.Visible = patients.Count == 0;
    }

    private void OnNewPatientPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/patient/PatientFormPanel.tscn");
    }
}
