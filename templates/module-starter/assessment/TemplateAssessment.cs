using System.Globalization;
using FreeRehabHub.Core;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.TemplateAssessment;

// ŞABLON: Bu dosyayı kopyaladıktan sonra sınıf adını, namespace'i, Manifest içeriğini ve
// Score() mantığını (şu an tek bir "score" alanını doğrudan normalize eden placeholder) kendi
// modülünüze göre değiştirin. Score() saf fonksiyon kalmalı — yan etki yok, DateTime.UtcNow gibi
// çağrılar yasak (bkz. module-development skill § 3b, ModuleContext.CompletedAt).
public sealed class TemplateAssessment : IAssessmentModule
{
    private const string ScoreFieldId = "score";
    private const string NotesFieldId = "notes";

    private const double ScaleMin = 0.0;
    private const double ScaleMax = 10.0;

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.yourorg.template-assessment",
        Version = "0.1.0",
        Kind = ModuleKind.Assessment,
        DisplayName = new LocalizedText { Tr = "Şablon Değerlendirme", En = "Template Assessment" },
        Description = new LocalizedText
        {
            Tr = "Yeni bir değerlendirme modülü için başlangıç şablonu.",
            En = "Starter template for a new assessment module."
        },
        Disciplines = new List<Discipline> { Discipline.Physiotherapy },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 1 },
        RequiredCapabilities = new List<string>(),
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.TemplateAssessment.TemplateAssessment",
        FormSchemaPath = "res://templates/module-starter/assessment/form-schema.json",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            [ScoreFieldId] = new LocalizedText { Tr = "Skor", En = "Score" }
        }
    };

    public ModuleResult Score(FormSubmission submission, ModuleContext context)
    {
        var score = ReadScaleValue(submission, ScoreFieldId);

        return new ModuleResult
        {
            ModuleId = Manifest.Id,
            PatientId = context.PatientId,
            SessionId = context.SessionId,
            CompletedAt = context.CompletedAt,
            NormalizedScore = score / ScaleMax,
            Metrics = new Dictionary<string, double> { [ScoreFieldId] = score },
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
}
