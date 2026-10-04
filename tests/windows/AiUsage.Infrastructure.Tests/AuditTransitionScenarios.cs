using System.Text.Json;
using System.Text.Json.Nodes;
using AiUsage.Adapters.Live;
using AiUsage.Adapters.Live.Audit;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed partial class AuditScenarioTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshCanGainOrLoseSubscriptionWindowsWithoutLosingMoneyCapsOrHistory(bool gain)
    {
        var scenario = gain ? "TRANS-01" : "TRANS-02";
        var money = new Case(scenario, "claude", Money(125.5m), 120, CardState.CapClose, 125.5m, 126.66m, CardLayout.Pool, 240);
        var input = Input(money, gain ? 4101 : 4102);
        var id = input.Accounts[0].AccountId;
        string WithWindows(decimal amount)
        {
            var json = JsonNode.Parse(Week("claude", 45, 35, true))!.AsObject();
            json["spend"] = JsonNode.Parse(Money(amount))!["spend"]!.DeepClone();
            return json.ToJsonString();
        }
        if (!gain)
        {
            var subscription = Input(money with { Json = Week("claude", 45, 35, true), Baseline = 40 }, 4102);
            input = input with { Accounts = [input.Accounts[0] with { Session = Parse("claude", WithWindows(125.5m), Now) }],
                Observations = [.. input.Observations, .. subscription.Observations] };
        }
        var nextNow = Now.AddMinutes(1);
        var nextJson = gain ? WithWindows(130) : Money(130);
        var nextAccount = input.Accounts[0] with { Session = Parse("claude", nextJson, nextNow) };
        input = input with { NextAccounts = [nextAccount], NextNow = nextNow };
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-transition-" + Guid.NewGuid().ToString("N"));
        using var store = new LocalBudgetStore(root);
        try
        {
            await store.AppendAsync(input.Observations, CancellationToken.None);
            var capture = new CapturingStore(store);
            var initialStates = new Dictionary<string, CardState>();
            var nextExpected = new List<object>();
            string? moneyCardId = null;
            for (var phase = 0; phase < 2; phase++)
            {
                var account = phase == 0 ? input.Accounts[0] : nextAccount;
                var now = phase == 0 ? Now : nextNow;
                await new QuotaObservationRecorder(capture).RecordAsync(id.ToString("N"), "claude", account.Session, CancellationToken.None);
                var limits = new List<LedgerLimit>();
                foreach (var fact in account.Session.Quota!.Limits!.Limits)
                {
                    var series = new ReadingSeriesKey(id.ToString("N"), fact.Key);
                    limits.Add(new(fact, series, (await store.ReadAsync(series, CancellationToken.None)).Value));
                }
                var model = LiveLedgerProjection.Account(account, input.Labels[id.ToString("N")], limits,
                    input.Configuration, now, TimeZoneInfo.Utc, null);
                var hasWindows = phase == 0 ? !gain : gain;
                Assert.Equal(hasWindows ? 2 : 1, model.Cards.Count);
                var moneyLimit = Assert.Single(limits, l => l.Facts.Key.Family == "CL-X");
                var card = Assert.Single(model.Cards, c => c.CardId == LiveLedgerProjection.CardId(moneyLimit.Series));
                moneyCardId ??= card.CardId;
                Assert.Equal(moneyCardId, card.CardId);
                Assert.Equal(phase == 0 ? 125.5m : 130m, card.Figures.Used);
                Assert.Equal(hasWindows ? CardLayout.Note : CardLayout.Pool, card.Layout);
                Assert.Equal(hasWindows ? CardState.PeriodUnknown : phase == 0 ? CardState.CapClose : CardState.OverToday, card.State);
                Assert.Equal(hasWindows ? null : 126.66m, card.Figures.TodayEnd);
                Assert.Equal(240, card.Cap!.Amount);
                Assert.Equal(hasWindows ? CapStatus.Inactive : CapStatus.Applied, card.Cap.Status);
                Assert.Equal(phase == 0 ? 5.5m : 10m, Assert.Single(LiveLedgerProjection.History(moneyLimit, now, TimeZoneInfo.Utc).Days, d => d.Used is not null).Used);
                if (hasWindows)
                {
                    var weekly = Assert.Single(model.Cards, c => c.CardId != moneyCardId);
                    Assert.Equal(CardLayout.FiveHourAndPeriod, weekly.Layout);
                    Assert.Equal(CardState.OnTrack, weekly.State);
                    Assert.Equal(45, weekly.Figures.Used);
                    Assert.Equal(phase == 0 ? 60 : 63.3m, weekly.Figures.TodayEnd);
                }
                if (phase == 0)
                {
                    foreach (var item in model.Cards) initialStates.Add(item.CardId, item.State);
                    input = input with { Observations = [.. input.Observations, .. capture.Observations] };
                }
                else foreach (var item in model.Cards) nextExpected.Add(new { Id = scenario, item.CardId, State = item.State.ToString(),
                    Layout = item.Layout.ToString(), item.Figures.Used, item.Figures.TodayEnd, Now = nextNow, MoneyCardId = moneyCardId, HistoryToday = 10m });
            }
            var directory = Environment.GetEnvironmentVariable("AIU_AUDIT_TRANSITION_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, scenario + ".json"), JsonSerializer.Serialize(input with { ExpectedStates = initialStates }, AuditJson.Default.AuditInput));
                File.WriteAllText(Path.Combine(directory, scenario + ".next.expectations.json"), JsonSerializer.Serialize(nextExpected));
                File.WriteAllText(Path.Combine(directory, scenario + ".provider.txt"), (gain ? Money(125.5m) : WithWindows(125.5m)) + Environment.NewLine + nextJson);
            }
        }
        finally { store.Dispose(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
