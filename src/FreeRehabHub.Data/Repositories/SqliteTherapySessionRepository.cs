using System.Globalization;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqliteTherapySessionRepository : ITherapySessionRepository
{
    private const string SelectColumns = "Id, PatientId, TherapistId, StartedAt, EndedAt, Notes";

    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteTherapySessionRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TherapySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {SelectColumns} FROM TherapySessions WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadSession(reader) : null;
    }

    public async Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {SelectColumns} FROM TherapySessions
            WHERE PatientId = $patientId
            ORDER BY StartedAt
            """;
        command.Parameters.AddWithValue("$patientId", patientId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sessions = new List<TherapySession>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sessions.Add(ReadSession(reader));
        }

        return sessions;
    }

    public async Task AddAsync(TherapySession session, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO TherapySessions (Id, PatientId, TherapistId, StartedAt, EndedAt, Notes)
            VALUES ($id, $patientId, $therapistId, $startedAt, $endedAt, $notes)
            """;
        AddSessionParameters(command, session);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(TherapySession session, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE TherapySessions
            SET PatientId = $patientId, TherapistId = $therapistId,
                StartedAt = $startedAt, EndedAt = $endedAt, Notes = $notes
            WHERE Id = $id
            """;
        AddSessionParameters(command, session);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM TherapySessions WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddSessionParameters(SqliteCommand command, TherapySession session)
    {
        command.Parameters.AddWithValue("$id", session.Id.ToString());
        command.Parameters.AddWithValue("$patientId", session.PatientId.ToString());
        command.Parameters.AddWithValue("$therapistId", session.TherapistId.ToString());
        command.Parameters.AddWithValue("$startedAt", session.StartedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue(
            "$endedAt", (object?)session.EndedAt?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)session.Notes ?? DBNull.Value);
    }

    private static TherapySession ReadSession(SqliteDataReader reader)
    {
        return new TherapySession
        {
            Id = Guid.Parse(reader.GetString(0)),
            PatientId = Guid.Parse(reader.GetString(1)),
            TherapistId = Guid.Parse(reader.GetString(2)),
            StartedAt = DateTime.ParseExact(
                reader.GetString(3), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            EndedAt = reader.IsDBNull(4)
                ? null
                : DateTime.ParseExact(
                    reader.GetString(4), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Notes = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }
}
