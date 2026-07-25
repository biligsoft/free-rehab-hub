namespace FreeRehabHub.Domain;

public sealed class KioskPin
{
    public string PinHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
