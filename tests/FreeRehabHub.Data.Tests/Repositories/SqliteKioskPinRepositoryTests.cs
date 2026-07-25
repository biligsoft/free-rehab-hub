using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqliteKioskPinRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteKioskPinRepository _repository;

    public SqliteKioskPinRepositoryTests()
    {
        var connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
        new DatabaseInitializer(connectionFactory).Initialize();
        _repository = new SqliteKioskPinRepository(connectionFactory);
    }

    [Fact]
    public async Task GetAsync_NoPinConfigured_ReturnsNull()
    {
        var pin = await _repository.GetAsync();

        Assert.Null(pin);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsSamePin()
    {
        var pin = new KioskPin
        {
            PinHash = "hash-value",
            Salt = "salt-value",
            UpdatedAt = new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc)
        };

        await _repository.SetAsync(pin);
        var fetched = await _repository.GetAsync();

        Assert.NotNull(fetched);
        Assert.Equal(pin.PinHash, fetched!.PinHash);
        Assert.Equal(pin.Salt, fetched.Salt);
        Assert.Equal(pin.UpdatedAt, fetched.UpdatedAt);
    }

    [Fact]
    public async Task SetAsync_CalledTwice_ReplacesPreviousPin()
    {
        await _repository.SetAsync(new KioskPin
        {
            PinHash = "old-hash",
            Salt = "old-salt",
            UpdatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc)
        });
        await _repository.SetAsync(new KioskPin
        {
            PinHash = "new-hash",
            Salt = "new-salt",
            UpdatedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc)
        });

        var fetched = await _repository.GetAsync();

        Assert.NotNull(fetched);
        Assert.Equal("new-hash", fetched!.PinHash);
        Assert.Equal("new-salt", fetched.Salt);
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
