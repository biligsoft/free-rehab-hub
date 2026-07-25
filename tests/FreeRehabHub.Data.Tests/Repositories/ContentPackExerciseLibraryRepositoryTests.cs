using FreeRehabHub.Core;
using FreeRehabHub.Data.Repositories;
using Xunit;

namespace FreeRehabHub.Data.Tests.Repositories;

public sealed class ContentPackExerciseLibraryRepositoryTests : IDisposable
{
    private const string SampleCardJson = """
        {
          "id": "sample-card",
          "displayName": { "tr": "Örnek Kart", "en": "Sample Card" },
          "instructions": { "tr": "Talimat", "en": "Instruction" },
          "disciplines": ["physiotherapy"],
          "difficultyLevel": 2,
          "suggestedRepetitions": 10,
          "suggestedSets": 3,
          "tags": ["sample"]
        }
        """;

    private readonly string _directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ContentPackExerciseLibraryRepositoryTests()
    {
        Directory.CreateDirectory(_directoryPath);
        File.WriteAllText(Path.Combine(_directoryPath, "sample-card.json"), SampleCardJson);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCardsInDirectory()
    {
        var repository = new ContentPackExerciseLibraryRepository(_directoryPath);

        var cards = await repository.GetAllAsync();

        var card = Assert.Single(cards);
        Assert.Equal("sample-card", card.Id);
        Assert.Equal("Örnek Kart", card.DisplayName.Tr);
        Assert.Equal(2, card.DifficultyLevel);
        Assert.Contains(Discipline.Physiotherapy, card.Disciplines);
    }

    [Fact]
    public async Task GetByIdAsync_KnownId_ReturnsCard()
    {
        var repository = new ContentPackExerciseLibraryRepository(_directoryPath);

        var card = await repository.GetByIdAsync("sample-card");

        Assert.NotNull(card);
        Assert.Equal("sample-card", card!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var repository = new ContentPackExerciseLibraryRepository(_directoryPath);

        var card = await repository.GetByIdAsync("does-not-exist");

        Assert.Null(card);
    }

    [Fact]
    public async Task GetByDisciplineAsync_NonMatchingDiscipline_ReturnsEmpty()
    {
        var repository = new ContentPackExerciseLibraryRepository(_directoryPath);

        var cards = await repository.GetByDisciplineAsync(Discipline.SpeechTherapy);

        Assert.Empty(cards);
    }

    [Fact]
    public async Task GetAllAsync_NonExistentDirectory_ReturnsEmptyInsteadOfThrowing()
    {
        var repository = new ContentPackExerciseLibraryRepository(Path.Combine(_directoryPath, "no-such-subfolder"));

        var cards = await repository.GetAllAsync();

        Assert.Empty(cards);
    }

    [Fact]
    public async Task GetAllAsync_RealExerciseLibraryContentPack_LoadsAllCards()
    {
        var realContentPackPath = Path.Combine(AppContext.BaseDirectory, "TestData", "exercise-library");
        var repository = new ContentPackExerciseLibraryRepository(realContentPackPath);

        var cards = await repository.GetAllAsync();

        Assert.Equal(3, cards.Count);
        Assert.Contains(cards, card => card.Id == "shoulder-flexion-supine");
        Assert.Contains(cards, card => card.Id == "ankle-pumps");
        Assert.Contains(cards, card => card.Id == "fine-motor-pegboard");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
    }
}
