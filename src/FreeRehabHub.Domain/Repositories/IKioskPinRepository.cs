namespace FreeRehabHub.Domain.Repositories;

public interface IKioskPinRepository
{
    Task<KioskPin?> GetAsync(CancellationToken cancellationToken = default);
    Task SetAsync(KioskPin pin, CancellationToken cancellationToken = default);
}
