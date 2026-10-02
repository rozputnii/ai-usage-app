using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public enum BudgetState { NoBudget, NotReady, Neutral, Ok, Attention, TodayUsed }
public enum AccountLimitState { NoBudget, Neutral, Ok, Attention, TodayUsed, AtLimit, Over }
public enum NoBudgetReason { None, LimitUnknown, Unlimited, ZeroLimit, PeriodUnknown, ShortWindow, NotReady, ArithmeticOverflow }

public sealed record BudgetInput(LimitFacts Facts, PersonalCap? Cap, PeriodBounds? Period,
    Quantity? Used, Quantity? DayStart, DateTimeOffset Now, TimeZoneInfo Zone)
{
    public IReadOnlySet<DayOfWeek>? WorkDays { get; init; }
    public bool IsStale { get; init; }
    public DateOnly? WorkToday { get; init; }
}

public sealed record BudgetResult
{
    public Quantity? Scale { get; init; }
    public decimal? Limit { get; init; }
    public decimal? Used { get; init; }
    public decimal? DayStart { get; init; }
    public LimitBinding Binding { get; init; }
    public bool CapRejected { get; init; }
    public WorkWeights? Weights { get; init; }
    public decimal? Norm { get; init; }
    public decimal? TodayShare { get; init; }
    public decimal? DayOffShare { get; init; }
    public decimal? Baseline { get; init; }
    public decimal? Deviation { get; init; }
    public decimal? UsedToday { get; init; }
    public decimal? LeftToday { get; init; }
    public decimal? Remaining => Limit - Used;
    public BudgetState BudgetState { get; init; }
    public AccountLimitState State { get; init; }
    public NoBudgetReason Reason { get; init; }
    public bool ProviderUsedUp { get; init; }
    public bool Rush { get; init; }
    public int? FiveHourWindowsBeforeReset { get; init; }
    public bool IsStale { get; init; }
}

public static class BudgetEngine
{
    public static BudgetResult Calculate(BudgetInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        try { return CalculateCore(input); }
        catch (OverflowException)
        {
            return new() { Reason = NoBudgetReason.ArithmeticOverflow, BudgetState = BudgetState.NotReady, IsStale = input.IsStale };
        }
    }

    private static BudgetResult CalculateCore(BudgetInput input)
    {
        var effective = EffectiveLimit.Resolve(input.Facts, input.Cap);
        var result = new BudgetResult { Binding = effective.Binding, CapRejected = effective.CapRejected, IsStale = input.IsStale };
        if (effective.Value is null) return Finish(result, BudgetState.NoBudget,
            input.Facts.Limit.State == LimitValueState.Unlimited ? NoBudgetReason.Unlimited : NoBudgetReason.LimitUnknown);
        if (!QuantityMath.TryAlign(effective.Value, input.Used, out var limit, out var used, out var scale))
            return Finish(result, BudgetState.NotReady, NoBudgetReason.NotReady);
        result = result with { Limit = limit, Used = used, Scale = scale };
        var factUsed = input.Facts.Kind == LimitKind.PercentWindow
            ? input.Facts.UsedPercent is { } percent ? new CountQuantity(percent, "percent") : input.Used
            : input.Facts.Used ?? (input.Period?.EndOrigin == ValueOrigin.Assumed ? null : input.Used);
        var provider = EffectiveLimit.Resolve(input.Facts, null).Value;
        bool providerKnown = QuantityMath.TryAlign(provider, factUsed, out var pl, out var pu, out _);
        var state = LimitState(used, limit);
        if (providerKnown) state = Max(state, LimitState(pu, pl));
        result = result with { State = state, ProviderUsedUp = providerKnown && pl > 0 && pu >= pl };
        if (limit <= 0) return Finish(result, BudgetState.NoBudget, NoBudgetReason.ZeroLimit);
        if (input.Period is not { } period || period.End <= period.Start)
            return Finish(result, BudgetState.NoBudget, NoBudgetReason.PeriodUnknown);
        if (input.Now >= period.End || input.Now < period.Start)
            return Finish(result, BudgetState.NotReady, NoBudgetReason.NotReady);
        if (input.Facts.Duration == TimeSpan.FromHours(5) || period.End - period.Start < TimeSpan.FromDays(1))
        {
            var shortState = input.Facts.Kind == LimitKind.PercentWindow
                ? used >= 90 ? AccountLimitState.TodayUsed : used >= 70 ? AccountLimitState.Attention : AccountLimitState.Ok
                : AccountLimitState.NoBudget;
            return Finish(result with { State = Max(state, shortState) }, BudgetState.NoBudget, NoBudgetReason.ShortWindow);
        }
        if (!QuantityMath.TryAlign(scale, input.DayStart, out var scaledLimit, out var u0, out var common))
            return Finish(result, BudgetState.NotReady, NoBudgetReason.NotReady);
        // Align all three quantities at the highest money exponent before any budget arithmetic.
        if (scale is MoneyQuantity && common is MoneyQuantity)
        {
            if (!QuantityMath.TryAlign(input.Used, common, out used, out _, out _))
                return Finish(result, BudgetState.NotReady, NoBudgetReason.NotReady);
            limit = scaledLimit;
        }
        var weights = WorkCalendar.Weights(period, input.Now, input.Zone, input.WorkDays);
        var usedToday = Math.Max(0, used - u0);
        decimal? baseline = weights.Total > 0 ? limit / weights.Total : null;
        decimal? norm = weights.Today > 0 && weights.Remaining > 0 ? Math.Max(0, limit - u0) / weights.Remaining : null;
        decimal? share = norm * weights.Today;
        decimal? dayOffShare = weights.Today == 0 ? Math.Max(0, limit - u0) / (weights.Remaining + 1) : null;
        bool workToday = weights.Today == 0 && input.WorkToday == WorkCalendar.Date(input.Now, input.Zone);
        decimal? left = (workToday ? dayOffShare : share) - usedToday;
        var budgetState = left is null ? BudgetState.Neutral : left <= 0 ? BudgetState.TodayUsed
            : left < (workToday ? dayOffShare : share) * .3m ? BudgetState.Attention : BudgetState.Ok;
        bool rush = !result.ProviderUsedUp && input.Cap is null && weights.Today > 0 && weights.Remaining == weights.Today &&
            period.EndOrigin == ValueOrigin.Provider && input.Facts.Reset is { Meaning: ResetMeaning.Replenish } &&
            input.Facts.Kind != LimitKind.MonetaryPool && input.Facts.Unit != "credits";
        return Finish(result with
        {
            Limit = limit, Used = used, DayStart = u0, Scale = common, Weights = weights,
            Norm = norm, TodayShare = share, DayOffShare = dayOffShare, Baseline = baseline,
            Deviation = baseline * weights.Elapsed - used, UsedToday = usedToday, LeftToday = left,
            Rush = rush, FiveHourWindowsBeforeReset = rush && input.Facts.Kind == LimitKind.PercentWindow
                ? checked((int)((period.End - input.Now).Ticks / TimeSpan.FromHours(5).Ticks)) : null
        }, budgetState, NoBudgetReason.None);
    }

    private static AccountLimitState LimitState(decimal used, decimal limit) => used > limit ? AccountLimitState.Over
        : used == limit && limit > 0 ? AccountLimitState.AtLimit : limit == 0 ? AccountLimitState.Neutral : AccountLimitState.NoBudget;
    private static AccountLimitState Max(AccountLimitState a, AccountLimitState b) => a > b ? a : b;
    private static BudgetResult Finish(BudgetResult result, BudgetState state, NoBudgetReason reason)
    {
        var rank = state switch { BudgetState.TodayUsed => AccountLimitState.TodayUsed, BudgetState.Attention => AccountLimitState.Attention,
            BudgetState.Ok => AccountLimitState.Ok, BudgetState.Neutral => AccountLimitState.Neutral, _ => AccountLimitState.NoBudget };
        return result with { BudgetState = state, Reason = reason, State = Max(result.State, rank) };
    }

    public static AccountLimitState AccountState(IEnumerable<BudgetResult> limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return limits.Select(x => x.State).DefaultIfEmpty(AccountLimitState.NoBudget).Max();
    }
}
