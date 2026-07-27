using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Modules.MemoryMatch.Scoring;
using Xunit;

namespace FreeRehabHub.Modules.MemoryMatch.Tests;

public sealed class MemoryMatchScorerTests
{
    private readonly MemoryMatchScorer _scorer = new();
    private readonly ModuleContext _context = new()
    {
        PatientId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        CompletedAt = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Score_AllPairsMatchedWithMinimumAttempts_ReturnsScoreOfOne()
    {
        var result = _scorer.Score(
            "com.freerehabhub.memory-match", totalPairs: 6, matchedPairs: 6, totalAttempts: 6, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
        Assert.Equal(6, result.Metrics["totalPairs"]);
        Assert.Equal(6, result.Metrics["matchedPairs"]);
        Assert.Equal(6, result.Metrics["totalAttempts"]);
    }

    [Fact]
    public void Score_AllPairsMatchedWithTwiceTheAttempts_ReturnsThreeQuarterScore()
    {
        // completionRate=6/6=1.0, efficiency=6/12=0.5 -> (1.0+0.5)/2 = 0.75
        var result = _scorer.Score(
            "com.freerehabhub.memory-match", totalPairs: 6, matchedPairs: 6, totalAttempts: 12, _context);

        Assert.Equal(0.75, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_EarlyExitWithFewPairsMatched_DoesNotInflateScoreAboveCompletionRate()
    {
        // Erken çıkış: 6 hedeften sadece 2'si bulunmuş, 5 denemede — sadece
        // totalPairs/totalAttempts kullanılsaydı (6/5=1.2, clamp 1.0) yanlış şekilde tam skor
        // verirdi. completionRate=2/6=0.333, efficiency=2/5=0.4 -> (0.333+0.4)/2=0.367
        var result = _scorer.Score(
            "com.freerehabhub.memory-match", totalPairs: 6, matchedPairs: 2, totalAttempts: 5, _context);

        Assert.True(result.NormalizedScore < 0.5, "Erken çıkışta skor 0.5'in altında kalmalı.");
        Assert.Equal((2.0 / 6.0 + 2.0 / 5.0) / 2.0, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_ZeroAttempts_ReturnsZeroInsteadOfThrowing()
    {
        var result = _scorer.Score(
            "com.freerehabhub.memory-match", totalPairs: 6, matchedPairs: 0, totalAttempts: 0, _context);

        Assert.Equal(0.0, result.NormalizedScore);
    }

    [Fact]
    public void Score_SinglePairPerfectPlay_ReturnsScoreOfOne()
    {
        var result = _scorer.Score(
            "com.freerehabhub.memory-match", totalPairs: 1, matchedPairs: 1, totalAttempts: 1, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
    }
}
