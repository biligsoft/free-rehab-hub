using FreeRehabHub.Core;
using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqliteTherapistRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteTherapistRepository _repository;

    public SqliteTherapistRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _repository = new SqliteTherapistRepository(connectionFactory);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSameTherapist()
    {
        var therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Dr. Elif Aydın",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc)
        };

        await _repository.AddAsync(therapist);
        var fetched = await _repository.GetByIdAsync(therapist.Id);

        Assert.NotNull(fetched);
        Assert.Equal(therapist.Id, fetched!.Id);
        Assert.Equal(therapist.FullName, fetched.FullName);
        Assert.Equal(therapist.Discipline, fetched.Discipline);
        Assert.Equal(therapist.CreatedAt, fetched.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var fetched = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTherapists_OrderedByFullName()
    {
        await _repository.AddAsync(NewTherapist("Zeynep Kaya", Discipline.Psychology));
        await _repository.AddAsync(NewTherapist("Ahmet Demir", Discipline.SpeechTherapy));

        var all = await _repository.GetAllAsync();

        Assert.Equal(["Ahmet Demir", "Zeynep Kaya"], all.Select(t => t.FullName));
    }

    [Fact]
    public async Task UpdateAsync_ChangesStoredFields()
    {
        var therapist = NewTherapist("Mehmet Can", Discipline.OccupationalTherapy);
        await _repository.AddAsync(therapist);

        therapist.Discipline = Discipline.SpecialEducation;
        await _repository.UpdateAsync(therapist);
        var fetched = await _repository.GetByIdAsync(therapist.Id);

        Assert.Equal(Discipline.SpecialEducation, fetched!.Discipline);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTherapist()
    {
        var therapist = NewTherapist("Elif Şahin", Discipline.Physiotherapy);
        await _repository.AddAsync(therapist);

        await _repository.DeleteAsync(therapist.Id);
        var fetched = await _repository.GetByIdAsync(therapist.Id);

        Assert.Null(fetched);
    }

    private static Therapist NewTherapist(string fullName, Discipline discipline)
    {
        return new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Discipline = discipline,
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
