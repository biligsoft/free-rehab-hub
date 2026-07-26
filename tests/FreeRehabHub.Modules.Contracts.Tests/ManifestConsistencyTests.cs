using FreeRehabHub.Modules.GeneralFunctionalCheckin;
using Xunit;

namespace FreeRehabHub.Modules.Contracts.Tests;

// manifest.json (keşif için, bkz. CLAUDE.md F4.02) ve C# sınıfındaki hardcoded ModuleManifest
// (çalışma zamanı otoritesi) bilinçli bir ikilik taşıyor — ikisi elle senkron tutulmalı. Bu test
// general-functional-checkin (Assessment, Godot-bağımsız) için bu iki kaynağın gerçekten aynı
// içeriği taşıdığını doğruluyor. Exercise modülleri (target-tap, arm-raise) Node-türevi olduğu
// için burada test edilemez — bkz. tests/scene-tests/ModuleManifestConsistencySceneTest.cs.
public sealed class ManifestConsistencyTests
{
    private const string GeneralFunctionalCheckinId = "com.freerehabhub.general-functional-checkin";

    private static readonly string ModulesRootPath = Path.Combine(AppContext.BaseDirectory, "TestModulesRoot");
    private static readonly string ModuleAssemblyDirectory = AppContext.BaseDirectory;

    [Fact]
    public void GeneralFunctionalCheckin_ManifestJson_MatchesHardcodedManifest()
    {
        var registry = new ModuleRegistry(ModulesRootPath, ModuleAssemblyDirectory);
        var fromManifestJson = registry.GetAvailableModules().Single(m => m.Id == GeneralFunctionalCheckinId);
        var fromCode = new GeneralFunctionalCheckinAssessment().Manifest;

        ManifestAssert.Equal(fromManifestJson, fromCode);
    }
}
