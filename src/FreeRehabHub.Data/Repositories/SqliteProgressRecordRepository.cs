using System.Globalization;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqliteProgressRecordRepository : IProgressRecordRepository
{
    private const string RecordColumns = "Id, PatientId, ModuleId, SessionId, CompletedAt, NormalizedScore, Notes";
    private const string MetricColumns = "Id, ProgressRecordId, MetricKey, MetricValue";

    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteProgressRecordRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(ProgressRecord record, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ProgressRecords (Id, PatientId, ModuleId, SessionId, CompletedAt, NormalizedScore, Notes)
                VALUES ($id, $patientId, $moduleId, $sessionId, $completedAt, $normalizedScore, $notes)
                """;
            command.Parameters.AddWithValue("$id", record.Id.ToString());
            command.Parameters.AddWithValue("$patientId", record.PatientId.ToString());
            command.Parameters.AddWithValue("$moduleId", record.ModuleId);
            command.Parameters.AddWithValue("$sessionId", record.SessionId.ToString());
            command.Parameters.AddWithValue(
                "$completedAt", record.CompletedAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$normalizedScore", record.NormalizedScore);
            command.Parameters.AddWithValue("$notes", (object?)record.Notes ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var (key, value) in record.Metrics)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO ProgressRecordMetrics (Id, ProgressRecordId, MetricKey, MetricValue)
                VALUES ($id, $progressRecordId, $metricKey, $metricValue)
                """;
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$progressRecordId", record.Id.ToString());
            command.Parameters.AddWithValue("$metricKey", key);
            command.Parameters.AddWithValue("$metricValue", value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyList<ProgressRecord>> GetHistoryByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();

        var records = new List<ProgressRecord>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT {RecordColumns} FROM ProgressRecords
                WHERE PatientId = $patientId
                ORDER BY CompletedAt DESC
                """;
            command.Parameters.AddWithValue("$patientId", patientId.ToString());

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(ReadRecord(reader));
            }
        }

        foreach (var record in records)
        {
            record.Metrics = await GetMetricsAsync(connection, record.Id, cancellationToken);
        }

        return records;
    }

    private static async Task<Dictionary<string, double>> GetMetricsAsync(
        SqliteConnection connection, Guid progressRecordId, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {MetricColumns} FROM ProgressRecordMetrics
            WHERE ProgressRecordId = $progressRecordId
            """;
        command.Parameters.AddWithValue("$progressRecordId", progressRecordId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var metrics = new Dictionary<string, double>();
        while (await reader.ReadAsync(cancellationToken))
        {
            metrics[reader.GetString(2)] = reader.GetDouble(3);
        }

        return metrics;
    }

    private static ProgressRecord ReadRecord(SqliteDataReader reader)
    {
        return new ProgressRecord
        {
            Id = Guid.Parse(reader.GetString(0)),
            PatientId = Guid.Parse(reader.GetString(1)),
            ModuleId = reader.GetString(2),
            SessionId = Guid.Parse(reader.GetString(3)),
            CompletedAt = DateTime.ParseExact(
                reader.GetString(4), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            NormalizedScore = reader.GetDouble(5),
            Notes = reader.IsDBNull(6) ? null : reader.GetString(6)
        };
    }
}
