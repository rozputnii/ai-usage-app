using System.Globalization;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class BudgetEngineTests
{
    internal static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
    internal static DateTimeOffset Local(string value) => new(DateTime.Parse(value, CultureInfo.InvariantCulture), Zone.GetUtcOffset(DateTime.Parse(value, CultureInfo.InvariantCulture)));
    internal static PeriodBounds Period(string start, string end) => new(Local(start), Local(end), ValueOrigin.Derived, ValueOrigin.Provider);
    internal static LimitFacts Window => new(new("claude", "shared", "weekly"), LimitKind.PercentWindow, "percent", LimitValue.NotApplicable)
    { Duration = TimeSpan.FromDays(7), Reset = new(Local("2026-10-08 15:00"), ValueOrigin.Provider, ResetMeaning.Replenish) };
    internal static BudgetInput Input(decimal limit, decimal used0, decimal used, string start, string end, string today) =>
        new(LimitModelTests.Pool(LimitValue.Finite(new CountQuantity(limit, "requests"))), null,
            Period(start, end), new CountQuantity(used, "requests"), new CountQuantity(used0, "requests"), Local(today), Zone);

    public static TheoryData<string, decimal, decimal, decimal, string, string, string, decimal, decimal, decimal?, decimal, decimal, decimal?, BudgetState> Cases => new()
    {
        { "E01", 30000, 21000, 21800, "2026-09-01", "2026-10-01", "2026-09-29 14:00", 22, 2, 4500, 1363.64m, 6836.36m, 3700, BudgetState.Ok },
        { "E02", 17000, 7200, 7650, "2026-10-01", "2026-11-01", "2026-10-14", 22, 13, 753.85m, 772.73m, 77.27m, 303.85m, BudgetState.Ok },
        { "E03", 100, 38, 47, "2026-10-01 15:00", "2026-10-08 15:00", "2026-10-06", 5, 2.63m, 23.62m, 20, 20.5m, 14.62m, BudgetState.Ok },
        { "E04a", 100, 25, 31, "2026-10-01 15:00", "2026-10-08 15:00", "2026-10-03", 5, 3.63m, null, 20, -3.5m, null, BudgetState.Neutral },
        { "E04b", 100, 31, 31, "2026-10-01 15:00", "2026-10-08 15:00", "2026-10-05", 5, 3.63m, 19.03m, 20, 16.5m, 19.03m, BudgetState.Ok },
        { "E05a", 25000, 21000, 21800, "2026-09-01", "2026-10-01", "2026-09-29 14:00", 22, 2, 2000, 1136.36m, 2063.64m, 1200, BudgetState.Ok },
        { "E05b", 21500, 21000, 21800, "2026-09-01", "2026-10-01", "2026-09-29 14:00", 22, 2, 250, 977.27m, -1277.27m, -550, BudgetState.TodayUsed },
        { "E05c", 20000, 21000, 21800, "2026-09-01", "2026-10-01", "2026-09-29 14:00", 22, 2, 0, 909.09m, -2709.09m, -800, BudgetState.TodayUsed },
        { "E06a", 100, 88, 95, "2026-10-01 15:00", "2026-10-08 15:00", "2026-10-08 14:00", 5, .63m, 19.2m, 20, 5, 5, BudgetState.Ok },
        { "E06b", 100, 0, 4, "2026-10-08 15:00", "2026-10-15 15:00", "2026-10-08 18:00", 5, 5, 20, 20, 3.5m, 3.5m, BudgetState.Ok },
        { "E07a", 100, 52, 60, "2026-10-21 11:00", "2026-10-28 10:00", "2026-10-26", 4.96m, 2.42m, 19.86m, 20.17m, 11.43m, 11.86m, BudgetState.Ok },
        { "E08a", 300, 0, 12, "2026-02-01", "2026-03-01", "2026-02-02", 20, 20, 15, 15, 3, 3, BudgetState.Attention },
        { "E08b", 300, 0, 5, "2026-07-01 01:00", "2026-08-01 01:00", "2026-07-01 12:00", 22.96m, 22.96m, 13.07m, 13.07m, 7.52m, 7.52m, BudgetState.Ok },
        { "E09", 100, 70, 72, "2026-10-04 09:00", "2026-10-11 09:00", "2026-10-10", 5, 0, null, 20, 28, null, BudgetState.Neutral },
        { "E10a", 100, 90, 100, "2026-10-01 15:00", "2026-10-08 15:00", "2026-10-03", 5, 3.63m, null, 20, -72.5m, null, BudgetState.Neutral },
        { "E10b", 30000, 29500, 30500, "2026-09-01", "2026-10-01", "2026-09-26", 22, 3, null, 1363.64m, -4590.91m, null, BudgetState.Neutral },
        { "E12a", 30000, 0, 300, "2026-10-01", "2026-11-01", "2026-10-01 08:00", 22, 22, 1363.64m, 1363.64m, 1063.64m, 1063.64m, BudgetState.Ok },
        { "E12b", 30000, 7000, 7460, "2026-10-01", "2026-11-01", "2026-10-15", 22, 12, 1916.67m, 1363.64m, 7540, 1456.67m, BudgetState.Ok }
    };

    [Theory, MemberData(nameof(Cases))]
    public void ResearchBudgetExamples(string id, decimal limit, decimal u0, decimal used, string start, string end, string now,
        decimal w, decimal wr, decimal? n, decimal b, decimal deviation, decimal? left, BudgetState state)
    {
        var result = BudgetEngine.Calculate(Input(limit, u0, used, start, end, now));
        Assert.Equal(w, Table(result.Weights!.Total));
        Assert.Equal(wr, Table(result.Weights.Remaining));
        Assert.Equal(n, Table(result.Norm));
        Assert.Equal(b, Table(result.Baseline));
        Assert.Equal(deviation, Table(result.Deviation));
        Assert.Equal(left, Table(result.LeftToday));
        Assert.Equal(used - u0, result.UsedToday);
        Assert.Equal(state, result.BudgetState);
        if (id is "E05b" or "E05c" or "E10b") Assert.Equal(AccountLimitState.Over, result.State);
        if (id == "E10a") Assert.Equal(AccountLimitState.AtLimit, result.State);
        if (id == "E03") Assert.Equal(62m / 2.625m, result.Norm);
    }

    [Fact]
    public void E07bUsesActual25HourDayAndSpringDayWeighsOne()
    {
        var input = Input(100, 0, 3, "2026-10-25 12:00", "2026-11-01 12:00", "2026-10-25 13:00") with { WorkDays = Enum.GetValues<DayOfWeek>().ToHashSet() };
        var result = BudgetEngine.Calculate(input);
        Assert.Equal(6.98m, result.Weights!.Total);
        Assert.Equal(.48m, result.Weights.Today);
        Assert.Equal(3.88m, Table(result.LeftToday));
        var spring = WorkCalendar.Weights(Period("2026-03-29", "2026-03-30"), Local("2026-03-29 12:00"), Zone, input.WorkDays);
        Assert.Equal(1, spring.Total);
    }

    [Fact]
    public void E11AndE13KeepNoBudgetReasonsAndZeroStatesDistinct()
    {
        var zero = Input(0, 0, 0, "2026-02-01", "2026-03-01", "2026-02-02");
        Assert.Equal(AccountLimitState.Neutral, BudgetEngine.Calculate(zero).State);
        Assert.Equal(NoBudgetReason.ZeroLimit, BudgetEngine.Calculate(zero).Reason);
        Assert.Equal(AccountLimitState.Over, BudgetEngine.Calculate(zero with { Used = new CountQuantity(3, "requests") }).State);
        var unlimited = zero with { Facts = zero.Facts with { Limit = LimitValue.Unlimited }, Used = new CountQuantity(40, "requests") };
        Assert.Equal(NoBudgetReason.Unlimited, BudgetEngine.Calculate(unlimited).Reason);
        var capped = zero with { Facts = zero.Facts with { Limit = LimitValue.Finite(new CountQuantity(50000, "requests")) }, Cap = new(new CountQuantity(0, "requests"), zero.Now), Used = new CountQuantity(250, "requests") };
        Assert.Equal(LimitBinding.PersonalCap, BudgetEngine.Calculate(capped).Binding);
        Assert.Equal(AccountLimitState.Over, BudgetEngine.Calculate(capped).State);
        Assert.Equal(NoBudgetReason.PeriodUnknown, BudgetEngine.Calculate(zero with { Facts = Window, Used = new CountQuantity(30, "percent"), DayStart = null, Period = null }).Reason);
    }

    [Fact]
    public void PeriodResolutionKeepsFactsAndUsesCalendarFallbackOnlyWithoutReplenishment()
    {
        var now = Local("2026-10-14");
        var facts = Window with { Reset = new(Local("2026-10-28 10:00"), ValueOrigin.Provider, ResetMeaning.Replenish) };
        Assert.Equal(DateTimeOffset.Parse("2026-10-21T10:00:00Z", CultureInfo.InvariantCulture), PeriodResolver.Resolve(facts, now, Zone)!.Start);
        Assert.Null(PeriodResolver.Resolve(facts with { Duration = null }, now, Zone));
        var monthly = PeriodResolver.Resolve(facts with { Duration = null, IsMonthly = true, Reset = new(Local("2026-03-31"), ValueOrigin.Provider, ResetMeaning.Replenish) }, now, Zone)!;
        Assert.Equal(28, monthly.Start.UtcDateTime.Day);
        Assert.Equal(ValueOrigin.Assumed, monthly.StartOrigin);
        var assumed = PeriodResolver.Resolve(facts with { Reset = facts.Reset with { Meaning = ResetMeaning.Expire } }, now, Zone)!;
        Assert.Equal(Local("2026-10-01"), assumed.Start);
        Assert.Equal(Local("2026-11-01"), assumed.End);
        Assert.Equal(ValueOrigin.Assumed, assumed.EndOrigin);
    }

    [Fact]
    public void DayOffWorkTodayRushAndUsedUpHaveIndependentOutputs()
    {
        var input = new BudgetInput(Window, null, Period("2026-10-01 15:00", "2026-10-08 15:00"), new CountQuantity(31, "percent"), new CountQuantity(25, "percent"), Local("2026-10-03 12:00"), Zone);
        var result = BudgetEngine.Calculate(input);
        Assert.Equal(75m / 4.625m, result.DayOffShare);
        Assert.Equal(BudgetState.Neutral, result.BudgetState);
        Assert.Null(result.TodayShare);
        Assert.Equal(BudgetState.Ok, BudgetEngine.Calculate(input with { WorkToday = new(2026, 10, 3) }).BudgetState);
        Assert.Equal(BudgetState.Neutral, BudgetEngine.Calculate(input with { WorkToday = new(2026, 10, 2) }).BudgetState);
        var last = input with { Now = Local("2026-10-08 04:00"), DayStart = new CountQuantity(88, "percent"), Used = new CountQuantity(95, "percent") };
        var rush = BudgetEngine.Calculate(last);
        Assert.True(rush.Rush);
        Assert.Equal(12, rush.TodayShare);
        Assert.Equal(2, rush.FiveHourWindowsBeforeReset);
        Assert.False(BudgetEngine.Calculate(last with { Cap = new(new CountQuantity(20, "percent"), last.Now) }).Rush);
        Assert.False(BudgetEngine.Calculate(last with { Period = last.Period! with { EndOrigin = ValueOrigin.Assumed } }).Rush);
        var full = BudgetEngine.Calculate(last with { Used = new CountQuantity(100, "percent") });
        Assert.True(full.ProviderUsedUp);
        Assert.False(full.Rush);
        Assert.Equal(AccountLimitState.AtLimit, full.State);
    }

    [Fact]
    public void PastResetMissingReadingAndCorrectionNeverInventUsage()
    {
        var input = Input(100, 58, 55, "2026-10-01", "2026-10-08", "2026-10-06");
        Assert.Equal(0, BudgetEngine.Calculate(input).UsedToday);
        Assert.Equal(NoBudgetReason.NotReady, BudgetEngine.Calculate(input with { DayStart = null }).Reason);
        Assert.Equal(NoBudgetReason.NotReady, BudgetEngine.Calculate(input with { Now = Local("2026-10-08") }).Reason);
        Assert.Equal(NoBudgetReason.NotReady, BudgetEngine.Calculate(input with { Used = new CountQuantity(55, "credits") }).Reason);
        Assert.Equal(BudgetEngine.Calculate(input).Norm, BudgetEngine.Calculate(input with { IsStale = true }).Norm);
        Assert.True(BudgetEngine.Calculate(input with { IsStale = true }).IsStale);
        Assert.Equal(-.1m, BudgetDisplay.Down(-.001m, .1m));
        Assert.Equal(0, BudgetDisplay.TowardZero(-.001m, .1m));
    }

    [Fact]
    public void TrackedUseCannotInventProviderExhaustionAndNoFutureReadingIsUsed()
    {
        var input = Input(100, 30, 110, "2026-10-01", "2026-11-01", "2026-10-06");
        input = input with { Period = input.Period! with { EndOrigin = ValueOrigin.Assumed } };
        Assert.False(BudgetEngine.Calculate(input).ProviderUsedUp);
        Assert.True(BudgetEngine.Calculate(input with { Facts = input.Facts with { Used = new CountQuantity(100, "requests") } }).ProviderUsedUp);
        var unknownMeaning = Window with { Reset = Window.Reset! with { Meaning = ResetMeaning.Unknown } };
        Assert.Null(PeriodResolver.Resolve(unknownMeaning, input.Now, Zone));
        var inconsistent = input with { Used = new MoneyQuantity(100, 2, "USD") };
        Assert.Equal(NoBudgetReason.NotReady, BudgetEngine.Calculate(inconsistent).Reason);
    }

    [Fact]
    public void RushNeverAppliesToMoneyCreditsOrACustomCap()
    {
        var now = Local("2026-10-30 12:00");
        var money = new LimitFacts(new("claude", "extra", "extra"), LimitKind.MonetaryPool, "USD", LimitValue.Finite(new MoneyQuantity(100, 0, "USD")))
        { Reset = new(Local("2026-11-01"), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var input = new BudgetInput(money, null, Period("2026-10-01", "2026-11-01"), new MoneyQuantity(30, 0, "USD"), new MoneyQuantity(20, 0, "USD"), now, Zone);
        Assert.False(BudgetEngine.Calculate(input).Rush);
        var credits = money with { Kind = LimitKind.CountablePool, Unit = "credits", Limit = LimitValue.Finite(new CountQuantity(100, "credits")) };
        Assert.False(BudgetEngine.Calculate(input with { Facts = credits, Used = new CountQuantity(30, "credits"), DayStart = new CountQuantity(20, "credits") }).Rush);
        var requests = credits with { Unit = "requests", Limit = LimitValue.Finite(new CountQuantity(100, "requests")) };
        var requestInput = input with { Facts = requests, Used = new CountQuantity(30, "requests"), DayStart = new CountQuantity(20, "requests") };
        Assert.True(BudgetEngine.Calculate(requestInput).Rush);
        Assert.False(BudgetEngine.Calculate(requestInput with { Cap = new(new CountQuantity(200, "requests"), now) }).Rush);
    }

    private static decimal? Table(decimal? value) => value is { } v ? decimal.Round(v, 2, MidpointRounding.AwayFromZero) : null;
}
