using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class AccessControlServiceTests
{
    private readonly FakeKioskPinRepository _kioskPinRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly AccessControlService _service;
    private readonly Guid _therapistId = Guid.NewGuid();

    public AccessControlServiceTests()
    {
        _service = new AccessControlService(_kioskPinRepository, _auditLogRepository);
    }

    [Fact]
    public async Task IsPinConfiguredAsync_NoPinSet_ReturnsFalse()
    {
        Assert.False(await _service.IsPinConfiguredAsync());
    }

    [Fact]
    public async Task SetPinAsync_FirstTime_LogsCreated()
    {
        await _service.SetPinAsync("1234", _therapistId);

        Assert.True(await _service.IsPinConfiguredAsync());
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditRecordType.KioskPin, entry.RecordType);
        Assert.Equal(Guid.Empty, entry.RecordId);
        Assert.Equal(_therapistId, entry.TherapistId);
    }

    [Fact]
    public async Task SetPinAsync_WhenAlreadyConfigured_LogsUpdated()
    {
        await _service.SetPinAsync("1234", _therapistId);

        await _service.SetPinAsync("5678", _therapistId);

        Assert.Equal(2, _auditLogRepository.Entries.Count);
        Assert.Equal(AuditAction.Updated, _auditLogRepository.Entries[1].Action);
    }

    [Fact]
    public async Task SetPinAsync_BlankPin_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetPinAsync("   ", _therapistId));
    }

    [Fact]
    public async Task VerifyPinAsync_NoPinConfigured_ReturnsFalse()
    {
        Assert.False(await _service.VerifyPinAsync("1234"));
    }

    [Fact]
    public async Task VerifyPinAsync_CorrectPin_ReturnsTrue()
    {
        await _service.SetPinAsync("1234", _therapistId);

        Assert.True(await _service.VerifyPinAsync("1234"));
    }

    [Fact]
    public async Task VerifyPinAsync_IncorrectPin_ReturnsFalse()
    {
        await _service.SetPinAsync("1234", _therapistId);

        Assert.False(await _service.VerifyPinAsync("0000"));
    }
}
