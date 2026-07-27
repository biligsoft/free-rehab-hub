using FreeRehabHub.Core;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Services.Tests;

public sealed class MetricKeyFormatterTests
{
    private const string PainLevelKey = "painLevel";

    private static readonly ModuleManifest ManifestWithLabel = new()
    {
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            [PainLevelKey] = new LocalizedText { Tr = "Ağrı Seviyesi", En = "Pain Level" }
        }
    };

    [Fact]
    public void Humanize_KeyInMetricLabels_TrLocale_ReturnsTurkishLabel()
    {
        var result = MetricKeyFormatter.Humanize(PainLevelKey, ManifestWithLabel, "tr");

        Assert.Equal("Ağrı Seviyesi", result);
    }

    [Fact]
    public void Humanize_KeyInMetricLabels_EnLocale_ReturnsEnglishLabel()
    {
        var result = MetricKeyFormatter.Humanize(PainLevelKey, ManifestWithLabel, "en");

        Assert.Equal("Pain Level", result);
    }

    [Fact]
    public void Humanize_KeyMissingFromMetricLabels_FallsBackToMechanicalTitleCase()
    {
        var result = MetricKeyFormatter.Humanize("symptomCount", ManifestWithLabel, "tr");

        Assert.Equal("Symptom Count", result);
    }

    [Fact]
    public void Humanize_ManifestIsNull_FallsBackToMechanicalTitleCase()
    {
        var result = MetricKeyFormatter.Humanize(PainLevelKey, null, "tr");

        Assert.Equal("Pain Level", result);
    }
}
