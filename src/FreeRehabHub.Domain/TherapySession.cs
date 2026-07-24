namespace FreeRehabHub.Domain;

public sealed class TherapySession
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid TherapistId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }
}
