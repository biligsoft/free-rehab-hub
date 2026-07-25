using FreeRehabHub.Core;
using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqlitePrescriptionRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqlitePrescriptionRepository _repository;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _therapistId = Guid.NewGuid();

    public SqlitePrescriptionRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _repository = new SqlitePrescriptionRepository(connectionFactory);

        // Prescriptions, Patients/Therapists'e FK ile bağlı (PRAGMA foreign_keys = ON) — önce seed lazım.
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
    public async Task AddAsync_ThenGetLatestByPatientIdAsync_ReturnsSamePrescriptionWithItemsInOrder()
    {
        var prescription = NewPrescription();
        prescription.Items.Add(new PrescriptionItem { ExerciseCardId = "shoulder-flexion-supine", Repetitions = 10, Sets = 3 });
        prescription.Items.Add(new PrescriptionItem { ExerciseCardId = "ankle-pumps", Repetitions = 15, Sets = 2 });

        await _repository.AddAsync(prescription);
        var fetched = await _repository.GetLatestByPatientIdAsync(_patientId);

        Assert.NotNull(fetched);
        Assert.Equal(prescription.Id, fetched!.Id);
        Assert.Equal(prescription.CreatedByTherapistId, fetched.CreatedByTherapistId);
        Assert.Equal(
            ["shoulder-flexion-supine", "ankle-pumps"],
            fetched.Items.Select(item => item.ExerciseCardId));
        Assert.Equal(10, fetched.Items[0].Repetitions);
        Assert.Equal(3, fetched.Items[0].Sets);
    }

    [Fact]
    public async Task AddAsync_WithNullNotesAndNullItemFields_RoundTrips()
    {
        var prescription = NewPrescription();
        prescription.Notes = null;
        prescription.Items.Add(new PrescriptionItem { ExerciseCardId = "ankle-pumps" });

        await _repository.AddAsync(prescription);
        var fetched = await _repository.GetLatestByPatientIdAsync(_patientId);

        Assert.Null(fetched!.Notes);
        Assert.Null(fetched.Items[0].Repetitions);
        Assert.Null(fetched.Items[0].Sets);
        Assert.Null(fetched.Items[0].FrequencyPerWeek);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_MultiplePrescriptions_ReturnsMostRecentOnly()
    {
        var older = NewPrescription();
        older.CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var newer = NewPrescription();
        newer.CreatedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);

        await _repository.AddAsync(older);
        await _repository.AddAsync(newer);
        var fetched = await _repository.GetLatestByPatientIdAsync(_patientId);

        Assert.Equal(newer.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetHistoryByPatientIdAsync_ReturnsAllOrderedByCreatedAtDescending()
    {
        var older = NewPrescription();
        older.CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var newer = NewPrescription();
        newer.CreatedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);

        await _repository.AddAsync(older);
        await _repository.AddAsync(newer);
        var history = await _repository.GetHistoryByPatientIdAsync(_patientId);

        Assert.Equal([newer.Id, older.Id], history.Select(p => p.Id));
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_NoPrescriptions_ReturnsNull()
    {
        var fetched = await _repository.GetLatestByPatientIdAsync(_patientId);

        Assert.Null(fetched);
    }

    private ExercisePrescription NewPrescription()
    {
        return new ExercisePrescription
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            CreatedByTherapistId = _therapistId,
            CreatedAt = DateTime.UtcNow,
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
