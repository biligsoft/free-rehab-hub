using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakeConsentRecordRepository : IConsentRecordRepository
{
    private readonly Dictionary<Guid, ConsentRecord> _records = new();

    public Task<ConsentRecord?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.TryGetValue(patientId, out var record) ? record : null);
    }

    public Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default)
    {
        _records[record.PatientId] = record;
        return Task.CompletedTask;
    }

    public Task WithdrawAsync(Guid patientId, DateTime withdrawnAt, CancellationToken cancellationToken = default)
    {
        if (_records.TryGetValue(patientId, out var record))
        {
            record.WithdrawnAt = withdrawnAt;
        }

        return Task.CompletedTask;
    }
}
