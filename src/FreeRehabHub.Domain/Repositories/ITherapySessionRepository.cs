namespace FreeRehabHub.Domain.Repositories;

public interface ITherapySessionRepository
{
    Task<TherapySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task AddAsync(TherapySession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(TherapySession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
