using System;
using System.Collections.Generic;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.MemoryMatch.Scoring;

public sealed class MemoryMatchScorer
{
    public ModuleResult Score(
        string moduleId,
        int totalPairs,
        int matchedPairs,
        int totalAttempts,
        ModuleContext context)
    {
        // İki bileşen: tamamlama oranı (hedeflenen çiftlerin kaçı gerçekten bulundu — erken
        // çıkışta bu düşük kalır) + verimlilik (bulunan çiftler kaç denemede bulundu, idealde
        // bulunan çift sayısı kadar denemeyle). Sadece totalPairs/totalAttempts kullanmak erken
        // çıkışta (matchedPairs < totalPairs) skoru olması gerekenden yüksek gösterirdi.
        var completionRate = totalPairs <= 0 ? 0.0 : Math.Clamp((double)matchedPairs / totalPairs, 0.0, 1.0);
        var efficiency = totalAttempts <= 0 ? 0.0 : Math.Clamp((double)matchedPairs / totalAttempts, 0.0, 1.0);
        var normalizedScore = (completionRate + efficiency) / 2.0;

        return new ModuleResult
        {
            ModuleId = moduleId,
            PatientId = context.PatientId,
            SessionId = context.SessionId,
            CompletedAt = context.CompletedAt,
            NormalizedScore = normalizedScore,
            Metrics = new Dictionary<string, double>
            {
                ["totalPairs"] = totalPairs,
                ["matchedPairs"] = matchedPairs,
                ["totalAttempts"] = totalAttempts
            }
        };
    }
}
