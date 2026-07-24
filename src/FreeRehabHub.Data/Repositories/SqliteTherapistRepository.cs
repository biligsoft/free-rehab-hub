using System.Globalization;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;
using Microsoft.Data.Sqlite;

namespace FreeRehabHub.Data.Repositories;

public sealed class SqliteTherapistRepository : ITherapistRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteTherapistRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Therapist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, Discipline, CreatedAt FROM Therapists WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTherapist(reader) : null;
    }

    public async Task<IReadOnlyList<Therapist>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, Discipline, CreatedAt FROM Therapists ORDER BY FullName";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var therapists = new List<Therapist>();
        while (await reader.ReadAsync(cancellationToken))
        {
            therapists.Add(ReadTherapist(reader));
        }

        return therapists;
    }

    public async Task AddAsync(Therapist therapist, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Therapists (Id, FullName, Discipline, CreatedAt)
            VALUES ($id, $fullName, $discipline, $createdAt)
            """;
        AddTherapistParameters(command, therapist);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Therapist therapist, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Therapists
            SET FullName = $fullName, Discipline = $discipline, CreatedAt = $createdAt
            WHERE Id = $id
            """;
        AddTherapistParameters(command, therapist);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Therapists WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddTherapistParameters(SqliteCommand command, Therapist therapist)
    {
        command.Parameters.AddWithValue("$id", therapist.Id.ToString());
        command.Parameters.AddWithValue("$fullName", therapist.FullName);
        command.Parameters.AddWithValue("$discipline", therapist.Discipline.ToString());
        command.Parameters.AddWithValue("$createdAt", therapist.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
    }

    private static Therapist ReadTherapist(SqliteDataReader reader)
    {
        return new Therapist
        {
            Id = Guid.Parse(reader.GetString(0)),
            FullName = reader.GetString(1),
            Discipline = Enum.Parse<Discipline>(reader.GetString(2)),
            CreatedAt = DateTime.ParseExact(
                reader.GetString(3), "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
