using System;
using System.IO;
using FreeRehabHub.Data;
using FreeRehabHub.Data.Repositories;
using FreeRehabHub.Domain.Repositories;
using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Services;
using Godot;

namespace FreeRehabHub.App.Autoload;

public partial class AppServices : Node
{
    private const string DatabaseResourcePath = "user://freerehabhub.db";
    private const string ExerciseLibraryResourcePath = "res://content-packs/exercise-library";
    private const string MediaPipeServiceResourcePath = "res://services/mediapipe-service";

    public PatientService? PatientService { get; private set; }
    public TherapistService? TherapistService { get; private set; }
    public TherapySessionService? TherapySessionService { get; private set; }
    public BackupService? BackupService { get; private set; }
    public PrescriptionService? PrescriptionService { get; private set; }
    public ProgressRecordService? ProgressRecordService { get; private set; }
    public IExerciseLibraryRepository? ExerciseLibraryRepository { get; private set; }
    public IPoseTrackingService? PoseTrackingService { get; private set; }

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
        IPrescriptionRepository prescriptionRepository = new SqlitePrescriptionRepository(connectionFactory);
        IProgressRecordRepository progressRecordRepository = new SqliteProgressRecordRepository(connectionFactory);

        PatientService = new PatientService(patientRepository, auditLogRepository);
        TherapistService = new TherapistService(therapistRepository, auditLogRepository);
        TherapySessionService = new TherapySessionService(sessionRepository, auditLogRepository);
        BackupService = new BackupService(backupRepository, auditLogRepository);
        PrescriptionService = new PrescriptionService(prescriptionRepository, auditLogRepository);
        ProgressRecordService = new ProgressRecordService(progressRecordRepository, auditLogRepository);

        // Statik içerik, DB parolasına bağımlı değil ama tek "kurulum kapısı" (IsUnlocked) tutarlılığı
        // için diğerleriyle aynı anda kuruluyor.
        ExerciseLibraryRepository = new ContentPackExerciseLibraryRepository(
            ProjectSettings.GlobalizePath(ExerciseLibraryResourcePath));

        // Burada da sadece kuruluyor, kamera/süreç bu noktada başlamıyor — IPoseTrackingService.StartAsync
        // ancak kamera gerektiren bir modül aktive olduğunda (ModuleHost tarafından) çağrılır.
        var mediaPipeServiceDirectory = ProjectSettings.GlobalizePath(MediaPipeServiceResourcePath);
        PoseTrackingService = new MediaPipePoseTrackingService(
            ResolvePythonExecutablePath(mediaPipeServiceDirectory), mediaPipeServiceDirectory);
    }

    // services/mediapipe-service/.venv, Faz 5'te Docker dışında kurulmadı (bkz. CLAUDE.md §13/§14) —
    // burada sadece venv'in konvansiyonel konumuna işaret ediyoruz, var olup olmadığını doğrulamıyoruz;
    // yoksa/bozuksa hata IPoseTrackingService.StartAsync çağrıldığında (fiilen kullanılınca) ortaya çıkar.
    private static string ResolvePythonExecutablePath(string mediaPipeServiceDirectory)
    {
        return OperatingSystem.IsWindows()
            ? Path.Combine(mediaPipeServiceDirectory, ".venv", "Scripts", "python.exe")
            : Path.Combine(mediaPipeServiceDirectory, ".venv", "bin", "python");
    }
}
