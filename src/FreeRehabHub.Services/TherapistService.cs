using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class TherapistService
{
    private readonly ITherapistRepository _therapistRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public TherapistService(ITherapistRepository therapistRepository, IAuditLogRepository auditLogRepository)
    {
        _therapistRepository = therapistRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Therapist?> GetByIdAsync(
        Guid id, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        var therapist = await _therapistRepository.GetByIdAsync(id, cancellationToken);
        if (therapist is not null)
        {
            await LogAsync(actingTherapistId, id, AuditAction.Viewed, cancellationToken);
        }

        return therapist;
    }

    public Task<IReadOnlyList<Therapist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _therapistRepository.GetAllAsync(cancellationToken);
    }

    public async Task AddAsync(
        Therapist therapist, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _therapistRepository.AddAsync(therapist, cancellationToken);
        await LogAsync(actingTherapistId, therapist.Id, AuditAction.Created, cancellationToken);
    }

    public async Task UpdateAsync(
        Therapist therapist, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _therapistRepository.UpdateAsync(therapist, cancellationToken);
        await LogAsync(actingTherapistId, therapist.Id, AuditAction.Updated, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _therapistRepository.DeleteAsync(id, cancellationToken);
        await LogAsync(actingTherapistId, id, AuditAction.Deleted, cancellationToken);
    }

    private Task LogAsync(
        Guid actingTherapistId, Guid therapistId, AuditAction action, CancellationToken cancellationToken)
    {
        return _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.Therapist,
                RecordId = therapistId,
                Action = action
            },
            cancellationToken);
    }
}
