using System;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.SceneTests;

// F8.33: oyunlar.md'deki "Renk Kutusu" tasarımının gerçek modül implementasyonu. Bu test hem
// modül kütüphanesi → ModuleHost → gerçek oynatış → skorlama → ProgressRecord akışını hem de
// IExerciseModule.Completed'in tam bir kez tetiklendiğini (godot-csharp-standards § 5 LSP
// garantisi — çift ProgressRecord kaydı olmadığını doğrulayarak) uçtan uca kapatıyor.
public sealed class ColorSortSceneTest : ISceneTest
{
    private const string ModuleLibraryScenePath = "res://scenes/module-library/ModuleLibraryPanel.tscn";
    private const string ColorSortDisplayName = "Renk Kutusu";
    private const int TotalRounds = 8;
    private const int CorrectPressCount = 6;

    public string Name => "ColorSort: renk eşleştirme oynatışı, skorlama ve tam-bir-kez Completed";

    public async Task RunAsync(SceneTree sceneTree)
    {
        var appServices = sceneTree.Root.GetNode<AppServices>("/root/AppServices");
        var sessionContext = sceneTree.Root.GetNode<SessionContext>("/root/SessionContext");

        var therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Sahne Testi Terapist",
            Discipline = Discipline.SpecialEducation,
            CreatedAt = DateTime.UtcNow
        };
        await appServices.TherapistService!.AddAsync(therapist, therapist.Id);
        sessionContext.SetActiveTherapist(therapist);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Renk Kutusu Testi Hastası",
            DateOfBirth = new DateOnly(2016, 9, 1),
            CreatedAt = DateTime.UtcNow
        };
        await appServices.PatientService!.AddAsync(patient, therapist.Id);
        sessionContext.SetActivePatient(patient);

        sceneTree.ChangeSceneToFile(ModuleLibraryScenePath);
        await WaitFramesAsync(sceneTree, 5);

        var root = sceneTree.Root;
        var moduleList = root.GetNode<ItemList>("ModuleLibraryPanel/Card/Content/ModuleItemList");
        var startButton = root.GetNode<Button>("ModuleLibraryPanel/Card/Content/Actions/StartButton");

        var colorSortIndex = Enumerable.Range(0, moduleList.ItemCount)
            .FirstOrDefault(i => moduleList.GetItemText(i).Contains(ColorSortDisplayName, StringComparison.Ordinal), -1);
        SceneAssert.True(colorSortIndex >= 0, "Modül kütüphanesinde Renk Kutusu listelenmeli.");

        moduleList.Select(colorSortIndex);
        moduleList.EmitSignal(ItemList.SignalName.ItemSelected, colorSortIndex);
        await WaitFramesAsync(sceneTree, 1);

        startButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 5);

        SceneAssert.True(root.HasNode("ModuleHost"), "Renk Kutusu seçilince ModuleHost'a geçilmeli.");

        const string colorSortBasePath = "ModuleHost/Layout/ModuleContainer/ColorSort/Layout";
        var targetDisplay = root.GetNode<ColorRect>($"{colorSortBasePath}/TargetDisplay");
        var redButton = root.GetNode<Button>($"{colorSortBasePath}/BinsContainer/RedBinButton");
        var blueButton = root.GetNode<Button>($"{colorSortBasePath}/BinsContainer/BlueBinButton");
        var greenButton = root.GetNode<Button>($"{colorSortBasePath}/BinsContainer/GreenBinButton");
        var yellowButton = root.GetNode<Button>($"{colorSortBasePath}/BinsContainer/YellowBinButton");

        for (var round = 0; round < TotalRounds; round++)
        {
            await WaitFramesAsync(sceneTree, 1);
            var currentColor = targetDisplay.Color;
            var buttonToPress = round < CorrectPressCount
                ? ResolveCorrectButton(currentColor, redButton, blueButton, greenButton, yellowButton)
                : ResolveDeliberatelyWrongButton(currentColor, redButton, blueButton);
            buttonToPress.EmitSignal(Button.SignalName.Pressed);
        }

        await WaitFramesAsync(sceneTree, 10);

        SceneAssert.True(root.HasNode("ModuleResultPanel"), "8 tur sonrası ModuleResultPanel'e geçilmeli.");

        var lastResult = sessionContext.LastModuleResult;
        SceneAssert.NotNull(lastResult, "LastModuleResult set edilmiş olmalı.");
        SceneAssert.Equal("com.freerehabhub.color-sort", lastResult!.ModuleId, "Sonuç doğru modüle ait olmalı.");
        SceneAssert.Equal((double)TotalRounds, lastResult.Metrics["totalRounds"], "8 tur tamamlanmalı.");
        SceneAssert.Equal(
            (double)CorrectPressCount, lastResult.Metrics["correctCount"], $"{CorrectPressCount} doğru cevap sayılmalı.");
        SceneAssert.Equal(
            (double)(TotalRounds - CorrectPressCount), lastResult.Metrics["incorrectCount"],
            "Kalan turlar yanlış sayılmalı.");

        // IExerciseModule.Completed tam bir kez tetiklenmeli — bunun kanıtı, çift kayıt OLMADAN
        // tam olarak 1 ProgressRecord'un kalıcı olarak yazılmış olması.
        var history = await appServices.ProgressRecordService!.GetHistoryByPatientIdAsync(patient.Id);
        SceneAssert.Equal(1, history.Count, "Completed tam bir kez tetiklenmeli — tam olarak 1 ProgressRecord kaydedilmeli.");
    }

    private static Button ResolveCorrectButton(Color targetColor, Button red, Button blue, Button green, Button yellow)
    {
        if (targetColor == Colors.Red)
        {
            return red;
        }

        if (targetColor == Colors.Blue)
        {
            return blue;
        }

        if (targetColor == Colors.Green)
        {
            return green;
        }

        return yellow;
    }

    private static Button ResolveDeliberatelyWrongButton(Color targetColor, Button red, Button blue)
    {
        // Hangi renk hedeflenirse hedeflensin, kasıtlı olarak yanlış bir butona basmak için
        // hedef kırmızı değilse kırmızıya, kırmızıysa maviye basıyoruz.
        return targetColor == Colors.Red ? blue : red;
    }

    private static async Task WaitFramesAsync(SceneTree sceneTree, int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
