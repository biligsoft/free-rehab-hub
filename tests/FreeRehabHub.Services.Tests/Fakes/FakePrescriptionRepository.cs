using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakePrescriptionRepository : IPrescriptionRepository
{
    private readonly List<ExercisePrescription> _prescriptions = new();

    public Task AddAsync(ExercisePrescription prescription, CancellationToken cancellationToken = default)
    {
        _prescriptions.Add(prescription);
        return Task.CompletedTask;
    }

    public Task<ExercisePrescription?> GetLatestByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var latest = _prescriptions
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task<IReadOnlyList<ExercisePrescription>> GetHistoryByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var history = _prescriptions
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ExercisePrescription>>(history);
    }
}
