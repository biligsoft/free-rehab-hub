using FreeRehabHub.App.Autoload;
using Godot;

namespace FreeRehabHub.App.Patients;

public partial class PatientListPanelController : Control
{
    private const string DateFormat = "yyyy-MM-dd";

    [Export] private ItemList _patientList = null!;

    public override async void _Ready()
    {
        var appServices = GetNode<AppServices>("/root/AppServices");
        var patients = await appServices.PatientService!.GetAllAsync();

        _patientList.Clear();
        foreach (var patient in patients)
        {
            _patientList.AddItem($"{patient.FullName} ({patient.DateOfBirth.ToString(DateFormat)})");
        }
    }
}
