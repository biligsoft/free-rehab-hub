using FreeRehabHub.Core;
using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqliteProgressRecordRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteProgressRecordRepository _repository;
    private readonly SqliteTherapySessionRepository _sessionRepository;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _therapistId = Guid.NewGuid();

    public SqliteProgressRecordRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _repository = new SqliteProgressRecordRepository(connectionFactory);
        _sessionRepository = new SqliteTherapySessionRepository(connectionFactory);

        // ProgressRecords, Patients'a VE (F8.31) TherapySessions'a FK ile bağlı
        // (PRAGMA foreign_keys = ON) — önce ikisini de seed etmek lazım.
        new SqlitePatientRepository(connectionFactory).AddAsync(new Patient
        {
            Id = _patientId,
            FullName = "Test Hasta",
            DateOfBirth = new DateOnly(2000, 1, 1),
            CreatedAt = DateTime.UtcNow
        }).GetAwaiter().GetResult();

        new SqliteTherapistRepository(connectionFactory).AddAsync(new Therapist
        {
            Id = _therapistId,
            FullName = "Test Terapist",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = DateTime.UtcNow
        }).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddAsync_ThenGetHistoryByPatientIdAsync_ReturnsSameRecordWithMetrics()
    {
        var record = NewRecord();
        record.Metrics["completedReps"] = 10;
        record.Metrics["averageAngleDegrees"] = 142.5;

        await _repository.AddAsync(record);
        var history = await _repository.GetHistoryByPatientIdAsync(_patientId);

        var fetched = Assert.Single(history);
        Assert.Equal(record.Id, fetched.Id);
        Assert.Equal(record.ModuleId, fetched.ModuleId);
        Assert.Equal(record.SessionId, fetched.SessionId);
        Assert.Equal(record.NormalizedScore, fetched.NormalizedScore);
        Assert.Equal(10, fetched.Metrics["completedReps"]);
        Assert.Equal(142.5, fetched.Metrics["averageAngleDegrees"]);
    }

    [Fact]
    public async Task AddAsync_WithNullNotesAndNoMetrics_RoundTrips()
    {
        var record = NewRecord();
        record.Notes = null;

        await _repository.AddAsync(record);
        var fetched = Assert.Single(await _repository.GetHistoryByPatientIdAsync(_patientId));

        Assert.Null(fetched.Notes);
        Assert.Empty(fetched.Metrics);
    }

    [Fact]
    public async Task GetHistoryByPatientIdAsync_MultipleRecords_ReturnsAllOrderedByCompletedAtDescending()
    {
        var older = NewRecord();
        older.CompletedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var newer = NewRecord();
        newer.CompletedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);

        await _repository.AddAsync(older);
        await _repository.AddAsync(newer);
        var history = await _repository.GetHistoryByPatientIdAsync(_patientId);

        Assert.Equal([newer.Id, older.Id], history.Select(r => r.Id));
    }

    [Fact]
    public async Task GetHistoryByPatientIdAsync_NoRecords_ReturnsEmpty()
    {
        var history = await _repository.GetHistoryByPatientIdAsync(_patientId);

        Assert.Empty(history);
    }

    private ProgressRecord NewRecord()
    {
        var session = new TherapySession
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            TherapistId = _therapistId,
            StartedAt = DateTime.UtcNow
        };
        _sessionRepository.AddAsync(session).GetAwaiter().GetResult();

        return new ProgressRecord
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            ModuleId = "com.freerehabhub.arm-raise",
            SessionId = session.Id,
            CompletedAt = DateTime.UtcNow,
            NormalizedScore = 0.8,
            Notes = "Test notu"
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
