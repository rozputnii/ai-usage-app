using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ReadingBoundaryTests
{
    private static readonly ReadingSeriesKey Key = new("opaque/測試", new("provider", "pool", "counter"));
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ResetJitterNearMaximumTimestampDoesNotOverflow()
    {
        var previous = Run(10, DateTimeOffset.MaxValue.AddSeconds(-20)) with { ResetAt = DateTimeOffset.MaxValue.AddSeconds(-10) };
        var current = Run(11, DateTimeOffset.MaxValue.AddSeconds(-5)) with { ResetAt = DateTimeOffset.MaxValue };
        Assert.Equal(PeriodChangeKind.Continuing, ReadingCalculations.Transition(previous, current, 1).Kind);
    }

    [Fact]
    public void SignedCounterTransitionHandlesDifferenceLargerThanDecimalRange()
    {
        var previous = Run(decimal.MaxValue, Start) with { ResetAt = Start.AddDays(7) };
        var current = Run(decimal.MinValue, Start.AddMinutes(5)) with { ResetAt = Start.AddDays(8) };
        Assert.Equal(PeriodChangeKind.EarlyReplenishment, ReadingCalculations.Transition(previous, current, 1).Kind);
        Assert.Equal(PeriodChangeKind.Continuing, ReadingCalculations.Transition(previous with { Value = current.Value },
            current with { Value = previous.Value }, 1).Kind);
    }

    [Fact]
    public void ClosedPeriodDoesNotClaimConfirmationAfterItsEnd()
    {
        var end = Start.AddDays(1);
        var run = Run(10, Start.AddHours(1)) with { LastConfirmed = end.AddHours(2) };
        var tracked = ReadingCalculations.Track([run], Key,
            new(Start, end, ValueOrigin.Assumed, ValueOrigin.Assumed), end.AddDays(1), false, 1);
        Assert.Equal(new CountQuantity(10, "requests"), tracked.Used);
        Assert.Equal(end, Assert.Single(tracked.Runs).LastConfirmed);
    }

    [Fact]
    public void KnownUnchangedRunPreservesZeroConsumptionAcrossClosedPeriod()
    {
        var end = Start.AddDays(1);
        var run = Run(10, Start.AddHours(-1)) with { LastConfirmed = end.AddHours(1) };
        var tracked = ReadingCalculations.Track([run], Key,
            new(Start, end, ValueOrigin.Assumed, ValueOrigin.Assumed), end.AddDays(1), false, 1);
        Assert.Equal(new CountQuantity(0, "requests"), tracked.Used);
        var cumulative = Assert.Single(tracked.Runs);
        Assert.Equal(Start, cumulative.FirstSeen);
        Assert.Equal(end, cumulative.LastConfirmed);
    }

    private static ReadingRun Run(decimal value, DateTimeOffset at) => new(Key, new CountQuantity(value, "requests"),
        at, at, "instance", "opaque plan", SnapshotSource.ProviderApi);
}
