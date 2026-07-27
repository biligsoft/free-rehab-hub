using System;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.SceneTests;

// F8.31: ProgressRecord.SessionId artık gerçek bir TherapySessions kaydına FK'li — ModuleHost
// (Exercise akışı) her modül başlatışında önce bir TherapySession açıp, modül tamamlanınca (veya
// erken çıkışta, OnDeactivated ile) EndedAt'i kapatıyor. AssessmentHostSceneTest aynı deseni
// Assessment tarafı için doğruluyor; bu test Exercise tarafını (kamerasız target-tap ile)
// doğruluyor. Round'ları gerçekten kazanmaya gerek yok — IExerciseModule sözleşmesi Completed'ın
// erken çıkışta da tam bir kez tetiklenmesini garanti ediyor (bkz. godot-csharp-standards § 5),
// bu yüzden hemen Exit'e basmak yeterli ve deterministik.
public sealed class ModuleHostTherapySessionSceneTest : ISceneTest
{
    private const string ModuleLibraryScenePath = "res://scenes/module-library/ModuleLibraryPanel.tscn";
    private const string TargetTapDisplayName = "Hedef Vurma";

    public string Name => "ModuleHost: modül oynatışı gerçek bir TherapySession açıp kapatıyor (Exercise)";

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
            FullName = "Exercise Seans Testi Hastası",
            DateOfBirth = new DateOnly(1992, 5, 20),
            CreatedAt = DateTime.UtcNow
        };
        await appServices.PatientService!.AddAsync(patient, therapist.Id);
        sessionContext.SetActivePatient(patient);

        sceneTree.ChangeSceneToFile(ModuleLibraryScenePath);
        await WaitFramesAsync(sceneTree, 5);

        var root = sceneTree.Root;
        var moduleList = root.GetNode<ItemList>("ModuleLibraryPanel/Card/Content/ModuleItemList");
        var startButton = root.GetNode<Button>("ModuleLibraryPanel/Card/Content/Actions/StartButton");

        var targetTapIndex = Enumerable.Range(0, moduleList.ItemCount)
            .FirstOrDefault(i => moduleList.GetItemText(i).Contains(TargetTapDisplayName, StringComparison.Ordinal), -1);
        SceneAssert.True(targetTapIndex >= 0, "Modül kütüphanesinde Hedef Vurma (target-tap) listelenmeli.");

        moduleList.Select(targetTapIndex);
        moduleList.EmitSignal(ItemList.SignalName.ItemSelected, targetTapIndex);
        await WaitFramesAsync(sceneTree, 1);

        startButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 5);

        SceneAssert.True(root.HasNode("ModuleHost"), "Exercise modülü seçilince ModuleHost'a geçilmeli.");

        var exitButton = root.GetNode<Button>("ModuleHost/Layout/ToolbarMargin/Toolbar/ExitButton");
        exitButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 10);

        SceneAssert.True(root.HasNode("ModuleResultPanel"), "Erken çıkış sonrası ModuleResultPanel'e geçilmeli.");

        var lastResult = sessionContext.LastModuleResult;
        SceneAssert.NotNull(lastResult, "LastModuleResult set edilmiş olmalı.");
        SceneAssert.Equal("com.freerehabhub.target-tap", lastResult!.ModuleId, "Sonuç doğru modüle ait olmalı.");

        var history = await appServices.ProgressRecordService!.GetHistoryByPatientIdAsync(patient.Id);
        SceneAssert.Equal(1, history.Count, "Erken çıkış sonrası tam olarak 1 ProgressRecord kaydedilmeli.");

        var therapySession = await appServices.TherapySessionService!.GetByIdAsync(history[0].SessionId, therapist.Id);
        SceneAssert.NotNull(therapySession, "ProgressRecord.SessionId gerçek bir TherapySessions kaydına işaret etmeli.");
        SceneAssert.Equal(patient.Id, therapySession!.PatientId, "TherapySession doğru hastaya bağlı olmalı.");
        SceneAssert.Equal(therapist.Id, therapySession.TherapistId, "TherapySession doğru terapiste bağlı olmalı.");
        SceneAssert.NotNull(therapySession.EndedAt, "Modül tamamlanınca (erken çıkış dahil) EndedAt set edilmeli.");
    }

    private static async Task WaitFramesAsync(SceneTree sceneTree, int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
