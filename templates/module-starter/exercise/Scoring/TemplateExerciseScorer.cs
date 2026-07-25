using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.TemplateExercise.Scoring;

// ŞABLON: Skorlama mantığı Controller'dan ayrı, Godot-bağımsız bir sınıfta tutuluyor
// (bkz. module-development skill § 3a) — böylece Godot açılmadan xUnit ile test edilebiliyor.
public sealed class TemplateExerciseScorer
{
    public ModuleResult Score(string moduleId, int completedRepetitions, int targetRepetitions, ModuleContext context)
    {
        var normalizedScore = targetRepetitions <= 0
            ? 0.0
            : Math.Clamp((double)completedRepetitions / targetRepetitions, 0.0, 1.0);

        return new ModuleResult
        {
            ModuleId = moduleId,
            PatientId = context.PatientId,
            SessionId = context.SessionId,
            CompletedAt = context.CompletedAt,
            NormalizedScore = normalizedScore,
            Metrics = new Dictionary<string, double>
            {
                ["completedRepetitions"] = completedRepetitions,
                ["targetRepetitions"] = targetRepetitions
            }
        };
    }
}
