using System.Net.Http.Headers;
using AiUsage.Core.History;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>Source-observed ChatGPT analytics, using only the session's existing grant.</summary>
internal sealed class CodexHistoryClient(HttpClient client, TimeProvider? timeProvider = null, ProviderTransportOptions? options = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private DateTimeOffset retryAt;
    private static readonly (string Id, string Path, string Query)[] Reports =
    [
        ("usage", "usage/daily-token-usage-breakdown", ""),
        ("credits", "usage/credit-usage-events", ""),
        ("activity", "analytics/daily-workspace-usage-counts", "&workspace_user=true"),
        ("plugins", "analytics/daily-plugin-usage-metrics", "&workspace_user=true&top_plugin_limit=100"),
        ("skills", "analytics/daily-skill-usage-metrics", "&workspace_user=true&top_skill_limit=100"),
        ("workspace", "usage/daily-workspace-user-token-usage-breakdown", ""),
        ("workspace-models", "usage/daily-workspace-user-token-usage-breakdown", "&breakdown_by=model&modes=codex&modes=work"),
        ("enterprise-product", "usage/daily-workspace-user-credit-usage", "&breakdown=product"),
        ("enterprise-model", "usage/daily-workspace-user-credit-usage", "&breakdown=model"),
        ("enterprise-speed", "usage/daily-workspace-user-credit-usage", "&breakdown=speed"),
        ("enterprise-reasoning", "usage/daily-workspace-user-credit-usage", "&breakdown=reasoning_effort")
    ];

    internal async Task<ProviderHistoryResult> FetchAsync(CodexCredentials credentials, HistoryRange range, CancellationToken token)
    {
        range.Validate();
        await credentials.Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            credentials.EnsureUsable();
            if (credentials.ExpiresAt <= clock.GetUtcNow()) throw new CodexException(ProviderFailureKind.AuthenticationRequired);
            if (retryAt > clock.GetUtcNow()) return new(range, clock.GetUtcNow(), [new("history", HistoryStatus.RateLimited, [], retryAt)]);
            var reports = new List<HistoryReport>();
            foreach (var report in Reports)
            {
                token.ThrowIfCancellationRequested();
                var grouping = report.Id.StartsWith("enterprise-", StringComparison.Ordinal) ? "" : "&group_by=day";
                var dates = report.Id == "credits" ? "" : $"?start_date={range.From:yyyy-MM-dd}&end_date={range.To:yyyy-MM-dd}{grouping}{report.Query}";
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://chatgpt.com/backend-api/wham/" + report.Path + dates);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
                request.Headers.Add("ChatGPT-Account-Id", credentials.AccountId);
                try
                {
                    using var response = await ProviderTransport.SendAsync(client, request, clock, token, options).ConfigureAwait(false);
                    if (!response.IsSuccess) throw ProviderTransport.Failure(response);
                    var root = response.Body!.RootElement;
                    if (HistoryJson.Text(root, "account_id") is { } account && account != credentials.AccountId)
                        throw new ProviderException(ProviderFailureKind.AccountMismatch);
                    reports.Add(HistoryJson.Report(report.Id, CodexHistoryParser.Parse(report.Id, root, range)));
                }
                catch (ProviderException error)
                {
                    if (error.Kind == ProviderFailureKind.RateLimited) retryAt = clock.GetUtcNow() + (error.RetryAfter ?? TimeSpan.FromMinutes(1));
                    reports.Add(new(report.Id, HistoryJson.Status(error.Kind), [], error.Kind == ProviderFailureKind.RateLimited ? retryAt : null));
                    if (error.Kind is ProviderFailureKind.AuthenticationRequired or ProviderFailureKind.RateLimited or ProviderFailureKind.AccountMismatch) break;
                }
            }
            return new(range, clock.GetUtcNow(), reports.AsReadOnly());
        }
        finally { credentials.Gate.Release(); }
    }
}
