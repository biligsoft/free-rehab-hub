using System.Globalization;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqliteAuditLogRepository : IAuditLogRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteAuditLogRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AuditLogs (Id, TherapistId, OccurredAt, RecordType, RecordId, Action)
            VALUES ($id, $therapistId, $occurredAt, $recordType, $recordId, $action)
            """;
        command.Parameters.AddWithValue("$id", entry.Id.ToString());
        command.Parameters.AddWithValue("$therapistId", entry.TherapistId.ToString());
        command.Parameters.AddWithValue("$occurredAt", entry.OccurredAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$recordType", entry.RecordType.ToString());
        command.Parameters.AddWithValue("$recordId", entry.RecordId.ToString());
        command.Parameters.AddWithValue("$action", entry.Action.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByRecordAsync(
        AuditRecordType recordType, Guid recordId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, TherapistId, OccurredAt, RecordType, RecordId, Action
            FROM AuditLogs
            WHERE RecordType = $recordType AND RecordId = $recordId
            ORDER BY OccurredAt
            """;
        command.Parameters.AddWithValue("$recordType", recordType.ToString());
        command.Parameters.AddWithValue("$recordId", recordId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entries = new List<AuditLogEntry>();
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(ReadEntry(reader));
        }

        return entries;
    }

    private static AuditLogEntry ReadEntry(SqliteDataReader reader)
    {
        return new AuditLogEntry
        {
            Id = Guid.Parse(reader.GetString(0)),
            TherapistId = Guid.Parse(reader.GetString(1)),
            OccurredAt = DateTime.ParseExact(
                reader.GetString(2), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            RecordType = Enum.Parse<AuditRecordType>(reader.GetString(3)),
            RecordId = Guid.Parse(reader.GetString(4)),
            Action = Enum.Parse<AuditAction>(reader.GetString(5))
        };
    }
}
