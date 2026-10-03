using System.Text;
using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Antigravity;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ParserLedgerTests
{
    [Theory]
    [InlineData("\"spend\":{\"enabled\":true,\"used\":{\"amount_minor\":1250,\"currency\":\"USD\",\"exponent\":2},\"limit\":{\"amount_minor\":50000,\"currency\":\"USD\",\"exponent\":2}},\"extra_usage\":{\"is_enabled\":true,\"used_credits\":9999,\"monthly_limit\":99999,\"currency\":\"EUR\",\"decimal_places\":2}")]
    [InlineData("\"extra_usage\":{\"is_enabled\":true,\"used_credits\":1250,\"monthly_limit\":50000,\"currency\":\"USD\",\"decimal_places\":2}")]
    public void CurrentAndLegacyFiniteMoneyProduceOneNeutralPoolWithoutPlanInference(string properties)
    {
        var now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        Assert.True(ClaudeQuotaParser.TryParse(Encoding.UTF8.GetBytes("{" + properties + "}"), now, out var reading));
        Assert.Null(reading!.Quota.PlanType);
        var facts = Assert.Single(reading.Quota.Limits!.Limits);
        Assert.Equal("CL-X", facts.Key.Family);
        Assert.Equal(new MoneyQuantity(1250, 2, "USD"), facts.Used);
        var card = Assert.Single(Project("claude", reading.Quota, now).Cards);
        Assert.Equal("Spending", card.ScopeLabel);
        Assert.Equal(12.50m, card.Figures.Used);
        Assert.Equal(500, card.Figures.ProviderLimit.Amount);
        Assert.Null(card.Figures.EffectiveLimit);
        Assert.Null(card.CapTargetId);
        Assert.Contains("scope unverified", card.Monetary!.Qualification);
    }

    private static AccountModel Project(string provider, QuotaSnapshot quota, DateTimeOffset now)
    {
        var id = Guid.NewGuid();
        var limits = quota.Limits!.Limits.Select(f =>
        {
            var key = new ReadingSeriesKey(id.ToString("N"), f.Key);
            var value = f.Used ?? f.Remaining;
            return new LedgerLimit(f, key, value is null ? [] : [new(key, value, now.AddMinutes(-5), now, "one", quota.PlanType, SnapshotSource.ProviderApi)]);
        }).ToArray();
        return LiveLedgerProjection.Account(new AccountSnapshot(id, provider, true, new(ProviderSessionStatus.QuotaAvailable, quota), false, null),
            "Synthetic", limits, BudgetConfiguration.Default, now, TimeZoneInfo.Utc, null);
    }

    [Fact]
    public void ClaudeParserPreservesMoneyAsNeutralAccountOwnedFacts()
    {
        var now = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        Assert.True(ClaudeQuotaParser.TryParse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "claude-usage.synthetic.json")), now, out var reading));
        var model = Project("claude", reading!.Quota, now);
        Assert.Contains(model.Cards, c => c.Layout == CardLayout.FiveHourAndPeriod && c.Figures.Used == 40);
        Assert.Contains(model.Cards, c => c.ScopeLabel == "Family / Fable" && c.Figures.Used == 55);
        var monetary = Assert.Single(model.Cards, c => c.ScopeLabel == "Spending");
        Assert.Equal(123.45m, monetary.Figures.Used);
        Assert.Null(monetary.Figures.TodayEnd);
        var extra = Assert.Single(reading.Quota.Limits!.Limits, f => f.Key.Family == "CL-X");
        Assert.Equal(new MoneyQuantity(12345, 2, "EUR"), extra.Used);
        Assert.Equal(LimitValueState.ExplicitNull, extra.Limit.State);
    }

    [Fact]
    public void CodexParserNeverTurnsMissingBalanceIntoZeroOrUnknownWindowIntoWeekly()
    {
        var now = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        var quota = CodexQuotaParser.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "codex-usage.synthetic.json")), now);
        var model = Project("codex", quota, now);
        var balance = Assert.Single(model.Cards, c => c.ScopeLabel == "Credits");
        Assert.Equal(CardState.NoCap, balance.State);
        Assert.Null(balance.Figures.ProviderBalance);
        Assert.Contains(model.Cards, c => c.State == CardState.ValueUnknown && c.Period.Kind == PeriodKind.Unknown);
    }

    [Fact]
    public void CopilotMonthlyCountPoolAndDateOnlyResetReachLedgerInNativeUnits()
    {
        var now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        var quota = CopilotQuotaParser.Parse(Encoding.UTF8.GetBytes("""
            {"quota_reset_date":"2026-11-01","quota_snapshots":{"premium_interactions":{"entitlement":2000,"remaining":790,"percent_remaining":39.5,"unlimited":false}}}
            """), now);
        var card = Assert.Single(Project("copilot", quota, now).Cards);
        Assert.Equal(1210, card.Figures.Used);
        Assert.Equal(2000, card.Figures.ProviderLimit.Amount);
        Assert.Equal(PeriodKind.CalendarMonth, card.Period.Kind);
        Assert.Equal(new DateOnly(2026, 11, 1), card.Reset!.Date);
        Assert.Null(card.Reset.At);
    }

    [Fact]
    public void AntigravityGroupsPairOnlyTheirOwnWindows()
    {
        var now = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var quota = AntigravityQuotaParser.Parse(Encoding.UTF8.GetBytes(AntigravityProtocolTests.Summary), now, "free-tier");
        var card = Assert.Single(Project("antigravity", quota, now).Cards);
        Assert.Equal("Gemini Models", card.ScopeLabel);
        Assert.Equal(12, card.Figures.Used);
        Assert.Equal(41, card.FiveHour!.CurrentWindowUsed);
        Assert.Equal(CardLayout.FiveHourAndPeriod, card.Layout);
    }
}
