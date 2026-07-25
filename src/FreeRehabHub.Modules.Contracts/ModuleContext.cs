namespace FreeRehabHub.Modules.Contracts;

public sealed class ModuleContext
{
    public Guid PatientId { get; set; }
    public Guid SessionId { get; set; }
}
