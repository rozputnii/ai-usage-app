using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using Xunit;
using static AiUsage.Infrastructure.Tests.BudgetEngineTests;
using static AiUsage.Infrastructure.Tests.ReadingBudgetTests;

namespace AiUsage.Infrastructure.Tests;

public sealed class ReadingContinuityTests
{
    private static readonly TimeSpan Ninety = TimeSpan.FromMinutes(90);

    [Theory]
    [InlineData(1, 15)]
    [InlineData(5, 15)]
    [InlineData(6, 18)]
    [InlineData(30, 90)]
    [InlineData(60, 180)]
    public void ToleranceIsThreeIntervalsWithAFifteenMinuteFloor(int intervalMinutes, int expectedMinutes)
    {
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), ReadingContinuity.Tolerance(TimeSpan.FromMinutes(intervalMinutes)));
        Assert.Equal(TimeSpan.FromMinutes(15), ReadingContinuity.Floor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ToleranceRejectsAnIntervalThatIsNotPositive(int intervalMinutes) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ReadingContinuity.Tolerance(TimeSpan.FromMinutes(intervalMinutes)));

    [Fact]
    public void DayStartCarriesWithinTheTolerance()
    {
        var now = Local("2026-10-06 14:00");
        // The last reading before midnight, confirmed 40 minutes before it, with a different value from the first after.
        ReadingRun[] runs = [Run(40, "2026-10-05 23:20"), Run(43, "2026-10-06 00:20")];

        var carried = ReadingCalculations.DayStart(runs, Key, "p1", now, Zone, Ninety);
        Assert.Equal(DayStartOrigin.Carried, carried.Origin);
        Assert.Equal(new CountQuantity(40, "percent"), carried.Value);

        var byDefault = ReadingCalculations.DayStart(runs, Key, "p1", now, Zone);
        Assert.Equal(DayStartOrigin.Since, byDefault.Origin);
        Assert.Equal(new CountQuantity(43, "percent"), byDefault.Value);
        Assert.Equal(Local("2026-10-06 00:20"), byDefault.Since);

        // The tolerance is inclusive at exactly 90 minutes and exclusive beyond it.
        ReadingRun[] exact = [Run(40, "2026-10-05 22:30"), Run(43, "2026-10-06 00:20")];
        Assert.Equal(DayStartOrigin.Carried, ReadingCalculations.DayStart(exact, Key, "p1", now, Zone, Ninety).Origin);
        ReadingRun[] tooOld = [Run(40, "2026-10-05 22:20"), Run(43, "2026-10-06 00:20")];
        Assert.Equal(DayStartOrigin.Since, ReadingCalculations.DayStart(tooOld, Key, "p1", now, Zone, Ninety).Origin);
    }

    [Fact]
    public void TrackFlagsADropOnlyAfterALongerGap()
    {
        var period = Period("2026-10-01", "2026-11-01");
        var now = Local("2026-10-15 18:00");
        // 50 to 20 is a drop; the second reading arrives 60 minutes after the first was last confirmed.
        ReadingRun[] runs = [Run(50, "2026-10-15 10:00"), Run(20, "2026-10-15 11:00")];
        Assert.False(ReadingCalculations.Track(runs, Key, period, now, false, 1, Ninety).Incomplete);
        Assert.True(ReadingCalculations.Track(runs, Key, period, now, false, 1).Incomplete);

        // A gap of exactly the tolerance is still continuous; a longer one is not.
        ReadingRun[] exact = [Run(50, "2026-10-15 10:00"), Run(20, "2026-10-15 11:30")];
        Assert.False(ReadingCalculations.Track(exact, Key, period, now, false, 1, Ninety).Incomplete);
        ReadingRun[] longer = [Run(50, "2026-10-15 10:00"), Run(20, "2026-10-15 11:40")];
        Assert.True(ReadingCalculations.Track(longer, Key, period, now, false, 1, Ninety).Incomplete);
    }

    [Fact]
    public void ExtraUsageEvidenceAcceptsAReadingWithinTheTolerance()
    {
        var spendKey = Key with { Limit = new("claude", "extra", "extra") };
        var now = Local("2026-10-06 14:00");
        var below = Run(90, "2026-10-06 09:00");
        var start = Run(0, "2026-10-06 10:00") with { Series = spendKey, Value = new MoneyQuantity(1000, 2, "USD") };
        var currentSpend = start with { FirstSeen = now, LastConfirmed = now, Value = new MoneyQuantity(1275, 2, "USD") };
        var spendFortyMinutesAgo = currentSpend with { FirstSeen = Local("2026-10-06 13:20"), LastConfirmed = Local("2026-10-06 13:20") };
        var currentFull = Run(100, "2026-10-06 10:00", "2026-10-06 14:00");
        var fullFortyMinutesAgo = Run(100, "2026-10-06 10:00", "2026-10-06 13:20");
        ExtraUsageResult Calculate(ReadingRun full, ReadingRun spend, TimeSpan? tolerance = null) =>
            ExtraUsageEvidence.Calculate([below, full], Key, TimeSpan.FromDays(7), [start, spend], spendKey, now, tolerance);
        var evidence = new ExtraUsageResult(true, new MoneyQuantity(275, 2, "USD"), Local("2026-10-06 10:00"));

        // A full window confirmed 40 minutes ago.
        Assert.Equal(evidence, Calculate(fullFortyMinutesAgo, currentSpend, Ninety));
        Assert.Equal(new ExtraUsageResult(null, null, null), Calculate(fullFortyMinutesAgo, currentSpend));
        // A spend reading confirmed 40 minutes ago.
        Assert.Equal(evidence, Calculate(currentFull, spendFortyMinutesAgo, Ninety));
        Assert.Equal(new ExtraUsageResult(null, null, Local("2026-10-06 10:00")), Calculate(currentFull, spendFortyMinutesAgo));
        // Beyond the tolerance the evidence is still withheld.
        var fullHundredMinutesAgo = Run(100, "2026-10-06 10:00", "2026-10-06 12:20");
        Assert.Equal(new ExtraUsageResult(null, null, null), Calculate(fullHundredMinutesAgo, currentSpend, Ninety));
    }
}
