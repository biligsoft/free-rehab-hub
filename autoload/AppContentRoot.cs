using System.IO;
using Godot;

namespace FreeRehabHub.App.Autoload;

// Godot'un res:// şeması editörde veya `godot --path .` ile çalışırken gerçek proje klasörüne
// eşleniyor (GlobalizePath gerçek bir dosya yolu döndürüyor) — ama export edilmiş bir build'de
// res:// bir .pck içindeki sanal dosya sistemi, bu Godot-bağımsız (raw System.IO kullanan)
// repository'lerin okuduğu klasörler orada gerçek dosya olarak yok. Bu yüzden önce GlobalizePath
// deneniyor, sonuç gerçekten diskte varsa o kullanılıyor; yoksa (export edilmiş build), çalışan
// executable'ın yanındaki aynı-adlı klasöre düşülüyor — paketleme scripti bu klasörleri oraya
// kopyalamalı (bkz. export_presets.cfg exclude_filter, bu klasörler .pck'e dahil edilmiyor).
public static class AppContentRoot
{
    public static string Resolve(string resourceRelativePath)
    {
        var globalized = ProjectSettings.GlobalizePath($"res://{resourceRelativePath}");
        if (Directory.Exists(globalized) || File.Exists(globalized))
        {
            return globalized;
        }

        var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath())!;
        return Path.Combine(executableDirectory, resourceRelativePath);
    }
}
