namespace FreeRehabHub.Domain;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid TherapistId { get; set; }
    public DateTime OccurredAt { get; set; }
    public AuditRecordType RecordType { get; set; }
    public Guid RecordId { get; set; }
    public AuditAction Action { get; set; }
}
