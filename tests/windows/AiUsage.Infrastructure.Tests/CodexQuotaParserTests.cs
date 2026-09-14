using AiUsage.Core.Providers.Codex;
using System.Text;
using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class CodexQuotaParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SeparatePoolsAndUnknownValuesAreNotAggregated()
    {
        var snapshot = CodexQuotaParser.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "codex-usage.synthetic.json")), Now);
        Assert.Equal("synthetic-plan", snapshot.PlanType);
        var main = Assert.Single(snapshot.Groups, group => group.Id == "codex");
        var window = Assert.Single(main.Windows);
        Assert.Equal(75, window.RemainingPercent);
        Assert.Equal(TimeSpan.FromHours(5), window.Duration);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1800000000), window.ResetsAt);
        Assert.True(main.Allowed);
        var extra = Assert.Single(snapshot.Groups, group => group.MeteredFeature == "synthetic-feature");
        Assert.False(extra.Allowed);
        Assert.True(extra.LimitReached);
        Assert.Equal(0, Assert.Single(extra.Windows).RemainingPercent);
        var unknown = Assert.Single(snapshot.Groups, group => group.MeteredFeature == "synthetic-unknown");
        Assert.Null(Assert.Single(unknown.Windows).RemainingPercent);
        Assert.Equal(Now.AddMinutes(1), unknown.Windows[0].ResetsAt);
        Assert.True(snapshot.Credits!.Unlimited);
        Assert.Null(snapshot.Credits.Balance);
        Assert.Equal(2, snapshot.AvailableResetCredits);
    }

    [Fact]
    public void OmittedQuotaDoesNotInventUnlimitedOrZeroWindows()
    {
        var snapshot = Parse("""{"plan_type":"future-plan","rate_limit":{"allowed":true},"credits":{"has_credits":false}}""");
        Assert.Empty(Assert.Single(snapshot.Groups).Windows);
        Assert.Null(snapshot.Credits!.Unlimited);
        Assert.Null(snapshot.Credits.Balance);
        Assert.Null(snapshot.AvailableResetCredits);
    }

    [Fact]
    public void DuplicateAdditionalNamesPreserveDistinctGroups()
    {
        var snapshot = Parse("""{"additional_rate_limits":[{"metered_feature":"same","rate_limit":{"allowed":true}},{"metered_feature":"same","rate_limit":{"allowed":false}}]}""");
        Assert.Equal(2, snapshot.Groups.Select(group => group.Id).Distinct().Count());
        Assert.Contains(snapshot.Groups, group => group.Allowed == true);
        Assert.Contains(snapshot.Groups, group => group.Allowed == false);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    [InlineData("1e400")]
    [InlineData("\"50\"")]
    public void InvalidPercentDoesNotBecomeAvailableQuota(string value)
    {
        var snapshot = Parse("{\"rate_limit\":{\"primary_window\":{\"used_percent\":" + value + "}}}");
        var window = Assert.Single(Assert.Single(snapshot.Groups).Windows);
        Assert.Null(window.UsedPercent);
        Assert.Null(window.RemainingPercent);
    }

    [Fact]
    public void InvalidAbsoluteResetFallsBackWithoutOverflow()
    {
        var snapshot = Parse("""{"rate_limit":{"primary_window":{"reset_at":1e30,"reset_after_seconds":30,"limit_window_seconds":1e30}},"credits":{"balance":"12.25","unlimited":false},"spend_control":{"reached":true},"rate_limit_reached_type":{"type":"workspace_member_usage_limit_reached"}}""");
        var window = Assert.Single(Assert.Single(snapshot.Groups).Windows);
        Assert.Equal(Now.AddSeconds(30), window.ResetsAt);
        Assert.Null(window.Duration);
        Assert.Equal(12.25m, snapshot.Credits!.Balance);
        Assert.True(snapshot.SpendControlReached);
        Assert.Equal("workspace_member_usage_limit_reached", snapshot.LimitReachedType);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"error\":\"synthetic-secret\"}")]
    [InlineData("{not-json-synthetic-secret")]
    [InlineData("{\"additional_rate_limits\":\"synthetic-secret\"}")]
    [InlineData("{\"rate_limit\":\"synthetic-secret\"}")]
    [InlineData("{\"additional_rate_limits\":[{\"rate_limit\":\"synthetic-secret\"}]}")]
    public void InvalidPayloadFailureDoesNotEchoInput(string payload)
    {
        var error = Assert.Throws<CodexException>(() => Parse(payload));
        Assert.Equal(CodexFailureKind.InvalidResponse, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
    }

    private static AiUsage.Core.Usage.QuotaSnapshot Parse(string json) => CodexQuotaParser.Parse(Encoding.UTF8.GetBytes(json), Now);
}
