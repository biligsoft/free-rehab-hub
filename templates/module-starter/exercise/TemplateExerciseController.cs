using FreeRehabHub.Core;
using FreeRehabHub.Modules.Contracts;
using FreeRehabHub.Modules.TemplateExercise.Scoring;
using Godot;

namespace FreeRehabHub.Modules.TemplateExercise;

// ŞABLON: Bu dosyayı kopyaladıktan sonra sınıf adını, namespace'i, Manifest içeriğini ve
// egzersiz mekaniğini (şu an placeholder "N kez tekrar tamamla" düğmesi) kendi modülünüze göre
// değiştirin. Sahne yaşam döngüsü (InitializeAsync/OnActivated/.../Completed) ve Dispose deseni
// (Node zaten IDisposable'ı GodotObject'ten miras alıyor — bkz. Dispose(bool) override'ı aşağıda)
// olduğu gibi korunmalı.
public partial class TemplateExerciseController : Node, IExerciseModule
{
    private const int TargetRepetitionCount = 5;

    [Export] private NodePath _repetitionLabelPath = null!;
    [Export] private NodePath _completeRepButtonPath = null!;

    private readonly TemplateExerciseScorer _scorer = new();

    private Label _repetitionLabel = null!;
    private Button _completeRepButton = null!;
    private ModuleContext _context = null!;
    private int _completedRepetitions;
    private bool _completed;

    public string ModuleId => Manifest.Id;

    public ModuleManifest Manifest { get; } = new()
    {
        Id = "com.yourorg.template-exercise",
        Version = "0.1.0",
        Kind = ModuleKind.Exercise,
        DisplayName = new LocalizedText { Tr = "Şablon Egzersiz", En = "Template Exercise" },
        Description = new LocalizedText
        {
            Tr = "Yeni bir egzersiz modülü için başlangıç şablonu.",
            En = "Starter template for a new exercise module."
        },
        Disciplines = new List<Discipline> { Discipline.Physiotherapy },
        DifficultyRange = new DifficultyRange { Min = 1, Max = 1 },
        RequiredCapabilities = new List<string>(),
        MinAppVersion = "0.1.0",
        EntryPointType = "FreeRehabHub.Modules.TemplateExercise.TemplateExerciseController",
        ScenePath = "res://templates/module-starter/exercise/TemplateExercise.tscn",
        MetricLabels = new Dictionary<string, LocalizedText>
        {
            ["completedRepetitions"] = new LocalizedText { Tr = "Tamamlanan Tekrar", En = "Completed Repetitions" },
            ["targetRepetitions"] = new LocalizedText { Tr = "Hedef Tekrar", En = "Target Repetitions" }
        }
    };

    public event EventHandler<ModuleResult>? Completed;

    public override void _Ready()
    {
        _repetitionLabel = GetNode<Label>(_repetitionLabelPath);
        _completeRepButton = GetNode<Button>(_completeRepButtonPath);
        _completeRepButton.Pressed += OnCompleteRepPressed;
    }

    public Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _completedRepetitions = 0;
        _completed = false;
        UpdateRepetitionLabel();
        return Task.CompletedTask;
    }

    public void OnActivated()
    {
        _completeRepButton.Disabled = false;
    }

    public void OnPaused()
    {
        _completeRepButton.Disabled = true;
    }

    public void OnResumed()
    {
        _completeRepButton.Disabled = false;
    }

    public void OnDeactivated()
    {
        // LSP: Completed tam olarak bir kez tetiklenmeli (bkz. godot-csharp-standards § 5) — kullanıcı
        // hedefe ulaşmadan modülden çıkarsa bile ModuleHost'un beklediği garanti burada sağlanıyor.
        RaiseCompletedIfNeeded();
    }

    private void OnCompleteRepPressed()
    {
        _completedRepetitions++;
        UpdateRepetitionLabel();

        if (_completedRepetitions >= TargetRepetitionCount)
        {
            _completeRepButton.Disabled = true;
            RaiseCompletedIfNeeded();
        }
    }

    private void UpdateRepetitionLabel()
    {
        _repetitionLabel.Text = $"{_completedRepetitions} / {TargetRepetitionCount}";
    }

    private void RaiseCompletedIfNeeded()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        var result = _scorer.Score(ModuleId, _completedRepetitions, TargetRepetitionCount, _context);
        Completed?.Invoke(this, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _completeRepButton is not null)
        {
            _completeRepButton.Pressed -= OnCompleteRepPressed;
        }

        base.Dispose(disposing);
    }
}
