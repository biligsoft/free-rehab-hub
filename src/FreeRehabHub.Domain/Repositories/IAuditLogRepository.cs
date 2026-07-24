namespace FreeRehabHub.Domain.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogEntry>> GetByRecordAsync(
        AuditRecordType recordType, Guid recordId, CancellationToken cancellationToken = default);
}
