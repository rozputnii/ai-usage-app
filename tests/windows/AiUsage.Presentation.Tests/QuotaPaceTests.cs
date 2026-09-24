using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>D-181 pace advice: daily shares for windows of a day or longer, a fixed floor for shorter windows.</summary>
public sealed class QuotaPaceTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly double Week = TimeSpan.FromDays(7).TotalSeconds;

    // A weekly window from Thursday 11 Sept 09:00 UTC to Thursday 18 Sept 09:00 UTC.
    private static readonly DateTimeOffset Reset = new(2026, 9, 18, 9, 0, 0, TimeSpan.Zero);

    private static WindowItem Window(double? remaining, double? seconds, DateTimeOffset? reset, ValueState state = ValueState.Known) =>
        new("w", "Weekly window", remaining, remaining is null ? null : 100 - remaining, state, null, seconds, reset, false);

    private static PaceAdvice At(DateTimeOffset now, double remaining) => QuotaPace.Evaluate(Window(remaining, Week, Reset), Freshness.Fresh, now, Utc);

    [Theory]
    [InlineData(20, ValueTone.Critical)]
    [InlineData(8, ValueTone.Critical)]
    [InlineData(21, ValueTone.Ok)]
    [InlineData(95, ValueTone.Ok)]
    public void WindowsShorterThanADayUseOnlyTheTwentyPercentFloor(double remaining, ValueTone tone)
    {
        var advice = QuotaPace.Evaluate(Window(remaining, TimeSpan.FromHours(5).TotalSeconds, Reset), Freshness.Fresh, Reset.AddHours(-1), Utc);
        Assert.Equal((PaceKind.ShortWindow, tone, (double?)null), (advice.Kind, advice.Tone, advice.MarkPercent));
    }

    [Fact]
    public void AFullDayGetsAnEvenSeventhAndTheMarkIsTheShareLeftAtMidnight()
    {
        // Monday 15 Sept 12:00: at midnight 57 of 168 hours remain, so keep 33.9 %; Monday's share is 24/168 = 14.3 %.
        var advice = At(new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero), 45);
        Assert.Equal(PaceKind.DailyShare, advice.Kind);
        Assert.Equal(100 * 57 / 168.0, advice.MarkPercent!.Value, 6);
        Assert.Equal(100 * 24 / 168.0, advice.TodayShare, 6);
        Assert.Equal(45 - 100 * 57 / 168.0, advice.TodayAvailable, 6);
        Assert.Equal(ValueTone.Ok, advice.Tone);
    }

    [Theory]
    [InlineData(48.2, ValueTone.Ok)]      // 14.3 % available: the whole share is still there
    [InlineData(37.0, ValueTone.Warning)] // 3.1 % available: less than 30 % of today's share
    [InlineData(33.9, ValueTone.Critical)] // at the mark: today's share is used
    [InlineData(30.0, ValueTone.Critical)] // below the mark: overspent
    public void TodayTurnsOrangeNearTheEndOfItsShareAndRedOnceItIsUsed(double remaining, ValueTone tone) =>
        Assert.Equal(tone, At(new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero), remaining).Tone);

    [Fact]
    public void UnusedEarlierDaysCarryIntoTodayAndOverspendingShrinksIt()
    {
        var monday = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);
        var fresh = At(monday, 90);
        Assert.Equal(ValueTone.Ok, fresh.Tone);
        Assert.True(fresh.TodayAvailable > fresh.TodayShare, "Nothing used since Thursday leaves more than one day's share for today.");
        var heavy = At(monday, 37);
        Assert.Equal(ValueTone.Warning, heavy.Tone);
        Assert.True(heavy.TodayAvailable < heavy.TodayShare, "Overspending earlier leaves less than one even share today.");
    }

    [Fact]
    public void PartialFirstAndLastDaysGetProportionalShares()
    {
        // Thursday 11 Sept from 09:00: 15 hours until midnight.
        var first = At(new(2026, 9, 11, 10, 0, 0, TimeSpan.Zero), 100);
        Assert.Equal(100 * 15 / 168.0, first.TodayShare, 6);
        // Thursday 18 Sept before the 09:00 reset: the mark is zero, so everything left may be used today.
        var last = At(new(2026, 9, 18, 7, 0, 0, TimeSpan.Zero), 10);
        Assert.Equal((0d, ValueTone.Ok), (last.MarkPercent!.Value, last.Tone));
        Assert.Equal(10, last.TodayAvailable, 6);
    }

    [Fact]
    public void DaysFollowTheDisplayTimeZone()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("UTC+3", TimeSpan.FromHours(3), "UTC+3", "UTC+3");
        // 22:00 UTC Monday is already Tuesday 01:00 at UTC+3, whose midnight is Tuesday 21:00 UTC.
        var advice = QuotaPace.Evaluate(Window(60, Week, Reset), Freshness.Fresh, new(2026, 9, 15, 22, 0, 0, TimeSpan.Zero), zone);
        Assert.Equal(100 * 36 / 168.0, advice.MarkPercent!.Value, 6);
    }

    [Fact]
    public void StaleUnknownUnlimitedAndUntimedReadingsGetNoAdvice()
    {
        var now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(PaceKind.None, QuotaPace.Evaluate(Window(45, Week, Reset), Freshness.Stale, now, Utc).Kind);
        Assert.Equal(PaceKind.None, QuotaPace.Evaluate(Window(null, Week, Reset, ValueState.Unknown), Freshness.Fresh, now, Utc).Kind);
        Assert.Equal(PaceKind.None, QuotaPace.Evaluate(Window(null, Week, Reset, ValueState.Unlimited), Freshness.Fresh, now, Utc).Kind);
        Assert.Equal(PaceKind.None, QuotaPace.Evaluate(Window(45, null, Reset), Freshness.Fresh, now, Utc).Kind);
        Assert.Equal(PaceKind.None, QuotaPace.Evaluate(Window(45, Week, null), Freshness.Fresh, now, Utc).Kind);
        Assert.Equal(PaceKind.None, QuotaPace.Evaluate(Window(45, Week, now.AddMinutes(-1)), Freshness.Fresh, now, Utc).Kind);
    }
}
