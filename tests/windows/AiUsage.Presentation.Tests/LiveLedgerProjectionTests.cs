using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using Xunit;
using FactValue = AiUsage.Core.Usage.LimitValue;

namespace AiUsage.Presentation.Tests;

public sealed class LiveLedgerProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.Parse("00000000-0000-0000-0000-000000000039");
    private static LimitFacts Weekly(decimal? used = 40) => new(new("claude", "CL-W", "shared"), LimitKind.PercentWindow, "percent", FactValue.NotApplicable)
    { UsedPercent = used, Duration = TimeSpan.FromDays(7), Reset = new(new DateTimeOffset(Now.AddDays(4).Date, TimeSpan.Zero), ValueOrigin.Provider, ResetMeaning.Replenish) };
    private static LedgerLimit Data(LimitFacts facts, decimal baseline = 20)
    {
        var key = new ReadingSeriesKey(Account.ToString("N"), facts.Key);
        return new(facts, key, [new(key, new CountQuantity(baseline, facts.Unit), new DateTimeOffset(Now.Date, TimeSpan.Zero), new DateTimeOffset(Now.Date, TimeSpan.Zero), "one", null, SnapshotSource.ProviderApi),
            new(key, facts.Used ?? new CountQuantity(facts.UsedPercent ?? 0, facts.Unit), Now, Now, "one", null, SnapshotSource.ProviderApi)]);
    }
    private static LimitCardModel Card(LedgerLimit data, PersonalCap? cap = null, DateTimeOffset? now = null, DateOnly? workToday = null) =>
        LiveLedgerProjection.Card(data, cap, now ?? Now, TimeZoneInfo.Utc, BudgetSettingsModel.MondayToFriday, workToday, false);

    [Fact]
    public void WeeklyUsesCoreBudgetAndRetainsUnknownAsUnknown()
    {
        var card = Card(Data(Weekly()));
        Assert.Equal(40, card.Figures.Used);
        Assert.Equal(20, card.Figures.DayStart);
        Assert.Equal(40, card.Figures.TodayEnd);
        Assert.Equal(CardState.TodayUsed, card.State);
        var unknown = Card(new(Weekly(null), new(Account.ToString("N"), Weekly().Key), []));
        Assert.Null(unknown.Figures.Used);
        Assert.Equal(CardState.ValueUnknown, unknown.State);
    }

    [Fact]
    public void ZeroUnlimitedAndUnmatchedCurrencyStayDistinct()
    {
        var facts = new LimitFacts(new("copilot", "GH-P", "premium"), LimitKind.CountablePool, "requests", FactValue.Finite(new CountQuantity(0, "requests")))
        { Used = new CountQuantity(0, "requests"), AllowsCalendarFallback = true };
        Assert.Equal(CardState.NotIncluded, Card(Data(facts, 0)).State);
        Assert.Equal(CardState.NoCap, Card(Data(facts with { Limit = FactValue.Unlimited }, 0)).State);
        var money = facts with { Kind = LimitKind.MonetaryPool, Unit = "USD", Limit = FactValue.Unknown, Used = new MoneyQuantity(100, 2, "USD") };
        var mismatch = Card(new(money, new(Account.ToString("N"), money.Key), []), new(new MoneyQuantity(1000, 2, "EUR"), Now));
        Assert.Equal(CapStatus.CurrencyMismatch, mismatch.Cap!.Status);
        Assert.Null(mismatch.Figures.EffectiveLimit);
    }

    [Fact]
    public void WorkTodayAndPastResetNeverInventFreshQuota()
    {
        var sunday = Now.AddDays(-1);
        var facts = Weekly(20);
        var key = new ReadingSeriesKey(Account.ToString("N"), facts.Key);
        var data = new LedgerLimit(facts, key, [new(key, new CountQuantity(20, "percent"), new DateTimeOffset(sunday.Date, TimeSpan.Zero), sunday, "one", null, SnapshotSource.ProviderApi)]);
        Assert.Equal(CardState.DayOff, Card(data, now: sunday).State);
        var extra = Card(data, now: sunday, workToday: DateOnly.FromDateTime(sunday.Date));
        Assert.Equal(CardState.OnTrack, extra.State);
        Assert.Contains(extra.Marks, m => m.Kind == MarkKind.ExtraDay);
        var expired = Card(data, now: Now.AddDays(5));
        Assert.Equal(CardState.NotReady, expired.State);
        Assert.Contains(expired.Marks, m => m.Kind == MarkKind.PastReset);
        Assert.Null(expired.Figures.TodayEnd);
    }

    [Fact]
    public void OnlyProvenSharedWindowsPairAndScopesRemainSeparate()
    {
        var weekly = Weekly();
        var shortWindow = weekly with { Key = new("claude", "CL-S", "short"), Duration = TimeSpan.FromHours(5), Reset = new(Now.AddHours(3), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var scoped = weekly with { Key = new("claude", "CL-M", "Custom / Scope") };
        var quota = new QuotaSnapshot(Now, null, [], null, null, null, null) { Limits = new(Now, null, SnapshotSource.ProviderApi, "test", [weekly, shortWindow, scoped]) };
        var account = new AccountSnapshot(Account, "claude", true, new(ProviderSessionStatus.QuotaAvailable, quota), false, null);
        var projected = LiveLedgerProjection.Account(account, "Work", [Data(weekly), Data(shortWindow), Data(scoped)], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
        Assert.Equal(2, projected.Cards.Count);
        Assert.Contains(projected.Cards, c => c.Layout == CardLayout.FiveHourAndPeriod);
        Assert.Contains(projected.Cards, c => c.ScopeLabel == "Custom / Scope" && c.Layout == CardLayout.Period);
        var other = LiveLedgerProjection.Account(account with { AccountId = Guid.NewGuid() }, "Work", [Data(weekly)], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
        Assert.NotEqual(projected.Cards[0].CardId, other.Cards[0].CardId);
    }

    [Fact]
    public void HistoryHasThirtyFiveDaysAndNoReadingsStayGaps()
    {
        var history = LiveLedgerProjection.History(Data(Weekly()), Now, TimeZoneInfo.Utc);
        Assert.Equal(35, history.Days.Count);
        Assert.All(history.Days.Take(34), d => Assert.Null(d.Used));
        Assert.Equal(20, history.Days[^1].Used);
    }
}
