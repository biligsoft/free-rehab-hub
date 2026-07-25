using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqliteBackupRepository : IBackupRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteBackupRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<string> CreateBackupAsync(
        string destinationDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);
        var destinationPath = Path.Combine(
            destinationDirectory, $"freerehabhub-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");

        // Kaynak ve hedef bağlantı da aynı parolayla açılıyor (CreateSiblingFactory), bu yüzden
        // sqlite3 backup API'sinin kopyaladığı sayfalar hedefte de SQLCipher ile şifreli kalıyor —
        // ayrı bir "şifresini çöz, plaintext yaz" adımı hiç yok.
        using var source = _connectionFactory.CreateOpenConnection();
        using var destination = _connectionFactory.CreateSiblingFactory(destinationPath).CreateOpenConnection();
        source.BackupDatabase(destination);

        return Task.FromResult(destinationPath);
    }
}
