using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakeTherapySessionRepository : ITherapySessionRepository
{
    private readonly Dictionary<Guid, TherapySession> _sessions = new();

    public Task<TherapySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TherapySession>>(
            _sessions.Values.Where(s => s.PatientId == patientId).ToList());
    }

    public Task AddAsync(TherapySession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TherapySession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _sessions.Remove(id);
        return Task.CompletedTask;
    }
}
