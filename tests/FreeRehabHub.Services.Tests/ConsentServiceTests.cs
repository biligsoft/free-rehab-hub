using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class ConsentServiceTests
{
    private readonly FakeConsentRecordRepository _consentRecordRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly ConsentService _service;
    private readonly Guid _therapistId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public ConsentServiceTests()
    {
        _service = new ConsentService(_consentRecordRepository, _auditLogRepository);
    }

    [Fact]
    public async Task GetByPatientIdAsync_NoRecord_ReturnsNullAndDoesNotLog()
    {
        var record = await _service.GetByPatientIdAsync(_patientId, _therapistId);

        Assert.Null(record);
        Assert.Empty(_auditLogRepository.Entries);
    }

    [Fact]
    public async Task AddAsync_ValidRecord_StoresAndLogsCreated()
    {
        await _service.AddAsync(
            new ConsentRecord
            {
                PatientId = _patientId,
                ConsentGivenByName = "Ayşe Yılmaz",
                IsGuardianConsent = false,
                ConsentedAt = DateTime.UtcNow
            },
            _therapistId);

        var fetched = await _service.GetByPatientIdAsync(_patientId, _therapistId);
        Assert.NotNull(fetched);
        Assert.Equal("Ayşe Yılmaz", fetched!.ConsentGivenByName);

        var createdEntry = Assert.Single(_auditLogRepository.Entries, e => e.Action == AuditAction.Created);
        Assert.Equal(AuditRecordType.ConsentRecord, createdEntry.RecordType);
        Assert.Equal(_patientId, createdEntry.RecordId);
        Assert.Equal(_therapistId, createdEntry.TherapistId);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ExistingRecord_LogsViewed()
    {
        await _service.AddAsync(
            new ConsentRecord
            {
                PatientId = _patientId,
                ConsentGivenByName = "Veli Kaya",
                IsGuardianConsent = true,
                ConsentedAt = DateTime.UtcNow
            },
            _therapistId);

        await _service.GetByPatientIdAsync(_patientId, _therapistId);

        Assert.Single(_auditLogRepository.Entries, e => e.Action == AuditAction.Viewed);
    }

    [Fact]
    public async Task AddAsync_BlankConsentGivenByName_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAsync(
            new ConsentRecord { PatientId = _patientId, ConsentGivenByName = "   ", ConsentedAt = DateTime.UtcNow },
            _therapistId));
    }

    [Fact]
    public async Task WithdrawAsync_ExistingRecord_SetsWithdrawnAtAndLogsWithdrawn()
    {
        await _service.AddAsync(
            new ConsentRecord
            {
                PatientId = _patientId,
                ConsentGivenByName = "Ayşe Yılmaz",
                ConsentedAt = DateTime.UtcNow
            },
            _therapistId);

        await _service.WithdrawAsync(_patientId, _therapistId);

        var fetched = await _consentRecordRepository.GetByPatientIdAsync(_patientId);
        Assert.NotNull(fetched!.WithdrawnAt);

        var withdrawnEntry = Assert.Single(_auditLogRepository.Entries, e => e.Action == AuditAction.Withdrawn);
        Assert.Equal(AuditRecordType.ConsentRecord, withdrawnEntry.RecordType);
        Assert.Equal(_patientId, withdrawnEntry.RecordId);
        Assert.Equal(_therapistId, withdrawnEntry.TherapistId);
    }

    [Fact]
    public async Task WithdrawAsync_NoRecord_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.WithdrawAsync(_patientId, _therapistId));
    }

    [Fact]
    public async Task WithdrawAsync_AlreadyWithdrawn_Throws()
    {
        await _service.AddAsync(
            new ConsentRecord
            {
                PatientId = _patientId,
                ConsentGivenByName = "Ayşe Yılmaz",
                ConsentedAt = DateTime.UtcNow
            },
            _therapistId);
        await _service.WithdrawAsync(_patientId, _therapistId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.WithdrawAsync(_patientId, _therapistId));
    }
}
