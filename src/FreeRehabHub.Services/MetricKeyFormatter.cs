using System.Text;

namespace FreeRehabHub.Services;

// ModuleResult.Metrics/ProgressRecord.Metrics anahtarları camelCase (ör. "completedReps") —
// sonuç/ilerleme/rapor ekranlarının üçünde de aynı okunabilir hale çevriliyor ("Completed Reps").
public static class MetricKeyFormatter
{
    public static string Humanize(string key)
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
