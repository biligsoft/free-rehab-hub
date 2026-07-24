using FreeRehabHub.Data;
using Xunit;

namespace FreeRehabHub.Data.Tests;

public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseInitializerTests()
    {
        _connectionFactory = new SqliteConnectionFactory(_databasePath, "test-password");
    }

    [Fact]
    public void Initialize_CreatesExpectedTables()
    {
        new DatabaseInitializer(_connectionFactory).Initialize();

        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        using var reader = command.ExecuteReader();

        var tableNames = new List<string>();
        while (reader.Read())
        {
            tableNames.Add(reader.GetString(0));
        }

        Assert.Contains("Patients", tableNames);
        Assert.Contains("Therapists", tableNames);
        Assert.Contains("TherapySessions", tableNames);
    }

    [Fact]
    public void Initialize_CalledTwice_DoesNotThrow()
    {
        var initializer = new DatabaseInitializer(_connectionFactory);

        initializer.Initialize();
        var exception = Record.Exception(initializer.Initialize);

        Assert.Null(exception);
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
