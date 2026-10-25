using System.Text;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Claude;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

/// <summary>Synthetic stream-json output shaped like the live Claude Code 2.1.296 `/usage` run of 2026-10-25.</summary>
public sealed class ClaudeCodeReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    internal const string Limits = """
        {"limits":[
          {"kind":"session","group":"session","percent":10,"resets_at":"2026-10-25T22:30:00+00:00","scope":null,"severity":"normal","is_active":false},
          {"kind":"weekly_all","group":"weekly","percent":52,"resets_at":"2026-10-27T10:00:00+00:00","scope":null,"severity":"normal","is_active":true},
          {"kind":"weekly_scoped","group":"weekly","percent":15,"resets_at":"2026-10-27T10:00:00+00:00","scope":{"model":{"display_name":"Fable"},"surface":null},"severity":"normal","is_active":false}],
         "extra_usage":{"is_enabled":false,"monthly_limit":null,"used_credits":null,"utilization":null,"currency":null}}
        """;

    internal static string Stream(string? rateLimits = Limits, bool report = true, string command = "usage", int turns = 0,
        double cost = 0, string modelUsage = "{}", string subtype = "success")
    {
        var reportJson = report ? ",\"usage_report\":{\"session\":{\"total_cost_usd\":0,\"total_api_duration_ms\":0,\"total_duration_ms\":0," +
            "\"total_lines_added\":0,\"total_lines_removed\":0,\"model_usage\":{}},\"rate_limits\":" + (rateLimits ?? "null").ReplaceLineEndings("") + "}" : "";
        return string.Join('\n',
            "{\"type\":\"system\",\"subtype\":\"init\",\"session_id\":\"synthetic\",\"claude_code_version\":\"2.1.296\"}",
            "{\"type\":\"assistant\",\"message\":{\"model\":\"<synthetic>\",\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"synthetic\"}]," +
                "\"usage\":{\"input_tokens\":0,\"output_tokens\":0}},\"local_command_source\":\"synthetic\",\"session_id\":\"synthetic\"" + reportJson +
                ",\"local_command_run\":{\"command\":\"" + command + "\",\"args\":\"\"}}",
            "{\"type\":\"result\",\"subtype\":\"" + subtype + "\",\"is_error\":false,\"num_turns\":" + turns + ",\"total_cost_usd\":" +
                cost.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"modelUsage\":" + modelUsage +
                ",\"local_command\":\"" + command + "\",\"session_id\":\"synthetic\"}") + "\n";
    }

    private static (ProviderFailureKind? Failure, Core.Providers.Claude.ClaudeQuotaReading? Reading) Read(string stdout)
    {
        var failure = ClaudeCodeReport.Read(Encoding.UTF8.GetBytes(stdout), Now, out var reading);
        return (failure, reading);
    }

    [Fact]
    public void TheUsageReportBecomesAClaudeReadingWithEveryRow()
    {
        var (failure, reading) = Read(Stream());
        Assert.Null(failure);
        Assert.NotNull(reading);
        Assert.Equal(Now, reading.Quota.FetchedAt);
        Assert.Equal(3, reading.Quota.Groups.Count);
        Assert.Contains(reading.Quota.Groups, group => group.Name?.Contains("Fable", StringComparison.Ordinal) == true);
        Assert.NotNull(reading.Quota.Limits);
    }

    [Theory]
    [InlineData(1, 0, "{}")]
    [InlineData(0, 0.01, "{}")]
    [InlineData(0, 0, "{\"claude-opus\":{\"inputTokens\":2}}")]
    public void AnySignOfAModelTurnRejectsTheRun(int turns, double cost, string modelUsage)
    {
        var (failure, reading) = Read(Stream(turns: turns, cost: cost, modelUsage: modelUsage));
        Assert.Equal(ProviderFailureKind.InvalidResponse, failure);
        Assert.Null(reading);
    }

    [Fact]
    public void APromptThatReachedTheModelInsteadOfTheLocalCommandIsRejected()
    {
        var model = "{\"type\":\"assistant\",\"message\":{\"model\":\"claude\",\"content\":[{\"type\":\"text\",\"text\":\"synthetic\"}]}}\n" +
            "{\"type\":\"result\",\"subtype\":\"success\",\"num_turns\":1,\"total_cost_usd\":0.18,\"modelUsage\":{\"claude\":{}}}\n";
        Assert.Equal(ProviderFailureKind.InvalidResponse, Read(model).Failure);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("cost")]
    public void ADifferentLocalCommandIsRejected(string command) =>
        Assert.Equal(ProviderFailureKind.InvalidResponse, Read(Stream(command: command)).Failure);

    [Fact]
    public void AnErrorResultIsRejected() =>
        Assert.Equal(ProviderFailureKind.InvalidResponse, Read(Stream(subtype: "error_during_execution")).Failure);

    [Fact]
    public void AReportWithoutTheStructuredFieldMeansNoSubscriptionSignIn() =>
        Assert.Equal(ProviderFailureKind.AuthenticationRequired, Read(Stream(report: false)).Failure);

    [Fact]
    public void AReportWithoutPlanRowsIsTreatedAsRateLimited() =>
        Assert.Equal(ProviderFailureKind.RateLimited, Read(Stream(rateLimits: null)).Failure);

    [Theory]
    [InlineData("")]
    [InlineData("not json\n")]
    [InlineData("{\"type\":\"result\"\n")]
    public void MalformedOutputIsRejected(string stdout) =>
        Assert.Equal(ProviderFailureKind.InvalidResponse, Read(stdout).Failure);

    [Fact]
    public void RowsTheParserCannotReadAreRejected() =>
        Assert.Equal(ProviderFailureKind.InvalidResponse, Read(Stream(rateLimits: "{\"limits\":\"broken\"}")).Failure);
}
