using FreeRehabHub.Core;

namespace FreeRehabHub.Modules.Contracts;

public sealed class FormSchema
{
    public string Id { get; set; } = string.Empty;
    public LocalizedText Title { get; set; } = new();
    public List<FormField> Fields { get; set; } = new();
}
