using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using Xunit;
using static AiUsage.Infrastructure.Tests.BudgetEngineTests;

namespace AiUsage.Infrastructure.Tests;

public sealed class BudgetScenarioTests
{
    [Fact]
    public void DesignBriefAllFiguresBarsAndAccountStates()
    {
        var now = Local("2026-10-14 14:20");
        var monthly = Period("2026-10-01", "2026-11-01") with { EndOrigin = ValueOrigin.Assumed };
        BudgetResult Weekly(decimal u0, decimal u, string start, string end, bool stale = false) => BudgetEngine.Calculate(
            new(Window with { Reset = new(Local(end), ValueOrigin.Provider, ResetMeaning.Replenish) }, null, Period(start, end),
                new CountQuantity(u, "percent"), new CountQuantity(u0, "percent"), now, Zone) { IsStale = stale });
        BudgetResult Short(decimal used, string end) => BudgetEngine.Calculate(new(Window with { Duration = TimeSpan.FromHours(5) }, null,
            new(Local(end).AddHours(-5), Local(end), ValueOrigin.Derived, ValueOrigin.Provider), new CountQuantity(used, "percent"), null, now, Zone));
        var a1 = Short(72, "2026-10-14 16:05");
        var a2 = Weekly(38, 47, "2026-10-12 09:00", "2026-10-19 09:00");
        var money = new LimitFacts(new("claude", "extra", "extra"), LimitKind.MonetaryPool, "USD", LimitValue.Finite(new MoneyQuantity(50000, 2, "USD"))) { Used = new MoneyQuantity(21800, 2, "USD") };
        var a3 = BudgetEngine.Calculate(new(money, new(new MoneyQuantity(30000, 2, "USD"), now), monthly, new MoneyQuantity(21800, 2, "USD"), new MoneyQuantity(20900, 2, "USD"), now, Zone));
        var b1 = Short(91, "2026-10-14 15:48");
        var b2 = Weekly(96, 100, "2026-10-09 09:30", "2026-10-16 09:30");
        var credits = new LimitFacts(new("codex", "credits", "credits"), LimitKind.CountablePool, "credits", LimitValue.Unknown) { Remaining = new CountQuantity(10160, "credits") };
        var b3 = BudgetEngine.Calculate(new(credits, new(new CountQuantity(17000, "credits"), now), monthly, new CountQuantity(6480, "credits"), new CountQuantity(6050, "credits"), now, Zone));
        var c1 = BudgetEngine.Calculate(Input(2000, 1190, 1210, "2026-10-01 01:00", "2026-11-01", "2026-10-14 14:20"));
        var c2 = BudgetEngine.Calculate(Input(50, 11, 12, "2026-10-01 01:00", "2026-11-01", "2026-10-14 14:20"));
        var c3 = BudgetEngine.Calculate(Input(0, 0, 0, "2026-10-01 01:00", "2026-11-01", "2026-10-14 14:20"));
        var d1 = Weekly(30, 36, "2026-10-10 11:00", "2026-10-17 11:00", true);
        var d2 = BudgetEngine.Calculate(new(Window with { Duration = null }, null, null, new CountQuantity(19, "percent"), null, now, Zone) { IsStale = true });

        Figures(a2, .1m, 18.3m, 9, 9.3m, 20, 5.5m, 51, new(38, 47, 56.4m, 52.5m));
        Figures(a3, 1, 700, 900, -200, 1363, -8163, null, new(69.7m, 72.7m, 72, 45.5m));
        Figures(b2, .1m, 1.6m, 4, -2.4m, 20, -27.9m, null, new(96, 100, 97.7m, 72.1m));
        Figures(b3, 1, 842, 430, 412, 772, 1247, 48.9m, new(35.6m, 38.1m, 40.5m, 45.5m));
        Figures(c1, 1, 62, 20, 42, 91, -302, 67.9m, new(59.5m, 60.5m, 62.6m, 45.4m));
        Figures(c2, 1, 3, 1, 2, 2, 10, 66.6m, new(22, 24, 28, 45.4m));
        Figures(d1, .1m, 23.3m, 6, 17.3m, 20, 24, 74.2m, new(30, 36, 53.3m, 60));
        Assert.Equal(new decimal?[] { 28, 53, 8200, 9, 0, 10520, 790, 38, 0, 64, 81 },
            new[] { a1.Remaining, a2.Remaining, a3.Remaining, b1.Remaining, b2.Remaining, b3.Remaining, c1.Remaining, c2.Remaining, c3.Remaining, d1.Remaining, d2.Remaining });
        Assert.Equal(AccountLimitState.Attention, a1.State);
        Assert.Equal(AccountLimitState.TodayUsed, b1.State);
        Assert.Equal(AccountLimitState.Neutral, c3.State);
        Assert.Equal(NoBudgetReason.PeriodUnknown, d2.Reason);
        Assert.True(d1.IsStale && d2.IsStale);
        Assert.Equal(AccountLimitState.TodayUsed, BudgetEngine.AccountState([a1, a2, a3]));
        Assert.Equal(AccountLimitState.AtLimit, BudgetEngine.AccountState([b1, b2, b3]));
        Assert.Equal(AccountLimitState.Ok, BudgetEngine.AccountState([c1, c2, c3]));
        Assert.Equal(AccountLimitState.Ok, BudgetEngine.AccountState([d1, d2]));
        var ready = new SessionEstimate(true, 12, 2, 19.8m, []);
        Assert.Equal(new SessionFigures(new(4, false), new(1, false)), SessionEstimator.Figures(ready, a2.Used!.Value, a2.TodayShare));
        Assert.Null(SessionEstimator.Figures(new(false, null, null, 0, []), b2.Used!.Value, b2.TodayShare).Weekly);
    }

    [Fact]
    public void MoneyBudgetAlignsAllThreeInputsBeforeDividing()
    {
        var facts = new LimitFacts(new("test", "money", "pool"), LimitKind.MonetaryPool, "USD", LimitValue.Finite(new MoneyQuantity(300, 0, "USD")));
        var result = BudgetEngine.Calculate(new(facts, null, Period("2026-09-01", "2026-10-01"), new MoneyQuantity(21800, 2, "USD"), new MoneyQuantity(2100, 1, "USD"), Local("2026-09-29"), Zone));
        Assert.Equal(30000, result.Limit);
        Assert.Equal(4500, result.Norm);
        Assert.Equal(800, result.UsedToday);
    }

    [Theory]
    [InlineData(30000, 4500, 3700, AccountLimitState.Ok)]
    [InlineData(25000, 2000, 1200, AccountLimitState.Ok)]
    [InlineData(21500, 250, -550, AccountLimitState.Over)]
    [InlineData(20000, 0, -800, AccountLimitState.Over)]
    public void E01E05CapChangesKeepDayStartAndIgnoreChangeTime(long cap, decimal norm, decimal left, AccountLimitState state)
    {
        var now = Local("2026-09-29 14:00");
        var facts = new LimitFacts(new("claude", "extra", "extra"), LimitKind.MonetaryPool, "USD", LimitValue.Finite(new MoneyQuantity(50000, 2, "USD")))
        { Used = new MoneyQuantity(21800, 2, "USD") };
        var input = new BudgetInput(facts, new(new MoneyQuantity(cap, 2, "USD"), now), Period("2026-09-01", "2026-10-01") with { EndOrigin = ValueOrigin.Assumed },
            new MoneyQuantity(21800, 2, "USD"), new MoneyQuantity(21000, 2, "USD"), now, Zone);
        var result = BudgetEngine.Calculate(input);
        Assert.Equal(norm, result.Norm);
        Assert.Equal(left, result.LeftToday);
        Assert.Equal(state, result.State);
        Assert.Equal(LimitBinding.PersonalCap, result.Binding);
        Assert.Equal(result, BudgetEngine.Calculate(input with { Now = now.AddHours(4), Cap = input.Cap! with { SetAt = now.AddHours(4) } }));
    }

    private static void Figures(BudgetResult r, decimal quantum, decimal norm, decimal used, decimal left,
        decimal baseline, decimal deviation, decimal? shareLeft, BudgetBarMarks bars)
    {
        Assert.Equal(norm, BudgetDisplay.Down(r.Norm!.Value, quantum));
        Assert.Equal(norm, BudgetDisplay.Down(r.TodayShare!.Value, quantum));
        Assert.Equal(used, r.UsedToday);
        Assert.Equal(left, BudgetDisplay.Down(r.LeftToday!.Value, quantum));
        Assert.Equal(baseline, BudgetDisplay.Down(r.Baseline!.Value, quantum));
        Assert.Equal(deviation, BudgetDisplay.TowardZero(r.Deviation!.Value, quantum));
        if (shareLeft is not null) Assert.Equal(shareLeft, BudgetDisplay.Down(r.LeftToday.Value / r.TodayShare.Value * 100, .1m));
        Assert.Equal(bars, BudgetDisplay.BarMarks(r));
    }
}
