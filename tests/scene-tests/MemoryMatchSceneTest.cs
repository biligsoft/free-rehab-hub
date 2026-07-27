using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.SceneTests;

// F8.35: oyunlar.md'deki "Hafıza Kartları" (Memory Match) tasarımının gerçek modül
// implementasyonu. Bu test bir "mükemmel hafızalı oyuncu" botu gibi davranıyor: her açılan
// kartın rengini (Button.Modulate — herkese açık bir Godot node property'si, private alanlara
// hiç dokunulmuyor) hatırlayıp, bilinen bir eşi varsa onu oynuyor, yoksa yeni bir kart açıyor.
// Kartların gerçek karışık sırası (RandomNumberGenerator) bilinmediği için tahmin edilemez,
// bu yüzden gerçek bir eşleşmeme (mismatch) ve geri çevirme zamanlayıcısının da gerçekten
// çalıştığını (kart State'e "White"a dönene kadar bekleyerek) doğruluyor.
public sealed class MemoryMatchSceneTest : ISceneTest
{
    private const string ModuleLibraryScenePath = "res://scenes/module-library/ModuleLibraryPanel.tscn";
    private const string MemoryMatchDisplayName = "Hafıza Kartları";
    private const int TotalCards = 12;
    private const int TotalPairs = 6;
    private const int MaxFramesToWaitForFlipBack = 400;

    public string Name => "MemoryMatch: mükemmel-hafızalı bot ile tam oyun çözümü ve skorlama";

    public async Task RunAsync(SceneTree sceneTree)
    {
        var appServices = sceneTree.Root.GetNode<AppServices>("/root/AppServices");
        var sessionContext = sceneTree.Root.GetNode<SessionContext>("/root/SessionContext");

        var therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Sahne Testi Terapist",
            Discipline = Discipline.Psychology,
            CreatedAt = DateTime.UtcNow
        };
        await appServices.TherapistService!.AddAsync(therapist, therapist.Id);
        sessionContext.SetActiveTherapist(therapist);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Hafıza Kartları Testi Hastası",
            DateOfBirth = new DateOnly(1960, 11, 3),
            CreatedAt = DateTime.UtcNow
        };
        await appServices.PatientService!.AddAsync(patient, therapist.Id);
        sessionContext.SetActivePatient(patient);

        sceneTree.ChangeSceneToFile(ModuleLibraryScenePath);
        await WaitFramesAsync(sceneTree, 5);

        var root = sceneTree.Root;
        var moduleList = root.GetNode<ItemList>("ModuleLibraryPanel/Card/Content/ModuleItemList");
        var startButton = root.GetNode<Button>("ModuleLibraryPanel/Card/Content/Actions/StartButton");

        var memoryMatchIndex = Enumerable.Range(0, moduleList.ItemCount)
            .FirstOrDefault(i => moduleList.GetItemText(i).Contains(MemoryMatchDisplayName, StringComparison.Ordinal), -1);
        SceneAssert.True(memoryMatchIndex >= 0, "Modül kütüphanesinde Hafıza Kartları listelenmeli.");

        moduleList.Select(memoryMatchIndex);
        moduleList.EmitSignal(ItemList.SignalName.ItemSelected, memoryMatchIndex);
        await WaitFramesAsync(sceneTree, 1);

        startButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 5);

        SceneAssert.True(root.HasNode("ModuleHost"), "Hafıza Kartları seçilince ModuleHost'a geçilmeli.");

        const string cardsBasePath = "ModuleHost/Layout/ModuleContainer/MemoryMatch/Layout/CardsContainer";
        var cardButtons = Enumerable.Range(0, TotalCards)
            .Select(i => root.GetNode<Button>($"{cardsBasePath}/CardButton{i}"))
            .ToList();

        var knownColors = new Dictionary<int, Color>();
        var matchedIndices = new HashSet<int>();
        var matchedPairCount = 0;

        while (matchedPairCount < TotalPairs)
        {
            var (firstIndex, secondIndex) = ChooseNextMove(knownColors, matchedIndices, TotalCards);

            cardButtons[firstIndex].EmitSignal(Button.SignalName.Pressed);
            await WaitFramesAsync(sceneTree, 1);
            knownColors[firstIndex] = cardButtons[firstIndex].Modulate;

            cardButtons[secondIndex].EmitSignal(Button.SignalName.Pressed);
            await WaitFramesAsync(sceneTree, 1);

            if (!GodotObject.IsInstanceValid(cardButtons[secondIndex]))
            {
                // Bu ikinci tıklama son çifti tamamladı ve modül anında ModuleResultPanel'e
                // geçti — kart node'ları (tüm ModuleHost alt ağacıyla birlikte) zaten serbest
                // bırakıldı, daha fazla okuma yapmadan döngüden çık.
                matchedPairCount = TotalPairs;
                break;
            }

            knownColors[secondIndex] = cardButtons[secondIndex].Modulate;

            if (knownColors[firstIndex] == knownColors[secondIndex])
            {
                matchedIndices.Add(firstIndex);
                matchedIndices.Add(secondIndex);
                matchedPairCount++;
            }
            else
            {
                await WaitUntilFlippedBackAsync(sceneTree, cardButtons[firstIndex]);
                await WaitUntilFlippedBackAsync(sceneTree, cardButtons[secondIndex]);
            }
        }

        await WaitFramesAsync(sceneTree, 10);

        SceneAssert.True(root.HasNode("ModuleResultPanel"), "Tüm çiftler bulunduktan sonra ModuleResultPanel'e geçilmeli.");

        var lastResult = sessionContext.LastModuleResult;
        SceneAssert.NotNull(lastResult, "LastModuleResult set edilmiş olmalı.");
        SceneAssert.Equal("com.freerehabhub.memory-match", lastResult!.ModuleId, "Sonuç doğru modüle ait olmalı.");
        SceneAssert.Equal((double)TotalPairs, lastResult.Metrics["totalPairs"], "6 çift hedeflenmeli.");
        SceneAssert.Equal((double)TotalPairs, lastResult.Metrics["matchedPairs"], "Tüm 6 çift gerçekten bulunmalı.");
        SceneAssert.True(
            lastResult.Metrics["totalAttempts"] >= TotalPairs,
            $"Deneme sayısı en az çift sayısı kadar olmalı, gerçek: {lastResult.Metrics["totalAttempts"]}.");

        var history = await appServices.ProgressRecordService!.GetHistoryByPatientIdAsync(patient.Id);
        SceneAssert.Equal(1, history.Count, "Completed tam bir kez tetiklenmeli — tam olarak 1 ProgressRecord kaydedilmeli.");
    }

    // Öncelik sırası: (1) rengi zaten bilinen ve eşi de bilinen bir çift varsa onu oyna —
    // garanti bir eşleşme. (2) Rengi bilinen ama eşi henüz keşfedilmemiş tek bir kart varsa,
    // onu yeni bir kartla eşleştirmeyi dene (körlemesine iki yeni kart açmaktan daha
    // bilgilendirici — ayrıca son çiftte "1 bilinen + 1 keşfedilmemiş" durumunu doğru ele alır).
    // (3) Hiçbir bilgi yoksa iki yeni kartı keşfetmek için aç.
    private static (int First, int Second) ChooseNextMove(
        Dictionary<int, Color> knownColors, HashSet<int> matchedIndices, int totalCards)
    {
        var pendingKnown = knownColors.Where(pair => !matchedIndices.Contains(pair.Key)).ToList();

        var knownGroup = pendingKnown.GroupBy(pair => pair.Value).FirstOrDefault(group => group.Count() >= 2);
        if (knownGroup is not null)
        {
            var indices = knownGroup.Select(pair => pair.Key).Take(2).ToList();
            return (indices[0], indices[1]);
        }

        var unexplored = Enumerable.Range(0, totalCards)
            .Where(i => !matchedIndices.Contains(i) && !knownColors.ContainsKey(i))
            .ToList();

        if (pendingKnown.Count > 0 && unexplored.Count > 0)
        {
            return (pendingKnown[0].Key, unexplored[0]);
        }

        return (unexplored[0], unexplored[1]);
    }

    private static async Task WaitUntilFlippedBackAsync(SceneTree sceneTree, Button card)
    {
        for (var i = 0; i < MaxFramesToWaitForFlipBack; i++)
        {
            if (card.Modulate == Colors.White)
            {
                return;
            }

            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }

        SceneAssert.True(false, "Eşleşmeyen kartlar beklenen sürede (mismatch timer) geri çevrilmedi.");
    }

    private static async Task WaitFramesAsync(SceneTree sceneTree, int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
