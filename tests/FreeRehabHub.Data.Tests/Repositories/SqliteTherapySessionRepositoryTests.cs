using FreeRehabHub.Core;
using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqliteTherapySessionRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteTherapySessionRepository _repository;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _therapistId = Guid.NewGuid();

    public SqliteTherapySessionRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _repository = new SqliteTherapySessionRepository(connectionFactory);

        // TherapySessions, Patients/Therapists'e FK ile bağlı (PRAGMA foreign_keys = ON) — önce seed lazım.
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
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSameSession_WithNullEndedAtAndNotes()
    {
        var session = NewSession();

        await _repository.AddAsync(session);
        var fetched = await _repository.GetByIdAsync(session.Id);

        Assert.NotNull(fetched);
        Assert.Equal(session.PatientId, fetched!.PatientId);
        Assert.Equal(session.TherapistId, fetched.TherapistId);
        Assert.Equal(session.StartedAt, fetched.StartedAt);
        Assert.Null(fetched.EndedAt);
        Assert.Null(fetched.Notes);
    }

    [Fact]
    public async Task AddAsync_WithEndedAtAndNotes_RoundTrips()
    {
        var session = NewSession();
        session.EndedAt = session.StartedAt.AddMinutes(45);
        session.Notes = "Seans notu";

        await _repository.AddAsync(session);
        var fetched = await _repository.GetByIdAsync(session.Id);

        Assert.Equal(session.EndedAt, fetched!.EndedAt);
        Assert.Equal(session.Notes, fetched.Notes);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsOnlyThatPatientsSessions_OrderedByStartedAt()
    {
        var otherPatientId = Guid.NewGuid();
        new SqlitePatientRepository(new SqliteConnectionFactory(_databasePath, "test-password"))
            .AddAsync(new Patient
            {
                Id = otherPatientId,
                FullName = "Diğer Hasta",
                DateOfBirth = new DateOnly(1995, 1, 1),
                CreatedAt = DateTime.UtcNow
            }).GetAwaiter().GetResult();

        var earlier = NewSession();
        earlier.StartedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var later = NewSession();
        later.StartedAt = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);
        var otherPatientsSession = NewSession();
        otherPatientsSession.PatientId = otherPatientId;

        await _repository.AddAsync(later);
        await _repository.AddAsync(earlier);
        await _repository.AddAsync(otherPatientsSession);

        var result = await _repository.GetByPatientIdAsync(_patientId);

        Assert.Equal([earlier.Id, later.Id], result.Select(s => s.Id));
    }

    [Fact]
    public async Task UpdateAsync_ChangesStoredFields()
    {
        var session = NewSession();
        await _repository.AddAsync(session);

        session.EndedAt = session.StartedAt.AddMinutes(30);
        session.Notes = "Güncellendi";
        await _repository.UpdateAsync(session);
        var fetched = await _repository.GetByIdAsync(session.Id);

        Assert.Equal(session.EndedAt, fetched!.EndedAt);
        Assert.Equal("Güncellendi", fetched.Notes);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSession()
    {
        var session = NewSession();
        await _repository.AddAsync(session);

        await _repository.DeleteAsync(session.Id);
        var fetched = await _repository.GetByIdAsync(session.Id);

        Assert.Null(fetched);
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

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
