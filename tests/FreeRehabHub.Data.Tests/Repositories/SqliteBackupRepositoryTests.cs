using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Data.Tests;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class SqliteBackupRepositoryTests : IDisposable
{
    private const string Password = "correct-password";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly string _backupDirectory = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-backups");
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteBackupRepositoryTests()
    {
        _connectionFactory = new SqliteConnectionFactory(_databasePath, Password);

        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE Patients (Id TEXT PRIMARY KEY, FullName TEXT)";
        command.ExecuteNonQuery();

        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO Patients (Id, FullName) VALUES ('1', 'Test Hasta')";
        insert.ExecuteNonQuery();
    }

    [Fact]
    public async Task CreateBackupAsync_ProducesFileReadableWithSamePassword()
    {
        var repository = new SqliteBackupRepository(_connectionFactory);

        var backupPath = await repository.CreateBackupAsync(_backupDirectory);

        Assert.True(File.Exists(backupPath));

        using var backupConnection = new SqliteConnectionFactory(backupPath, Password).CreateOpenConnection();
        using var command = backupConnection.CreateCommand();
        command.CommandText = "SELECT FullName FROM Patients WHERE Id = '1'";
        var fullName = (string?)command.ExecuteScalar();

        Assert.Equal("Test Hasta", fullName);
    }

    [Fact]
    public async Task CreateBackupAsync_ProducedFile_RejectsWrongPassword()
    {
        var repository = new SqliteBackupRepository(_connectionFactory);

        var backupPath = await repository.CreateBackupAsync(_backupDirectory);

        Assert.Throws<SqliteException>(
            () => new SqliteConnectionFactory(backupPath, "wrong-password").CreateOpenConnection());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        TestFileCleanup.DeleteFile(_databasePath);
        TestFileCleanup.DeleteDirectory(_backupDirectory);
    }
}
