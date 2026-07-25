namespace FreeRehabHub.Domain;

public sealed class PrescriptionItem
{
    public string ExerciseCardId { get; set; } = string.Empty;
    public int? Repetitions { get; set; }
    public int? Sets { get; set; }
    public int? FrequencyPerWeek { get; set; }
}
