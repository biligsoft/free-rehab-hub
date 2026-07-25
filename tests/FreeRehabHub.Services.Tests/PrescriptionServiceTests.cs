using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class PrescriptionServiceTests
{
    private readonly FakePrescriptionRepository _prescriptionRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly PrescriptionService _service;
    private readonly Guid _therapistId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public PrescriptionServiceTests()
    {
        _service = new PrescriptionService(_prescriptionRepository, _auditLogRepository);
    }

    [Fact]
    public async Task AddAsync_WritesPrescription_AndLogsCreated()
    {
        var prescription = NewPrescription();

        await _service.AddAsync(prescription, _therapistId);

        Assert.Equal(prescription, await _prescriptionRepository.GetLatestByPatientIdAsync(_patientId));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditRecordType.Prescription, entry.RecordType);
        Assert.Equal(prescription.Id, entry.RecordId);
        Assert.Equal(_therapistId, entry.TherapistId);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_ExistingPrescription_LogsViewed()
    {
        var prescription = NewPrescription();
        await _prescriptionRepository.AddAsync(prescription);

        var fetched = await _service.GetLatestByPatientIdAsync(_patientId, _therapistId);

        Assert.Equal(prescription, fetched);
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Viewed, entry.Action);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_NoPrescriptions_DoesNotLog()
    {
        var fetched = await _service.GetLatestByPatientIdAsync(_patientId, _therapistId);

        Assert.Null(fetched);
        Assert.Empty(_auditLogRepository.Entries);
    }

    [Fact]
    public async Task GetHistoryByPatientIdAsync_DoesNotLog()
    {
        await _prescriptionRepository.AddAsync(NewPrescription());

        var history = await _service.GetHistoryByPatientIdAsync(_patientId);

        Assert.Single(history);
        Assert.Empty(_auditLogRepository.Entries);
    }

    private ExercisePrescription NewPrescription()
    {
        return new ExercisePrescription
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            CreatedByTherapistId = _therapistId,
            CreatedAt = DateTime.UtcNow,
            Items = { new PrescriptionItem { ExerciseCardId = "ankle-pumps", Repetitions = 15 } }
        };
    }
}
