using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class ProgressRecordServiceTests
{
    private readonly FakeProgressRecordRepository _progressRecordRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly ProgressRecordService _service;
    private readonly Guid _therapistId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public ProgressRecordServiceTests()
    {
        _service = new ProgressRecordService(_progressRecordRepository, _auditLogRepository);
    }

    [Fact]
    public async Task AddAsync_WritesRecord_AndLogsCreated()
    {
        var record = NewRecord();

        await _service.AddAsync(record, _therapistId);

        var history = await _progressRecordRepository.GetHistoryByPatientIdAsync(_patientId);
        Assert.Equal(record, Assert.Single(history));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditRecordType.ProgressRecord, entry.RecordType);
        Assert.Equal(record.Id, entry.RecordId);
        Assert.Equal(_therapistId, entry.TherapistId);
    }

    [Fact]
    public async Task GetHistoryByPatientIdAsync_DoesNotLog()
    {
        await _progressRecordRepository.AddAsync(NewRecord());

        var history = await _service.GetHistoryByPatientIdAsync(_patientId);

        Assert.Single(history);
        Assert.Empty(_auditLogRepository.Entries);
    }

    private ProgressRecord NewRecord()
    {
        return new ProgressRecord
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            ModuleId = "com.freerehabhub.arm-raise",
            SessionId = Guid.NewGuid(),
            CompletedAt = DateTime.UtcNow,
            NormalizedScore = 0.9,
            Metrics = { ["completedReps"] = 10 }
        };
    }
}
