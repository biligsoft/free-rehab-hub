using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakePatientRepository : IPatientRepository
{
    private readonly Dictionary<Guid, Patient> _patients = new();

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_patients.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Patient>>(_patients.Values.ToList());
    }

    public Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _patients[patient.Id] = patient;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _patients[patient.Id] = patient;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _patients.Remove(id);
        return Task.CompletedTask;
    }
}
