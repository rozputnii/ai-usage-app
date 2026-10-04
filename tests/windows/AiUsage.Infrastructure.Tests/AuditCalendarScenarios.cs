using System.Text.Json;
using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed partial class AuditScenarioTests
{
    [Theory]
    [InlineData("CAL-01")]
    [InlineData("CAL-02")]
    [InlineData("CAL-04")]
    public async Task CalendarRolloverAndWorkTodayUseLocalPeriodsWithoutChangingProviderFacts(string scenario)
    {
        var workToday = scenario == "CAL-04";
        var before = workToday ? new DateTimeOffset(2026, 10, 10, 23, 59, 59, TimeSpan.Zero)
            : new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.Zero);
        var after = scenario == "CAL-01" ? before.AddMinutes(5).AddSeconds(1) : before.AddSeconds(1);
        var item = new Case(scenario, "claude", Money(workToday ? 125.5m : 1900, 2000), workToday ? 120 : 1900,
            CardState.DayOff, workToday ? 125.5m : 1900, workToday ? 237.5m : 2000, CardLayout.Pool, Time: before);
        var input = Input(item, workToday ? 4304 : scenario == "CAL-01" ? 4301 : 4302);
        var account = input.Accounts[0];
        var next = account with { Session = Parse("claude", Money(workToday ? 125.5m : 10, 2000), after) };
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-calendar-" + Guid.NewGuid().ToString("N"));
        using var store = new LocalBudgetStore(root);
        try
        {
            await store.AppendAsync(input.Observations, CancellationToken.None);
            var capture = new CapturingStore(store);
            await new QuotaObservationRecorder(capture).RecordAsync(account.AccountId.ToString("N"), "claude", account.Session, CancellationToken.None);
            input = input with { Observations = [.. input.Observations, .. capture.Observations], NextAccounts = [next], NextNow = after };
            async Task<(AccountModel Account, LedgerLimit Data)> Project(AccountSnapshot current, DateTimeOffset at, DateOnly? extra = null)
            {
                var fact = Assert.Single(current.Session.Quota!.Limits!.Limits);
                var series = new ReadingSeriesKey(current.AccountId.ToString("N"), fact.Key);
                var data = LiveLedgerProjection.AccountLimits([new(fact, series, (await store.ReadAsync(series, CancellationToken.None)).Value)])[0];
                return (LiveLedgerProjection.Account(current, input.Labels[current.AccountId.ToString("N")], [data],
                    input.Configuration, at, TimeZoneInfo.Utc, extra), data);
            }
            var initial = await Project(account, before);
            var card = Assert.Single(initial.Account.Cards);
            Assert.Equal(CardState.DayOff, card.State);
            Assert.Equal(item.TodayEnd, card.Figures.TodayEnd);
            Assert.Equal(ResetProvenance.Assumed, card.Reset!.Provenance);
            if (workToday)
            {
                var active = Assert.Single((await Project(account, before, new(2026, 10, 10))).Account.Cards);
                Assert.Equal(CardState.OnTrack, active.State);
                Assert.Equal(237.5m, active.Figures.TodayEnd);
                Assert.Contains(active.Marks, mark => mark.Kind == MarkKind.ExtraDay);
            }
            input = input with { ExpectedStates = new() { [card.CardId] = card.State } };
            await new QuotaObservationRecorder(capture).RecordAsync(account.AccountId.ToString("N"), "claude", next.Session, CancellationToken.None);
            var refreshed = await Project(next, after, workToday ? new(2026, 10, 10) : null);
            var nextCard = Assert.Single(refreshed.Account.Cards);
            Assert.Equal(CardState.DayOff, nextCard.State);
            Assert.DoesNotContain(nextCard.Marks, mark => mark.Kind == MarkKind.ExtraDay);
            Assert.Equal(workToday ? 125.5m : 0, nextCard.Figures.DayStart);
            Assert.Equal(workToday ? 125.5m : 10, nextCard.Figures.Used);
            Assert.Equal(workToday ? 242.65m : 90.90m, nextCard.Figures.TodayEnd);
            var history = LiveLedgerProjection.History(refreshed.Data, after, TimeZoneInfo.Utc);
            Assert.Equal(workToday ? 0 : 10, history.Days[^1].Used);
            if (!workToday)
            {
                Assert.Contains(new DateOnly(2026, 11, 1), history.ResetDays);
                Assert.Equal(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero), nextCard.Reset!.At);
                Assert.Contains(refreshed.Data.Runs, run => run.Value == new MoneyQuantity(190000, 2, "USD"));
            }
            var directory = Environment.GetEnvironmentVariable("AIU_AUDIT_TRANSITION_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, scenario + ".json"), JsonSerializer.Serialize(input, AiUsage.Adapters.Live.Audit.AuditJson.Default.AuditInput));
                File.WriteAllText(Path.Combine(directory, scenario + ".next.expectations.json"), JsonSerializer.Serialize(new { nextCard.CardId,
                    Now = after, nextCard.Figures.DayStart, nextCard.Figures.Used, nextCard.Figures.TodayEnd, HistoryToday = history.Days[^1].Used,
                    State = nextCard.State.ToString(), Reset = nextCard.Reset!.At }));
            }
        }
        finally { store.Dispose(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
