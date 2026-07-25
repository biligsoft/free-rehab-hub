namespace FreeRehabHub.Modules.Contracts;

public sealed class DetectedPose
{
    public IReadOnlyList<PoseLandmark> Landmarks { get; set; } = new List<PoseLandmark>();
}
