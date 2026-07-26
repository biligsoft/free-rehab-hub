using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FreeRehabHub.App.Autoload;
using Godot;

namespace FreeRehabHub.SceneTests;

// Kalıcı bir autoload (bkz. project.godot [autoload]) — ama normal uygulama çalışırken tamamen
// etkisiz: sadece FREEREHABHUB_RUN_SCENE_TESTS ortam değişkeni set edilmişse devreye giriyor.
// Bu sayede project.godot'a hiç dokunmadan (geçici autoload ekleme/çıkarma gerekmeden) normal
// `godot --headless --path .` çalıştırması hiç etkilenmiyor.
//
// Çalıştırma: FREEREHABHUB_RUN_SCENE_TESTS=1 godot --headless --path .
// Bir Node'un ChangeSceneToFile çağırdığı testlerde ana sahnenin kendisi değiştiği için, bu
// runner'ın BİR AUTOLOAD olması zorunlu — ana sahne olsaydı testin kendi sahne geçişi runner'ı
// da yok ederdi (bu, ilk denemede gerçekten yaşanan bir bug'dı).
// Çıkış kodu: tüm testler geçerse 0, en az biri başarısız olursa 1 — CI bunu kullanıyor
// (bkz. .github/workflows/ci.yml).
public partial class SceneTestRunner : Node
{
    private const string RunTestsEnvironmentVariable = "FREEREHABHUB_RUN_SCENE_TESTS";

    public override async void _Ready()
    {
        if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(RunTestsEnvironmentVariable)))
        {
            return;
        }

        string? testDatabasePath = null;

        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var appServices = GetNode<AppServices>("/root/AppServices");
            testDatabasePath = Path.Combine(Path.GetTempPath(), $"freerehabhub-scenetests-{Guid.NewGuid()}.db");
            appServices.Unlock("scene-test-password", testDatabasePath);

            var tests = DiscoverTests();
            GD.Print($"SCENE-TESTS: {tests.Count} test bulundu.");

            var failures = new List<string>();
            foreach (var test in tests)
            {
                GD.Print($"SCENE-TESTS: [ÇALIŞIYOR] {test.Name}");
                try
                {
                    await test.RunAsync(GetTree());
                    GD.Print($"SCENE-TESTS: [GEÇTİ] {test.Name}");
                }
                catch (Exception exception)
                {
                    failures.Add(test.Name);
                    GD.PrintErr($"SCENE-TESTS: [BAŞARISIZ] {test.Name} — {exception}");
                }
            }

            GD.Print($"SCENE-TESTS: SONUÇ {tests.Count - failures.Count}/{tests.Count} geçti.");
            if (failures.Count > 0)
            {
                GD.PrintErr($"SCENE-TESTS: Başarısız olanlar: {string.Join(", ", failures)}");
            }

            GetTree().Quit(failures.Count > 0 ? 1 : 0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"SCENE-TESTS: Runner hatası: {exception}");
            GetTree().Quit(1);
        }
        finally
        {
            if (testDatabasePath is not null && File.Exists(testDatabasePath))
            {
                File.Delete(testDatabasePath);
            }
        }
    }

    private static List<ISceneTest> DiscoverTests()
    {
        var testInterfaceType = typeof(ISceneTest);
        return Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => testInterfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => (ISceneTest)Activator.CreateInstance(type)!)
            .OrderBy(test => test.Name, StringComparer.Ordinal)
            .ToList();
    }
}
