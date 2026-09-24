using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>F09 history charts (the account detail sparkline): segments never join gaps or resets.</summary>
public sealed class HistoryTests
{
    [Fact]
    public async Task F09GapAndResetSplitTheLineIntoSeparateSegments()
    {
        using var host = new TestHost();
        var now = host.Clock.UtcNow;
        var result = await host.History.QueryHistoryAsync(new("demo-claude-1", null, null, "cl-a-w1", now - TimeSpan.FromHours(24), now,
            HistoryResolution.Auto, HistoryPreset.Hours24), TestContext.Current.CancellationToken);
        var chart = ChartModel.Build(result.Points, UsageDisplay.Remaining);
        Assert.Equal(4, chart.ObservedCount);
        // 08:00 alone (gap at 09:00), 10:00 alone before the reset, then 11:00–12:00 in segment b.
        Assert.Equal([1, 1, 2], chart.Segments.Select(s => s.Points.Count));
        Assert.Single(chart.Gaps);
        Assert.Single(chart.Resets);
    }

    [Fact]
    public void ChartNeverJoinsPointsAcrossAGapEvenInTheSameSegment()
    {
        using var host = new TestHost();
        var t0 = host.Clock.UtcNow;
        HistoryPoint[] points =
        [
            new(t0 - TimeSpan.FromHours(3), 50, HistoryCoverage.Observed, "a"),
            new(t0 - TimeSpan.FromHours(2), 45, HistoryCoverage.Observed, "a"),
            new(t0 - TimeSpan.FromHours(1), null, HistoryCoverage.Gap, "a"),
            new(t0, 40, HistoryCoverage.Observed, "a"),
        ];
        var chart = ChartModel.Build(points, UsageDisplay.Used);
        Assert.Equal([2, 1], chart.Segments.Select(s => s.Points.Count));
        Assert.Empty(chart.Resets);
        Assert.Equal(0.5, chart.Segments[0].Points[0].Y, 3); // 50 % used plots at the middle
    }
}
