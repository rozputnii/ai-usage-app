using System.Text;
using System.Text.Json;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Antigravity;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ProviderLimitParserTests
{
    [Fact]
    public void UnlimitedCopilotRetainsIndependentNativePercentages()
    {
        var quota = CopilotQuotaParser.Parse(Bytes("""{"quota_snapshots":{"chat":{"unlimited":true,"entitlement":0,"percent_remaining":73}}}"""), Now);
        var facts = Assert.Single(quota.Limits!.Limits);
        Assert.Equal(LimitValueState.Unlimited, facts.Limit.State);
        Assert.Equal(73m, facts.RemainingPercent);
        Assert.Equal(27m, facts.UsedPercent);
        Assert.Null(quota.Groups.Single().Windows.Single().UsedPercent);
    }

    [Fact]
    public void ClaudeUnknownKindsKeepDistinctStableKeysForTheSameScope()
    {
        const string a = """{"kind":"future-a","percent":10,"scope":{"model":{"display_name":"Same"}}}""";
        const string b = """{"kind":"future-b","percent":20,"scope":{"model":{"display_name":"Same"}}}""";
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("{\"limits\":[" + a + "," + b + "]}"), Now, out var both));
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("{\"limits\":[" + b + "," + a + "]}"), Now, out var reversed));
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("{\"limits\":[" + b + "]}"), Now, out var single));
        var first = both.Quota.Limits!.Limits.Single(f => f.UsedPercent == 10);
        var second = both.Quota.Limits.Limits.Single(f => f.UsedPercent == 20);
        Assert.NotEqual(first.Key, second.Key);
        Assert.Equal(first.Key, reversed.Quota.Limits!.Limits.Single(f => f.UsedPercent == 10).Key);
        Assert.Equal(second.Key, single.Quota.Limits!.Limits.Single().Key);
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

    [Theory]
    [InlineData("{}", null)]
    [InlineData("{\"used_percent\":null}", null)]
    [InlineData("{\"used_percent\":0}", 0)]
    [InlineData("{\"used_percent\":100}", 100)]
    public void CodexEveryWindowFamilyUsesOnlyReportedPercentAndDuration(string fields, int? used)
    {
        var quota = CodexQuotaParser.Parse(Bytes("{\"rate_limit\":{\"primary_window\":" + fields + ",\"secondary_window\":" + fields +
            "},\"additional_rate_limits\":[{\"metered_feature\":\" Meter / Ω \",\"normal_model_slug\":\" Model / Ω \",\"rate_limit\":{\"primary_window\":" + fields + "}}]}"), Now);
        Assert.Equal(new[] { "CX-P", "CX-S", "CX-A" }, quota.Limits!.Limits.Select(f => f.Key.Family));
        Assert.All(quota.Limits.Limits, f => { Assert.Equal((decimal?)used, f.UsedPercent); Assert.Equal(LimitValueState.NotApplicable, f.Limit.State); Assert.Null(f.Duration); Assert.Null(f.PeriodStart); });
        using var key = JsonDocument.Parse(quota.Limits.Limits[2].Key.NativeDiscriminator);
        Assert.Equal("codex: Meter / Ω ", key.RootElement[0].GetString());
        Assert.Equal(" Model / Ω ", quota.Groups[1].NormalModelSlug);
    }

    [Theory]
    [InlineData("{}", LimitValueState.Unknown, null)]
    [InlineData("{\"balance\":null,\"unlimited\":null}", LimitValueState.Unknown, null)]
    [InlineData("{\"balance\":0,\"unlimited\":false}", LimitValueState.Unknown, 0)]
    [InlineData("{\"balance\":0,\"unlimited\":true}", LimitValueState.Unlimited, 0)]
    public void CodexBalanceNeverInventsAnAllotment(string fields, LimitValueState state, int? balance)
    {
        var fact = Assert.Single(CodexQuotaParser.Parse(Bytes("{\"credits\":" + fields + "}"), Now).Limits!.Limits);
        Assert.Equal(state, fact.Limit.State); Assert.Null(fact.Used); Assert.Null(fact.Reset);
        Assert.Equal(balance is { } b ? new CountQuantity(b, "credits") : null, fact.Remaining);
    }

    [Fact]
    public void IndividualControlWithOnlyUnknownAmountsHasNoBudgetPercent()
    {
        var fact = Assert.Single(CodexQuotaParser.Parse(Bytes("""{"spend_control":{"individual_limit":{"used":"1","limit":"2","remaining":"1"}}}"""), Now).Limits!.Limits);
        Assert.Equal("CX-I", fact.Key.Family); Assert.Null(fact.UsedPercent); Assert.Null(fact.RemainingPercent);
        Assert.Null(fact.Used); Assert.Equal("unknown", fact.SecondaryAmounts!.Unit);
    }

    [Theory]
    [InlineData("five_hour", "CL-S", 5)]
    [InlineData("seven_day", "CL-W", 168)]
    [InlineData("seven_day_sonnet", "CL-M", 168)]
    public void ClaudeLegacyWindowsHaveTheirOwnFamiliesAndDerivedStarts(string field, string family, int hours)
    {
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("{\"" + field + "\":{\"utilization\":0,\"resets_at\":\"2026-10-10T12:00:00Z\"}}"), Now, out var reading));
        var fact = Assert.Single(reading.Quota.Limits!.Limits);
        Assert.Equal(family, fact.Key.Family); Assert.Equal(0m, fact.UsedPercent); Assert.Equal(100m, fact.RemainingPercent);
        Assert.Equal(fact.Reset!.At.AddHours(-hours), fact.PeriodStart); Assert.Equal(ValueOrigin.Derived, fact.PeriodStartOrigin);
    }

    [Theory]
    [InlineData("spend", "{}", LimitValueState.Unknown)]
    [InlineData("spend", "{\"limit\":null}", LimitValueState.ExplicitNull)]
    [InlineData("spend", "{\"limit\":{\"amount_minor\":0,\"currency\":\"USD\",\"exponent\":2}}", LimitValueState.Finite)]
    [InlineData("extra_usage", "{}", LimitValueState.Unknown)]
    [InlineData("extra_usage", "{\"monthly_limit\":null}", LimitValueState.ExplicitNull)]
    [InlineData("extra_usage", "{\"monthly_limit\":0,\"currency\":\"USD\",\"decimal_places\":2}", LimitValueState.Finite)]
    public void ClaudeBothSpendFormatsKeepNullAndZeroDistinct(string field, string fields, LimitValueState state)
    {
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("{\"" + field + "\":" + fields + "}"), Now, out var reading));
        var fact = Assert.Single(reading.Quota.Limits!.Limits);
        Assert.Equal(LimitKind.MonetaryPool, fact.Kind); Assert.Equal(state, fact.Limit.State);
        if (state == LimitValueState.Finite) Assert.Equal(new MoneyQuantity(0, 2, "USD"), fact.Limit.Value);
        Assert.Null(fact.Reset); Assert.True(fact.AllowsCalendarFallback);
    }

    [Fact]
    public void ClaudeModernSpendWinsAndCurrencyMismatchDoesNotProduceRemaining()
    {
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("""
            {"extra_usage":{"used_credits":10,"monthly_limit":100},"spend":{
            "used":{"amount_minor":30,"exponent":2,"currency":"USD"},
            "limit":{"amount_minor":100,"exponent":2,"currency":"usd"}}}
            """), Now, out var reading));
        var fact = Assert.Single(reading.Quota.Limits!.Limits);
        Assert.Equal(new MoneyQuantity(30, 2, "USD"), fact.Used); Assert.Null(fact.Remaining);
    }

    [Theory]
    [InlineData("chat", "GH-C")]
    [InlineData("completions", "GH-I")]
    [InlineData("premium_interactions", "GH-P")]
    public void CopilotPoolAmountsPercentAndOverageAreIndependent(string pool, string family)
    {
        var quota = CopilotQuotaParser.Parse(Bytes("{\"quota_reset_date\":\"2026-11-01T12:00:00+02:00\",\"quota_snapshots\":{\"" + pool +
            "\":{\"entitlement\":100,\"remaining\":80,\"percent_remaining\":50,\"quota_remaining\":77,\"overage_count\":3,\"overage_permitted\":true}}}"), Now);
        var fact = Assert.Single(quota.Limits!.Limits);
        Assert.Equal(family, fact.Key.Family); Assert.Equal(new CountQuantity(20, "requests"), fact.Used);
        Assert.Equal(50, fact.UsedPercent); Assert.Equal(77, fact.SourceDetails!.QuotaRemaining); Assert.True(fact.OveragePermitted);
        Assert.Equal(ResetPrecision.Instant, fact.Reset!.Precision); Assert.Equal(ResetZone.Explicit, fact.Reset.Zone);
        Assert.Equal(ValueOrigin.Assumed, fact.PeriodStartOrigin);
    }

    [Theory]
    [InlineData("{}", null)]
    [InlineData("{\"remainingFraction\":null}", null)]
    [InlineData("{\"remainingFraction\":0}", 100)]
    [InlineData("{\"remainingFraction\":1}", 0)]
    public void AntigravityWeeklyBucketsRetainUnknownVersusExhausted(string bucket, int? used)
    {
        bucket = bucket.Insert(1, "\"window\":\"weekly\",\"bucketId\":\"shared\"" + (bucket.Length > 2 ? "," : ""));
        var fact = Assert.Single(AntigravityQuotaParser.Parse(Bytes("{\"buckets\":[" + bucket + "]}"), Now).Limits!.Limits);
        Assert.Equal("AG-W", fact.Key.Family); Assert.Equal((decimal?)used, fact.UsedPercent);
        Assert.Equal(LimitValueState.NotApplicable, fact.Limit.State);
    }

    [Fact]
    public void UiOnlyFamiliesAreNeverSynthesizedFromPlanNames()
    {
        Assert.Empty(CodexQuotaParser.Parse(Bytes("""{"plan_type":"enterprise","credits":null,"rate_limit":null}"""), Now).Limits!.Limits);
        Assert.Empty(CopilotQuotaParser.Parse(Bytes("""{"copilot_plan":"business","quota_snapshots":{}}"""), Now).Limits!.Limits);
        Assert.Empty(AntigravityQuotaParser.Parse(Bytes("""{"buckets":[]}"""), Now, "Google AI Plus").Limits!.Limits);
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("""{"five_hour":null,"spend":null}"""), Now, out var reading));
        Assert.Empty(reading.Quota.Limits!.Limits);
    }

    [Fact]
    public void MonthlyStartUsesCalendarArithmeticInUtc()
    {
        var fact = Assert.Single(CopilotQuotaParser.Parse(Bytes("""
            {"quota_reset_date":"2026-07-01T00:30:00+02:00","quota_snapshots":{"chat":{"entitlement":1}}}
            """), Now).Limits!.Limits);
        Assert.Equal(new DateTimeOffset(2026, 5, 30, 22, 30, 0, TimeSpan.Zero), fact.PeriodStart);
    }

    [Fact]
    public void CodexSignedBalanceAndIndividualControlRetainIndependentFacts()
    {
        var quota = CodexQuotaParser.Parse(Bytes("""
            {"credits":{"balance":"-12.50","has_credits":false},
             "spend_control":{"individual_limit":{"used_percent":25,"remaining_percent":70,"used":"001.2300",
             "limit":"opaque","remaining":"-2e1","reset_after_seconds":600}}}
            """), Now);
        Assert.Equal(-12.5m, quota.Credits!.Balance);
        var limits = Assert.IsType<LimitSnapshot>(quota.Limits).Limits;
        var balance = Assert.Single(limits, l => l.Key.Family == "CX-B");
        Assert.Equal(new CountQuantity(-12.5m, "credits"), balance.Remaining);
        Assert.Equal(LimitValueState.Unknown, balance.Limit.State);
        var control = Assert.Single(limits, l => l.Key.Family == "CX-I");
        Assert.Equal(25m, control.UsedPercent);
        Assert.Equal(70m, control.RemainingPercent);
        Assert.Equal(new OpaqueQuotaAmount("001.2300", "opaque", "-2e1"), control.SecondaryAmounts);
        Assert.Equal(Now.AddSeconds(600), control.Reset!.At);
        Assert.Equal(ValueOrigin.Assumed, control.PeriodStartOrigin);
    }

    [Theory]
    [InlineData("{}", LimitValueState.Unknown)]
    [InlineData("{\"entitlement\":null}", LimitValueState.ExplicitNull)]
    [InlineData("{\"entitlement\":0}", LimitValueState.Finite)]
    [InlineData("{\"entitlement\":0,\"unlimited\":true}", LimitValueState.Unlimited)]
    public void CopilotEntitlementStatesRemainDistinct(string fields, LimitValueState expected)
    {
        var quota = CopilotQuotaParser.Parse(Bytes("{\"quota_reset_date\":\"2026-11-01\",\"quota_snapshots\":{\"chat\":" + fields + "}}"), Now);
        var fact = Assert.Single(Assert.IsType<LimitSnapshot>(quota.Limits).Limits);
        Assert.Equal(expected, fact.Limit.State);
        Assert.Equal("GH-C", fact.Key.Family);
        Assert.Equal(ResetPrecision.Date, fact.Reset!.Precision);
        Assert.Equal(ResetZone.AssumedUtc, fact.Reset.Zone);
        if (expected == LimitValueState.Finite) Assert.Equal(new CountQuantity(0, "requests"), fact.Limit.Value);
    }

    [Fact]
    public void ClaudeModernScopeReplacesLegacyAndKeepsOpaqueName()
    {
        Assert.True(ClaudeQuotaParser.TryParse(Bytes("""
            {"seven_day_opus":{"utilization":10},"limits":[
             {"kind":"weekly_scoped","percent":30,"scope":{"model":{"display_name":"Opus"}}},
             {"kind":"weekly_scoped","percent":0,"scope":{"model":{"display_name":"  Model / Ω  "}}}],
             "spend":{"enabled":true,"limit":null,"used":{"amount_minor":0,"exponent":2,"currency":"uSd"}}}
            """), Now, out var reading));
        var limits = Assert.IsType<LimitSnapshot>(reading.Quota.Limits).Limits;
        Assert.Equal(2, limits.Count(l => l.Key.Family == "CL-M"));
        Assert.Equal(30m, Assert.Single(limits, l => l.Key.NativeDiscriminator == "Opus").UsedPercent);
        Assert.Contains(limits, l => l.Key.NativeDiscriminator == "  Model / Ω  " && l.UsedPercent == 0);
        var spend = Assert.Single(limits, l => l.Kind == LimitKind.MonetaryPool);
        Assert.Equal(LimitValueState.ExplicitNull, spend.Limit.State);
        Assert.Equal(new MoneyQuantity(0, 2, "uSd"), spend.Used);
    }

    [Fact]
    public void AntigravityRetainsBucketPrecisionAndUnknownSecondaryUnit()
    {
        var quota = AntigravityQuotaParser.Parse(Bytes("""
            {"groups":[{"displayName":"Shared / Ω","buckets":[
            {"bucketId":"  opaque / ID  ","window":"5h","remainingFraction":0,
             "remainingAmount":"123.45","resetTime":"2026-10-03T17:00:00"}]}]}
            """), Now);
        var fact = Assert.Single(Assert.IsType<LimitSnapshot>(quota.Limits).Limits);
        Assert.Equal("AG-5", fact.Key.Family);
        Assert.Equal(100m, fact.UsedPercent);
        Assert.Equal(new CountQuantity(123.45m, "unknown"), fact.SecondaryAmount);
        Assert.Equal(ResetZone.AssumedUtc, fact.Reset!.Zone);
        Assert.Contains("  opaque / ID  ", fact.Key.NativeDiscriminator);
    }
}
