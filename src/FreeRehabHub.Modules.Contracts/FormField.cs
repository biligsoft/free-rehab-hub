namespace FreeRehabHub.Modules.Contracts;

public sealed class FormField
{
    public string Id { get; set; } = string.Empty;
    public FormFieldType Type { get; set; }
    public LocalizedText Label { get; set; } = new();
    public bool Required { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public List<FormFieldOption> Options { get; set; } = new();
}
