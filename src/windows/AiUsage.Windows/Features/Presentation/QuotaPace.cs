using AiUsage.Features.Accounts;

namespace AiUsage.Features.Presentation;

public enum PaceKind { None, ShortWindow, DailyShare }

/// <summary>
/// Pace advice for one window. <paramref name="MarkPercent"/> is the remaining percentage that keeps an even daily pace at
/// the end of today; <paramref name="TodayAvailable"/> is what may still be used today (negative when overspent) and
/// <paramref name="TodayShare"/> is today's even share. Advice only: it never changes the reading or notification rules.
/// </summary>
public sealed record PaceAdvice(PaceKind Kind, ValueTone Tone, double? MarkPercent = null, double TodayAvailable = 0, double TodayShare = 0)
{
    public static PaceAdvice None { get; } = new(PaceKind.None, ValueTone.Normal);
}

/// <summary>
/// D-181 pace coloring. Windows shorter than a day turn critical at or below <see cref="ShortWindowCriticalPercent"/>
/// remaining, with no time pacing. Longer windows are split into even local-calendar-day shares from the window start
/// to its reset: what was left unused earlier carries into today and overspending shrinks it. Today's share is
/// critical once used up and a warning when less than <see cref="WarningShareFraction"/> of it remains. Stale,
/// unknown and unlimited readings get no advice, so they never look current.
/// </summary>
public static class QuotaPace
{
    public const double ShortWindowCriticalPercent = 20;
    public const double WarningShareFraction = 0.3;
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    public static PaceAdvice Evaluate(WindowItem window, Freshness freshness, DateTimeOffset now, TimeZoneInfo zone)
    {
        if (freshness == Freshness.Stale || window.ValueState != ValueState.Known || window.RemainingPercent is not { } reported
            || window.DurationSeconds is not { } seconds || seconds <= 0)
            return PaceAdvice.None;
        var remaining = Math.Clamp(reported, 0, 100);
        var duration = TimeSpan.FromSeconds(seconds);
        if (duration < OneDay)
            return new(PaceKind.ShortWindow, remaining <= ShortWindowCriticalPercent ? ValueTone.Critical : ValueTone.Ok);
        if (window.ResetsAt is not { } reset || reset <= now)
            return PaceAdvice.None;

        var start = reset - duration;
        var moment = now < start ? start : now;
        var local = TimeZoneInfo.ConvertTime(moment, zone).DateTime.Date;
        var todayStart = Max(LocalMidnight(local, zone), start);
        var todayEnd = Min(LocalMidnight(local.AddDays(1), zone), reset);
        double Target(DateTimeOffset at) => Math.Clamp(100 * (reset - at).TotalSeconds / duration.TotalSeconds, 0, 100);

        var mark = Target(todayEnd);
        var share = Target(todayStart) - mark;
        var available = remaining - mark;
        var tone = available <= 0 ? ValueTone.Critical : available < WarningShareFraction * share ? ValueTone.Warning : ValueTone.Ok;
        return new(PaceKind.DailyShare, tone, mark, available, share);
    }

    private static DateTimeOffset LocalMidnight(DateTime date, TimeZoneInfo zone)
    {
        var midnight = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        // A skipped local midnight (DST starting at 00:00) moves to the first valid instant after it.
        while (zone.IsInvalidTime(midnight))
            midnight = midnight.AddMinutes(30);
        return new DateTimeOffset(midnight, zone.GetUtcOffset(midnight));
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
}
