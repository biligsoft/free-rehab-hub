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

    public SqliteConnectionFactory CreateSiblingFactory(string databasePath)
    {
        return new SqliteConnectionFactory(databasePath, _password);
    }

    public SqliteConnection CreateOpenConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Password = _password
        }.ToString();

        var connection = new SqliteConnection(connectionString);

        // Open() (ör. yanlış parola) veya PRAGMA başarısız olursa connection burada dispose
        // edilmezse native handle sızıyor — Linux/macOS'ta gözlemlenmiyor ama Windows dosya
        // silmeyi tamamen engelliyor (CI'da F8.09'da yakalandı, bkz. docs/PROGRESS.md).
        try
        {
            connection.Open();

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }

            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
