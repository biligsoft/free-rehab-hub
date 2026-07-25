namespace FreeRehabHub.Modules.Contracts;

// Implementasyon FreeRehabHub.Services'te (WebSocket + mediapipe-service süreç yönetimi) — bu
// arayüz IPatientRepository/Data ayrımıyla aynı desen: soyutlama burada, ağır iş orada.
public interface IPoseTrackingService
{
    PoseTrackingStatus Status { get; }

    // Status == Error olduğunda son hatanın kullanıcıya gösterilebilir kısa açıklaması; aksi halde null.
    string? LastError { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    event EventHandler<PoseFrame> FrameReceived;
    event EventHandler<PoseTrackingStatus> StatusChanged;
}
