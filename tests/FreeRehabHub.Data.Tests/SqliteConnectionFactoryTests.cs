using FreeRehabHub.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FreeRehabHub.Data.Tests;

public sealed class SqliteConnectionFactoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

    [Fact]
    public void CreateOpenConnection_WithCorrectPassword_AllowsReadAndWrite()
    {
        const string password = "correct-password";

        using (var connection = new SqliteConnectionFactory(_databasePath, password).CreateOpenConnection())
        {
            Execute(connection, "CREATE TABLE Patients (Id TEXT PRIMARY KEY, FullName TEXT)");
            Execute(connection, "INSERT INTO Patients (Id, FullName) VALUES ('1', 'Test Hasta')");
        }

        using (var connection = new SqliteConnectionFactory(_databasePath, password).CreateOpenConnection())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT FullName FROM Patients WHERE Id = '1'";
            var fullName = (string?)command.ExecuteScalar();

            Assert.Equal("Test Hasta", fullName);
        }
    }

    [Fact]
    public void CreateOpenConnection_WithWrongPassword_Throws()
    {
        using (var connection = new SqliteConnectionFactory(_databasePath, "correct-password").CreateOpenConnection())
        {
            Execute(connection, "CREATE TABLE Patients (Id TEXT PRIMARY KEY, FullName TEXT)");
        }

        Assert.Throws<SqliteException>(
            () => new SqliteConnectionFactory(_databasePath, "wrong-password").CreateOpenConnection());
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
