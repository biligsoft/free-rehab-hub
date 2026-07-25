using System;
using FreeRehabHub.Domain;
using FreeRehabHub.Modules.Contracts;
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

    // ModuleHost'un oynatacağı modülü taşımak için — bir önceki ekran (ör. modül kütüphanesi)
    // bunu set edip ModuleHost sahnesine geçiyor, ActivePatient'la aynı desen.
    public ModuleManifest? ActiveModuleManifest { get; private set; }

    // ModuleHost'un tamamlanan modülün sonucunu ModuleResultPanel'e taşımak için — aynı desen.
    public ModuleResult? LastModuleResult { get; private set; }

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

    public void SetActiveModuleManifest(ModuleManifest? manifest)
    {
        ActiveModuleManifest = manifest;
    }

    public void SetLastModuleResult(ModuleResult? result)
    {
        LastModuleResult = result;
    }
}
