using System;
using System.Collections.Generic;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.BalloonPop.Scoring;

public sealed class BalloonPopScorer
{
    // arm-raise'deki ShoulderFlexionCalculator ile aynı klinik referans: tam omuz fleksiyonuna
    // yakın kabul edilen açı — kalite skorunu normalize etmek için.
    private const double FullFlexionAngleDegrees = 160.0;

    public ModuleResult Score(
        string moduleId,
        int totalBalloons,
        int poppedCount,
        double sumOfMaxAnglesForPopped,
        ModuleContext context)
    {
        var completionRate = totalBalloons <= 0 ? 0.0 : Math.Clamp((double)poppedCount / totalBalloons, 0.0, 1.0);
        var averageMaxAngle = poppedCount > 0 ? sumOfMaxAnglesForPopped / poppedCount : 0.0;
        var angleQuality = Math.Clamp(averageMaxAngle / FullFlexionAngleDegrees, 0.0, 1.0);
        var normalizedScore = (completionRate + angleQuality) / 2.0;

        return new ModuleResult
        {
            ModuleId = moduleId,
            PatientId = context.PatientId,
            SessionId = context.SessionId,
            CompletedAt = context.CompletedAt,
            NormalizedScore = normalizedScore,
            Metrics = new Dictionary<string, double>
            {
                ["totalBalloons"] = totalBalloons,
                ["poppedCount"] = poppedCount,
                ["averageMaxAngleDegrees"] = averageMaxAngle
            }
        };
    }
}
