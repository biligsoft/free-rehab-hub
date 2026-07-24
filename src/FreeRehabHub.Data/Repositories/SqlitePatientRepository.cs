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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Patients WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

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
