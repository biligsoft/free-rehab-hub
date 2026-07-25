using System.Text.Json;
using System.Text.Json.Serialization;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;

namespace FreeRehabHub.Data.Repositories;

public sealed class ContentPackExerciseLibraryRepository : IExerciseLibraryRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _contentPackDirectoryPath;

    public ContentPackExerciseLibraryRepository(string contentPackDirectoryPath)
    {
        _contentPackDirectoryPath = contentPackDirectoryPath;
    }

    public async Task<IReadOnlyList<ExerciseCard>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_contentPackDirectoryPath))
        {
            return Array.Empty<ExerciseCard>();
        }

        var cards = new List<ExerciseCard>();
        foreach (var filePath in Directory.EnumerateFiles(_contentPackDirectoryPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var card = JsonSerializer.Deserialize<ExerciseCard>(json, SerializerOptions);
            if (card is not null)
            {
                cards.Add(card);
            }
        }

        return cards;
    }

    public async Task<IReadOnlyList<ExerciseCard>> GetByDisciplineAsync(
        Discipline discipline, CancellationToken cancellationToken = default)
    {
        var allCards = await GetAllAsync(cancellationToken);
        return allCards.Where(card => card.Disciplines.Contains(discipline)).ToList();
    }

    public async Task<ExerciseCard?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var allCards = await GetAllAsync(cancellationToken);
        return allCards.FirstOrDefault(card => card.Id == id);
    }
}
