using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class TherapySessionServiceTests
{
    private readonly FakeTherapySessionRepository _sessionRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly TherapySessionService _service;
    private readonly Guid _therapistId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public TherapySessionServiceTests()
    {
        _service = new TherapySessionService(_sessionRepository, _auditLogRepository);
    }

    [Fact]
    public async Task AddAsync_WritesSession_AndLogsCreated()
    {
        var session = NewSession();

        await _service.AddAsync(session, _therapistId);

        Assert.Equal(session, await _sessionRepository.GetByIdAsync(session.Id));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditRecordType.TherapySession, entry.RecordType);
    }

    [Fact]
    public async Task GetByPatientIdAsync_DoesNotLog()
    {
        await _sessionRepository.AddAsync(NewSession());

        var sessions = await _service.GetByPatientIdAsync(_patientId);

        Assert.Single(sessions);
        Assert.Empty(_auditLogRepository.Entries);
    }

    [Fact]
    public async Task UpdateAsync_LogsUpdated()
    {
        var session = NewSession();
        await _sessionRepository.AddAsync(session);

        await _service.UpdateAsync(session, _therapistId);

        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Updated, entry.Action);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSession_AndLogsDeleted()
    {
        var session = NewSession();
        await _sessionRepository.AddAsync(session);

        await _service.DeleteAsync(session.Id, _therapistId);

        Assert.Null(await _sessionRepository.GetByIdAsync(session.Id));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Deleted, entry.Action);
    }

    private TherapySession NewSession()
    {
        return new TherapySession
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            TherapistId = _therapistId,
            StartedAt = DateTime.UtcNow
        };
    }
}
