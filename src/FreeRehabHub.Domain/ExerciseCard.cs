using FreeRehabHub.Core;

namespace FreeRehabHub.Domain;

public sealed class ExerciseCard
{
    public string Id { get; set; } = string.Empty;
    public LocalizedText DisplayName { get; set; } = new();
    public LocalizedText Instructions { get; set; } = new();
    public List<Discipline> Disciplines { get; set; } = new();
    public int DifficultyLevel { get; set; }
    public string? VideoPath { get; set; }
    public string? ImagePath { get; set; }
    public int? SuggestedRepetitions { get; set; }
    public int? SuggestedSets { get; set; }
    public List<string> Tags { get; set; } = new();
}
