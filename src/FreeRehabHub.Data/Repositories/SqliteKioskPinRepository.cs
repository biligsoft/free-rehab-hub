using System.Globalization;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqliteKioskPinRepository : IKioskPinRepository
{
    private const string PinColumns = "PinHash, Salt, UpdatedAt";

    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteKioskPinRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<KioskPin?> GetAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {PinColumns} FROM KioskPin LIMIT 1";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new KioskPin
        {
            PinHash = reader.GetString(0),
            Salt = reader.GetString(1),
            UpdatedAt = DateTime.ParseExact(
                reader.GetString(2), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    public async Task SetAsync(KioskPin pin, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM KioskPin";
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                INSERT INTO KioskPin (PinHash, Salt, UpdatedAt)
                VALUES ($pinHash, $salt, $updatedAt)
                """;
            insertCommand.Parameters.AddWithValue("$pinHash", pin.PinHash);
            insertCommand.Parameters.AddWithValue("$salt", pin.Salt);
            insertCommand.Parameters.AddWithValue(
                "$updatedAt", pin.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }
}
