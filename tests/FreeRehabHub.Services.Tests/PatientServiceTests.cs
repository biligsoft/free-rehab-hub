using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class PatientServiceTests
{
    private readonly FakePatientRepository _patientRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly PatientService _service;
    private readonly Guid _therapistId = Guid.NewGuid();

    public PatientServiceTests()
    {
        _service = new PatientService(_patientRepository, _auditLogRepository);
    }

    [Fact]
    public async Task AddAsync_WritesPatient_AndLogsCreated()
    {
        var patient = NewPatient();

        await _service.AddAsync(patient, _therapistId);

        Assert.Equal(patient, await _patientRepository.GetByIdAsync(patient.Id));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditRecordType.Patient, entry.RecordType);
        Assert.Equal(patient.Id, entry.RecordId);
        Assert.Equal(_therapistId, entry.TherapistId);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingPatient_LogsViewed()
    {
        var patient = NewPatient();
        await _patientRepository.AddAsync(patient);

        var fetched = await _service.GetByIdAsync(patient.Id, _therapistId);

        Assert.Equal(patient, fetched);
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Viewed, entry.Action);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownPatient_DoesNotLog()
    {
        var fetched = await _service.GetByIdAsync(Guid.NewGuid(), _therapistId);

        Assert.Null(fetched);
        Assert.Empty(_auditLogRepository.Entries);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotLog()
    {
        await _patientRepository.AddAsync(NewPatient());

        var all = await _service.GetAllAsync();

        Assert.Single(all);
        Assert.Empty(_auditLogRepository.Entries);
    }

    [Fact]
    public async Task UpdateAsync_LogsUpdated()
    {
        var patient = NewPatient();
        await _patientRepository.AddAsync(patient);

        await _service.UpdateAsync(patient, _therapistId);

        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Updated, entry.Action);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPatient_AndLogsDeleted()
    {
        var patient = NewPatient();
        await _patientRepository.AddAsync(patient);

        await _service.DeleteAsync(patient.Id, _therapistId);

        Assert.Null(await _patientRepository.GetByIdAsync(patient.Id));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Deleted, entry.Action);
    }

    private static Patient NewPatient()
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Test Hasta",
            DateOfBirth = new DateOnly(2000, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
    }
}
