using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakeKioskPinRepository : IKioskPinRepository
{
    private KioskPin? _pin;

    public Task<KioskPin?> GetAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_pin);
    }

    public Task SetAsync(KioskPin pin, CancellationToken cancellationToken = default)
    {
        _pin = pin;
        return Task.CompletedTask;
    }
}
