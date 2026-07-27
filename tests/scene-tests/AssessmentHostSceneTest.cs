using System;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.SceneTests;

// F8.18'de eklenen Assessment modülü oynatma akışını uçtan uca doğruluyor: modül kütüphanesi
// → form render → gerçek form etkileşimi → skorlama → ProgressRecord kalıcılığı → sonuç ekranı.
public sealed class AssessmentHostSceneTest : ISceneTest
{
    private const string ModuleLibraryScenePath = "res://scenes/module-library/ModuleLibraryPanel.tscn";

    public string Name => "AssessmentHost: form doldurup gönderme, skorlama ve kayıt";

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
        sessionContext.SetActivePatient(patient);

        sceneTree.ChangeSceneToFile(ModuleLibraryScenePath);
        await WaitFramesAsync(sceneTree, 5);

        var root = sceneTree.Root;
        var moduleList = root.GetNode<ItemList>("ModuleLibraryPanel/Card/Content/ModuleItemList");
        var startButton = root.GetNode<Button>("ModuleLibraryPanel/Card/Content/Actions/StartButton");

        var assessmentIndex = Enumerable.Range(0, moduleList.ItemCount)
            .FirstOrDefault(i => moduleList.GetItemText(i).Contains("Genel Fonksiyonel", StringComparison.Ordinal), -1);
        SceneAssert.True(assessmentIndex >= 0, "Modül kütüphanesinde Assessment modülü listelenmeli.");

        moduleList.Select(assessmentIndex);
        moduleList.EmitSignal(ItemList.SignalName.ItemSelected, assessmentIndex);
        await WaitFramesAsync(sceneTree, 1);

        startButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 5);

        SceneAssert.True(root.HasNode("AssessmentHost"), "Assessment modülü seçilince AssessmentHost'a geçilmeli.");

        var fieldsContainer = root.GetNode<VBoxContainer>(
            "AssessmentHost/Layout/FormRenderer/Layout/ScrollContainer/FieldsContainer");
        var submitButton = root.GetNode<Button>("AssessmentHost/Layout/FormRenderer/Layout/Actions/SubmitButton");
        SceneAssert.Equal(5, fieldsContainer.GetChildCount(), "Form şeması 5 alan render etmeli.");

        var painSlider = fieldsContainer.GetChild<VBoxContainer>(0).GetChild<HSlider>(1);
        painSlider.Value = 8;

        var difficultySlider = fieldsContainer.GetChild<VBoxContainer>(1).GetChild<HSlider>(1);
        difficultySlider.Value = 6;

        var affectedSideOption = fieldsContainer.GetChild<VBoxContainer>(2).GetChild<OptionButton>(1);
        affectedSideOption.Selected = 1;
        affectedSideOption.EmitSignal(OptionButton.SignalName.ItemSelected, 1);

        var symptomsContainer = fieldsContainer.GetChild<VBoxContainer>(3).GetChild<VBoxContainer>(1);
        var swellingCheckBox = symptomsContainer.GetChildren().OfType<CheckBox>().First(c => c.Name == "swelling");
        swellingCheckBox.ButtonPressed = true;

        var notesInput = fieldsContainer.GetChild<VBoxContainer>(4).GetChild<LineEdit>(1);
        notesInput.Text = "Sahne testi notu";

        submitButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 10);

        SceneAssert.True(root.HasNode("ModuleResultPanel"), "Form gönderilince ModuleResultPanel'e geçilmeli.");

        var lastResult = sessionContext.LastModuleResult;
        SceneAssert.NotNull(lastResult, "LastModuleResult set edilmiş olmalı.");
        SceneAssert.Equal(0.3, Math.Round(lastResult!.NormalizedScore, 4), "NormalizedScore beklenen (0.2+0.4)/2=0.3 olmalı.");
        SceneAssert.Equal(8.0, lastResult.Metrics["painLevel"], "painLevel metriği doğru okunmalı.");
        SceneAssert.Equal(6.0, lastResult.Metrics["functionalDifficulty"], "functionalDifficulty metriği doğru okunmalı.");
        SceneAssert.Equal(1.0, lastResult.Metrics["symptomCount"], "symptomCount metriği doğru sayılmalı.");
        SceneAssert.Equal("Sahne testi notu", lastResult.Notes, "Notes alanı doğru taşınmalı.");

        // F8.28: metrik etiketleri artık manifest'in MetricLabels sözlüğünden yerelleştiriliyor —
        // ModuleResultPanel'in gösterdiği metin, mekanik Title-Case yerine gerçek TR etiket içermeli.
        var metricsContainer = root.GetNode<VBoxContainer>("ModuleResultPanel/Card/Content/MetricsContainer");
        var metricsText = string.Join(
            " | ", metricsContainer.GetChildren().OfType<Label>().Select(label => label.Text));
        SceneAssert.True(
            metricsText.Contains("Ağrı Seviyesi", StringComparison.Ordinal),
            $"ModuleResultPanel metrik listesi 'painLevel' için yerelleştirilmiş TR etiket göstermeli, gerçek: '{metricsText}'");

        var history = await appServices.ProgressRecordService!.GetHistoryByPatientIdAsync(patient.Id);
        SceneAssert.Equal(1, history.Count, "Gönderim sonrası tam olarak 1 ProgressRecord kaydedilmeli.");
        SceneAssert.Equal("com.freerehabhub.general-functional-checkin", history[0].ModuleId, "Kaydedilen ProgressRecord doğru modül id'sini taşımalı.");
    }

    private static async Task WaitFramesAsync(SceneTree sceneTree, int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
