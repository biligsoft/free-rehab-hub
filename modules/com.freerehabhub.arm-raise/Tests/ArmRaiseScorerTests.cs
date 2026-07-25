using FreeRehabHub.Modules.ArmRaise.Scoring;
using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.ArmRaise.Tests;

public sealed class ArmRaiseScorerTests
{
    private readonly ArmRaiseScorer _scorer = new();
    private readonly ModuleContext _context = new()
    {
        PatientId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        CompletedAt = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Score_AllRepsCompletedWithFullRangeOfMotion_ReturnsScoreOfOne()
    {
        var result = _scorer.Score("com.freerehabhub.arm-raise", targetReps: 10, completedReps: 10,
            sumOfMaxAnglesForCompletedReps: 160.0 * 10, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
        Assert.Equal(10, result.Metrics["completedReps"]);
        Assert.Equal(160.0, result.Metrics["averageMaxAngleDegrees"], 3);
    }

    [Fact]
    public void Score_NoRepsCompleted_ReturnsZero()
    {
        var result = _scorer.Score("com.freerehabhub.arm-raise", targetReps: 10, completedReps: 0,
            sumOfMaxAnglesForCompletedReps: 0.0, _context);

        Assert.Equal(0.0, result.NormalizedScore, 3);
        Assert.Equal(0.0, result.Metrics["averageMaxAngleDegrees"], 3);
    }

    [Fact]
    public void Score_PartialCompletionWithModerateAngle_ReturnsMidRangeScore()
    {
        // 5/10 tekrar (completionRate=0.5) + ortalama 80° (kaliteRate=0.5) -> (0.5+0.5)/2 = 0.5
        var result = _scorer.Score("com.freerehabhub.arm-raise", targetReps: 10, completedReps: 5,
            sumOfMaxAnglesForCompletedReps: 80.0 * 5, _context);

        Assert.Equal(0.5, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_AngleExceedsFullFlexionReference_ClampsQualityToOne()
    {
        var result = _scorer.Score("com.freerehabhub.arm-raise", targetReps: 10, completedReps: 10,
            sumOfMaxAnglesForCompletedReps: 180.0 * 10, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_ZeroTargetReps_DoesNotThrowAndReturnsZeroCompletionRate()
    {
        var result = _scorer.Score("com.freerehabhub.arm-raise", targetReps: 0, completedReps: 0,
            sumOfMaxAnglesForCompletedReps: 0.0, _context);

        Assert.Equal(0.0, result.NormalizedScore, 3);
    }
}
