namespace FreeRehabHub.Modules.Contracts;

public sealed class PoseLandmark
{
    public PoseLandmarkType Type { get; set; }

    // Görüntüye göre normalize (0-1), ekran-üstü etkileşim/oyun mantığı için.
    public PosePoint Normalized { get; set; } = new();

    // Kalça orta noktasına göre gerçek dünya koordinatları (metre), kameraya olan mesafeden
    // bağımsız — eklem açısı/ROM (range of motion) gibi klinik ölçümler için gerekli.
    public PosePoint World { get; set; } = new();

    public float Visibility { get; set; }
    public float Presence { get; set; }
}
