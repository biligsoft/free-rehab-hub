using System;
using System.Collections.Generic;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.ColorSort.Scoring;

public sealed class ColorSortScorer
{
    private const double MaxResponseTimeSeconds = 3.0;

    public ModuleResult Score(
        string moduleId,
        int totalRounds,
        int correctCount,
        double totalResponseTimeSecondsForCorrect,
        ModuleContext context)
    {
        var accuracy = totalRounds <= 0 ? 0.0 : (double)correctCount / totalRounds;
        var averageResponseTimeSeconds = correctCount > 0 ? totalResponseTimeSecondsForCorrect / correctCount : 0.0;
        var speedComponent = correctCount > 0
            ? Math.Clamp(1.0 - (averageResponseTimeSeconds / MaxResponseTimeSeconds), 0.0, 1.0)
            : 0.0;
        var normalizedScore = (accuracy + speedComponent) / 2.0;

        return new ModuleResult
        {
            ModuleId = moduleId,
            PatientId = context.PatientId,
            SessionId = context.SessionId,
            CompletedAt = context.CompletedAt,
            NormalizedScore = normalizedScore,
            Metrics = new Dictionary<string, double>
            {
                ["totalRounds"] = totalRounds,
                ["correctCount"] = correctCount,
                ["incorrectCount"] = totalRounds - correctCount,
                ["averageResponseTimeSeconds"] = averageResponseTimeSeconds
            }
        };
    }
}
