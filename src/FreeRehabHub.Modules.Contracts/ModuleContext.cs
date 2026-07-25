namespace FreeRehabHub.Modules.Contracts;

public sealed class ModuleContext
{
    public Guid PatientId { get; set; }
    public Guid SessionId { get; set; }

    // IAssessmentModule.Score() saf fonksiyon olmalı (aynı girdi -> aynı çıktı); "şu an ne zaman"
    // bilgisini Score() içinde DateTime.UtcNow ile okumak yerine çağıran taraf burada sağlıyor.
    public DateTime CompletedAt { get; set; }
}
