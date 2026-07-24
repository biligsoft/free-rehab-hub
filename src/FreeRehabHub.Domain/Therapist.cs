using FreeRehabHub.Core;

namespace FreeRehabHub.Domain;

public sealed class Therapist
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Discipline Discipline { get; set; }
    public DateTime CreatedAt { get; set; }
}
