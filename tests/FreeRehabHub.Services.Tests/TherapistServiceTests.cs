using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class TherapistServiceTests
{
    private readonly FakeTherapistRepository _therapistRepository = new();
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly TherapistService _service;
    private readonly Guid _actingTherapistId = Guid.NewGuid();

    public TherapistServiceTests()
    {
        _service = new TherapistService(_therapistRepository, _auditLogRepository);
    }

    [Fact]
    public async Task AddAsync_WritesTherapist_AndLogsCreated()
    {
        var therapist = NewTherapist();

        await _service.AddAsync(therapist, _actingTherapistId);

        Assert.Equal(therapist, await _therapistRepository.GetByIdAsync(therapist.Id));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditRecordType.Therapist, entry.RecordType);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownTherapist_DoesNotLog()
    {
        var fetched = await _service.GetByIdAsync(Guid.NewGuid(), _actingTherapistId);

        Assert.Null(fetched);
        Assert.Empty(_auditLogRepository.Entries);
    }

    [Fact]
    public async Task UpdateAsync_LogsUpdated()
    {
        var therapist = NewTherapist();
        await _therapistRepository.AddAsync(therapist);

        await _service.UpdateAsync(therapist, _actingTherapistId);

        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Updated, entry.Action);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTherapist_AndLogsDeleted()
    {
        var therapist = NewTherapist();
        await _therapistRepository.AddAsync(therapist);

        await _service.DeleteAsync(therapist.Id, _actingTherapistId);

        Assert.Null(await _therapistRepository.GetByIdAsync(therapist.Id));
        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Deleted, entry.Action);
    }

    private static Therapist NewTherapist()
    {
        return new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Test Terapist",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = DateTime.UtcNow
        };
    }
}
