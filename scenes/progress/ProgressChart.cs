using System;
using System.Collections.Generic;
using Godot;

namespace FreeRehabHub.App.Progress;

// Basit bir çizgi grafiği: 0-1 aralığındaki normalize skorları kronolojik sırayla çizer.
// Godot'ta hazır bir grafik kontrolü olmadığı için _Draw() ile elle çiziliyor — üçüncü parti
// bağımlılık eklemeden (bkz. CLAUDE.md modülerlik/bağımlılık ilkesi).
public partial class ProgressChart : Control
{
    private const float Padding = 24f;
    private const float PointRadius = 4f;
    private const float AxisLineWidth = 2f;
    private const float PlotLineWidth = 2f;

    private static readonly Color AxisColor = new(0.55f, 0.55f, 0.6f);
    private static readonly Color LineColor = new(0.28f, 0.56f, 0.98f);
    private static readonly Color PointColor = new(0.16f, 0.42f, 0.86f);

    private IReadOnlyList<double> _normalizedScores = Array.Empty<double>();

    public void SetValues(IReadOnlyList<double> normalizedScores)
    {
        _normalizedScores = normalizedScores;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var plotLeft = Padding;
        var plotRight = Size.X - Padding;
        var plotTop = Padding;
        var plotBottom = Size.Y - Padding;

        DrawLine(new Vector2(plotLeft, plotTop), new Vector2(plotLeft, plotBottom), AxisColor, AxisLineWidth);
        DrawLine(new Vector2(plotLeft, plotBottom), new Vector2(plotRight, plotBottom), AxisColor, AxisLineWidth);

        if (_normalizedScores.Count == 0)
        {
            return;
        }

        if (_normalizedScores.Count == 1)
        {
            DrawCircle(PlotPoint(0, plotLeft, plotRight, plotTop, plotBottom), PointRadius, PointColor);
            return;
        }

        var points = new Vector2[_normalizedScores.Count];
        for (var index = 0; index < _normalizedScores.Count; index++)
        {
            points[index] = PlotPoint(index, plotLeft, plotRight, plotTop, plotBottom);
        }

        DrawPolyline(points, LineColor, PlotLineWidth, antialiased: true);
        foreach (var point in points)
        {
            DrawCircle(point, PointRadius, PointColor);
        }
    }

    private Vector2 PlotPoint(int index, float plotLeft, float plotRight, float plotTop, float plotBottom)
    {
        var x = _normalizedScores.Count == 1
            ? (plotLeft + plotRight) / 2f
            : plotLeft + ((plotRight - plotLeft) * index / (float)(_normalizedScores.Count - 1));
        var y = plotBottom - ((float)_normalizedScores[index] * (plotBottom - plotTop));
        return new Vector2(x, y);
    }
}
