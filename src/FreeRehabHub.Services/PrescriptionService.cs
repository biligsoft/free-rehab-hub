using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class PrescriptionService
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public PrescriptionService(IPrescriptionRepository prescriptionRepository, IAuditLogRepository auditLogRepository)
    {
        _prescriptionRepository = prescriptionRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task AddAsync(
        ExercisePrescription prescription, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _prescriptionRepository.AddAsync(prescription, cancellationToken);
        await LogAsync(actingTherapistId, prescription.Id, AuditAction.Created, cancellationToken);
    }

    public async Task<ExercisePrescription?> GetLatestByPatientIdAsync(
        Guid patientId, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        var prescription = await _prescriptionRepository.GetLatestByPatientIdAsync(patientId, cancellationToken);
        if (prescription is not null)
        {
            await LogAsync(actingTherapistId, prescription.Id, AuditAction.Viewed, cancellationToken);
        }

        return prescription;
    }

    public Task<IReadOnlyList<ExercisePrescription>> GetHistoryByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        return _prescriptionRepository.GetHistoryByPatientIdAsync(patientId, cancellationToken);
    }

    private Task LogAsync(
        Guid actingTherapistId, Guid prescriptionId, AuditAction action, CancellationToken cancellationToken)
    {
        return _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.Prescription,
                RecordId = prescriptionId,
                Action = action
            },
            cancellationToken);
    }
}
