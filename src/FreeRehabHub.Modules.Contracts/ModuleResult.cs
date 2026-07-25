namespace FreeRehabHub.Modules.Contracts;

public sealed class ModuleResult
{
    public string ModuleId { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid SessionId { get; set; }
    public DateTime CompletedAt { get; set; }
    public double NormalizedScore { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public string? Notes { get; set; }
}
