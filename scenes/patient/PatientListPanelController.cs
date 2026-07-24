using FreeRehabHub.App.Autoload;
using Godot;

namespace FreeRehabHub.App.Patients;

public partial class PatientListPanelController : Control
{
    private const string DateFormat = "yyyy-MM-dd";

    [Export] private NodePath _patientListPath = null!;
    [Export] private NodePath _newPatientButtonPath = null!;

    private ItemList _patientList = null!;
    private Button _newPatientButton = null!;

    public override async void _Ready()
    {
        _patientList = GetNode<ItemList>(_patientListPath);
        _newPatientButton = GetNode<Button>(_newPatientButtonPath);
        _newPatientButton.Pressed += OnNewPatientPressed;

        var appServices = GetNode<AppServices>("/root/AppServices");
        var patients = await appServices.PatientService!.GetAllAsync();

        _patientList.Clear();
        foreach (var patient in patients)
        {
            _patientList.AddItem($"{patient.FullName} ({patient.DateOfBirth.ToString(DateFormat)})");
        }
    }

    private void OnNewPatientPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/patient/PatientFormPanel.tscn");
    }
}
