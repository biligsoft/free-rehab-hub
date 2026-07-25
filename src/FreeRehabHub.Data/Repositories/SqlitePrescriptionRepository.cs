using System.Globalization;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqlitePrescriptionRepository : IPrescriptionRepository
{
    private const string PrescriptionColumns = "Id, PatientId, CreatedByTherapistId, CreatedAt, Notes";
    private const string ItemColumns = "Id, PrescriptionId, ExerciseCardId, Repetitions, Sets, FrequencyPerWeek, SortOrder";

    private readonly SqliteConnectionFactory _connectionFactory;

    public SqlitePrescriptionRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(ExercisePrescription prescription, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Prescriptions (Id, PatientId, CreatedByTherapistId, CreatedAt, Notes)
                VALUES ($id, $patientId, $createdByTherapistId, $createdAt, $notes)
                """;
            command.Parameters.AddWithValue("$id", prescription.Id.ToString());
            command.Parameters.AddWithValue("$patientId", prescription.PatientId.ToString());
            command.Parameters.AddWithValue("$createdByTherapistId", prescription.CreatedByTherapistId.ToString());
            command.Parameters.AddWithValue(
                "$createdAt", prescription.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$notes", (object?)prescription.Notes ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var index = 0; index < prescription.Items.Count; index++)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PrescriptionItems
                    (Id, PrescriptionId, ExerciseCardId, Repetitions, Sets, FrequencyPerWeek, SortOrder)
                VALUES ($id, $prescriptionId, $exerciseCardId, $repetitions, $sets, $frequencyPerWeek, $sortOrder)
                """;
            var item = prescription.Items[index];
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$prescriptionId", prescription.Id.ToString());
            command.Parameters.AddWithValue("$exerciseCardId", item.ExerciseCardId);
            command.Parameters.AddWithValue("$repetitions", (object?)item.Repetitions ?? DBNull.Value);
            command.Parameters.AddWithValue("$sets", (object?)item.Sets ?? DBNull.Value);
            command.Parameters.AddWithValue("$frequencyPerWeek", (object?)item.FrequencyPerWeek ?? DBNull.Value);
            command.Parameters.AddWithValue("$sortOrder", index);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    public async Task<ExercisePrescription?> GetLatestByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();

        ExercisePrescription? prescription;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT {PrescriptionColumns} FROM Prescriptions
                WHERE PatientId = $patientId
                ORDER BY CreatedAt DESC
                LIMIT 1
                """;
            command.Parameters.AddWithValue("$patientId", patientId.ToString());

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            prescription = await reader.ReadAsync(cancellationToken) ? ReadPrescription(reader) : null;
        }

        if (prescription is not null)
        {
            prescription.Items = await GetItemsAsync(connection, prescription.Id, cancellationToken);
        }

        return prescription;
    }

    public async Task<IReadOnlyList<ExercisePrescription>> GetHistoryByPatientIdAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();

        var prescriptions = new List<ExercisePrescription>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT {PrescriptionColumns} FROM Prescriptions
                WHERE PatientId = $patientId
                ORDER BY CreatedAt DESC
                """;
            command.Parameters.AddWithValue("$patientId", patientId.ToString());

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                prescriptions.Add(ReadPrescription(reader));
            }
        }

        foreach (var prescription in prescriptions)
        {
            prescription.Items = await GetItemsAsync(connection, prescription.Id, cancellationToken);
        }

        return prescriptions;
    }

    private static async Task<List<PrescriptionItem>> GetItemsAsync(
        SqliteConnection connection, Guid prescriptionId, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {ItemColumns} FROM PrescriptionItems
            WHERE PrescriptionId = $prescriptionId
            ORDER BY SortOrder
            """;
        command.Parameters.AddWithValue("$prescriptionId", prescriptionId.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<PrescriptionItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PrescriptionItem
            {
                ExerciseCardId = reader.GetString(2),
                Repetitions = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Sets = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                FrequencyPerWeek = reader.IsDBNull(5) ? null : reader.GetInt32(5)
            });
        }

        return items;
    }

    private static ExercisePrescription ReadPrescription(SqliteDataReader reader)
    {
        return new ExercisePrescription
        {
            Id = Guid.Parse(reader.GetString(0)),
            PatientId = Guid.Parse(reader.GetString(1)),
            CreatedByTherapistId = Guid.Parse(reader.GetString(2)),
            CreatedAt = DateTime.ParseExact(
                reader.GetString(3), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Notes = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
    }
}
