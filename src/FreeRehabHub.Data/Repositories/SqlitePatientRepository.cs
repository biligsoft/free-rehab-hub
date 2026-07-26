using System.Globalization;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqlitePatientRepository : IPatientRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqlitePatientRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, DateOfBirth, CreatedAt FROM Patients WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPatient(reader) : null;
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, DateOfBirth, CreatedAt FROM Patients ORDER BY FullName";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var patients = new List<Patient>();
        while (await reader.ReadAsync(cancellationToken))
        {
            patients.Add(ReadPatient(reader));
        }

        return patients;
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Patients (Id, FullName, DateOfBirth, CreatedAt)
            VALUES ($id, $fullName, $dateOfBirth, $createdAt)
            """;
        AddPatientParameters(command, patient);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Patients
            SET FullName = $fullName, DateOfBirth = $dateOfBirth, CreatedAt = $createdAt
            WHERE Id = $id
            """;
        AddPatientParameters(command, patient);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // TherapySessions/Prescriptions/ProgressRecords, PatientId üzerinden Patients'a FK ile
    // bağlı (PRAGMA foreign_keys = ON) — hastayı silmeden önce bu ilişkili klinik kayıtların
    // (ve onların kendi çocuk tablolarının: PrescriptionItems, ProgressRecordMetrics) hepsi
    // tek transaction içinde silinmeli, yoksa FOREIGN KEY constraint failed hatası alınır.
    // AuditLogs kasıtlı olarak dokunulmuyor — RecordId polimorfik, gerçek bir FK'ye bağlı değil,
    // erişim izi (kim ne zaman sildi) veriyle birlikte silinmemeli (bkz. clinical-data-handling
    // skill § 2).
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        var patientIdText = id.ToString();
        await ExecuteAsync(connection, transaction, """
            DELETE FROM ProgressRecordMetrics
            WHERE ProgressRecordId IN (SELECT Id FROM ProgressRecords WHERE PatientId = $patientId)
            """, patientIdText, cancellationToken);
        await ExecuteAsync(connection, transaction,
            "DELETE FROM ProgressRecords WHERE PatientId = $patientId", patientIdText, cancellationToken);
        await ExecuteAsync(connection, transaction, """
            DELETE FROM PrescriptionItems
            WHERE PrescriptionId IN (SELECT Id FROM Prescriptions WHERE PatientId = $patientId)
            """, patientIdText, cancellationToken);
        await ExecuteAsync(connection, transaction,
            "DELETE FROM Prescriptions WHERE PatientId = $patientId", patientIdText, cancellationToken);
        await ExecuteAsync(connection, transaction,
            "DELETE FROM TherapySessions WHERE PatientId = $patientId", patientIdText, cancellationToken);
        await ExecuteAsync(connection, transaction,
            "DELETE FROM Patients WHERE Id = $patientId", patientIdText, cancellationToken);

        transaction.Commit();
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection, SqliteTransaction transaction, string commandText, string patientIdText,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.Parameters.AddWithValue("$patientId", patientIdText);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddPatientParameters(SqliteCommand command, Patient patient)
    {
        command.Parameters.AddWithValue("$id", patient.Id.ToString());
        command.Parameters.AddWithValue("$fullName", patient.FullName);
        command.Parameters.AddWithValue("$dateOfBirth", patient.DateOfBirth.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$createdAt", patient.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
    }

    private static Patient ReadPatient(SqliteDataReader reader)
    {
        return new Patient
        {
            Id = Guid.Parse(reader.GetString(0)),
            FullName = reader.GetString(1),
            DateOfBirth = DateOnly.ParseExact(reader.GetString(2), "O", CultureInfo.InvariantCulture),
            CreatedAt = DateTime.ParseExact(
                reader.GetString(3), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
