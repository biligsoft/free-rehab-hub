namespace FreeRehabHub.Modules.Contracts;

public sealed class FormSubmission
{
    public Dictionary<string, string> FieldValues { get; set; } = new();
}
