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
    private sealed record ExpectedCard(int Account, string Family, string Native, decimal? Used, decimal? TodayEnd,
        CardLayout Layout, CardState State = CardState.OnTrack);

    [Theory]
    [InlineData("ACCT-01")]
    [InlineData("ACCT-02")]
    [InlineData("ACCT-03")]
    [InlineData("ACCT-04")]
    [InlineData("ACCT-05")]
    [InlineData("ACCT-06")]
    public async Task CompositeProviderAccountsKeepIndependentLimitsAndNativeFacts(string scenario)
    {
        var (cases, expected) = Composite(scenario);
        var inputs = cases.Select((item, index) => Input(item, 4000 + index)).ToArray();
        var input = new AuditInput(Now, "UTC", inputs.SelectMany(i => i.Accounts).ToArray(),
            inputs.SelectMany(i => i.Labels).ToDictionary(), inputs.SelectMany(i => i.Observations).ToArray(), BudgetConfiguration.Default);
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-composite-" + Guid.NewGuid().ToString("N"));
        using var store = new LocalBudgetStore(root);
        try
        {
            await store.AppendAsync(input.Observations, CancellationToken.None);
            var capture = new CapturingStore(store);
            var exported = new List<object>();
            var states = new Dictionary<string, CardState>();
            for (var accountIndex = 0; accountIndex < input.Accounts.Length; accountIndex++)
            {
                var account = input.Accounts[accountIndex];
                await new QuotaObservationRecorder(capture).RecordAsync(account.AccountId.ToString("N"), account.Provider, account.Session, CancellationToken.None);
                var limits = new List<LedgerLimit>();
                foreach (var fact in account.Session.Quota!.Limits!.Limits)
                {
                    var series = new ReadingSeriesKey(account.AccountId.ToString("N"), fact.Key);
                    limits.Add(new(fact, series, (await store.ReadAsync(series, CancellationToken.None)).Value));
                }
                var model = LiveLedgerProjection.Account(account, input.Labels[account.AccountId.ToString("N")], limits,
                    input.Configuration, input.Now, TimeZoneInfo.Utc, null);
                var accountExpected = expected.Where(e => e.Account == accountIndex).ToArray();
                Assert.Equal(accountExpected.Length, model.Cards.Count);
                foreach (var item in accountExpected)
                {
                    var fact = Assert.Single(limits, l => l.Facts.Key.Family == item.Family && l.Facts.Key.NativeDiscriminator == item.Native);
                    var cardId = LiveLedgerProjection.CardId(fact.Series);
                    var card = Assert.Single(model.Cards, c => c.CardId == cardId);
                    Assert.Equal(item.Used, card.Figures.Used);
                    Assert.Equal(item.TodayEnd, card.Figures.TodayEnd);
                    Assert.Equal(item.State, card.State);
                    Assert.Equal(item.Layout, card.Layout);
                    Assert.Equal(account.AccountId.ToString("N"), fact.Series.AccountTarget);
                    states.Add(cardId, item.State);
                    exported.Add(new { Id = scenario, CardId = cardId, account.Provider, input.Now, Baseline = cases[accountIndex].Baseline,
                        item.Used, item.TodayEnd, Layout = item.Layout.ToString(), State = item.State.ToString(), Cap = (decimal?)null,
                        Deterministic = "PASS", Test = nameof(CompositeProviderAccountsKeepIndependentLimitsAndNativeFacts) });
                }
                if (scenario == "ACCT-04")
                {
                    Assert.Equal(8, limits.Count);
                    var credits = Assert.Single(limits, l => l.Facts.Key.Family == "CX-B");
                    Assert.Equal(new CountQuantity(500, "credits"), credits.Facts.Remaining);
                    Assert.NotEmpty(credits.Runs);
                    Assert.DoesNotContain(model.Cards, c => c.CardId == LiveLedgerProjection.CardId(credits.Series));
                }
                if (scenario == "ACCT-06") Assert.Equal(4, limits.Count); // Disabled bucket is omitted, shared group retained.
            }
            input = input with { ExpectedStates = states, Observations = [.. input.Observations, .. capture.Observations] };
            Assert.Equal(expected.Count, states.Count);
            Assert.Equal(states.Count, states.Keys.Distinct(StringComparer.Ordinal).Count());
            var directory = Environment.GetEnvironmentVariable("AIU_AUDIT_COMPOSITE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, scenario + ".json"), JsonSerializer.Serialize(input, AuditJson.Default.AuditInput));
                File.WriteAllText(Path.Combine(directory, scenario + ".expectations.json"), JsonSerializer.Serialize(exported));
                File.WriteAllText(Path.Combine(directory, scenario + ".provider.txt"), string.Join(Environment.NewLine, cases.Select(c => c.Json)));
            }
        }
        finally { store.Dispose(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static (Case[] Cases, IReadOnlyList<ExpectedCard> Expected) Composite(string scenario)
    {
        const string claudeWeek = "[\"claude:7d\",\"7d\"]";
        var ordinary = new Case(scenario, "claude", Week("claude", 45), 40, CardState.OnTrack, 45, 60, CardLayout.Period);
        switch (scenario)
        {
            case "ACCT-01":
                return ([ordinary with { Id = scenario + "-A", Json = Week("claude", 25), Baseline = 20 },
                         ordinary with { Id = scenario + "-B", Json = Week("claude", 75), Baseline = 60 }],
                    [new(0, "CL-W", claudeWeek, 25, 46.6m, CardLayout.Period),
                     new(1, "CL-W", claudeWeek, 75, 73.3m, CardLayout.Period, CardState.OverToday)]);
            case "ACCT-02":
                return ([ordinary with { Id = scenario + "-subscription" },
                         ordinary with { Id = scenario + "-money", Json = Money(125.5m), Baseline = 120 }],
                    [new(0, "CL-W", claudeWeek, 45, 60, CardLayout.Period),
                     new(1, "CL-X", "extra-usage", 125.5m, 130, CardLayout.Pool)]);
            case "ACCT-03":
                var claude = JsonNode.Parse(Week("claude", 45, 35, true))!.AsObject();
                claude["seven_day_opus"] = JsonNode.Parse("""{"utilization":45,"resets_at":"2026-10-12T00:00:00Z"}""");
                claude["seven_day_sonnet"] = claude["seven_day_opus"]!.DeepClone();
                claude["limits"] = JsonNode.Parse("""[{"kind":"weekly_scoped","percent":45,"resets_at":"2026-10-12T00:00:00Z","scope":{"model":{"display_name":"SYNTHETIC shared model group"}}},{"kind":"future_window","percent":25,"resets_at":"2026-10-12T00:00:00Z"}]""");
                claude["spend"] = JsonNode.Parse(Money(125.5m))!["spend"]!.DeepClone();
                return ([ordinary with { Json = claude.ToJsonString() }],
                    [new(0, "CL-W", claudeWeek, 45, 60, CardLayout.FiveHourAndPeriod),
                     new(0, "CL-M", "Opus", 45, 60, CardLayout.Period), new(0, "CL-M", "Sonnet", 45, 60, CardLayout.Period),
                     new(0, "CL-M", "SYNTHETIC shared model group", 45, 60, CardLayout.Period),
                     new(0, "CL-unknown", "[\"future_window\",\"2\"]", 25, null, CardLayout.UsedOnly, CardState.PeriodUnknown),
                     new(0, "CL-X", "extra-usage", 125.5m, null, CardLayout.Note, CardState.PeriodUnknown)]);
            case "ACCT-04":
                var codex = JsonNode.Parse(Week("codex", 45, 35, true))!.AsObject();
                var additional = JsonNode.Parse("""{"limit_name":"SYNTHETIC same group","metered_feature":"synthetic-feature"}""")!.AsObject();
                additional["rate_limit"] = codex["rate_limit"]!.DeepClone();
                codex["additional_rate_limits"] = new JsonArray(additional, additional.DeepClone());
                codex["spend_control"] = JsonSerializer.SerializeToNode(new { individual_limit = new { used_percent = 45, reset_at = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds() } });
                return ([ordinary with { Provider = "codex", Json = codex.ToJsonString() }],
                    [new(0, "CX-S", "[\"codex\",\"secondary\"]", 45, 60, CardLayout.FiveHourAndPeriod),
                     new(0, "CX-A", "[\"codex:synthetic-feature\",\"secondary\"]", 45, 60, CardLayout.FiveHourAndPeriod),
                     new(0, "CX-A", "[\"codex:synthetic-feature:2\",\"secondary\"]", 45, 60, CardLayout.FiveHourAndPeriod),
                     new(0, "CX-I", "individual_limit", 45, 43.3m, CardLayout.Period, CardState.OverToday)]);
            case "ACCT-05":
                var copilot = JsonNode.Parse(Pool(125))!.AsObject();
                var pools = copilot["quota_snapshots"]!.AsObject();
                foreach (var pool in new[] { "chat", "completions", "future_pool" }) pools[pool] = pools["premium_interactions"]!.DeepClone();
                return ([ordinary with { Provider = "copilot", Json = copilot.ToJsonString(), Baseline = 120 }],
                    [new(0, "GH-P", "[\"premium_interactions\",\"monthly\"]", 125, 130, CardLayout.Pool),
                     new(0, "GH-C", "[\"chat\",\"monthly\"]", 125, 130, CardLayout.Pool),
                     new(0, "GH-I", "[\"completions\",\"monthly\"]", 125, 130, CardLayout.Pool),
                     new(0, "GH-unknown", "[\"future_pool\",\"monthly\"]", 125, null, CardLayout.Note, CardState.LimitUnknown)]);
            case "ACCT-06":
                var antigravity = JsonNode.Parse(Week("antigravity", 45, 35, true))!.AsObject();
                var groups = antigravity["groups"]!.AsArray();
                var shared = groups[0]!.DeepClone(); shared["displayName"] = "Claude + GPT Models";
                shared["buckets"]![0]!["remainingFraction"] = .35m;
                shared["buckets"]!.AsArray().Add(JsonNode.Parse("""{"bucketId":"disabled","window":"weekly","remainingFraction":0,"disabled":true}"""));
                groups.Add(shared);
                return ([ordinary with { Provider = "antigravity", Json = antigravity.ToJsonString() }],
                    [new(0, "AG-W", "[\"Gemini Models\",\"weekly\"]", 45, 60, CardLayout.FiveHourAndPeriod),
                     new(0, "AG-W", "[\"Claude \\u002B GPT Models\",\"weekly\"]", 65, 60, CardLayout.FiveHourAndPeriod, CardState.OverToday)]);
            default: throw new InvalidDataException("Unknown synthetic composite");
        }
    }
}
