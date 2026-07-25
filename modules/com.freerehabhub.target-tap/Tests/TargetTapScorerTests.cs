using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Modules.TargetTap.Scoring;
using Xunit;

namespace FreeRehabHub.Modules.TargetTap.Tests;

public sealed class TargetTapScorerTests
{
    private readonly TargetTapScorer _scorer = new();

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
    public void Score_AllHitsInstantReaction_ReturnsMaxScore()
    {
        var result = _scorer.Score(
            "com.freerehabhub.target-tap", totalRounds: 8, hitCount: 8,
            totalReactionTimeSecondsForHits: 0.0, SampleContext());

        Assert.Equal(1.0, result.NormalizedScore, precision: 5);
        Assert.Equal(8, result.Metrics["hitCount"]);
        Assert.Equal(0, result.Metrics["missCount"]);
    }

    [Fact]
    public void Score_AllMisses_ReturnsZero()
    {
        var result = _scorer.Score(
            "com.freerehabhub.target-tap", totalRounds: 8, hitCount: 0,
            totalReactionTimeSecondsForHits: 0.0, SampleContext());

        Assert.Equal(0.0, result.NormalizedScore);
        Assert.Equal(8, result.Metrics["missCount"]);
        Assert.Equal(0.0, result.Metrics["averageReactionTimeSeconds"]);
    }

    [Fact]
    public void Score_PartialHitsWithMidRangeReactionTime_ReturnsMidRangeScore()
    {
        // 4/8 isabet (hitRate=0.5), ortalama 1.5s tepki (MaxReactionTimeSeconds=3.0 => speedComponent=0.5)
        var result = _scorer.Score(
            "com.freerehabhub.target-tap", totalRounds: 8, hitCount: 4,
            totalReactionTimeSecondsForHits: 6.0, SampleContext());

        Assert.Equal(0.5, result.NormalizedScore, precision: 5);
        Assert.Equal(1.5, result.Metrics["averageReactionTimeSeconds"], precision: 5);
    }

    [Fact]
    public void Score_ReactionTimeAtOrBeyondMax_ClampsSpeedComponentToZero()
    {
        var result = _scorer.Score(
            "com.freerehabhub.target-tap", totalRounds: 4, hitCount: 4,
            totalReactionTimeSecondsForHits: 20.0, SampleContext());

        // hitRate=1.0, speedComponent=0 (aşırı yavaş) => (1.0 + 0) / 2
        Assert.Equal(0.5, result.NormalizedScore, precision: 5);
    }

    [Fact]
    public void Score_ZeroTotalRounds_ReturnsZeroInsteadOfThrowing()
    {
        var result = _scorer.Score(
            "com.freerehabhub.target-tap", totalRounds: 0, hitCount: 0,
            totalReactionTimeSecondsForHits: 0.0, SampleContext());

        Assert.Equal(0.0, result.NormalizedScore);
    }
}
