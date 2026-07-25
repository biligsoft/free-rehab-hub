using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.GeneralFunctionalCheckin.Tests;

public sealed class GeneralFunctionalCheckinAssessmentTests
{
    private readonly GeneralFunctionalCheckinAssessment _assessment = new();

    private readonly ModuleContext _context = new()
    {
        PatientId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        CompletedAt = new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Score_TypicalSubmission_ComputesNormalizedScoreAndMetrics()
    {
        var submission = new FormSubmission
        {
            FieldValues =
            {
                ["painLevel"] = "4",
                ["functionalDifficulty"] = "2",
                ["affectedSide"] = "right",
                ["symptoms"] = "swelling,stiffness",
                ["notes"] = "Test notu"
            }
        };

        var result = _assessment.Score(submission, _context);

        Assert.Equal("com.freerehabhub.general-functional-checkin", result.ModuleId);
        Assert.Equal(_context.PatientId, result.PatientId);
        Assert.Equal(_context.SessionId, result.SessionId);
        Assert.Equal(_context.CompletedAt, result.CompletedAt);
        Assert.Equal(0.7, result.NormalizedScore, 3);
        Assert.Equal(4, result.Metrics["painLevel"]);
        Assert.Equal(2, result.Metrics["functionalDifficulty"]);
        Assert.Equal(2, result.Metrics["symptomCount"]);
        Assert.Equal("Test notu", result.Notes);
    }

    [Fact]
    public void Score_BestCaseSubmission_ReturnsMaximumNormalizedScore()
    {
        var submission = new FormSubmission
        {
            FieldValues = { ["painLevel"] = "0", ["functionalDifficulty"] = "0" }
        };

        var result = _assessment.Score(submission, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_WorstCaseSubmission_ReturnsMinimumNormalizedScore()
    {
        var submission = new FormSubmission
        {
            FieldValues = { ["painLevel"] = "10", ["functionalDifficulty"] = "10" }
        };

        var result = _assessment.Score(submission, _context);

        Assert.Equal(0.0, result.NormalizedScore, 3);
    }

    [Fact]
    public void Score_MissingFields_TreatsMissingScaleValuesAsScaleMinimum()
    {
        var submission = new FormSubmission();

        var result = _assessment.Score(submission, _context);

        Assert.Equal(1.0, result.NormalizedScore, 3);
        Assert.Equal(0, result.Metrics["symptomCount"]);
        Assert.Null(result.Notes);
    }

    [Fact]
    public void Score_OutOfRangePainLevel_ClampsToScaleBounds()
    {
        var submission = new FormSubmission
        {
            FieldValues = { ["painLevel"] = "999", ["functionalDifficulty"] = "0" }
        };

        var result = _assessment.Score(submission, _context);

        Assert.Equal(10, result.Metrics["painLevel"]);
    }
}
