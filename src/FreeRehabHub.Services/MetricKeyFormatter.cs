using System.Text;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Services;

// ModuleResult.Metrics/ProgressRecord.Metrics anahtarları camelCase (ör. "completedReps") —
// sonuç/ilerleme/rapor ekranlarının üçünde de aynı okunabilir hale çevriliyor. Önce modülün
// manifest'indeki MetricLabels sözlüğüne (F8.28) bakılır; orada karşılığı olmayan bir anahtar
// (manifest null olması dahil) mekanik camelCase→Title Case dönüşümüne düşer — hiçbir zaman
// boş/çökük görünmemesi için bilinçli bir zarif geri düşme (bkz. TtsAutoload'daki aynı prensip).
public static class MetricKeyFormatter
{
    private const string EnglishLocale = "en";

    public static string Humanize(string key, ModuleManifest? manifest, string locale)
    {
        if (manifest is not null && manifest.MetricLabels.TryGetValue(key, out var label))
        {
            return locale == EnglishLocale ? label.En : label.Tr;
        }

        return MechanicalHumanize(key);
    }

    private static string MechanicalHumanize(string key)
    {
        var builder = new StringBuilder();
        foreach (var character in key)
        {
            if (char.IsUpper(character) && builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(builder.Length == 0 ? char.ToUpperInvariant(character) : character);
        }

        return builder.ToString();
    }
}
