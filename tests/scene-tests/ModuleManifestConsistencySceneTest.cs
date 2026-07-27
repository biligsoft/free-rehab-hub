using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Modules.ArmRaise;
using FreeRehabHub.Modules.BalloonPop;
using FreeRehabHub.Modules.ColorSort;
using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Modules.MemoryMatch;
using FreeRehabHub.Modules.TargetTap;
using Godot;

namespace FreeRehabHub.SceneTests;

// modules/*/manifest.json (keşif için, bkz. CLAUDE.md F4.02) ve modülün C# sınıfındaki hardcoded
// ModuleManifest (çalışma zamanı otoritesi) bilinçli bir ikilik taşıyor — ikisi elle senkron
// tutulmalı. Exercise modülleri (target-tap, arm-raise) Node-türevi olduğu için xUnit'te
// kurulamıyor, bu yüzden bu tutarlılık kontrolü burada, gerçek Godot çalışma zamanında yapılıyor.
// Assessment modülü (general-functional-checkin, Godot-bağımsız) için eşdeğer kontrol
// tests/FreeRehabHub.Modules.Contracts.Tests/ManifestConsistencyTests.cs'te.
public sealed class ModuleManifestConsistencySceneTest : ISceneTest
{
    public string Name => "Modül manifest.json ↔ C# Manifest tutarlılığı (Exercise modülleri)";

    public Task RunAsync(SceneTree sceneTree)
    {
        var moduleRegistryAutoload = sceneTree.Root.GetNode<ModuleRegistryAutoload>("/root/ModuleRegistryAutoload");
        var manifestsFromJson = moduleRegistryAutoload.Registry.GetAvailableModules();

        AssertMatches(manifestsFromJson, new TargetTapController().Manifest);
        AssertMatches(manifestsFromJson, new ArmRaiseController().Manifest);
        AssertMatches(manifestsFromJson, new ColorSortController().Manifest);
        AssertMatches(manifestsFromJson, new BalloonPopController().Manifest);
        AssertMatches(manifestsFromJson, new MemoryMatchController().Manifest);

        return Task.CompletedTask;
    }

    private static void AssertMatches(IReadOnlyList<ModuleManifest> manifestsFromJson, ModuleManifest fromCode)
    {
        var fromManifestJson = manifestsFromJson.FirstOrDefault(m => m.Id == fromCode.Id);
        SceneAssert.NotNull(fromManifestJson, $"'{fromCode.Id}' için manifest.json bulunamadı.");

        SceneAssert.Equal(fromCode.Version, fromManifestJson!.Version, $"Version ({fromCode.Id})");
        SceneAssert.Equal(fromCode.Kind, fromManifestJson.Kind, $"Kind ({fromCode.Id})");
        SceneAssert.Equal(fromCode.DisplayName.Tr, fromManifestJson.DisplayName.Tr, $"DisplayName.Tr ({fromCode.Id})");
        SceneAssert.Equal(fromCode.DisplayName.En, fromManifestJson.DisplayName.En, $"DisplayName.En ({fromCode.Id})");
        SceneAssert.Equal(fromCode.Description.Tr, fromManifestJson.Description.Tr, $"Description.Tr ({fromCode.Id})");
        SceneAssert.Equal(fromCode.Description.En, fromManifestJson.Description.En, $"Description.En ({fromCode.Id})");
        SceneAssert.True(
            fromCode.Disciplines.SequenceEqual(fromManifestJson.Disciplines), $"Disciplines eşleşmeli ({fromCode.Id})");
        SceneAssert.Equal(fromCode.DifficultyRange.Min, fromManifestJson.DifficultyRange.Min, $"DifficultyRange.Min ({fromCode.Id})");
        SceneAssert.Equal(fromCode.DifficultyRange.Max, fromManifestJson.DifficultyRange.Max, $"DifficultyRange.Max ({fromCode.Id})");
        SceneAssert.True(
            fromCode.RequiredCapabilities.SequenceEqual(fromManifestJson.RequiredCapabilities),
            $"RequiredCapabilities eşleşmeli ({fromCode.Id})");
        SceneAssert.Equal(fromCode.MinAppVersion, fromManifestJson.MinAppVersion, $"MinAppVersion ({fromCode.Id})");
        SceneAssert.Equal(fromCode.EntryPointType, fromManifestJson.EntryPointType, $"EntryPointType ({fromCode.Id})");
        SceneAssert.Equal(fromCode.ScenePath, fromManifestJson.ScenePath, $"ScenePath ({fromCode.Id})");
        SceneAssert.True(
            fromCode.MetricLabels.Keys.OrderBy(key => key).SequenceEqual(fromManifestJson.MetricLabels.Keys.OrderBy(key => key)),
            $"MetricLabels anahtarları eşleşmeli ({fromCode.Id})");
        foreach (var key in fromCode.MetricLabels.Keys)
        {
            SceneAssert.Equal(
                fromCode.MetricLabels[key].Tr, fromManifestJson.MetricLabels[key].Tr, $"MetricLabels[{key}].Tr ({fromCode.Id})");
            SceneAssert.Equal(
                fromCode.MetricLabels[key].En, fromManifestJson.MetricLabels[key].En, $"MetricLabels[{key}].En ({fromCode.Id})");
        }
    }
}
