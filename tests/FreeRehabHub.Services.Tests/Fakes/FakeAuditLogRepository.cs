using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLogEntry> Entries { get; } = new();

    public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetByRecordAsync(
        AuditRecordType recordType, Guid recordId, CancellationToken cancellationToken = default)
    {
        var matches = Entries.Where(e => e.RecordType == recordType && e.RecordId == recordId).ToList();
        return Task.FromResult<IReadOnlyList<AuditLogEntry>>(matches);
    }
}
