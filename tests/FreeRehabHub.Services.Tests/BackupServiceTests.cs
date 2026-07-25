using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class BackupServiceTests
{
    private readonly FakeBackupRepository _backupRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly BackupService _service;
    private readonly Guid _therapistId = Guid.NewGuid();

    public BackupServiceTests()
    {
        _service = new BackupService(_backupRepository, _auditLogRepository);
    }

    [Fact]
    public async Task CreateBackupAsync_DelegatesToRepository_AndReturnsPath()
    {
        _backupRepository.ResultPath = "/backups/freerehabhub-backup-20260725-000000.db";

        var backupPath = await _service.CreateBackupAsync("/backups", _therapistId);

        Assert.Equal("/backups", _backupRepository.LastDestinationDirectory);
        Assert.Equal("/backups/freerehabhub-backup-20260725-000000.db", backupPath);
    }

    [Fact]
    public async Task CreateBackupAsync_LogsDatabaseBackupEvent()
    {
        await _service.CreateBackupAsync("/backups", _therapistId);

        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditRecordType.Database, entry.RecordType);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(Guid.Empty, entry.RecordId);
        Assert.Equal(_therapistId, entry.TherapistId);
    }
}
