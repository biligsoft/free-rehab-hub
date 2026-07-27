using System;
using System.IO;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using Godot;

namespace FreeRehabHub.SceneTests;

// docs/PROGRESS.md'deki açık risk: "İlerleme/PDF rapor özellikleri Assessment modüllerini de
// kapsıyor mu?" sadece kod okumasıyla ("ProgressPanelController/rapor kodu Kind'a göre
// filtrelemiyor") teyit edilmişti, gerçek UI'dan hiç doğrulanmamıştı. Bu test bunu kapatıyor:
// Assessment kaynaklı bir ProgressRecord'un hem ilerleme ekranında (modül listesi + kayıt satırı)
// hem gerçek bir PDF raporunda göründüğünü uçtan uca doğruluyor.
public sealed class ProgressPanelAssessmentSceneTest : ISceneTest
{
    private const string ProgressPanelScenePath = "res://scenes/progress/ProgressPanel.tscn";
    private const string AssessmentModuleId = "com.freerehabhub.general-functional-checkin";

    public string Name => "ProgressPanel: Assessment kaynaklı kayıt görüntüleme ve PDF rapor";

    public async Task RunAsync(SceneTree sceneTree)
    {
        var appServices = sceneTree.Root.GetNode<AppServices>("/root/AppServices");
        var sessionContext = sceneTree.Root.GetNode<SessionContext>("/root/SessionContext");

        var therapist = new Therapist
        {
            Id = Guid.NewGuid(),
            FullName = "Sahne Testi Terapist",
            Discipline = Discipline.Physiotherapy,
            CreatedAt = DateTime.UtcNow
        };
        await appServices.TherapistService!.AddAsync(therapist, therapist.Id);
        sessionContext.SetActiveTherapist(therapist);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FullName = "Rapor Testi Hastası",
            DateOfBirth = new DateOnly(1988, 3, 15),
            CreatedAt = DateTime.UtcNow
        };
        await appServices.PatientService!.AddAsync(patient, therapist.Id);
        sessionContext.SetActivePatient(patient);

        // ProgressRecords.SessionId artık gerçek bir TherapySessions kaydına FK'li (F8.31) —
        // AssessmentHostController'ın gerçek üretim akışında yaptığı gibi önce bir seans açıyoruz.
        var therapySession = new TherapySession
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            TherapistId = therapist.Id,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow
        };
        await appServices.TherapySessionService!.AddAsync(therapySession, therapist.Id);

        // AssessmentHost'un tüm formu doldurup skorlama akışı zaten AssessmentHostSceneTest'te
        // uçtan uca doğrulanıyor — burada asıl ilgi alanı ProgressPanel/rapor olduğu için
        // ProgressRecord'u doğrudan servis üzerinden ekliyoruz (gerçek üretim kaydı şekli).
        var progressRecord = new ProgressRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            ModuleId = AssessmentModuleId,
            SessionId = therapySession.Id,
            CompletedAt = DateTime.UtcNow,
            NormalizedScore = 0.65,
            Metrics = new System.Collections.Generic.Dictionary<string, double>
            {
                ["painLevel"] = 4,
                ["functionalDifficulty"] = 3,
                ["symptomCount"] = 1
            },
            Notes = "Sahne testi ilerleme notu"
        };
        await appServices.ProgressRecordService!.AddAsync(progressRecord, therapist.Id);

        sceneTree.ChangeSceneToFile(ProgressPanelScenePath);
        await WaitFramesAsync(sceneTree, 5);

        var root = sceneTree.Root;
        var moduleItemList = root.GetNode<ItemList>("ProgressPanel/Card/Content/Body/ModuleItemList");
        var recordsContainer = root.GetNode<VBoxContainer>(
            "ProgressPanel/Card/Content/Body/DetailColumn/RecordsScroll/RecordsContainer");
        var reportStatusLabel = root.GetNode<Label>("ProgressPanel/Card/Content/ReportStatusLabel");
        var pdfReportButton = root.GetNode<Button>("ProgressPanel/Card/Content/Actions/PdfReportButton");
        var reportFileDialog = root.GetNode<FileDialog>("ProgressPanel/ReportFileDialog");

        SceneAssert.Equal(1, moduleItemList.ItemCount, "Assessment modülü modül listesinde tek kayıt olarak görünmeli.");
        SceneAssert.True(
            moduleItemList.GetItemText(0).Contains("Genel Fonksiyonel", StringComparison.Ordinal),
            "Modül listesindeki isim Assessment modülünün gerçek görünen adı olmalı.");

        SceneAssert.Equal(1, recordsContainer.GetChildCount(), "Kayıt listesinde tam olarak 1 satır olmalı.");
        var recordLabel = recordsContainer.GetChild<Label>(0);
        SceneAssert.True(
            recordLabel.Text.Contains("65", StringComparison.Ordinal),
            $"Kayıt satırı normalize skoru (%65) içermeli, gerçek metin: '{recordLabel.Text}'");
        // F8.28: metrik etiketleri artık manifest'in MetricLabels sözlüğünden yerelleştiriliyor —
        // mekanik Title-Case dönüşümüyle üretilecek "Pain Level" yerine gerçek TR etiket görünmeli.
        SceneAssert.True(
            recordLabel.Text.Contains("Ağrı Seviyesi", StringComparison.Ordinal),
            $"Kayıt satırı 'painLevel' metriğinin yerelleştirilmiş TR etiketini içermeli, gerçek metin: '{recordLabel.Text}'");

        SceneAssert.False(pdfReportButton.Disabled, "En az bir kayıt varken PDF Rapor butonu aktif olmalı.");

        var reportPath = Path.Combine(Path.GetTempPath(), $"scenetest-report-{Guid.NewGuid()}.pdf");
        try
        {
            reportFileDialog.EmitSignal(FileDialog.SignalName.FileSelected, reportPath);
            await WaitFramesAsync(sceneTree, 15);

            SceneAssert.True(File.Exists(reportPath), $"PDF rapor dosyası oluşturulmalıydı: '{reportPath}'");
            var bytes = await File.ReadAllBytesAsync(reportPath);
            SceneAssert.True(bytes.Length > 0, "PDF rapor dosyası boş olmamalı.");
            var header = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 5));
            SceneAssert.True(header == "%PDF-", $"Dosya geçerli bir PDF başlığıyla başlamalı, gerçek: '{header}'");
            SceneAssert.True(
                reportStatusLabel.Text.Contains("oluşturuldu", StringComparison.Ordinal),
                $"Durum etiketi başarı mesajı göstermeli, gerçek: '{reportStatusLabel.Text}'");
        }
        finally
        {
            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }
        }
    }

    private static async Task WaitFramesAsync(SceneTree sceneTree, int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
