using System.Security.Cryptography;
using System.Text;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services;

public sealed class AccessControlService
{
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    private readonly IKioskPinRepository _kioskPinRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public AccessControlService(IKioskPinRepository kioskPinRepository, IAuditLogRepository auditLogRepository)
    {
        _kioskPinRepository = kioskPinRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<bool> IsPinConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var pin = await _kioskPinRepository.GetAsync(cancellationToken);
        return pin is not null;
    }

    public async Task SetPinAsync(string pin, Guid actingTherapistId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            throw new ArgumentException("PIN boş olamaz.", nameof(pin));
        }

        var existingPin = await _kioskPinRepository.GetAsync(cancellationToken);
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = ComputeHash(pin, salt);

        await _kioskPinRepository.SetAsync(
            new KioskPin
            {
                PinHash = Convert.ToBase64String(hash),
                Salt = Convert.ToBase64String(salt),
                UpdatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = actingTherapistId,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.KioskPin,
                RecordId = Guid.Empty,
                Action = existingPin is null ? AuditAction.Created : AuditAction.Updated
            },
            cancellationToken);
    }

    public async Task<bool> VerifyPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        var storedPin = await _kioskPinRepository.GetAsync(cancellationToken);
        if (storedPin is null)
        {
            return false;
        }

        var salt = Convert.FromBase64String(storedPin.Salt);
        var expectedHash = Convert.FromBase64String(storedPin.PinHash);
        var actualHash = ComputeHash(pin, salt);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(string pin, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
    }
}
