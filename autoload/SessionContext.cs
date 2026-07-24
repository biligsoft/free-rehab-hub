using System;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.App.Autoload;

public partial class SessionContext : Node
{
    public event Action<Therapist?>? ActiveTherapistChanged;
    public event Action<Patient?>? ActivePatientChanged;
    public event Action<UserRole>? RoleChanged;

    public Therapist? ActiveTherapist { get; private set; }
    public Patient? ActivePatient { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Therapist;

    public void SetActiveTherapist(Therapist? therapist)
    {
        ActiveTherapist = therapist;
        ActiveTherapistChanged?.Invoke(therapist);
    }

    public void SetActivePatient(Patient? patient)
    {
        ActivePatient = patient;
        ActivePatientChanged?.Invoke(patient);
    }

    public void SetRole(UserRole role)
    {
        Role = role;
        RoleChanged?.Invoke(role);
    }
}
