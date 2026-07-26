using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class ConsentService
{
    private readonly IConsentRecordRepository _consentRecordRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ConsentService(IConsentRecordRepository consentRecordRepository, IAuditLogRepository auditLogRepository)
    {
        _consentRecordRepository = consentRecordRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task AddAsync(ConsentRecord record, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(record.ConsentGivenByName))
        {
            throw new ArgumentException("Rıza veren adı boş olamaz.", nameof(record));
        }

        await _consentRecordRepository.AddAsync(record, cancellationToken);
        await LogAsync(actingTherapistId, record.PatientId, AuditAction.Created, cancellationToken);
    }

    public async Task<ConsentRecord?> GetByPatientIdAsync(
        Guid patientId, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        var record = await _consentRecordRepository.GetByPatientIdAsync(patientId, cancellationToken);
        if (record is not null)
        {
            await LogAsync(actingTherapistId, patientId, AuditAction.Viewed, cancellationToken);
        }

        return record;
    }

    private Task LogAsync(Guid actingTherapistId, Guid patientId, AuditAction action, CancellationToken cancellationToken)
    {
        return _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.ConsentRecord,
                RecordId = patientId,
                Action = action
            },
            cancellationToken);
    }
}
