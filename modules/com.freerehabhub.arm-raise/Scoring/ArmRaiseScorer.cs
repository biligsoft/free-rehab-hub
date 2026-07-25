using System;
using System.Collections.Generic;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.ArmRaise.Scoring;

public sealed class ArmRaiseScorer
{
    // Klinik olarak tam omuz fleksiyonuna yakın kabul edilen açı — kalite skorunu normalize etmek için.
    private const double FullFlexionAngleDegrees = 160.0;

    public ModuleResult Score(
        string moduleId,
        int targetReps,
        int completedReps,
        double sumOfMaxAnglesForCompletedReps,
        ModuleContext context)
    {
        var completionRate = targetReps <= 0 ? 0.0 : Math.Clamp((double)completedReps / targetReps, 0.0, 1.0);
        var averageMaxAngle = completedReps > 0 ? sumOfMaxAnglesForCompletedReps / completedReps : 0.0;
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
                ["targetReps"] = targetReps,
                ["completedReps"] = completedReps,
                ["averageMaxAngleDegrees"] = averageMaxAngle
            }
        };
    }
}
