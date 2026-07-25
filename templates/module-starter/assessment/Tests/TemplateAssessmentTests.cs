using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.TemplateAssessment.Tests;

public sealed class TemplateAssessmentTests
{
    private readonly TemplateAssessment _assessment = new();

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
    public void Score_TypicalSubmission_ReturnsProportionalScore()
    {
        var submission = new FormSubmission { FieldValues = new Dictionary<string, string> { ["score"] = "5" } };

        var result = _assessment.Score(submission, SampleContext());

        Assert.Equal(0.5, result.NormalizedScore, precision: 5);
        Assert.Equal(5, result.Metrics["score"]);
    }

    [Fact]
    public void Score_MaxScore_ReturnsOne()
    {
        var submission = new FormSubmission { FieldValues = new Dictionary<string, string> { ["score"] = "10" } };

        var result = _assessment.Score(submission, SampleContext());

        Assert.Equal(1.0, result.NormalizedScore, precision: 5);
    }

    [Fact]
    public void Score_MissingScoreField_DefaultsToZeroInsteadOfThrowing()
    {
        var submission = new FormSubmission { FieldValues = new Dictionary<string, string>() };

        var result = _assessment.Score(submission, SampleContext());

        Assert.Equal(0.0, result.NormalizedScore);
    }

    [Fact]
    public void Score_OutOfRangeValue_ClampsToScaleMax()
    {
        var submission = new FormSubmission { FieldValues = new Dictionary<string, string> { ["score"] = "999" } };

        var result = _assessment.Score(submission, SampleContext());

        Assert.Equal(1.0, result.NormalizedScore, precision: 5);
    }
}
