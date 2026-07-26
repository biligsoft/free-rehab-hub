namespace FreeRehabHub.Domain;

public sealed class ConsentRecord
{
    public Guid PatientId { get; set; }
    public string ConsentGivenByName { get; set; } = string.Empty;
    public bool IsGuardianConsent { get; set; }
    public DateTime ConsentedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
}
