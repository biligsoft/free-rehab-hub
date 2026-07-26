using System;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.SceneTests;

// F8.20'de eklenen rıza geri çekme akışını uçtan uca doğruluyor: hasta düzenleme ekranı →
// mevcut rıza durumunu gösterme → geri çekme butonu → durumun güncellenmesi ve kalıcılığı.
public sealed class PatientConsentWithdrawalSceneTest : ISceneTest
{
    private const string PatientFormScenePath = "res://scenes/patient/PatientFormPanel.tscn";

    public string Name => "PatientForm: rıza durumu gösterme ve geri çekme";

    public async Task RunAsync(SceneTree sceneTree)
    {
        var appServices = sceneTree.Root.GetNode<AppServices>("/root/AppServices");
        var sessionContext = sceneTree.Root.GetNode<SessionContext>("/root/SessionContext");

        var therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Sahne Testi Terapist",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = DateTime.UtcNow
        };
        await appServices.TherapistService!.AddAsync(therapist, therapist.Id);
        sessionContext.SetActiveTherapist(therapist);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Sahne Testi Hasta",
            DateOfBirth = new DateOnly(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        await appServices.PatientService!.AddAsync(patient, therapist.Id);
        await appServices.ConsentService!.AddAsync(
            new ConsentRecord
            {
                PatientId = patient.Id,
                ConsentGivenByName = "Sahne Testi Veli",
                IsGuardianConsent = true,
                ConsentedAt = DateTime.UtcNow
            },
            therapist.Id);
        sessionContext.SetActivePatient(patient);

        sceneTree.ChangeSceneToFile(PatientFormScenePath);
        await WaitFramesAsync(sceneTree, 5);

        var root = sceneTree.Root;
        var consentStatusSection = root.GetNode<Control>("PatientFormPanel/Card/Content/ConsentStatusSection");
        var consentStatusLabel = root.GetNode<Label>("PatientFormPanel/Card/Content/ConsentStatusSection/ConsentStatusLabel");
        var withdrawButton = root.GetNode<Button>("PatientFormPanel/Card/Content/ConsentStatusSection/WithdrawConsentButton");

        SceneAssert.True(consentStatusSection.Visible, "Düzenleme modunda rıza durumu bölümü görünür olmalı.");
        SceneAssert.True(
            consentStatusLabel.Text.Contains("Sahne Testi Veli", StringComparison.Ordinal),
            "Rıza durumu metni rıza vereni içermeli.");
        SceneAssert.True(withdrawButton.Visible, "Rıza henüz geri çekilmediği için buton görünür olmalı.");

        withdrawButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 5);

        SceneAssert.True(
            consentStatusLabel.Text.Contains("geri çekildi", StringComparison.Ordinal),
            "Geri çekme sonrası metin durumu yansıtmalı.");
        SceneAssert.False(withdrawButton.Visible, "Geri çekildikten sonra buton gizlenmeli.");

        var persisted = await appServices.ConsentService!.GetByPatientIdAsync(patient.Id, therapist.Id);
        SceneAssert.NotNull(persisted, "Rıza kaydı hâlâ var olmalı.");
        SceneAssert.True(persisted!.WithdrawnAt is not null, "WithdrawnAt veritabanında kalıcı olarak set edilmeli.");
    }

    private static async Task WaitFramesAsync(SceneTree sceneTree, int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
