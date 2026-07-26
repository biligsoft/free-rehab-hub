using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqliteConsentRecordRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqlitePatientRepository _patientRepository;
    private readonly SqliteConsentRecordRepository _repository;

    public SqliteConsentRecordRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _patientRepository = new SqlitePatientRepository(connectionFactory);
        _repository = new SqliteConsentRecordRepository(connectionFactory);
    }

    [Fact]
    public async Task GetByPatientIdAsync_NoRecord_ReturnsNull()
    {
        var record = await _repository.GetByPatientIdAsync(Guid.NewGuid());

        Assert.Null(record);
    }

    [Fact]
    public async Task AddAsync_ThenGetByPatientIdAsync_ReturnsSameRecord()
    {
        var patient = await AddPatientAsync();
        var consent = new ConsentRecord
        {
            PatientId = patient.Id,
            ConsentGivenByName = "Ayşe Yılmaz",
            IsGuardianConsent = false,
            ConsentedAt = new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc)
        };

        await _repository.AddAsync(consent);
        var fetched = await _repository.GetByPatientIdAsync(patient.Id);

        Assert.NotNull(fetched);
        Assert.Equal(consent.PatientId, fetched!.PatientId);
        Assert.Equal(consent.ConsentGivenByName, fetched.ConsentGivenByName);
        Assert.Equal(consent.IsGuardianConsent, fetched.IsGuardianConsent);
        Assert.Equal(consent.ConsentedAt, fetched.ConsentedAt);
        Assert.Null(fetched.WithdrawnAt);
    }

    [Fact]
    public async Task AddAsync_GuardianConsentWithWithdrawnAt_PersistsBothFields()
    {
        var patient = await AddPatientAsync();
        var consent = new ConsentRecord
        {
            PatientId = patient.Id,
            ConsentGivenByName = "Veli Kaya",
            IsGuardianConsent = true,
            ConsentedAt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc),
            WithdrawnAt = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)
        };

        await _repository.AddAsync(consent);
        var fetched = await _repository.GetByPatientIdAsync(patient.Id);

        Assert.NotNull(fetched);
        Assert.True(fetched!.IsGuardianConsent);
        Assert.Equal(consent.WithdrawnAt, fetched.WithdrawnAt);
    }

    private async Task<Patient> AddPatientAsync()
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Test Hasta",
            DateOfBirth = new DateOnly(2015, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        await _patientRepository.AddAsync(patient);
        return patient;
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
