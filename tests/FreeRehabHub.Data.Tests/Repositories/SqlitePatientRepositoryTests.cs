using FreeRehabHub.Core;
using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqlitePatientRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqlitePatientRepository _repository;

    public SqlitePatientRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _repository = new SqlitePatientRepository(connectionFactory);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSamePatient()
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Ayşe Yılmaz",
            DateOfBirth = new DateOnly(1990, 5, 17),
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        await _repository.AddAsync(patient);
        var fetched = await _repository.GetByIdAsync(patient.Id);

        Assert.NotNull(fetched);
        Assert.Equal(patient.Id, fetched!.Id);
        Assert.Equal(patient.FullName, fetched.FullName);
        Assert.Equal(patient.DateOfBirth, fetched.DateOfBirth);
        Assert.Equal(patient.CreatedAt, fetched.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var fetched = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPatients_OrderedByFullName()
    {
        await _repository.AddAsync(NewPatient("Zeynep Kaya"));
        await _repository.AddAsync(NewPatient("Ahmet Demir"));

        var all = await _repository.GetAllAsync();

        Assert.Equal(["Ahmet Demir", "Zeynep Kaya"], all.Select(p => p.FullName));
    }

    [Fact]
    public async Task UpdateAsync_ChangesStoredFields()
    {
        var patient = NewPatient("Mehmet Can");
        await _repository.AddAsync(patient);

        patient.FullName = "Mehmet Can Öz";
        await _repository.UpdateAsync(patient);
        var fetched = await _repository.GetByIdAsync(patient.Id);

        Assert.Equal("Mehmet Can Öz", fetched!.FullName);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPatient()
    {
        var patient = NewPatient("Elif Şahin");
        await _repository.AddAsync(patient);

        await _repository.DeleteAsync(patient.Id);
        var fetched = await _repository.GetByIdAsync(patient.Id);

        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteAsync_PatientWithSessionsPrescriptionsAndProgressRecords_CascadesInsteadOfThrowing()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        var therapistRepository = new SqliteTherapistRepository(connectionFactory);
        var sessionRepository = new SqliteTherapySessionRepository(connectionFactory);
        var prescriptionRepository = new SqlitePrescriptionRepository(connectionFactory);
        var progressRecordRepository = new SqliteProgressRecordRepository(connectionFactory);
        var consentRecordRepository = new SqliteConsentRecordRepository(connectionFactory);

        var therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Dr. Terapist",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = DateTime.UtcNow
        };
        await therapistRepository.AddAsync(therapist);

        var patient = NewPatient("Can Yıldız");
        await _repository.AddAsync(patient);

        await sessionRepository.AddAsync(new TherapySession
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            TherapistId = therapist.Id,
            StartedAt = DateTime.UtcNow
        });

        await prescriptionRepository.AddAsync(new ExercisePrescription
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            CreatedByTherapistId = therapist.Id,
            CreatedAt = DateTime.UtcNow,
            Items = [new PrescriptionItem { ExerciseCardId = "ankle-pumps", Repetitions = 10, Sets = 2 }]
        });

        await progressRecordRepository.AddAsync(new ProgressRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            ModuleId = "com.freerehabhub.arm-raise",
            SessionId = Guid.NewGuid(),
            CompletedAt = DateTime.UtcNow,
            NormalizedScore = 1.0,
            Metrics = new Dictionary<string, double> { ["completedReps"] = 10 }
        });

        await consentRecordRepository.AddAsync(new ConsentRecord
        {
            PatientId = patient.Id,
            ConsentGivenByName = "Veli Yıldız",
            IsGuardianConsent = true,
            ConsentedAt = DateTime.UtcNow
        });

        await _repository.DeleteAsync(patient.Id);

        Assert.Null(await _repository.GetByIdAsync(patient.Id));
        Assert.Empty(await sessionRepository.GetByPatientIdAsync(patient.Id));
        Assert.Empty(await prescriptionRepository.GetHistoryByPatientIdAsync(patient.Id));
        Assert.Empty(await progressRecordRepository.GetHistoryByPatientIdAsync(patient.Id));
        Assert.Null(await consentRecordRepository.GetByPatientIdAsync(patient.Id));
    }

    private static Patient NewPatient(string fullName)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            DateOfBirth = new DateOnly(2000, 1, 1),
            CreatedAt = DateTime.UtcNow
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
