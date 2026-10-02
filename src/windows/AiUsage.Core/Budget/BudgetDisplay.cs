namespace AiUsage.Core.Budget;

public sealed record BudgetBarMarks(decimal DayStart, decimal Used, decimal TodayEnd, decimal Pace);

public static class BudgetDisplay
{
    public static decimal Down(decimal value, decimal quantum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantum);
        return decimal.Floor(value / quantum) * quantum;
    }

    public static decimal TowardZero(decimal value, decimal quantum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantum);
        return decimal.Truncate(value / quantum) * quantum;
    }

    public static BudgetBarMarks? BarMarks(BudgetResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Limit is not > 0 || result.DayStart is null || result.Used is null || result.TodayShare is null || result.Weights is null || result.Baseline is null) return null;
        decimal Mark(decimal value) => decimal.Round(value / result.Limit.Value * 100, 1, MidpointRounding.AwayFromZero);
        return new(Mark(result.DayStart.Value), Mark(result.Used.Value), Mark(result.DayStart.Value + result.TodayShare.Value), Mark(result.Baseline.Value * result.Weights.Elapsed));
    }
}
