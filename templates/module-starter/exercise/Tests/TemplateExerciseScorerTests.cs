using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Modules.TemplateExercise.Scoring;
using Xunit;

namespace FreeRehabHub.Modules.TemplateExercise.Tests;

public sealed class TemplateExerciseScorerTests
{
    private readonly TemplateExerciseScorer _scorer = new();

    private static ModuleContext SampleContext()
    {
        return new ModuleContext
        {
            PatientId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            CompletedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public void Score_PartialRepetitions_ReturnsProportionalScore()
    {
        var result = _scorer.Score("com.yourorg.template-exercise", completedRepetitions: 3, targetRepetitions: 5, SampleContext());

        Assert.Equal(0.6, result.NormalizedScore, precision: 5);
        Assert.Equal(3, result.Metrics["completedRepetitions"]);
        Assert.Equal(5, result.Metrics["targetRepetitions"]);
    }

    [Fact]
    public void Score_AllRepetitionsCompleted_ReturnsMaxScore()
    {
        var result = _scorer.Score("com.yourorg.template-exercise", completedRepetitions: 5, targetRepetitions: 5, SampleContext());

        Assert.Equal(1.0, result.NormalizedScore, precision: 5);
    }

    [Fact]
    public void Score_MoreRepetitionsThanTarget_ClampsToMaxScore()
    {
        var result = _scorer.Score("com.yourorg.template-exercise", completedRepetitions: 8, targetRepetitions: 5, SampleContext());

        Assert.Equal(1.0, result.NormalizedScore, precision: 5);
    }

    [Fact]
    public void Score_ZeroTargetRepetitions_ReturnsZeroInsteadOfThrowing()
    {
        var result = _scorer.Score("com.yourorg.template-exercise", completedRepetitions: 0, targetRepetitions: 0, SampleContext());

        Assert.Equal(0.0, result.NormalizedScore);
    }
}
