namespace FreeRehabHub.Domain.Repositories;

public interface IConsentRecordRepository
{
    Task<ConsentRecord?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default);
    Task WithdrawAsync(Guid patientId, DateTime withdrawnAt, CancellationToken cancellationToken = default);
}
