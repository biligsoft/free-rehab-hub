namespace FreeRehabHub.Domain;

public sealed class Patient
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
}
