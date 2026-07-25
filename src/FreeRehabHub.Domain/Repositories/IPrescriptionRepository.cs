namespace FreeRehabHub.Domain.Repositories;

public interface IPrescriptionRepository
{
    Task AddAsync(ExercisePrescription prescription, CancellationToken cancellationToken = default);
    Task<ExercisePrescription?> GetLatestByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExercisePrescription>> GetHistoryByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}
