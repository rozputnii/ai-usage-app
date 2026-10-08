namespace AiUsage.Core.Budget;

public static class ReadingContinuity
{
    /// <summary>The least time that still counts as one continuous series of readings.</summary>
    public static readonly TimeSpan Floor = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How old or how far apart readings may be and still count as current or continuous:
    /// three refresh intervals, never less than <see cref="Floor"/>.
    /// </summary>
    public static TimeSpan Tolerance(TimeSpan refreshInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(refreshInterval, TimeSpan.Zero);
        var threeIntervals = refreshInterval * 3;
        return threeIntervals > Floor ? threeIntervals : Floor;
    }
}
