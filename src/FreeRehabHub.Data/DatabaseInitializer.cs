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

        CREATE TABLE IF NOT EXISTS AuditLogs (
            Id TEXT PRIMARY KEY,
            TherapistId TEXT NOT NULL REFERENCES Therapists(Id),
            OccurredAt TEXT NOT NULL,
            RecordType TEXT NOT NULL,
            RecordId TEXT NOT NULL,
            Action TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_auditlogs_record ON AuditLogs(RecordType, RecordId);

        CREATE TABLE IF NOT EXISTS Prescriptions (
            Id TEXT PRIMARY KEY,
            PatientId TEXT NOT NULL REFERENCES Patients(Id),
            CreatedByTherapistId TEXT NOT NULL REFERENCES Therapists(Id),
            CreatedAt TEXT NOT NULL,
            Notes TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_prescriptions_patientid ON Prescriptions(PatientId);

        CREATE TABLE IF NOT EXISTS PrescriptionItems (
            Id TEXT PRIMARY KEY,
            PrescriptionId TEXT NOT NULL REFERENCES Prescriptions(Id),
            ExerciseCardId TEXT NOT NULL,
            Repetitions INTEGER NULL,
            Sets INTEGER NULL,
            FrequencyPerWeek INTEGER NULL,
            SortOrder INTEGER NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_prescriptionitems_prescriptionid ON PrescriptionItems(PrescriptionId);

        -- SessionId'ye kasıtlı olarak FK yok: ModuleHost henüz her modül oynatışında gerçek bir
        -- TherapySessions kaydı oluşturmuyor (bkz. docs/PROGRESS.md açık riskler), sadece tekil bir Guid.
        CREATE TABLE IF NOT EXISTS ProgressRecords (
            Id TEXT PRIMARY KEY,
            PatientId TEXT NOT NULL REFERENCES Patients(Id),
            ModuleId TEXT NOT NULL,
            SessionId TEXT NOT NULL,
            CompletedAt TEXT NOT NULL,
            NormalizedScore REAL NOT NULL,
            Notes TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_progressrecords_patientid ON ProgressRecords(PatientId);

        CREATE TABLE IF NOT EXISTS ProgressRecordMetrics (
            Id TEXT PRIMARY KEY,
            ProgressRecordId TEXT NOT NULL REFERENCES ProgressRecords(Id),
            MetricKey TEXT NOT NULL,
            MetricValue REAL NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_progressrecordmetrics_progressrecordid ON ProgressRecordMetrics(ProgressRecordId);

        -- Tekil ayar satırı (app-geneli kiosk-çıkış PIN'i) — Id yok, SetAsync eski satırı
        -- silip yenisini ekliyor (bkz. SqliteKioskPinRepository).
        CREATE TABLE IF NOT EXISTS KioskPin (
            PinHash TEXT NOT NULL,
            Salt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        -- PatientId doğal anahtar (primary key) — hasta başına tek bir rıza kaydı (geri çekme
        -- yeni bir satır değil, WithdrawnAt'in bu kayıt üzerinde güncellenmesiyle olacak).
        CREATE TABLE IF NOT EXISTS ConsentRecords (
            PatientId TEXT PRIMARY KEY REFERENCES Patients(Id),
            ConsentGivenByName TEXT NOT NULL,
            IsGuardianConsent INTEGER NOT NULL,
            ConsentedAt TEXT NOT NULL,
            WithdrawnAt TEXT NULL
        );
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
