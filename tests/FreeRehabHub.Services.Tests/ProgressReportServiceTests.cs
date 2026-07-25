using System.Text;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Services.Tests.Fakes;
using PdfSharp.Pdf.IO;
using Xunit;

namespace FreeRehabHub.Services.Tests;

public sealed class ProgressReportServiceTests : IDisposable
{
    private readonly string _fontDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "fonts", "liberation-sans");
    private readonly string _outputFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
    private readonly FakeAuditLogRepository _auditLogRepository = new();
    private readonly Patient _patient;
    private readonly Therapist _therapist;

    public ProgressReportServiceTests()
    {
        _patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Ayşe Yılmaz",
            DateOfBirth = new DateOnly(2015, 6, 10),
            CreatedAt = DateTime.UtcNow
        };
        _therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Test Terapist",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GeneratePdfAsync_WritesValidPdfFile_AndLogsExported()
    {
        var service = new ProgressReportService(_fontDirectory, _auditLogRepository);
        var history = new List<ProgressRecord>
        {
            NewRecord("com.freerehabhub.arm-raise", 0.5, new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc)),
            NewRecord("com.freerehabhub.arm-raise", 0.8, new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc))
        };
        var modules = new List<(string ModuleId, string DisplayName)> { ("com.freerehabhub.arm-raise", "Kol Kaldırma") };

        await service.GeneratePdfAsync(_patient, _therapist, history, modules, _outputFilePath);

        Assert.True(File.Exists(_outputFilePath));
        var header = Encoding.ASCII.GetString(File.ReadAllBytes(_outputFilePath), 0, 5);
        Assert.Equal("%PDF-", header);

        var entry = Assert.Single(_auditLogRepository.Entries);
        Assert.Equal(AuditAction.Exported, entry.Action);
        Assert.Equal(AuditRecordType.Patient, entry.RecordType);
        Assert.Equal(_patient.Id, entry.RecordId);
        Assert.Equal(_therapist.Id, entry.TherapistId);
    }

    [Fact]
    public async Task GeneratePdfAsync_ManyRecords_ProducesMultiplePages()
    {
        var service = new ProgressReportService(_fontDirectory, _auditLogRepository);
        var history = Enumerable.Range(0, 80)
            .Select(index => NewRecord(
                "com.freerehabhub.arm-raise",
                0.5,
                new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc).AddDays(index)))
            .ToList();
        var modules = new List<(string ModuleId, string DisplayName)> { ("com.freerehabhub.arm-raise", "Kol Kaldırma") };

        await service.GeneratePdfAsync(_patient, _therapist, history, modules, _outputFilePath);

        using var reopened = PdfReader.Open(_outputFilePath, PdfDocumentOpenMode.Import);
        Assert.True(reopened.PageCount > 1);
    }

    private static ProgressRecord NewRecord(string moduleId, double score, DateTime completedAt)
    {
        return new ProgressRecord
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            ModuleId = moduleId,
            SessionId = Guid.NewGuid(),
            CompletedAt = completedAt,
            NormalizedScore = score,
            Metrics = { ["completedReps"] = 8 }
        };
    }

    public void Dispose()
    {
        if (File.Exists(_outputFilePath))
        {
            File.Delete(_outputFilePath);
        }
    }
}
