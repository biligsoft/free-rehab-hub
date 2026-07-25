using System;
using System.Collections.Generic;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.TargetTap.Scoring;

public sealed class TargetTapScorer
{
    private const double MaxReactionTimeSeconds = 3.0;

    public ModuleResult Score(
        string moduleId,
        int totalRounds,
        int hitCount,
        double totalReactionTimeSecondsForHits,
        ModuleContext context)
    {
        var hitRate = totalRounds <= 0 ? 0.0 : (double)hitCount / totalRounds;
        var averageReactionTimeSeconds = hitCount > 0 ? totalReactionTimeSecondsForHits / hitCount : 0.0;
        var speedComponent = hitCount > 0
            ? Math.Clamp(1.0 - (averageReactionTimeSeconds / MaxReactionTimeSeconds), 0.0, 1.0)
            : 0.0;
        var normalizedScore = (hitRate + speedComponent) / 2.0;

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
                ["hitCount"] = hitCount,
                ["missCount"] = totalRounds - hitCount,
                ["averageReactionTimeSeconds"] = averageReactionTimeSeconds
            }
        };
    }
}
