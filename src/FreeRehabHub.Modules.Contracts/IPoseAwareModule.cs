namespace FreeRehabHub.Modules.Contracts;

// ISP: sadece kamera/poz verisi gerektiren IExerciseModule implementasyonları bunu da implemente eder
// (bkz. godot-csharp-standards § SOLID). ModuleHost, aktif modül bunu implemente ediyorsa
// IPoseTrackingService'ten gelen her PoseFrame'i buraya iletir.
public interface IPoseAwareModule
{
    void OnPoseFrame(PoseFrame frame);
}
