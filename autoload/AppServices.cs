using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain.Repositories;
using FreeRehabHub.Services;
using Godot;

namespace FreeRehabHub.App.Autoload;

public partial class AppServices : Node
{
    private const string DatabaseResourcePath = "user://freerehabhub.db";

    public PatientService? PatientService { get; private set; }
    public TherapistService? TherapistService { get; private set; }
    public TherapySessionService? TherapySessionService { get; private set; }
    public BackupService? BackupService { get; private set; }

    public bool IsUnlocked => PatientService is not null;

    public void Unlock(string password)
    {
        var databasePath = ProjectSettings.GlobalizePath(DatabaseResourcePath);
        var connectionFactory = new SqliteConnectionFactory(databasePath, password);

        // Yanlış parola burada (Initialize -> CreateOpenConnection -> Open) SqliteException fırlatır.
        new DatabaseInitializer(connectionFactory).Initialize();

        IPatientRepository patientRepository = new SqlitePatientRepository(connectionFactory);
        ITherapistRepository therapistRepository = new SqliteTherapistRepository(connectionFactory);
        ITherapySessionRepository sessionRepository = new SqliteTherapySessionRepository(connectionFactory);
        IAuditLogRepository auditLogRepository = new SqliteAuditLogRepository(connectionFactory);
        IBackupRepository backupRepository = new SqliteBackupRepository(connectionFactory);

        PatientService = new PatientService(patientRepository, auditLogRepository);
        TherapistService = new TherapistService(therapistRepository, auditLogRepository);
        TherapySessionService = new TherapySessionService(sessionRepository, auditLogRepository);
        BackupService = new BackupService(backupRepository, auditLogRepository);
    }
}
