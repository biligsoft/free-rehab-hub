using Xunit;

namespace FreeRehabHub.Modules.Contracts.Tests;

// manifest.json'dan parse edilmiş bir ModuleManifest ile C# kodundaki hardcoded ModuleManifest'i
// alan alan karşılaştırır. ModuleManifest ve LocalizedText/DifficultyRange Equals override etmediği
// için xUnit'in Assert.Equal'ı referans eşitliğine düşer — bu yüzden elle karşılaştırma gerekiyor.
public static class ManifestAssert
{
    public static void Equal(ModuleManifest fromManifestJson, ModuleManifest fromCode)
    {
        Assert.Equal(fromCode.Id, fromManifestJson.Id);
        Assert.Equal(fromCode.Version, fromManifestJson.Version);
        Assert.Equal(fromCode.Kind, fromManifestJson.Kind);
        Assert.Equal(fromCode.DisplayName.Tr, fromManifestJson.DisplayName.Tr);
        Assert.Equal(fromCode.DisplayName.En, fromManifestJson.DisplayName.En);
        Assert.Equal(fromCode.Description.Tr, fromManifestJson.Description.Tr);
        Assert.Equal(fromCode.Description.En, fromManifestJson.Description.En);
        Assert.Equal(fromCode.Disciplines, fromManifestJson.Disciplines);
        Assert.Equal(fromCode.DifficultyRange.Min, fromManifestJson.DifficultyRange.Min);
        Assert.Equal(fromCode.DifficultyRange.Max, fromManifestJson.DifficultyRange.Max);
        Assert.Equal(fromCode.RequiredCapabilities, fromManifestJson.RequiredCapabilities);
        Assert.Equal(fromCode.MinAppVersion, fromManifestJson.MinAppVersion);
        Assert.Equal(fromCode.EntryPointType, fromManifestJson.EntryPointType);
        Assert.Equal(fromCode.ScenePath, fromManifestJson.ScenePath);
        Assert.Equal(fromCode.FormSchemaPath, fromManifestJson.FormSchemaPath);
    }
}
