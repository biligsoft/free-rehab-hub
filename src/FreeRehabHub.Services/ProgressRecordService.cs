using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class ProgressRecordService
{
    private readonly IProgressRecordRepository _progressRecordRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ProgressRecordService(
        IProgressRecordRepository progressRecordRepository, IAuditLogRepository auditLogRepository)
    {
        _progressRecordRepository = progressRecordRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task AddAsync(
        ProgressRecord record, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _progressRecordRepository.AddAsync(record, cancellationToken);
        await _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.ProgressRecord,
                RecordId = record.Id,
                Action = AuditAction.Created
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<ProgressRecord>> GetHistoryByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        return _progressRecordRepository.GetHistoryByPatientIdAsync(patientId, cancellationToken);
    }
}
