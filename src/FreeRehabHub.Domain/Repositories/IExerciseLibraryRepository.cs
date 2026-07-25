using FreeRehabHub.Core;

namespace FreeRehabHub.Domain.Repositories;

public interface IExerciseLibraryRepository
{
    Task<IReadOnlyList<ExerciseCard>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseCard>> GetByDisciplineAsync(Discipline discipline, CancellationToken cancellationToken = default);
    Task<ExerciseCard?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
