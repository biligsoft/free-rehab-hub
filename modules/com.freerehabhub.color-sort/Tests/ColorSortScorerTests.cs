using FreeRehabHub.Modules.ColorSort.Scoring;
using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.ColorSort.Tests;

public sealed class ColorSortScorerTests
{
    private readonly ColorSortScorer _scorer = new();

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
    public void Score_AllCorrectInstantResponse_ReturnsMaxScore()
    {
        var result = _scorer.Score(
            "com.freerehabhub.color-sort", totalRounds: 8, correctCount: 8,
            totalResponseTimeSecondsForCorrect: 0.0, SampleContext());

        Assert.Equal(1.0, result.NormalizedScore, precision: 5);
        Assert.Equal(8, result.Metrics["correctCount"]);
        Assert.Equal(0, result.Metrics["incorrectCount"]);
    }

    [Fact]
    public void Score_AllIncorrect_ReturnsZero()
    {
        var result = _scorer.Score(
            "com.freerehabhub.color-sort", totalRounds: 8, correctCount: 0,
            totalResponseTimeSecondsForCorrect: 0.0, SampleContext());

        Assert.Equal(0.0, result.NormalizedScore);
        Assert.Equal(8, result.Metrics["incorrectCount"]);
        Assert.Equal(0.0, result.Metrics["averageResponseTimeSeconds"]);
    }

    [Fact]
    public void Score_PartialCorrectWithMidRangeResponseTime_ReturnsMidRangeScore()
    {
        // 4/8 doğru (accuracy=0.5), ortalama 1.5s yanıt (MaxResponseTimeSeconds=3.0 => speedComponent=0.5)
        var result = _scorer.Score(
            "com.freerehabhub.color-sort", totalRounds: 8, correctCount: 4,
            totalResponseTimeSecondsForCorrect: 6.0, SampleContext());

        Assert.Equal(0.5, result.NormalizedScore, precision: 5);
        Assert.Equal(1.5, result.Metrics["averageResponseTimeSeconds"], precision: 5);
    }

    [Fact]
    public void Score_ResponseTimeAtOrBeyondMax_ClampsSpeedComponentToZero()
    {
        var result = _scorer.Score(
            "com.freerehabhub.color-sort", totalRounds: 4, correctCount: 4,
            totalResponseTimeSecondsForCorrect: 20.0, SampleContext());

        // accuracy=1.0, speedComponent=0 (aşırı yavaş) => (1.0 + 0) / 2
        Assert.Equal(0.5, result.NormalizedScore, precision: 5);
    }

    [Fact]
    public void Score_ZeroTotalRounds_ReturnsZeroInsteadOfThrowing()
    {
        var result = _scorer.Score(
            "com.freerehabhub.color-sort", totalRounds: 0, correctCount: 0,
            totalResponseTimeSecondsForCorrect: 0.0, SampleContext());

        Assert.Equal(0.0, result.NormalizedScore);
    }
}
