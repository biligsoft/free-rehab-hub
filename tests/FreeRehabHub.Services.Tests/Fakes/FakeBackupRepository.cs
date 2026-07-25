using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Services.Tests.Fakes;

public sealed class FakeBackupRepository : IBackupRepository
{
    public string? LastDestinationDirectory { get; private set; }
    public string ResultPath { get; set; } = "fake-backup.db";

    public Task<string> CreateBackupAsync(
        string destinationDirectory, CancellationToken cancellationToken = default)
    {
        LastDestinationDirectory = destinationDirectory;
        return Task.FromResult(ResultPath);
    }
}
