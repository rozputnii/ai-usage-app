using AiUsage.Features.History;
using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

/// <summary>Synthetic history derived deterministically from the current reading. Includes the F09 gap/reset fixture.</summary>
internal sealed class DemoHistorySource(DemoState state) : IHistorySource
{
    public async Task<HistoryResult> QueryHistoryAsync(HistoryQuery query, CancellationToken cancellationToken)
    {
        await state.DelayAsync(DemoLatency.History, cancellationToken);
        var world = state.World;
        var account = state.Account(query.AccountId);
        var window = account?.AllWindows.FirstOrDefault(w => w.Id == query.WindowId);
        if (account is null || window is null)
            return new(HistoryOutcome.Empty, [], world.HistoryEnabled, false);
        if (account.HistoryDeleted)
            return new(HistoryOutcome.DataDeleted, [], world.HistoryEnabled, false);

        var t0 = DemoScenarioCatalog.T0;
        if (window.Id == "cl-a-w1" && query.Preset == HistoryPreset.Hours24)
            return new(HistoryOutcome.Points, DemoScenarioCatalog.F09Points(window.Remaining ?? 8), world.HistoryEnabled, false);
        if (window.State is not (ValueState.Known or ValueState.Exhausted) && window.ProviderState is not (ValueState.Known or ValueState.Exhausted))
            return new(HistoryOutcome.NoComparablePercentage, [], world.HistoryEnabled, false);

        var spanHours = (query.To - query.From).TotalHours;
        if (query.From >= t0 || spanHours <= 0)
            return new(HistoryOutcome.Empty, [], world.HistoryEnabled, false);
        var stepHours = spanHours > 24 * 120 ? 24 * 7 : spanHours > 72 ? 24 : 1;
        var count = (int)Math.Min(60, Math.Floor(spanHours / stepHours));
        var end = query.To > t0 ? t0 : query.To;
        var cycle = Math.Max((window.DurationSeconds ?? 18000) / 3600, stepHours * 2);
        var current = window.Remaining ?? window.ProviderRemaining ?? 50;
        var points = new List<HistoryPoint>();
        var segment = 0;
        for (var i = 0; i <= count; i++)
        {
            var hoursBack = (count - i) * stepHours;
            var at = end - TimeSpan.FromHours(hoursBack);
            if (i > 0 && Math.Floor(hoursBack / cycle) != Math.Floor((hoursBack + stepHours) / cycle))
                segment++;
            if ((i * 7 + 3) % 11 == 5 && i > 0 && i < count)
            {
                points.Add(new(at, null, HistoryCoverage.Gap, "s" + segment));
                continue;
            }
            var remaining = i == count ? current : Math.Clamp(Math.Round(current + (hoursBack % cycle) / cycle * (100 - current)), 0, 100);
            points.Add(new(at, remaining, HistoryCoverage.Observed, "s" + segment));
        }
        // Samples older than the retention window were pruned, so the range is only partially covered.
        var partial = RetentionDays(world.Retention) is { } days && query.From < t0 - TimeSpan.FromDays(days);
        if (partial)
            points.RemoveAll(p => p.At < t0 - TimeSpan.FromDays(RetentionDays(world.Retention)!.Value));
        return new(points.Count(p => p.Coverage == HistoryCoverage.Observed) == 0 ? HistoryOutcome.Empty : HistoryOutcome.Points, points, world.HistoryEnabled, partial);
    }

    private static int? RetentionDays(HistoryRetention retention) => retention switch
    {
        HistoryRetention.Days30 => 30,
        HistoryRetention.Days90 => 90,
        HistoryRetention.Year1 => 365,
        _ => null,
    };
}
