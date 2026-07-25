namespace FreeRehabHub.Domain;

// Her yeni atama ayrı, değişmez bir kayıt (bkz. IPrescriptionRepository — Update yok, sadece
// Add + geçmiş/en-son sorgusu). Hastanın "aktif" reçetesi CreatedAt'e göre en sonuncusu.
public sealed class ExercisePrescription
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid CreatedByTherapistId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public List<PrescriptionItem> Items { get; set; } = new();
}
