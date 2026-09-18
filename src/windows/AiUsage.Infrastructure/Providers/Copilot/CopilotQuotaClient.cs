using AiUsage.Core.Usage;
using System.Net.Http.Headers;

namespace AiUsage.Infrastructure.Providers.Copilot;

public sealed class CopilotQuotaClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly object sync = new();
    private string? throttledIdentity;
    private DateTimeOffset retryAt;

    /// <summary>Reads subscription quota without inference, routing overrides or automatic retries.</summary>
    public async Task<QuotaSnapshot> GetQuotaAsync(CopilotCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (credentials.AccountId == throttledIdentity && clock.GetUtcNow() < retryAt)
                throw new CopilotException(ProviderFailureKind.RateLimited, retryAfter: retryAt - clock.GetUtcNow());
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, CopilotHttp.UsageUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        using var response = await CopilotHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            var error = CopilotHttp.Failure(response);
            if (error.Kind == ProviderFailureKind.RateLimited)
            {
                lock (sync)
                {
                    throttledIdentity = credentials.AccountId;
                    var delay = error.RetryAfter ?? TimeSpan.FromMinutes(1);
                    retryAt = delay >= DateTimeOffset.MaxValue - clock.GetUtcNow() ? DateTimeOffset.MaxValue : clock.GetUtcNow() + delay;
                }
            }
            throw error;
        }
        return CopilotQuotaParser.Parse(response.Body!.RootElement, clock.GetUtcNow());
    }
}
