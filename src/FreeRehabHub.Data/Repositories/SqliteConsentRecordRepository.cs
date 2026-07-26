using System.Globalization;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqliteConsentRecordRepository : IConsentRecordRepository
{
    private const string ConsentColumns =
        "PatientId, ConsentGivenByName, IsGuardianConsent, ConsentedAt, WithdrawnAt";

    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteConsentRecordRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ConsentRecord?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {ConsentColumns} FROM ConsentRecords WHERE PatientId = $patientId";
        command.Parameters.AddWithValue("$patientId", patientId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ConsentRecord
        {
            PatientId = Guid.Parse(reader.GetString(0)),
            ConsentGivenByName = reader.GetString(1),
            IsGuardianConsent = reader.GetInt64(2) != 0,
            ConsentedAt = DateTime.ParseExact(
                reader.GetString(3), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            WithdrawnAt = reader.IsDBNull(4)
                ? null
                : DateTime.ParseExact(
                    reader.GetString(4), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    public async Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ConsentRecords (PatientId, ConsentGivenByName, IsGuardianConsent, ConsentedAt, WithdrawnAt)
            VALUES ($patientId, $consentGivenByName, $isGuardianConsent, $consentedAt, $withdrawnAt)
            """;
        command.Parameters.AddWithValue("$patientId", record.PatientId.ToString());
        command.Parameters.AddWithValue("$consentGivenByName", record.ConsentGivenByName);
        command.Parameters.AddWithValue("$isGuardianConsent", record.IsGuardianConsent ? 1 : 0);
        command.Parameters.AddWithValue(
            "$consentedAt", record.ConsentedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue(
            "$withdrawnAt", (object?)record.WithdrawnAt?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
