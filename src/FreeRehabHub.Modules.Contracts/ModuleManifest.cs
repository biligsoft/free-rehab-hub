using FreeRehabHub.Core;

namespace FreeRehabHub.Modules.Contracts;

public sealed class ModuleManifest
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ModuleKind Kind { get; set; }
    public LocalizedText DisplayName { get; set; } = new();
    public LocalizedText Description { get; set; } = new();
    public List<Discipline> Disciplines { get; set; } = new();
    public DifficultyRange DifficultyRange { get; set; } = new();
    public List<string> RequiredCapabilities { get; set; } = new();
    public string MinAppVersion { get; set; } = string.Empty;
    public string EntryPointType { get; set; } = string.Empty;
    public string? ScenePath { get; set; }
    public string? FormSchemaPath { get; set; }
    public Dictionary<string, LocalizedText> MetricLabels { get; set; } = new();
}
