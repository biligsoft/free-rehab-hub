namespace FreeRehabHub.Domain.Repositories;

public interface ITherapistRepository
{
    Task<Therapist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Therapist>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Therapist therapist, CancellationToken cancellationToken = default);
    Task UpdateAsync(Therapist therapist, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
