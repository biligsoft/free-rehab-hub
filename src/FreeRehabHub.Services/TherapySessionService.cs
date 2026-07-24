using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class TherapySessionService
{
    private readonly ITherapySessionRepository _sessionRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public TherapySessionService(
        ITherapySessionRepository sessionRepository, IAuditLogRepository auditLogRepository)
    {
        _sessionRepository = sessionRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<TherapySession?> GetByIdAsync(
        Guid id, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(id, cancellationToken);
        if (session is not null)
        {
            await LogAsync(actingTherapistId, id, AuditAction.Viewed, cancellationToken);
        }

        return session;
    }

    public Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        return _sessionRepository.GetByPatientIdAsync(patientId, cancellationToken);
    }

    public async Task AddAsync(
        TherapySession session, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _sessionRepository.AddAsync(session, cancellationToken);
        await LogAsync(actingTherapistId, session.Id, AuditAction.Created, cancellationToken);
    }

    public async Task UpdateAsync(
        TherapySession session, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await LogAsync(actingTherapistId, session.Id, AuditAction.Updated, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _sessionRepository.DeleteAsync(id, cancellationToken);
        await LogAsync(actingTherapistId, id, AuditAction.Deleted, cancellationToken);
    }

    private Task LogAsync(
        Guid actingTherapistId, Guid sessionId, AuditAction action, CancellationToken cancellationToken)
    {
        return _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.TherapySession,
                RecordId = sessionId,
                Action = action
            },
            cancellationToken);
    }
}
