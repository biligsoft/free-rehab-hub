using System.Globalization;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.GeneralFunctionalCheckin;

public sealed class GeneralFunctionalCheckinAssessment : IAssessmentModule
{
    private const string PainLevelFieldId = "painLevel";
    private const string FunctionalDifficultyFieldId = "functionalDifficulty";
    private const string SymptomsFieldId = "symptoms";
    private const string NotesFieldId = "notes";

    private const double ScaleMin = 0.0;
    private const double ScaleMax = 10.0;
    private const char MultiChoiceValueSeparator = ',';

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.freerehabhub.general-functional-checkin",
        Version = "1.0.0",
        Kind = ModuleKind.Assessment,
        DisplayName = new LocalizedText
        {
            Tr = "Genel Fonksiyonel Değerlendirme",
            En = "General Functional Check-in"
        },
        Description = new LocalizedText
        {
            Tr = "Ağrı seviyesini ve günlük aktivitelerdeki zorluğu hasta öz bildirimiyle kaydeden, " +
                 "telifsiz basit bir kontrol formu.",
            En = "A simple, royalty-free check-in form that records pain level and daily-activity " +
                 "difficulty via patient self-report."
        },
        Disciplines = new List<Discipline> { Discipline.Physiotherapy, Discipline.OccupationalTherapy },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 1 },
        RequiredCapabilities = new List<string>(),
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.GeneralFunctionalCheckin.GeneralFunctionalCheckinAssessment",
        FormSchemaPath = "res://content-packs/assessment-forms/general-functional-checkin.json",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            [PainLevelFieldId] = new LocalizedText { Tr = "Ağrı Seviyesi", En = "Pain Level" },
            [FunctionalDifficultyFieldId] = new LocalizedText { Tr = "Fonksiyonel Zorluk", En = "Functional Difficulty" },
            ["symptomCount"] = new LocalizedText { Tr = "Belirti Sayısı", En = "Symptom Count" }
        }
    };

    public ModuleResult Score(FormSubmission submission, ModuleContext context)
    {
        var painLevel = ReadScaleValue(submission, PainLevelFieldId);
        var functionalDifficulty = ReadScaleValue(submission, FunctionalDifficultyFieldId);

        // Ham ölçekler "yüksek = kötü" (daha çok ağrı/zorluk); NormalizedScore ise ilerleme
        // grafiklerinde "yüksek = iyi" tutarlılığı için ters çevrilip ortalanıyor.
        var painComponent = 1.0 - (painLevel / ScaleMax);
        var functionComponent = 1.0 - (functionalDifficulty / ScaleMax);
        var normalizedScore = (painComponent + functionComponent) / 2.0;

        var metrics = new Dictionary<string, double>
        {
            [PainLevelFieldId] = painLevel,
            [FunctionalDifficultyFieldId] = functionalDifficulty,
            ["symptomCount"] = CountSymptoms(submission)
        };

        return new ModuleResult
        {
            ModuleId = Manifest.Id,
            PatientId = context.PatientId,
            SessionId = context.SessionId,
            CompletedAt = context.CompletedAt,
            NormalizedScore = normalizedScore,
            Metrics = metrics,
            Notes = submission.FieldValues.GetValueOrDefault(NotesFieldId)
        };
    }

    private static double ReadScaleValue(FormSubmission submission, string fieldId)
    {
        var raw = submission.FieldValues.GetValueOrDefault(fieldId);
        if (raw is null || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return ScaleMin;
        }

        return Math.Clamp(value, ScaleMin, ScaleMax);
    }

    private static double CountSymptoms(FormSubmission submission)
    {
        var raw = submission.FieldValues.GetValueOrDefault(SymptomsFieldId);
        return string.IsNullOrWhiteSpace(raw) ? 0 : raw.Split(MultiChoiceValueSeparator).Length;
    }
}
