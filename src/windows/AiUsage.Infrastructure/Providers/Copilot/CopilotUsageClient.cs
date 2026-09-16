using AiUsage.Core.Providers.Copilot;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>Reads GitHub's documented personal billing usage reports. Read-only; no retries.</summary>
public sealed class CopilotUsageClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task<CopilotUsageReport> GetUsageAsync(CopilotCredentials credentials, CopilotUsageReportKind kind,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        cancellationToken.ThrowIfCancellationRequested();
        if (!CopilotAuthClient.IsLogin(credentials.Identity.Login)) throw new CopilotException(CopilotFailureKind.InvalidResponse);
        var report = kind switch
        {
            CopilotUsageReportKind.AiCredits => "ai_credit",
            CopilotUsageReportKind.PremiumRequests => "premium_request",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        using var request = CopilotHttp.ApiGet($"/users/{credentials.Identity.Login}/settings/billing/{report}/usage", credentials.AccessToken);
        using var response = await CopilotHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) throw CopilotHttp.Failure(response);
        if (response.Body is null ||
            !CopilotUsageParser.TryParse(response.Body.RootElement, kind, clock.GetUtcNow(), out var parsed, out var user))
            throw new CopilotException(CopilotFailureKind.InvalidResponse);
        if (!string.Equals(user, credentials.Identity.Login, StringComparison.OrdinalIgnoreCase))
            throw new CopilotException(CopilotFailureKind.AccountMismatch);
        return parsed!;
    }
}
