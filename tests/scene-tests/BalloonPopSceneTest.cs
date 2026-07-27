using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeRehabHub.App.Autoload;
using FreeRehabHub.Core;
using FreeRehabHub.Domain;
using FreeRehabHub.Modules.BalloonPop;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.SceneTests;

// F8.35: oyunlar.md'deki "Balon Patlat" tasarımının gerçek modül implementasyonu — bu proje için
// İLK KEZ kamera-tabanlı bir IPoseAwareModule'ün gerçek UI akışında sentetik PoseFrame'lerle
// uçtan uca doğrulandığı sahne testi (arm-raise'in kendisi bunu hiç yapmamıştı, bkz.
// docs/PROGRESS.md). Gerçek kamera bu ortamda çalışmadığı için (bkz. CLAUDE.md § 14,
// motion_bridge) OnPoseFrame'e doğrudan bilinen açılarda sentetik kare besleniyor.
public sealed class BalloonPopSceneTest : ISceneTest
{
    private const string ModuleLibraryScenePath = "res://scenes/module-library/ModuleLibraryPanel.tscn";
    private const string BalloonPopDisplayName = "Balon Patlat";

    // required=60,75,90,105,120,135 (BalloonPopController'daki ilerleme) — ilk 4 tur bunun
    // üzerine çıkan bir tepe açısıyla "patlatılıyor", son 2 tur bilerek hedefin altında bırakılıp
    // "kaçırılıyor" — deterministik bir kısmi skor bekleniyor.
    private static readonly double[] PeakAngles = { 70, 85, 100, 115, 110, 125 };
    private const int ExpectedPoppedCount = 4;
    private const int TotalBalloons = 6;

    public string Name => "BalloonPop: sentetik poz kareleriyle omuz fleksiyonu oynatışı ve skorlama";

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
            FullName = "Balon Patlat Testi Hastası",
            DateOfBirth = new DateOnly(1975, 4, 12),
            CreatedAt = DateTime.UtcNow
        };
        await appServices.PatientService!.AddAsync(patient, therapist.Id);
        sessionContext.SetActivePatient(patient);

        sceneTree.ChangeSceneToFile(ModuleLibraryScenePath);
        await WaitFramesAsync(sceneTree, 5);

        var root = sceneTree.Root;
        var moduleList = root.GetNode<ItemList>("ModuleLibraryPanel/Card/Content/ModuleItemList");
        var startButton = root.GetNode<Button>("ModuleLibraryPanel/Card/Content/Actions/StartButton");

        var balloonPopIndex = Enumerable.Range(0, moduleList.ItemCount)
            .FirstOrDefault(i => moduleList.GetItemText(i).Contains(BalloonPopDisplayName, StringComparison.Ordinal), -1);
        SceneAssert.True(balloonPopIndex >= 0, "Modül kütüphanesinde Balon Patlat listelenmeli.");

        moduleList.Select(balloonPopIndex);
        moduleList.EmitSignal(ItemList.SignalName.ItemSelected, balloonPopIndex);
        await WaitFramesAsync(sceneTree, 1);

        startButton.EmitSignal(Button.SignalName.Pressed);
        await WaitFramesAsync(sceneTree, 5);

        SceneAssert.True(root.HasNode("ModuleHost"), "Balon Patlat seçilince ModuleHost'a geçilmeli.");

        var balloonPop = root.GetNode<BalloonPopController>("ModuleHost/Layout/ModuleContainer/BalloonPop");

        foreach (var peakAngle in PeakAngles)
        {
            balloonPop.OnPoseFrame(BuildFrame(peakAngle));
            await WaitFramesAsync(sceneTree, 1);
            balloonPop.OnPoseFrame(BuildFrame(0));
            await WaitFramesAsync(sceneTree, 1);
        }

        await WaitFramesAsync(sceneTree, 10);

        SceneAssert.True(root.HasNode("ModuleResultPanel"), "6 tur sonrası ModuleResultPanel'e geçilmeli.");

        var lastResult = sessionContext.LastModuleResult;
        SceneAssert.NotNull(lastResult, "LastModuleResult set edilmiş olmalı.");
        SceneAssert.Equal("com.freerehabhub.balloon-pop", lastResult!.ModuleId, "Sonuç doğru modüle ait olmalı.");
        SceneAssert.Equal((double)TotalBalloons, lastResult.Metrics["totalBalloons"], "6 balon tamamlanmalı.");
        SceneAssert.Equal(
            (double)ExpectedPoppedCount, lastResult.Metrics["poppedCount"],
            $"{ExpectedPoppedCount} balon patlatılmış sayılmalı (son 2 tur bilerek hedefin altında bırakıldı).");

        var history = await appServices.ProgressRecordService!.GetHistoryByPatientIdAsync(patient.Id);
        SceneAssert.Equal(1, history.Count, "Completed tam bir kez tetiklenmeli — tam olarak 1 ProgressRecord kaydedilmeli.");
    }

    // Sentetik bir omuz fleksiyon karesi üretir: kalça-omuz ekseni sabit tutulup, dirsek
    // istenen açıya (derece) denk gelecek şekilde yerleştiriliyor (bkz. ShoulderFlexionCalculator
    // — 0°=kol yanda, 90°=kol yatay, 180°=kol tam kaldırılmış).
    private static PoseFrame BuildFrame(double angleDegrees)
    {
        var angleRad = angleDegrees * Math.PI / 180.0;
        var shoulder = new PosePoint { X = 0f, Y = 0f, Z = 0f };
        var hip = new PosePoint { X = 0f, Y = -1f, Z = 0f };
        var elbow = new PosePoint
        {
            X = (float)Math.Sin(angleRad),
            Y = (float)-Math.Cos(angleRad),
            Z = 0f
        };

        return new PoseFrame
        {
            CapturedAt = DateTime.UtcNow,
            Poses = new List<DetectedPose>
            {
                new()
                {
                    Landmarks = new List<PoseLandmark>
                    {
                        new() { Type = PoseLandmarkType.RightHip, World = hip, Visibility = 1f, Presence = 1f },
                        new() { Type = PoseLandmarkType.RightShoulder, World = shoulder, Visibility = 1f, Presence = 1f },
                        new() { Type = PoseLandmarkType.RightElbow, World = elbow, Visibility = 1f, Presence = 1f }
                    }
                }
            }
        };
    }

    private static async Task WaitFramesAsync(SceneTree sceneTree, int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
        {
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
