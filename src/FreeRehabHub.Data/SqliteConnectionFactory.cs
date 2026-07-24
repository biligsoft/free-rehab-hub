using Microsoft.Data.Sqlite;

namespace FreeRehabHub.Data;

public sealed class SqliteConnectionFactory
{
    private readonly string _databasePath;
    private readonly string _password;

    static SqliteConnectionFactory()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public SqliteConnectionFactory(string databasePath, string password)
    {
        _databasePath = databasePath;
        _password = password;
    }

    public SqliteConnection CreateOpenConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Password = _password
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();

        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        return connection;
    }
}
