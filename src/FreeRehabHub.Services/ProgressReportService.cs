using System.Text;
using FreeRehabHub.Domain;
using FreeRehabHub.Domain.Repositories;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace FreeRehabHub.Services;

public sealed class ProgressReportService
{
    private const double PageMarginPoints = 40;
    private const double TitleFontSize = 18;
    private const double HeadingFontSize = 13;
    private const double BodyFontSize = 10;
    private const double LineHeightPoints = 16;
    private const double SectionSpacingPoints = 10;

    // Bkz. clinical-data-handling skill § 5 ("Tıbbi cihaz değildir" kapsamı) — her PDF raporunda
    // bulunması zorunlu feragatname.
    private const string DisclaimerText =
        "Bu rapor bir tıbbi tanı veya otomatik klinik karar niteliği taşımaz; sonuçlar terapistin " +
        "klinik değerlendirmesiyle birlikte yorumlanmalıdır.";

    private static bool _fontResolverRegistered;

    private readonly IAuditLogRepository _auditLogRepository;

    public ProgressReportService(string fontDirectory, IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;

        if (!_fontResolverRegistered)
        {
            GlobalFontSettings.FontResolver = new LiberationSansFontResolver(fontDirectory);
            _fontResolverRegistered = true;
        }
    }

    public async Task GeneratePdfAsync(
        Patient patient,
        Therapist therapist,
        IReadOnlyList<ProgressRecord> history,
        IReadOnlyList<(string ModuleId, string DisplayName)> modules,
        string outputFilePath,
        CancellationToken cancellationToken = default)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);

        var titleFont = new XFont(LiberationSansFontResolver.FamilyName, TitleFontSize, XFontStyleEx.Bold);
        var headingFont = new XFont(LiberationSansFontResolver.FamilyName, HeadingFontSize, XFontStyleEx.Bold);
        var bodyFont = new XFont(LiberationSansFontResolver.FamilyName, BodyFontSize, XFontStyleEx.Regular);

        var contentWidth = page.Width.Point - (2 * PageMarginPoints);
        var cursorY = PageMarginPoints;

        cursorY = DrawLine(graphics, "FreeRehabHub — İlerleme Raporu", titleFont, cursorY);
        cursorY += SectionSpacingPoints;
        cursorY = DrawLine(
            graphics, $"Hasta: {patient.FullName} (Doğum Tarihi: {patient.DateOfBirth:dd.MM.yyyy})", bodyFont, cursorY);
        cursorY = DrawLine(graphics, $"Terapist: {therapist.FullName}", bodyFont, cursorY);
        cursorY = DrawLine(graphics, $"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}", bodyFont, cursorY);
        cursorY += SectionSpacingPoints;
        cursorY = DrawWrapped(graphics, DisclaimerText, bodyFont, cursorY, contentWidth);
        cursorY += SectionSpacingPoints;

        foreach (var (moduleId, displayName) in modules)
        {
            var moduleRecords = history
                .Where(record => record.ModuleId == moduleId)
                .OrderBy(record => record.CompletedAt)
                .ToList();
            if (moduleRecords.Count == 0)
            {
                continue;
            }

            (page, graphics, cursorY) = EnsureSpace(document, page, graphics, cursorY, LineHeightPoints * 2);
            cursorY = DrawLine(graphics, displayName, headingFont, cursorY);

            foreach (var record in moduleRecords)
            {
                (page, graphics, cursorY) = EnsureSpace(document, page, graphics, cursorY, LineHeightPoints);
                cursorY = DrawLine(graphics, FormatRecordLine(record), bodyFont, cursorY);
            }

            cursorY += SectionSpacingPoints;
        }

        document.Save(outputFilePath);

        await _auditLogRepository.AddAsync(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                TherapistId = therapist.Id,
                OccurredAt = DateTime.UtcNow,
                RecordType = AuditRecordType.Patient,
                RecordId = patient.Id,
                Action = AuditAction.Exported
            },
            cancellationToken);
    }

    private static string FormatRecordLine(ProgressRecord record)
    {
        var metricsText = string.Join(
            ", ",
            record.Metrics.Select(metric => $"{MetricKeyFormatter.Humanize(metric.Key)}: {metric.Value:0.##}"));
        var scoreText = $"{record.CompletedAt:dd.MM.yyyy HH:mm} — {record.NormalizedScore:P0}";
        return metricsText.Length == 0 ? scoreText : $"{scoreText} ({metricsText})";
    }

    private static (PdfPage Page, XGraphics Graphics, double CursorY) EnsureSpace(
        PdfDocument document, PdfPage page, XGraphics graphics, double cursorY, double neededHeight)
    {
        if (cursorY + neededHeight <= page.Height.Point - PageMarginPoints)
        {
            return (page, graphics, cursorY);
        }

        var newPage = document.AddPage();
        return (newPage, XGraphics.FromPdfPage(newPage), PageMarginPoints);
    }

    private static double DrawLine(XGraphics graphics, string text, XFont font, double y)
    {
        graphics.DrawString(text, font, XBrushes.Black, new XPoint(PageMarginPoints, y));
        return y + LineHeightPoints;
    }

    private static double DrawWrapped(XGraphics graphics, string text, XFont font, double y, double maxWidth)
    {
        var words = text.Split(' ');
        var line = new StringBuilder();

        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (line.Length > 0 && graphics.MeasureString(candidate, font).Width > maxWidth)
            {
                y = DrawLine(graphics, line.ToString(), font, y);
                line.Clear();
                line.Append(word);
            }
            else
            {
                line.Clear();
                line.Append(candidate);
            }
        }

        if (line.Length > 0)
        {
            y = DrawLine(graphics, line.ToString(), font, y);
        }

        return y;
    }
}
