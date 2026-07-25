namespace FreeRehabHub.Domain;

// Bir modülün (Exercise) tamamlanmasının kalıcı, değişmez kaydı — ExercisePrescription gibi,
// geçmiş asla Update edilmez, sadece Add + geçmiş sorgusu. Alanlar bilinçli olarak
// Modules.Contracts.ModuleResult ile birebir aynı isimlendirmede (o tip Domain'den referans
// verilemiyor, katman kuralı) — ModuleResult'tan bu kayda dönüşüm Services katmanında yapılacak.
public sealed class ProgressRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string ModuleId { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public DateTime CompletedAt { get; set; }
    public double NormalizedScore { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public string? Notes { get; set; }
}
