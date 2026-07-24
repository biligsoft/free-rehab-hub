using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class PatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public PatientService(IPatientRepository patientRepository, IAuditLogRepository auditLogRepository)
    {
        _patientRepository = patientRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Patient?> GetByIdAsync(
        Guid id, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (patient is not null)
        {
            await LogAsync(actingTherapistId, id, AuditAction.Viewed, cancellationToken);
        }

        return patient;
    }

    public Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _patientRepository.GetAllAsync(cancellationToken);
    }

    public async Task AddAsync(Patient patient, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _patientRepository.AddAsync(patient, cancellationToken);
        await LogAsync(actingTherapistId, patient.Id, AuditAction.Created, cancellationToken);
    }

    public async Task UpdateAsync(
        Patient patient, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _patientRepository.UpdateAsync(patient, cancellationToken);
        await LogAsync(actingTherapistId, patient.Id, AuditAction.Updated, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        await _patientRepository.DeleteAsync(id, cancellationToken);
        await LogAsync(actingTherapistId, id, AuditAction.Deleted, cancellationToken);
    }

    private Task LogAsync(
        Guid actingTherapistId, Guid patientId, AuditAction action, CancellationToken cancellationToken)
    {
        return _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.Patient,
                RecordId = patientId,
                Action = action
            },
            cancellationToken);
    }
}
