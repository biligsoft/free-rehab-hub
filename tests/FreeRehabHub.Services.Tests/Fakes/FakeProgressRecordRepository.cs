using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakeProgressRecordRepository : IProgressRecordRepository
{
    private readonly List<ProgressRecord> _records = new();

    public Task AddAsync(ProgressRecord record, CancellationToken cancellationToken = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProgressRecord>> GetHistoryByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var history = _records
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CompletedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ProgressRecord>>(history);
    }
}
