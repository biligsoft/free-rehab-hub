namespace FreeRehabHub.Modules.Contracts;

public sealed class FormFieldOption
{
    public string Value { get; set; } = string.Empty;
    public LocalizedText Label { get; set; } = new();
}
