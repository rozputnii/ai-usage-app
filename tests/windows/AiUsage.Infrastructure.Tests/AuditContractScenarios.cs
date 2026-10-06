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
    [InlineData("EXTRA-01", true)]
    [InlineData("EXTRA-02-unknown", false)]
    [InlineData("EXTRA-02-shared", false)]
    [InlineData("EXTRA-02-disabled", false)]
    [InlineData("EXTRA-02-stale", false)]
    [InlineData("EXTRA-02-missing-baseline", false)]
    [InlineData("EXTRA-02-correction", false)]
    [InlineData("EXTRA-02-currency", false)]
    [InlineData("EXTRA-02-other-account", false)]
    public async Task ExtraUsageUsesNativeSpendEvidenceAndExplicitScopeWithoutCurrencyConversion(string scenario, bool expectedExtra)
    {
        var body = JsonNode.Parse(Week("claude", 100))!.AsObject();
        var spendBody = JsonNode.Parse(Money(12.75m, 300, scenario == "EXTRA-02-disabled" ? false : true))!.AsObject();
        body["spend"] = spendBody["spend"]!.DeepClone();
        var payload = body.ToJsonString();
        var input = Input(new Case(scenario, "claude", payload, null, CardState.UsedUp, 100, null, CardLayout.Period), 4501);
        var account = input.Accounts[0]; var facts = account.Session.Quota!.Limits!.Limits;
        var week = facts.Single(f => f.Key.Family == "CL-W"); var spend = facts.Single(f => f.Key.Family == "CL-X");
        var weeklyKey = new ReadingSeriesKey(account.AccountId.ToString("N"), week.Key);
        var moneyKey = new ReadingSeriesKey(account.AccountId.ToString("N"), spend.Key);
        var fill = new DateTimeOffset(Now.Date, TimeSpan.Zero).AddHours(-2);
        var observations = new List<ReadingObservation>
        {
            new(weeklyKey, new CountQuantity(90, "percent"), fill.AddHours(-1)) { ResetAt = week.Reset!.At },
            new(weeklyKey, new CountQuantity(100, "percent"), fill) { ResetAt = week.Reset.At },
        };
        var baselineKey = scenario == "EXTRA-02-other-account" ? moneyKey with { AccountTarget = "other-synthetic-account" } : moneyKey;
        if (scenario != "EXTRA-02-missing-baseline") observations.Add(new(baselineKey, new MoneyQuantity(1000, 2, "USD"), fill));
        observations.Add(new(moneyKey, new MoneyQuantity(scenario == "EXTRA-02-correction" ? 900 : 1100, 2,
            scenario == "EXTRA-02-currency" ? "EUR" : "USD"), new DateTimeOffset(Now.Date, TimeSpan.Zero)));
        var moneyId = LiveLedgerProjection.CardId(moneyKey);
        input = input with { Observations = observations.ToArray() };
        Dictionary<string, MonetaryScope> scopes = scenario == "EXTRA-02-unknown" ? []
            : new() { [moneyId] = scenario == "EXTRA-02-shared" ? MonetaryScope.Shared : MonetaryScope.Account };
        input.Labels[account.AccountId.ToString("N")] += " · explicit presentation scope";
        if (scenario == "EXTRA-02-stale")
        {
            account = account with { Session = account.Session with { Quota = account.Session.Quota with { FetchedAt = Now.AddMinutes(-16) } } };
            input = input with { Accounts = [account] };
        }
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-extra-" + Guid.NewGuid().ToString("N"));
        using var store = new LocalBudgetStore(root);
        try
        {
            await store.AppendAsync(input.Observations, CancellationToken.None); var capture = new CapturingStore(store);
            await new QuotaObservationRecorder(capture).RecordAsync(account.AccountId.ToString("N"), "claude", account.Session, CancellationToken.None);
            var data = new List<LedgerLimit>();
            foreach (var fact in facts)
            {
                var key = new ReadingSeriesKey(account.AccountId.ToString("N"), fact.Key);
                data.Add(new(fact, key, (await store.ReadAsync(key, CancellationToken.None)).Value)
                    { MonetaryScope = scopes.GetValueOrDefault(LiveLedgerProjection.CardId(key)) });
            }
            var model = LiveLedgerProjection.Account(account, input.Labels[account.AccountId.ToString("N")], data, input.Configuration, input.Now, TimeZoneInfo.Utc, null);
            var card = Assert.Single(model.Cards, c => c.CardId == LiveLedgerProjection.CardId(weeklyKey));
            Assert.Equal(CardState.UsedUp, card.State); Assert.Equal(100, card.Figures.Used);
            Assert.Equal(expectedExtra, card.Marks.Any(m => m.Kind == MarkKind.OnExtraUsage));
            if (expectedExtra)
            {
                var mark = Assert.Single(card.Marks, m => m.Kind == MarkKind.OnExtraUsage);
                Assert.Equal(2.75m, mark.Amount); Assert.Equal("USD", mark.Currency); Assert.Equal(2, mark.Exponent); Assert.Equal(fill, mark.Since);
            }
            Assert.Equal(new MoneyQuantity(1275, 2, "USD"), spend.Used);
            input = input with { Observations = [.. input.Observations, .. capture.Observations], ExpectedStates = new() { [card.CardId] = card.State } };
            // Weekly visual expectations are independent constants. Subordinate monetary rendering is exercised separately.
            ExportContractScenario(scenario, input, [card], payload);
        }
        finally { store.Dispose(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("MONEY-01", true)]
    [InlineData("MONEY-02", false)]
    public async Task ExplicitScopeAnnotationKeepsSharedRestrictionsAndAccountIsolation(string scenario, bool shared)
    {
        var item = new Case(scenario, "claude", Money(125.50m), 120, CardState.OnTrack, 125.50m, 130, CardLayout.Pool, Cap: 240);
        var first = Input(item, 4401); var second = Input(item, 4402);
        var firstId = LiveLedgerProjection.CardId(new(first.Accounts[0].AccountId.ToString("N"), first.Accounts[0].Session.Quota!.Limits!.Limits[0].Key));
        var secondId = LiveLedgerProjection.CardId(new(second.Accounts[0].AccountId.ToString("N"), second.Accounts[0].Session.Quota!.Limits!.Limits[0].Key));
        first.Labels[first.Accounts[0].AccountId.ToString("N")] += " · explicit scope annotation";
        second.Labels[second.Accounts[0].AccountId.ToString("N")] += " · scope unknown";
        var input = first with { Accounts = [.. first.Accounts, .. second.Accounts], Labels = first.Labels.Concat(second.Labels).ToDictionary(),
            Observations = [.. first.Observations, .. second.Observations] };
        var scopes = new Dictionary<string, MonetaryScope> { [firstId] = shared ? MonetaryScope.Shared : MonetaryScope.Account };
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-scope-" + Guid.NewGuid().ToString("N"));
        using var store = new LocalBudgetStore(root);
        try
        {
            await store.SaveConfigurationAsync(input.Configuration, CancellationToken.None); await store.AppendAsync(input.Observations, CancellationToken.None);
            var capture = new CapturingStore(store); var cards = new List<LimitCardModel>();
            foreach (var account in input.Accounts)
            {
                await new QuotaObservationRecorder(capture).RecordAsync(account.AccountId.ToString("N"), "claude", account.Session, CancellationToken.None);
                var facts = Assert.Single(account.Session.Quota!.Limits!.Limits); var key = new ReadingSeriesKey(account.AccountId.ToString("N"), facts.Key);
                var data = new LedgerLimit(facts, key, (await store.ReadAsync(key, CancellationToken.None)).Value)
                    { MonetaryScope = scopes.GetValueOrDefault(LiveLedgerProjection.CardId(key)) };
                cards.Add(Assert.Single(LiveLedgerProjection.Account(account, input.Labels[account.AccountId.ToString("N")], [data], input.Configuration, input.Now, TimeZoneInfo.Utc, null).Cards));
            }
            var annotated = cards[0]; var ordinary = cards[1];
            Assert.Equal(shared ? CardLayout.Note : CardLayout.Pool, annotated.Layout);
            Assert.Equal(shared ? CardState.PeriodUnknown : CardState.CapClose, annotated.State);
            Assert.Equal(125.50m, annotated.Figures.Used);
            Assert.Equal(shared ? null : firstId, annotated.CapTargetId);
            Assert.Equal(shared ? CapStatus.Inactive : CapStatus.Applied, annotated.Cap!.Status);
            Assert.Contains(shared ? "Shared spending" : "Monthly work budget", annotated.Monetary!.Qualification, StringComparison.Ordinal);
            Assert.Equal(CardLayout.Pool, ordinary.Layout); Assert.Equal(CardState.OnTrack, ordinary.State);
            Assert.Equal(secondId, ordinary.CapTargetId); Assert.Null(ordinary.Cap);
            Assert.Equal(new MoneyQuantity(12550, 2, "USD"), input.Accounts[0].Session.Quota!.Limits!.Limits[0].Used);
            Assert.Equal(shared ? (decimal?)null : 126.66m, annotated.Figures.TodayEnd); Assert.Equal(130, ordinary.Figures.TodayEnd);
            input = input with { Observations = [.. input.Observations, .. capture.Observations], ExpectedStates = cards.ToDictionary(c => c.CardId, c => c.State) };
            ExportContractScenario(scenario, input, cards, item.Json);
        }
        finally { store.Dispose(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void ExportContractScenario(string scenario, AuditInput input, IReadOnlyList<LimitCardModel> cards, string providerInput)
    {
        var directory = Environment.GetEnvironmentVariable("AIU_AUDIT_COMPOSITE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, scenario + ".json"), JsonSerializer.Serialize(input, AuditJson.Default.AuditInput));
        File.WriteAllText(Path.Combine(directory, scenario + ".expectations.json"), JsonSerializer.Serialize(cards.Select(card => new
        {
            Id = scenario, card.CardId, Provider = "claude", input.Now, Used = card.Figures.Used, TodayEnd = card.Figures.TodayEnd,
            Layout = card.Layout.ToString(), State = card.State.ToString(), Cap = card.Cap?.Amount, Deterministic = "PASS",
            Test = "AuditScenarioTests explicit presentation scope annotations with production parsing/recording/projection"
        })));
        File.WriteAllText(Path.Combine(directory, scenario + ".provider.txt"), providerInput);
    }
}
