using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>F09 history: segments never join gaps or resets; range validation, disabled collection and empty outcomes.</summary>
public sealed class HistoryTests
{
    private static async Task<(TestHost Host, HistoryViewModel History)> OpenF09()
    {
        var host = new TestHost();
        var history = host.HistoryPage();
        host.Navigation.Navigate(new(PageKey.History, "demo-claude-1", WindowId: "cl-a-w1"));
        await history.LoadAsync();
        return (host, history);
    }

    [Fact]
    public async Task F09GapAndResetSplitTheLineIntoSeparateSegments()
    {
        var (host, history) = await OpenF09();
        using var _ = host;
        Assert.True(history.HasChart);
        Assert.Equal("Research · Workspace A · Usage limits · Session window", history.ChartTitle);
        var chart = history.Chart;
        Assert.Equal(4, chart.ObservedCount);
        // 08:00 alone (gap at 09:00), 10:00 alone before the reset, then 11:00–12:00 in segment b.
        Assert.Equal([1, 1, 2], chart.Segments.Select(s => s.Points.Count));
        Assert.Single(chart.Gaps);
        Assert.Single(chart.Resets);
        Assert.Equal("Remaining percent over time, 4 observed points, 1 gaps, 1 resets", history.ChartAccessibleName);
        Assert.Equal("Sep 15, 8:00 AM · 30 % left", chart.Markers[0].Tooltip);
        history.ShowTooltipCommand.Execute(chart.Markers[2]);
        Assert.Equal("Sep 15, 11:00 AM · 100 % left", history.Tooltip);
        history.HideTooltipCommand.Execute(null);
        Assert.False(history.HasTooltip);
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
        var chart = ChartModel.Build(points, UsageDisplay.Used, host.Format);
        Assert.Equal([2, 1], chart.Segments.Select(s => s.Points.Count));
        Assert.Empty(chart.Resets);
        Assert.Equal(0.5, chart.Segments[0].Points[0].Y, 3); // 50 % used plots at the middle
    }

    [Fact]
    public async Task CustomRangeValidatesFormatOrderAndFutureSeparately()
    {
        var (host, history) = await OpenF09();
        using var _ = host;
        await history.SelectPresetCommand.ExecuteAsync(history.Presets.Single(p => p.Preset == HistoryPreset.Custom));
        Assert.True(history.IsCustom);
        history.CustomFrom = "15/09/2026";
        await history.ApplyCustomCommand.ExecuteAsync(null);
        Assert.Equal("Use the format YYYY-MM-DD HH:MM for both fields.", history.RangeError);
        history.CustomFrom = "2026-09-15 10:00";
        history.CustomTo = "2026-09-15 09:00";
        await history.ApplyCustomCommand.ExecuteAsync(null);
        Assert.Equal("“From” must be earlier than “To”.", history.RangeError);
        history.CustomTo = "2026-09-16 09:00";
        await history.ApplyCustomCommand.ExecuteAsync(null);
        Assert.Equal("“To” is in the future; no samples can exist there.", history.RangeError);
        history.CustomFrom = "2026-09-13 00:00";
        history.CustomTo = "2026-09-15 12:00";
        await history.ApplyCustomCommand.ExecuteAsync(null);
        Assert.False(history.HasRangeError);
        Assert.True(history.HasChart);
    }

    [Fact]
    public async Task DisabledCollectionKeepsExistingSamplesReadableWithBanner()
    {
        var (host, history) = await OpenF09();
        using var _ = host;
        await host.Preferences.SetPreferenceAsync(new(AiUsage.Features.Settings.PreferenceKey.HistoryEnabled, false), CancellationToken.None);
        await history.LoadAsync();
        Assert.True(history.CollectionDisabled);
        Assert.True(history.HasChart);
        history.OpenDataPrivacyCommand.Execute(null);
        Assert.Equal(new NavigationRequest(PageKey.Settings, Tab: SettingsTab.DataPrivacy), host.Navigation.Last);
    }

    [Fact]
    public async Task WindowWithoutComparablePercentageShowsEmptyReason()
    {
        using var host = new TestHost();
        var history = host.HistoryPage();
        host.Navigation.Navigate(new(PageKey.History, "demo-copilot-1"));
        await history.LoadAsync();
        Assert.True(history.IsEmpty);
        Assert.Equal("This window has no comparable percentage (unknown, unlimited or unavailable).", history.EmptyReason);
        Assert.False(history.HasChart);
    }

    [Fact]
    public async Task LoadingShowsSkeletonAndPresetChangesQuerySyntheticResults()
    {
        using var host = new TestHost(autoDelays: false);
        var history = host.HistoryPage();
        history.SelectedAccount = history.AccountOptions.Single(a => a.Id == "demo-codex-1");
        Assert.True(history.IsLoading);
        await host.Delays.Drain();
        Assert.False(history.IsLoading);
        Assert.True(history.HasChart);
        var daily = history.Chart.ObservedCount;
        var select = history.SelectPresetCommand.ExecuteAsync(history.Presets.Single(p => p.Preset == HistoryPreset.Days30));
        Assert.True(history.IsLoading);
        await host.Delays.Drain();
        await select;
        Assert.NotEqual(daily, history.Chart.ObservedCount);
        Assert.Contains("Personal · Usage limits · 5-hour window", history.ChartTitle);
    }

    [Fact]
    public async Task EmptyRangeBeforeAnySampleShowsEmptyState()
    {
        using var host = new TestHost();
        var history = host.HistoryPage();
        history.SelectedAccount = history.AccountOptions.Single(a => a.Id == "demo-codex-1");
        await history.SelectPresetCommand.ExecuteAsync(history.Presets.Single(p => p.Preset == HistoryPreset.Custom));
        history.CustomFrom = "2026-09-15 12:10";
        history.CustomTo = "2026-09-15 12:50";
        await history.ApplyCustomCommand.ExecuteAsync(null);
        Assert.True(history.IsEmpty);
        Assert.Equal("The selected range has no samples.", history.EmptyReason);
    }
}
