namespace FreeRehabHub.Domain.Repositories;

public interface IBackupRepository
{
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
}
