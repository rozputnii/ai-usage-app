using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using Xunit;
using FactLimit = AiUsage.Core.Usage.LimitValue;

namespace AiUsage.Presentation.Tests;

public sealed class MonetaryUsageTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000044");
    private static readonly LimitFacts Facts = new(new("claude", "CL-X", "extra-usage"), LimitKind.MonetaryPool, "USD", FactLimit.Finite(new MoneyQuantity(50000, 2, "USD")))
    {
        Used = new MoneyQuantity(11000, 2, "USD"), Enabled = true, IsMonthly = true,
        PeriodStart = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), PeriodStartOrigin = ValueOrigin.Provider,
        Reset = new(new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), ValueOrigin.Provider, ResetMeaning.Replenish)
    };

    private static LedgerLimit Data(LimitFacts? facts = null, MonetaryScope scope = MonetaryScope.Account)
    {
        facts ??= Facts;
        var key = new ReadingSeriesKey(Id.ToString("N"), facts.Key);
        return new(facts, key,
            [new(key, new MoneyQuantity(10000, 2, "USD"), new DateTimeOffset(Now.Date, TimeSpan.Zero), new DateTimeOffset(Now.Date, TimeSpan.Zero), "month", null, SnapshotSource.ProviderApi),
             new(key, facts.Used ?? new MoneyQuantity(11000, 2, "USD"), Now, Now, "month", null, SnapshotSource.ProviderApi)])
        { MonetaryScope = scope };
    }

    private static AccountModel Project(LedgerLimit data, PersonalCap? cap = null, string name = "Work")
    {
        var quota = new QuotaSnapshot(Now, null, [], null, null, null, null);
        var account = new AccountSnapshot(Id, "claude", true, new(ProviderSessionStatus.QuotaAvailable, quota), false, null);
        return LiveLedgerProjection.Account(account, name, [data], BudgetConfiguration.Default with
        { Caps = cap is null ? [] : [new(data.Series, cap)] }, Now, TimeZoneInfo.Utc, null);
    }

    [Fact]
    public void MonetaryOnlyAccountUsesReportedMonthlyAmountAndDailyBarsWithoutScopeMetadata()
    {
        var facts = Facts with { Used = new MoneyQuantity(2966, 2, "USD"), Limit = FactLimit.Finite(new MoneyQuantity(200000, 2, "USD")),
            Reset = null, PeriodStart = null, PeriodStartOrigin = null, IsMonthly = false, AllowsCalendarFallback = true };
        var data = Data(facts, MonetaryScope.Unknown);
        data = data with { Runs = [data.Runs[^1]] };
        var model = Project(data, name: "Claude");
        var card = Assert.Single(model.Cards);
        Assert.Equal(CardLayout.Pool, card.Layout);
        Assert.Equal(29.66m, card.Figures.Used);
        Assert.Equal(2000m, card.Figures.EffectiveLimit);
        Assert.Equal(29.66m, card.Figures.DayStart);
        Assert.True(card.Figures.TodayEnd > 29.66m);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), card.Reset!.At);
        Assert.Equal(ResetProvenance.Assumed, card.Reset.Provenance);
        Assert.Null(card.Monetary!.BudgetUnavailable);
        Assert.True(CardVisuals.Build(card, model, ValueMode.Used, Now).HasPeriodBar);
        Assert.True(CardVisuals.Build(card, model, ValueMode.Used, Now).HasStrip);
    }

    [Fact]
    public void CompatibleMonthlyMoneyRetainsBudgetHistoryAndCapWithoutCommercialLabel()
    {
        var data = Data();
        var cap = new PersonalCap(new MoneyQuantity(30000, 2, "USD"), Now);
        var model = Project(data, cap);
        var card = Assert.Single(model.Cards);
        Assert.Equal("Spending", card.ScopeLabel);
        Assert.Equal(CardLayout.Pool, card.Layout);
        Assert.Equal(500, card.Figures.ProviderLimit.Amount);
        Assert.Equal(300, card.Figures.EffectiveLimit);
        Assert.Equal(100, card.Figures.DayStart);
        Assert.True(card.Figures.TodayEnd > 100);
        Assert.Equal(PeriodKind.CalendarMonth, card.Period.Kind);
        Assert.Equal(CapStatus.Applied, card.Cap!.Status);
        Assert.Equal(card.CardId, card.CapTargetId);
        Assert.Equal(10, LiveLedgerProjection.History(data, Now, TimeZoneInfo.Utc).Days[^1].Used);
        Assert.Equal(card.Figures, Assert.Single(Project(data, cap, "Personal").Cards).Figures);
        Assert.True(CardVisuals.Build(card, model, ValueMode.Used, Now).HasPeriodBar);
        Assert.True(CardVisuals.Build(card, model, ValueMode.Left, Now).HasStrip);
    }

    [Theory]
    [InlineData(MonetaryScope.Shared)]
    internal void ExplicitSharedScopeKeepsFactsAndCapWithoutPersonalBudget(MonetaryScope scope)
    {
        var card = Assert.Single(Project(Data(scope: scope), new(new MoneyQuantity(30000, 2, "USD"), Now)).Cards);
        Assert.Equal(CardLayout.Note, card.Layout);
        Assert.Equal(110, card.Figures.Used);
        Assert.Null(card.Figures.ProviderRemaining);
        Assert.Null(card.Figures.EffectiveLimit);
        Assert.Null(card.CapTargetId);
        Assert.Equal(CapStatus.Inactive, card.Cap!.Status);
        Assert.False(card.Cap.Binding);
        Assert.Contains("scope", card.Monetary!.BudgetUnavailable);
    }

    [Fact]
    public void MissingZeroDisabledUnknownLimitAndCurrencyMismatchRemainNativeFacts()
    {
        foreach (var facts in new[] { Facts with { Used = null },
            Facts with { Enabled = false },
            Facts with { Limit = FactLimit.Finite(new MoneyQuantity(50000, 2, "EUR")) },
            Facts with { Used = new MoneyQuantity(123, null, null) } })
        {
            var model = Project(Data(facts, MonetaryScope.Unknown));
            var card = Assert.Single(model.Cards);
            var visual = CardVisuals.Build(card, model, ValueMode.Left, Now);
            Assert.Equal(LedgerFormat.NativeMoney(card.Monetary!.Used), visual.NoteLines[0].Value);
            Assert.Null(card.Figures.TodayEnd);
            Assert.False(visual.HasPeriodBar);
            Assert.Equal(facts.Enabled == false ? "disabled" : "no budget", visual.Pill);
            Assert.DoesNotContain(visual.NoteLines, l => l.Value.Contains("not included in plan", StringComparison.Ordinal));
        }
        var mismatch = Assert.Single(Project(Data(), new(new MoneyQuantity(30000, 2, "EUR"), Now)).Cards);
        Assert.Equal(CapStatus.CurrencyMismatch, mismatch.Cap!.Status);
        Assert.Equal(500, mismatch.Figures.EffectiveLimit);
        Assert.Equal("EUR", mismatch.Monetary!.PersonalCap!.Currency);
        var noLimit = Assert.Single(Project(Data(Facts with { Limit = FactLimit.ExplicitNull })).Cards);
        Assert.Null(noLimit.Figures.EffectiveLimit);
        Assert.Equal(CardAction.SetCap, noLimit.Action);
        var zero = Assert.Single(Project(Data(Facts with { Used = new MoneyQuantity(0, 2, "USD") }, MonetaryScope.Unknown)).Cards);
        Assert.Equal(0m, zero.Figures.Used);
        Assert.Equal(500m, zero.Figures.EffectiveLimit);
        Assert.Null(zero.Monetary!.BudgetUnavailable);
    }

    [Fact]
    public void ZeroProviderLimitIsNotMisrepresentedAsAPlanExclusion()
    {
        var model = Project(Data(Facts with { Limit = FactLimit.Finite(new MoneyQuantity(0, 2, "USD")) }));
        var visual = CardVisuals.Build(model.Cards[0], model, ValueMode.Used, Now);
        Assert.Equal("zero limit", visual.Pill);
        Assert.Contains(visual.NoteLines, line => line.Value == "0.00 USD (provider)");
    }

    [Fact]
    public void CalendarFallbackRemainsAssumedAndDoesNotImplyProviderReset()
    {
        var data = Data(Facts with { Reset = null, PeriodStart = null, AllowsCalendarFallback = true });
        var card = Assert.Single(Project(data).Cards);
        Assert.Equal(ResetProvenance.Assumed, card.Reset!.Provenance);
        Assert.Contains("calendar month assumed", card.Monetary!.Qualification);
        Assert.Contains("provider period unknown", card.Monetary.Qualification);
        Assert.Equal(CardLayout.Pool, card.Layout);
    }

    [Fact]
    public void ExtraUsageKeepsWindowExhaustionBaselineAcrossMidnightAndRejectsUnsupportedEvidence()
    {
        var fill = new DateTimeOffset(Now.Date, TimeSpan.Zero).AddHours(-1);
        var weekly = new LimitFacts(new("claude", "CL-W", "weekly"), LimitKind.PercentWindow, "percent", FactLimit.NotApplicable)
        { UsedPercent = 100, Duration = TimeSpan.FromDays(7), Reset = new(Now.AddDays(3), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var key = new ReadingSeriesKey(Id.ToString("N"), weekly.Key);
        var below = new ReadingRun(key, new CountQuantity(90, "percent"), fill.AddHours(-1), fill.AddHours(-1), "week", null, SnapshotSource.ProviderApi) { ResetAt = weekly.Reset.At };
        var full = below with { Value = new CountQuantity(100, "percent"), FirstSeen = fill, LastConfirmed = Now };
        var spend = Data();
        var baseline = spend.Runs[0] with { FirstSeen = fill, LastConfirmed = fill };
        spend = spend with { Runs = [baseline, baseline with { Value = new MoneyQuantity(10500, 2, "USD"), FirstSeen = new DateTimeOffset(Now.Date, TimeSpan.Zero), LastConfirmed = new DateTimeOffset(Now.Date, TimeSpan.Zero) }, spend.Runs[1]] };
        var account = new AccountSnapshot(Id, "claude", true, new(ProviderSessionStatus.QuotaAvailable,
            new QuotaSnapshot(Now, null, [], null, null, null, null)), false, null);
        LimitCardModel Window(LedgerLimit money, AccountSnapshot? snapshot = null) => LiveLedgerProjection.Account(snapshot ?? account,
            "Arbitrary name", [new(weekly, key, [below, full]), money], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null).Cards[0];
        var mark = Assert.Single(Window(spend).Marks, m => m.Kind == MarkKind.OnExtraUsage);
        Assert.Equal(10, mark.Amount); // Includes the five dollars before midnight.
        Assert.Equal(fill, mark.Since);
        Assert.Equal(weekly.Reset.At, mark.Until);
        foreach (var invalid in new[] { spend with { MonetaryScope = MonetaryScope.Unknown }, spend with { MonetaryScope = MonetaryScope.Shared },
            spend with { Facts = spend.Facts with { Enabled = false } }, spend with { Runs = [spend.Runs[^1]] },
            spend with { Facts = spend.Facts with { Used = null } },
            spend with { Facts = spend.Facts with { Used = new MoneyQuantity(9000, 2, "USD") } },
            spend with { Runs = [baseline, spend.Runs[^1] with { Value = new MoneyQuantity(11000, 2, "EUR") }] },
            spend with { Runs = [baseline, baseline with { FirstSeen = fill.AddMinutes(1), LastConfirmed = fill.AddMinutes(1), Value = new MoneyQuantity(9000, 2, "USD") }, spend.Runs[^1]] } })
            Assert.DoesNotContain(Window(invalid).Marks, m => m.Kind == MarkKind.OnExtraUsage);
        Assert.DoesNotContain(Window(spend, account with { Session = account.Session with { Quota = account.Session.Quota! with { FetchedAt = Now.AddMinutes(-16) } } }).Marks, m => m.Kind == MarkKind.OnExtraUsage);
    }
}
