using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public sealed record PeriodBounds(DateTimeOffset Start, DateTimeOffset End, ValueOrigin StartOrigin, ValueOrigin EndOrigin);
public sealed record WorkWeights(decimal Total, decimal Remaining, decimal Elapsed, decimal Today);

public static class WorkCalendar
{
    public static DateOnly Date(DateTimeOffset instant, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    public static DateTimeOffset Midnight(DateOnly date, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // Some zones advance at midnight. The first valid instant is that day's boundary.
        while (zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new(local, offset);
    }

    public static WorkWeights Weights(PeriodBounds period, DateTimeOffset now, TimeZoneInfo zone, IReadOnlySet<DayOfWeek>? workDays = null)
    {
        ArgumentNullException.ThrowIfNull(period);
        var today = Date(now, zone);
        decimal total = 0, remaining = 0, elapsed = 0, current = 0;
        var last = Date(period.End, zone);
        for (var date = Date(period.Start, zone); date <= last; date = date.AddDays(1))
        {
            bool work = workDays?.Contains(date.DayOfWeek) ?? date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
            if (!work) continue;
            var start = Midnight(date, zone);
            var end = Midnight(date.AddDays(1), zone);
            var a = start > period.Start ? start : period.Start;
            var b = end < period.End ? end : period.End;
            decimal weight = b > a && end > start ? (decimal)(b - a).Ticks / (end - start).Ticks : 0;
            total += weight;
            if (date >= today) remaining += weight;
            if (date <= today) elapsed += weight;
            if (date == today) current = weight;
        }
        return new(total, remaining, elapsed, current);
    }
}

public static class PeriodResolver
{
    public static PeriodBounds? Resolve(LimitFacts facts, DateTimeOffset now, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Reset is { Meaning: ResetMeaning.Replenish } reset)
        {
            if (facts.PeriodStart is { } start && facts.PeriodStartOrigin is { } origin)
                return start < reset.At ? new(start, reset.At, origin, reset.Origin) : null;
            if (facts.Duration is { } duration && duration > TimeSpan.Zero)
                return new(reset.At - duration, reset.At, ValueOrigin.Derived, reset.Origin);
            return facts.IsMonthly ? new(reset.At.ToUniversalTime().AddMonths(-1), reset.At, ValueOrigin.Assumed, reset.Origin) : null;
        }
        var date = WorkCalendar.Date(now, zone);
        var first = new DateOnly(date.Year, date.Month, 1);
        return new(WorkCalendar.Midnight(first, zone), WorkCalendar.Midnight(first.AddMonths(1), zone), ValueOrigin.Assumed, ValueOrigin.Assumed);
    }
}
