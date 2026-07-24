using FreeRehabHub.Core;
using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqliteAuditLogRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteAuditLogRepository _repository;
    private readonly Guid _therapistId = Guid.NewGuid();

    public SqliteAuditLogRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _repository = new SqliteAuditLogRepository(connectionFactory);

        // AuditLogs.TherapistId, Therapists'e FK ile bağlı — önce seed lazım.
        new SqliteTherapistRepository(connectionFactory).AddAsync(new Therapist
        {
            Id = _therapistId,
            FullName = "Test Terapist",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = DateTime.UtcNow
        }).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddAsync_ThenGetByRecordAsync_ReturnsEntry()
    {
        var patientId = Guid.NewGuid();
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            TherapistId = _therapistId,
            OccurredAt = DateTime.UtcNow,
            RecordType = AuditRecordType.Patient,
            RecordId = patientId,
            Action = AuditAction.Viewed
        };

        await _repository.AddAsync(entry);
        var result = await _repository.GetByRecordAsync(AuditRecordType.Patient, patientId);

        var fetched = Assert.Single(result);
        Assert.Equal(entry.TherapistId, fetched.TherapistId);
        Assert.Equal(entry.OccurredAt, fetched.OccurredAt);
        Assert.Equal(entry.Action, fetched.Action);
    }

    [Fact]
    public async Task GetByRecordAsync_OnlyReturnsMatchingRecordType()
    {
        var recordId = Guid.NewGuid();
        await _repository.AddAsync(NewEntry(AuditRecordType.Patient, recordId, AuditAction.Viewed));
        await _repository.AddAsync(NewEntry(AuditRecordType.TherapySession, recordId, AuditAction.Created));

        var result = await _repository.GetByRecordAsync(AuditRecordType.Patient, recordId);

        var fetched = Assert.Single(result);
        Assert.Equal(AuditRecordType.Patient, fetched.RecordType);
    }

    [Fact]
    public async Task GetByRecordAsync_ReturnsEntries_OrderedByOccurredAt()
    {
        var patientId = Guid.NewGuid();
        var earlier = NewEntry(AuditRecordType.Patient, patientId, AuditAction.Created);
        earlier.OccurredAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var later = NewEntry(AuditRecordType.Patient, patientId, AuditAction.Updated);
        later.OccurredAt = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);

        await _repository.AddAsync(later);
        await _repository.AddAsync(earlier);

        var result = await _repository.GetByRecordAsync(AuditRecordType.Patient, patientId);

        Assert.Equal([earlier.Id, later.Id], result.Select(e => e.Id));
    }

    [Fact]
    public async Task GetByRecordAsync_UnknownRecord_ReturnsEmpty()
    {
        var result = await _repository.GetByRecordAsync(AuditRecordType.Patient, Guid.NewGuid());

        Assert.Empty(result);
    }

    private AuditLogEntry NewEntry(AuditRecordType recordType, Guid recordId, AuditAction action)
    {
        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            TherapistId = _therapistId,
            OccurredAt = DateTime.UtcNow,
            RecordType = recordType,
            RecordId = recordId,
            Action = action
        };
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
