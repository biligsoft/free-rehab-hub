using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakeTherapistRepository : ITherapistRepository
{
    private readonly Dictionary<Guid, Therapist> _therapists = new();

    public Task<Therapist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_therapists.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<Therapist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Therapist>>(_therapists.Values.ToList());
    }

    public Task AddAsync(Therapist therapist, CancellationToken cancellationToken = default)
    {
        _therapists[therapist.Id] = therapist;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Therapist therapist, CancellationToken cancellationToken = default)
    {
        _therapists[therapist.Id] = therapist;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _therapists.Remove(id);
        return Task.CompletedTask;
    }
}
