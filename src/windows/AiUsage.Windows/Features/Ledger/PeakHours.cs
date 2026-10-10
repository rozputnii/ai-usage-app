namespace AiUsage.Features.Ledger;

/// <summary>The peak hint in the title row and the tray header: its chip text, its tooltip and its spoken name.</summary>
internal sealed record PeakHint(string Text, IReadOnlyList<string> Tip, string AccessibleName);

/// <summary>
/// The shared peak hint: weekdays 05:00 to 11:00 Pacific, the only window a provider has announced. It is a schedule for every
/// account, not account data; the evidence and its monthly review are in docs/providers/peak-hours.md.
/// </summary>
internal static class PeakHours
{
    private static readonly TimeSpan Start = TimeSpan.FromHours(5), End = TimeSpan.FromHours(11);
    private static readonly TimeZoneInfo? Pacific =
        TimeZoneInfo.TryFindSystemTimeZoneById("America/Los_Angeles", out var iana) ? iana
        : TimeZoneInfo.TryFindSystemTimeZoneById("Pacific Standard Time", out var windows) ? windows : null;

    /// <summary>The current peak window in <paramref name="now"/>'s offset, or null outside it or without a Pacific zone.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End)? Window(DateTimeOffset now)
    {
        if (Pacific is not { } zone)
            return null;
        var local = TimeZoneInfo.ConvertTime(now, zone);
        if (local.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || local.TimeOfDay < Start || local.TimeOfDay >= End)
            return null;
        // 05:00 and 11:00 Pacific never fall in a daylight-saving gap, which moves 02:00.
        DateTimeOffset At(TimeSpan time) => new DateTimeOffset(local.Date + time, zone.GetUtcOffset(local.Date + time)).ToOffset(now.Offset);
        return (At(Start), At(End));
    }

    /// <summary>The hint for <paramref name="now"/> in the owner's local time, or null outside the window.</summary>
    public static PeakHint? Hint(DateTimeOffset now)
    {
        if (Window(now) is not { } window)
            return null;
        var (start, end) = window;
        var until = LedgerFormat.Clock(end);
        return new("Peak · until " + until,
            ["Peak hours", "Weekdays 05:00–11:00 Pacific (" + LedgerFormat.Clock(start) + "–" + until + " here)",
             "Requests may be slower or use more of a limit", "An announced schedule, not your account's data"],
            "Peak hours until " + until);
    }
}
