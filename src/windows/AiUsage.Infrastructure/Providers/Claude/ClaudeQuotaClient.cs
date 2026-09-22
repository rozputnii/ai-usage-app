using AiUsage.Core.Usage;
using AiUsage.Core.Providers.Claude;
using System.Net.Http.Headers;

namespace AiUsage.Infrastructure.Providers.Claude;

public sealed class ClaudeQuotaClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly object sync = new();
    private ClaudeIdentity? throttledIdentity;
    private DateTimeOffset retryAt;

    /// <summary>Reads subscription quota without inference, routing overrides or automatic retries.</summary>
    public async Task<ClaudeQuotaReading> GetQuotaAsync(ClaudeCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (credentials.Identity == throttledIdentity && clock.GetUtcNow() < retryAt)
                throw new ProviderException(ProviderFailureKind.RateLimited, retryAfter: retryAt - clock.GetUtcNow());
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, ClaudeHttp.UsageUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
        request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
        using var response = await ProviderTransport.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            var error = ProviderTransport.Failure(response);
            if (error.Kind == ProviderFailureKind.RateLimited)
            {
                lock (sync)
                {
                    throttledIdentity = credentials.Identity;
                    var delay = error.RetryAfter ?? TimeSpan.FromMinutes(1);
                    retryAt = delay >= DateTimeOffset.MaxValue - clock.GetUtcNow() ? DateTimeOffset.MaxValue : clock.GetUtcNow() + delay;
                }
            }
            throw error;
        }
        if (!ClaudeQuotaParser.TryParse(response.Body!.RootElement, clock.GetUtcNow(), out var reading))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        return reading;
    }
}
