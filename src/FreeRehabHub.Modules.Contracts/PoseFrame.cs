namespace FreeRehabHub.Modules.Contracts;

// Hiç poz tespit edilmediyse Poses boş liste olur (null değil) — çağıran kod her zaman
// koleksiyon üzerinde çalışabilir, null-check'e gerek kalmaz.
public sealed class PoseFrame
{
    public DateTime CapturedAt { get; set; }
    public IReadOnlyList<DetectedPose> Poses { get; set; } = new List<DetectedPose>();
}
