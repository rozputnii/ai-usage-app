using AiUsage.Features.Presentation;

namespace AiUsage.Features.History;

/// <summary>Normalized chart point: X and Y in 0..1, Y measured from the top so renderers map directly to pixels.</summary>
public readonly record struct ChartPoint(double X, double Y);

public sealed record ChartSegment(IReadOnlyList<ChartPoint> Points);

public sealed record ChartGap(double X, double Width);

/// <summary>Sparkline geometry derived from history points. Never joins across gaps or reset segments.</summary>
public sealed record ChartModel(
    IReadOnlyList<ChartSegment> Segments,
    IReadOnlyList<ChartGap> Gaps,
    IReadOnlyList<double> Resets,
    int ObservedCount)
{
    public static ChartModel Empty { get; } = new([], [], [], 0);

    internal static ChartModel Build(IReadOnlyList<HistoryPoint> points, UsageDisplay display)
    {
        if (points.Count == 0)
            return Empty;
        var start = points[0].At;
        var end = points[^1].At;
        var span = (end - start).TotalSeconds;
        double X(DateTimeOffset at) => span <= 0 ? 0.5 : (at - start).TotalSeconds / span;
        double Shown(double remaining) => display == UsageDisplay.Used ? 100 - remaining : remaining;
        double Y(double remaining) => 1 - Shown(remaining) / 100;

        var segments = new List<ChartSegment>();
        var resets = new List<double>();
        List<ChartPoint>? current = null;
        string? currentSegment = null;
        foreach (var point in points)
        {
            if (point.Coverage != HistoryCoverage.Observed || point.RemainingPercent is null)
            {
                // A gap ends the drawn line but not the reset segment.
                current = null;
                continue;
            }
            if (current is null || currentSegment != point.SegmentId)
            {
                if (currentSegment is not null && currentSegment != point.SegmentId)
                    resets.Add(X(point.At));
                current = [];
                currentSegment = point.SegmentId;
                segments.Add(new ChartSegment(current));
            }
            current.Add(new ChartPoint(X(point.At), Y(point.RemainingPercent.Value)));
        }

        var gaps = new List<ChartGap>();
        for (var i = 0; i < points.Count; i++)
            if (points[i].Coverage == HistoryCoverage.Gap && i > 0 && i < points.Count - 1)
                gaps.Add(new ChartGap(X(points[i - 1].At), X(points[i + 1].At) - X(points[i - 1].At)));

        return new ChartModel(segments, gaps, resets, segments.Sum(s => s.Points.Count));
    }
}
