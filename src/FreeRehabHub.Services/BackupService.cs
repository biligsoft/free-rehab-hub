using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class BackupService
{
    private readonly IBackupRepository _backupRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public BackupService(IBackupRepository backupRepository, IAuditLogRepository auditLogRepository)
    {
        _backupRepository = backupRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<string> CreateBackupAsync(
        string destinationDirectory, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        var backupPath = await _backupRepository.CreateBackupAsync(destinationDirectory, cancellationToken);

        await _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.Database,
                // Yedekleme tek bir hasta/terapist kaydına değil tüm veritabanına uygulanıyor;
                // şema RecordId'yi NOT NULL istiyor, bu yüzden "tüm veritabanı" için Guid.Empty kullanılıyor.
                RecordId = Guid.Empty,
                Action = AuditAction.Created
            },
            cancellationToken);

        return backupPath;
    }
}
