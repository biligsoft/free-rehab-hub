using FreeRehabHub.Modules.BalloonPop.Scoring;
using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.BalloonPop.Tests;

public sealed class BalloonPopScorerTests
{
    private readonly BalloonPopScorer _scorer = new();
    private readonly ModuleContext _context = new()
    {
        PatientId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        CompletedAt = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Score_AllBalloonsPoppedWithFullRangeOfMotion_ReturnsScoreOfOne()
    {
        var result = _scorer.Score("com.freerehabhub.balloon-pop", totalBalloons: 6, poppedCount: 6,
            sumOfMaxAnglesForPopped: 160.0 * 6, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
        Assert.Equal(6, result.Metrics["poppedCount"]);
        Assert.Equal(160.0, result.Metrics["averageMaxAngleDegrees"], 3);
    }

    [Fact]
    public void Score_NoBalloonsPopped_ReturnsZero()
    {
        var result = _scorer.Score("com.freerehabhub.balloon-pop", totalBalloons: 6, poppedCount: 0,
            sumOfMaxAnglesForPopped: 0.0, _context);

        Assert.Equal(0.0, result.NormalizedScore, 3);
        Assert.Equal(0.0, result.Metrics["averageMaxAngleDegrees"], 3);
    }

    [Fact]
    public void Score_PartialCompletionWithModerateAngle_ReturnsMidRangeScore()
    {
        // 3/6 balon (completionRate=0.5) + ortalama 80° (angleQuality=0.5) -> (0.5+0.5)/2 = 0.5
        var result = _scorer.Score("com.freerehabhub.balloon-pop", totalBalloons: 6, poppedCount: 3,
            sumOfMaxAnglesForPopped: 80.0 * 3, _context);

        Assert.Equal(0.5, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_AngleExceedsFullFlexionReference_ClampsQualityToOne()
    {
        var result = _scorer.Score("com.freerehabhub.balloon-pop", totalBalloons: 6, poppedCount: 6,
            sumOfMaxAnglesForPopped: 180.0 * 6, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_ZeroTotalBalloons_DoesNotThrowAndReturnsZeroCompletionRate()
    {
        var result = _scorer.Score("com.freerehabhub.balloon-pop", totalBalloons: 0, poppedCount: 0,
            sumOfMaxAnglesForPopped: 0.0, _context);

        Assert.Equal(0.0, result.NormalizedScore, 3);
    }
}
