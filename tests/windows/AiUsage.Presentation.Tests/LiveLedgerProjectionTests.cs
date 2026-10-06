using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using Xunit;
using FactValue = AiUsage.Core.Usage.LimitValue;

namespace AiUsage.Presentation.Tests;

public sealed class LiveLedgerProjectionTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void ExpiredShortWindowDoesNotClaimCurrentUsageOrDiscardTheWeeklyBudget(int seconds, bool expired)
    {
        var reset = Now.AddHours(3);
        var weekly = Weekly();
        var shortWindow = weekly with { Key = new("claude", "CL-S", "short"), UsedPercent = 100,
            Duration = TimeSpan.FromHours(5), Reset = new(reset, ValueOrigin.Provider, ResetMeaning.Replenish) };
        var account = new AccountSnapshot(Account, "claude", true,
            new(ProviderSessionStatus.QuotaAvailable, new QuotaSnapshot(Now, null, [], null, null, null, null)), false, null);
        var now = reset.AddSeconds(seconds);
        var model = LiveLedgerProjection.Account(account, "SYNTHETIC short reset", [Data(weekly), Data(shortWindow)],
            BudgetConfiguration.Default, now, TimeZoneInfo.Utc, null);
        var card = Assert.Single(model.Cards);
        Assert.Equal(40, card.Figures.Used);
        Assert.Equal(40, card.Figures.TodayEnd);
        if (expired)
        {
            Assert.Null(card.FiveHour);
            Assert.Equal(CardLayout.Period, card.Layout);
            Assert.Equal(CardState.TodayUsed, card.State);
            Assert.Contains(card.Marks, m => m.Kind == MarkKind.PastReset && m.Since == reset);
            var visual = CardVisuals.Build(card, model, ValueMode.Used, now);
            Assert.Contains(visual.Marks, m => m.Text == "5h past reset");
            Assert.DoesNotContain(visual.Cells.SelectMany(c => c.Tip), line => line.Contains("Current 5h window", StringComparison.Ordinal));
        }
        else
        {
            Assert.Equal(CardState.FiveHourFull, card.State);
            Assert.Equal(100, card.FiveHour!.CurrentWindowUsed);
            Assert.Equal(CardLayout.FiveHourAndPeriod, card.Layout);
        }
        Assert.Equal(100, shortWindow.UsedPercent); // Retain the provider fact without inventing a replacement.
    }

    [Fact]
    public void SuccessfulEmptyQuotaUsesTheSupportedNoDisplayedLimitsState()
    {
        foreach (var provider in new[] { "claude", "codex", "copilot", "antigravity" })
        {
            var account = new AccountSnapshot(Account, provider, true,
                new(ProviderSessionStatus.QuotaAvailable, new QuotaSnapshot(Now, null, [], null, null, null, null)), false, null);
            var model = LiveLedgerProjection.Account(account, "Synthetic empty", [], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
            var card = Assert.Single(model.Cards);
            Assert.Equal(CardState.NoDisplayedLimits, card.State);
            Assert.Contains(CardVisuals.Build(card, model, ValueMode.Used, Now).NoteLines,
                line => line.Value == "No subscription limits to display");
        }
    }

    [Fact]
    public void TimestampResetAndReadingClocksUseTheSelectedLocalZone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
        var weekly = Weekly();
        var shortWindow = weekly with { Key = new("claude", "CL-S", "short"), Duration = TimeSpan.FromHours(5),
            Reset = new(Now.AddHours(3), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var quota = new QuotaSnapshot(Now, null, [], null, null, null, null);
        var account = new AccountSnapshot(Account, "claude", true, new(ProviderSessionStatus.QuotaAvailable, quota,
            Failure: ProviderFailureKind.NetworkFailure), false, Now);
        var model = LiveLedgerProjection.Account(account, "Synthetic clocks", [Data(weekly), Data(shortWindow)], BudgetConfiguration.Default, Now, zone, null);
        var card = Assert.Single(model.Cards);
        Assert.Equal(TimeSpan.FromHours(1), card.Reset!.At!.Value.Offset);
        Assert.Equal(1, card.Reset.At.Value.Hour); // Provider reset is UTC midnight.
        Assert.Equal(16, card.FiveHour!.CurrentWindowEndsAt!.Value.Hour); // 15:00 UTC -> 16:00 local.
        Assert.Equal(13, model.LastReadingAt!.Value.Hour);
        Assert.Equal(13, model.LastSyncFailedAt!.Value.Hour);
        Assert.Equal(13, card.Marks.Single(m => m.Kind == MarkKind.SyncFailed).Since!.Value.Hour);
        Assert.Equal(Now.AddHours(3), card.FiveHour.CurrentWindowEndsAt); // Same instant.
    }
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
    public void CodexSubscriptionWindowsDoNotDisplayACreditBalanceCard()
    {
        var weekly = Weekly() with { Key = new("codex", "CX-S", "weekly") };
        var credits = new LimitFacts(new("codex", "CX-B", "credits"), LimitKind.CountablePool, "credits", FactValue.Unknown)
        { Remaining = new CountQuantity(0, "credits"), HasCredits = false, AllowsCalendarFallback = true };
        var account = new AccountSnapshot(Account, "codex", true,
            new(ProviderSessionStatus.QuotaAvailable, new QuotaSnapshot(Now, "prolite", [], null, null, null, null)), false, null);
        var model = LiveLedgerProjection.Account(account, "Codex prolite", [Data(weekly), Data(credits)], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
        Assert.Equal(LiveLedgerProjection.CardId(Data(weekly).Series), Assert.Single(model.Cards).CardId);
        var onlyCredits = LiveLedgerProjection.Account(account, "Codex", [Data(credits)], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
        Assert.Equal(ScaleKind.Count, Assert.Single(onlyCredits.Cards).Scale.Kind);
    }

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
    public void MonetaryReadingsRemainVisibleUnderTheirAccountWithoutClassification()
    {
        var spend = new LimitFacts(new("claude", "CL-X", "extra"), LimitKind.MonetaryPool, "USD", FactValue.Finite(new MoneyQuantity(5000, 2, "USD")))
        { Used = new MoneyQuantity(100, 2, "USD"), AllowsCalendarFallback = true };
        var account = new AccountSnapshot(Account, "claude", true,
            new(ProviderSessionStatus.QuotaAvailable, new QuotaSnapshot(Now, null, [], null, null, null, null)), false, null);
        foreach (var enabled in new bool?[] { true, false, null })
        foreach (var name in new[] { "Work", "Personal", "Anything" })
        {
            var extra = Data(spend with { Enabled = enabled });
            var model = LiveLedgerProjection.Account(account, name, [Data(Weekly()), extra], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
            var only = LiveLedgerProjection.Account(account, name, [extra], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
            // Disabled spending is hidden beside subscription windows but stays visible on a spending-only account.
            Assert.Equal(enabled == false ? 1 : 2, model.Cards.Count);
            if (enabled == false) model = only;
            var money = Assert.Single(model.Cards, c => c.ScopeLabel == "Spending");
            Assert.Equal(LiveLedgerProjection.CardId(extra.Series), money.CardId);
            Assert.Equal(1m, money.Figures.Used);
            Assert.Null(money.Figures.TodayEnd);
            Assert.Null(money.Figures.EffectiveLimit);
            Assert.Equal(money.CardId, Assert.Single(only.Cards).CardId);
            var visual = CardVisuals.Build(money, model, ValueMode.Left, Now);
            Assert.Contains(visual.NoteLines, l => l.Value.Contains("1.00 USD", StringComparison.Ordinal));
            Assert.Contains(visual.NoteLines, l => l.Value.Contains("50.00 USD", StringComparison.Ordinal));
            Assert.DoesNotContain(visual.NoteLines, l => l.Value.Contains("left", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void HistoryHasThirtyFiveDaysAndNoReadingsStayGaps()
    {
        var history = LiveLedgerProjection.History(Data(Weekly()), Now, TimeZoneInfo.Utc);
        Assert.Equal(35, history.Days.Count);
        Assert.All(history.Days.Take(34), d => Assert.Null(d.Used));
        Assert.Equal(20, history.Days[^1].Used);
    }

    [Fact]
    public void CapNearTodayAllowanceAndReachedRemainDistinctFromProviderUsedUp()
    {
        var facts = Weekly() with { Key = new("copilot", "GH-P", "premium"), Kind = LimitKind.CountablePool, Unit = "requests",
            UsedPercent = null, Used = new CountQuantity(488, "requests"), Limit = FactValue.Finite(new CountQuantity(1000, "requests")) };
        var cap = new PersonalCap(new CountQuantity(500, "requests"), Now);
        Assert.Equal(CardState.CapClose, Card(Data(facts, 485), cap).State);
        Assert.Equal(CardState.CapReached, Card(Data(facts with { Used = new CountQuantity(500, "requests") }, 485), cap).State);
        Assert.Equal(CardState.OverCap, Card(Data(facts with { Used = new CountQuantity(501, "requests") }, 485), cap).State);
        Assert.Equal(CardState.UsedUp, Card(Data(facts with { Used = new CountQuantity(1000, "requests") }, 485), cap).State);
    }

    [Fact]
    public void LastWorkDayRushRequiresProviderReplenishmentAndNoCap()
    {
        var thursday = Now.AddDays(3);
        var key = new ReadingSeriesKey(Account.ToString("N"), Weekly().Key);
        var facts = Weekly(50);
        var data = new LedgerLimit(facts, key, [new(key, new CountQuantity(40, "percent"), new DateTimeOffset(thursday.Date, TimeSpan.Zero), thursday, "one", null, SnapshotSource.ProviderApi)]);
        Assert.Equal(CardState.Rush, Card(data, now: thursday).State);
        var assumed = facts with { Reset = null, Duration = null, AllowsCalendarFallback = true };
        Assert.NotEqual(CardState.Rush, Card(data with { Facts = assumed }, now: thursday).State);
    }

    [Fact]
    public void FailedStaleAccountRetainsItsFiguresAndFailureMark()
    {
        var facts = Weekly();
        var fetched = Now.AddHours(-1);
        var quota = new QuotaSnapshot(fetched, null, [], null, null, null, null) { Limits = new(fetched, null, SnapshotSource.ProviderApi, "test", [facts]) };
        var snapshot = new AccountSnapshot(Account, "claude", true,
            new(ProviderSessionStatus.QuotaAvailable, quota, Failure: ProviderFailureKind.RateLimited), false, Now);
        var model = LiveLedgerProjection.Account(snapshot, "Work", [Data(facts)], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
        Assert.Equal(AccountHealth.SyncFailedStale, model.Health);
        Assert.True(model.Cards[0].Freshness.IsStale);
        Assert.Equal(40, model.Cards[0].Figures.Used);
        Assert.Contains(model.Cards[0].Marks, m => m.Kind == MarkKind.SyncFailed && m.Since == Now);
    }

    [Fact]
    public void ObservedExtraUsageIsProjectedOnlyWithSpendCoverageAtFill()
    {
        var facts = Weekly(100);
        var key = new ReadingSeriesKey(Account.ToString("N"), facts.Key);
        var fill = Now.AddHours(-2);
        var below = new ReadingRun(key, new CountQuantity(90, "percent"), fill.AddHours(-1), fill.AddHours(-1), "week", null, SnapshotSource.ProviderApi) { ResetAt = facts.Reset!.At };
        var full = below with { Value = new CountQuantity(100, "percent"), FirstSeen = fill, LastConfirmed = Now };
        var spend = new LimitFacts(new("claude", "CL-X", "extra"), LimitKind.MonetaryPool, "USD", FactValue.Unknown)
            { Used = new MoneyQuantity(1275, 2, "USD"), AllowsCalendarFallback = true };
        var spendKey = new ReadingSeriesKey(Account.ToString("N"), spend.Key);
        var start = new ReadingRun(spendKey, new MoneyQuantity(1000, 2, "USD"), fill, fill, "month", null, SnapshotSource.ProviderApi);
        var end = start with { Value = new MoneyQuantity(1275, 2, "USD"), FirstSeen = Now, LastConfirmed = Now };
        var quota = new QuotaSnapshot(Now, null, [], null, null, null, null) { Limits = new(Now, null, SnapshotSource.ProviderApi, "test", [facts, spend]) };
        var account = new AccountSnapshot(Account, "claude", true, new(ProviderSessionStatus.QuotaAvailable, quota), false, null);
        AccountModel Project(ReadingRun[] spends) => LiveLedgerProjection.Account(account, "Work", [new(facts, key, [below, full]), new(spend, spendKey, spends) { MonetaryScope = MonetaryScope.Account }], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
        var mark = Assert.Single(Project([start, end]).Cards[0].Marks, m => m.Kind == MarkKind.OnExtraUsage);
        Assert.Equal(2.75m, mark.Amount);
        Assert.Equal("USD", mark.Currency);
        Assert.DoesNotContain(Project([end]).Cards[0].Marks, m => m.Kind == MarkKind.OnExtraUsage);
    }

    [Fact]
    public void PairedWindowProjectsLearnedSessionCostWithoutInventingUnknownWeeklyRemainder()
    {
        var week = Weekly(40);
        var shortFacts = week with { Key = new("claude", "CL-S", "short"), Duration = TimeSpan.FromHours(5), UsedPercent = 30,
            Reset = new(Now.AddHours(3), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var weekData = Data(week);
        var shortKey = new ReadingSeriesKey(Account.ToString("N"), shortFacts.Key);
        List<ReadingRun> weeklyRuns = [.. weekData.Runs];
        List<ReadingRun> shortRuns = [];
        var samples = new[] { (50m, 6m), (40m, 4m), (70m, 9.8m) };
        for (int i = 0; i < samples.Length; i++)
        {
            var at = Now.AddDays(-3 + i);
            var shortStart = new ReadingRun(shortKey, new CountQuantity(10, "percent"), at, at, "short" + i, null, SnapshotSource.ProviderApi);
            shortRuns.Add(shortStart);
            shortRuns.Add(shortStart with { Value = new CountQuantity(10 + samples[i].Item1, "percent"), FirstSeen = at.AddHours(4), LastConfirmed = at.AddHours(4) });
            var weeklyStart = shortStart with { Series = weekData.Series, Value = new CountQuantity(20, "percent"), PeriodInstance = "one" };
            weeklyRuns.Add(weeklyStart);
            weeklyRuns.Add(weeklyStart with { Value = new CountQuantity(20 + samples[i].Item2, "percent"), FirstSeen = at.AddHours(4), LastConfirmed = at.AddHours(4) });
        }
        var quota = new QuotaSnapshot(Now, null, [], null, null, null, null) { Limits = new(Now, null, SnapshotSource.ProviderApi, "test", [week, shortFacts]) };
        var account = new AccountSnapshot(Account, "claude", true, new(ProviderSessionStatus.QuotaAvailable, quota), false, null);
        AccountModel Project(LimitFacts weekly) => LiveLedgerProjection.Account(account, "Work", [weekData with { Facts = weekly, Runs = weeklyRuns.OrderBy(r => r.FirstSeen).ToArray() }, new(shortFacts, shortKey, shortRuns)], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);
        var card = Assert.Single(Project(week).Cards);
        // Parts newest first: [100*8.8/71, 100*10.8/69], [100*3/41, 100*5/39], [100*5/51, 100*7/49];
        // pooled [12.394, 12.821] is settled, C = sqrt(L*H) = 12.6056, and floor(60 / C) = 4.
        Assert.Equal(12.6m, card.FiveHour!.WindowShare);
        Assert.Equal(4, card.FiveHour.WindowsLeftInPeriod);
        Assert.Null(Assert.Single(Project(week with { UsedPercent = null }).Cards).FiveHour!.WindowsLeftInPeriod);
    }

    [Fact]
    public void PairedCardCarriesRoughBoundsAndRange()
    {
        var week = Weekly(40);
        var shortFacts = week with { Key = new("claude", "CL-S", "short"), Duration = TimeSpan.FromHours(5), UsedPercent = 30,
            Reset = new(Now.AddHours(2), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var weekData = Data(week);
        var shortKey = new ReadingSeriesKey(Account.ToString("N"), shortFacts.Key);
        List<ReadingRun> weeklyRuns = [.. weekData.Runs];
        List<ReadingRun> shortRuns = [];
        for (int i = 0; i < 3; i++)
        {
            var at = Now.AddDays(-3 + i);
            var shortStart = new ReadingRun(shortKey, new CountQuantity(10, "percent"), at, at, "s" + i, null, SnapshotSource.ProviderApi);
            shortRuns.Add(shortStart);
            shortRuns.Add(shortStart with { Value = new CountQuantity(60, "percent"), FirstSeen = at.AddHours(4), LastConfirmed = at.AddHours(4) });
            var weeklyStart = shortStart with { Series = weekData.Series, Value = new CountQuantity(20 + 6 * i, "percent"), PeriodInstance = "one" };
            weeklyRuns.Add(weeklyStart);
            weeklyRuns.Add(weeklyStart with { Value = new CountQuantity(26 + 6 * i, "percent"), FirstSeen = at.AddHours(4), LastConfirmed = at.AddHours(4) });
        }
        var quota = new QuotaSnapshot(Now, null, [], null, null, null, null) { Limits = new(Now, null, SnapshotSource.ProviderApi, "test", [week, shortFacts]) };
        var account = new AccountSnapshot(Account, "claude", true, new(ProviderSessionStatus.QuotaAvailable, quota), false, null);
        AccountModel Project(IReadOnlyList<ReadingRun> weekly, IReadOnlyList<ReadingRun> shortSeries) =>
            LiveLedgerProjection.Account(account, "Work", [weekData with { Runs = weekly }, new(shortFacts, shortKey, shortSeries)], BudgetConfiguration.Default, Now, TimeZoneInfo.Utc, null);

        var rough = Assert.Single(Project(weeklyRuns.OrderBy(r => r.FirstSeen).ToArray(), shortRuns).Cards).FiveHour!;
        Assert.True(rough.Rough);
        Assert.Equal(9.8m, rough.WindowShareLow);
        Assert.Equal(14.3m, rough.WindowShareHigh);
        Assert.Equal(11.8m, rough.WindowShare);
        Assert.Equal(4, rough.WindowsLeftInPeriod);
        Assert.Equal(6, rough.WindowsLeftMax);
        Assert.Equal(3, rough.Windows);

        var card = Assert.Single(Project(weekData.Runs, []).Cards);
        Assert.Equal(CardLayout.FiveHourAndPeriod, card.Layout);
        Assert.Null(card.FiveHour!.WindowShare);
        Assert.False(card.FiveHour.Rough);
        Assert.Null(card.FiveHour.WindowsLeftMax);
    }
}
