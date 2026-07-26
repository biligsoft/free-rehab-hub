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
    private const string ExerciseLibraryRelativePath = "content-packs/exercise-library";
    private const string MediaPipeServiceRelativePath = "services/mediapipe-service";
    private const string ReportFontRelativePath = "assets/fonts/liberation-sans";

    public PatientService? PatientService { get; private set; }
    public TherapistService? TherapistService { get; private set; }
    public TherapySessionService? TherapySessionService { get; private set; }
    public BackupService? BackupService { get; private set; }
    public PrescriptionService? PrescriptionService { get; private set; }
    public ProgressRecordService? ProgressRecordService { get; private set; }
    public ProgressReportService? ProgressReportService { get; private set; }
    public AccessControlService? AccessControlService { get; private set; }
    public ConsentService? ConsentService { get; private set; }
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
        IKioskPinRepository kioskPinRepository = new SqliteKioskPinRepository(connectionFactory);
        IConsentRecordRepository consentRecordRepository = new SqliteConsentRecordRepository(connectionFactory);

        PatientService = new PatientService(patientRepository, auditLogRepository);
        TherapistService = new TherapistService(therapistRepository, auditLogRepository);
        TherapySessionService = new TherapySessionService(sessionRepository, auditLogRepository);
        BackupService = new BackupService(backupRepository, auditLogRepository);
        PrescriptionService = new PrescriptionService(prescriptionRepository, auditLogRepository);
        ProgressRecordService = new ProgressRecordService(progressRecordRepository, auditLogRepository);
        ProgressReportService = new ProgressReportService(
            AppContentRoot.Resolve(ReportFontRelativePath), auditLogRepository);
        AccessControlService = new AccessControlService(kioskPinRepository, auditLogRepository);
        ConsentService = new ConsentService(consentRecordRepository, auditLogRepository);

        // Statik içerik, DB parolasına bağımlı değil ama tek "kurulum kapısı" (IsUnlocked) tutarlılığı
        // için diğerleriyle aynı anda kuruluyor.
        ExerciseLibraryRepository = new ContentPackExerciseLibraryRepository(
            AppContentRoot.Resolve(ExerciseLibraryRelativePath));

        // Burada da sadece kuruluyor, kamera/süreç bu noktada başlamıyor — IPoseTrackingService.StartAsync
        // ancak kamera gerektiren bir modül aktive olduğunda (ModuleHost tarafından) çağrılır.
        var mediaPipeServiceDirectory = AppContentRoot.Resolve(MediaPipeServiceRelativePath);
        var (executablePath, argumentsTemplate) = ResolveMediaPipeCommand(mediaPipeServiceDirectory);
        PoseTrackingService = new MediaPipePoseTrackingService(executablePath, argumentsTemplate, mediaPipeServiceDirectory);
    }

    // Paketlenmiş build'de mediapipe-service, build_exe.py'nin (PyInstaller) ürettiği tek
    // çalıştırılabilir olarak çalışır (bkz. services/mediapipe-service/run_server.py) — bu ikili
    // varsa öncelik ona verilir. Yoksa (dev/editör modu) services/mediapipe-service/.venv,
    // Faz 5'te Docker dışında kurulmadı (bkz. CLAUDE.md §13/§14) — burada sadece venv'in
    // konvansiyonel konumuna işaret ediyoruz, var olup olmadığını doğrulamıyoruz; yoksa/bozuksa
    // hata IPoseTrackingService.StartAsync çağrıldığında (fiilen kullanılınca) ortaya çıkar.
    private static (string ExecutablePath, string ArgumentsTemplate) ResolveMediaPipeCommand(
        string mediaPipeServiceDirectory)
    {
        var bundledExecutableName = OperatingSystem.IsWindows() ? "mediapipe-service.exe" : "mediapipe-service";
        var bundledExecutablePath = Path.Combine(mediaPipeServiceDirectory, "dist", "mediapipe-service", bundledExecutableName);
        if (File.Exists(bundledExecutablePath))
        {
            return (bundledExecutablePath, "--host 127.0.0.1 --port {0}");
        }

        var pythonExecutablePath = OperatingSystem.IsWindows()
            ? Path.Combine(mediaPipeServiceDirectory, ".venv", "Scripts", "python.exe")
            : Path.Combine(mediaPipeServiceDirectory, ".venv", "bin", "python");
        return (pythonExecutablePath, "-m uvicorn app.main:app --host 127.0.0.1 --port {0}");
    }
}
