using System.Threading.Tasks;
using Godot;

namespace FreeRehabHub.SceneTests;

// GUT (Godot Unit Test) sadece GDScript içindir ("write tests for your gdscript in gdscript",
// bkz. resmi README) — bu proje tamamen C# olduğu için kullanılmadı (kullanıcıyla konuşuldu).
// Bu arayüz, sahne/controller davranışını gerçek Godot çalışma zamanında (headless) doğrulayan
// testler için minimal bir sözleşme. SceneTestRunner, bunu implemente eden tüm tipleri
// reflection'la keşfedip çalıştırıyor.
public interface ISceneTest
{
    string Name { get; }
    Task RunAsync(SceneTree sceneTree);
}
