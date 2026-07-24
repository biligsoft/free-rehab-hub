namespace FreeRehabHub.Data;

public sealed class DatabaseInitializer
{
    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS Therapists (
            Id TEXT PRIMARY KEY,
            FullName TEXT NOT NULL,
            Discipline TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Patients (
            Id TEXT PRIMARY KEY,
            FullName TEXT NOT NULL,
            DateOfBirth TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS TherapySessions (
            Id TEXT PRIMARY KEY,
            PatientId TEXT NOT NULL REFERENCES Patients(Id),
            TherapistId TEXT NOT NULL REFERENCES Therapists(Id),
            StartedAt TEXT NOT NULL,
            EndedAt TEXT NULL,
            Notes TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_therapysessions_patientid ON TherapySessions(PatientId);
        """;

    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = SchemaSql;
        command.ExecuteNonQuery();
    }
}
