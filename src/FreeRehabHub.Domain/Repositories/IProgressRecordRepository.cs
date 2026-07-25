namespace FreeRehabHub.Domain.Repositories;

public interface IProgressRecordRepository
{
    Task AddAsync(ProgressRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProgressRecord>> GetHistoryByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default);
}
