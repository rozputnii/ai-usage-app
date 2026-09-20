using AiUsage.Core.Usage;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// Reads the Cloud Code Assist control plane: one read-only workspace discovery and the quota
/// summary Antigravity's own usage surface uses. No inference, onboarding, model enablement or
/// automatic retry happens here, and no sandbox or legacy model-catalog endpoint is contacted.
/// </summary>
public sealed class AntigravityQuotaClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly object sync = new();
    private string? throttledIdentity;
    private DateTimeOffset retryAt;

    /// <summary>
    /// Resolves the workspace for an access token with a single loadCodeAssist read. An account with
    /// no project is reported, never provisioned: onboardUser changes provider-side entitlement.
    /// </summary>
    internal async Task<AntigravityWorkspace> DiscoverWorkspaceAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = Post(AntigravityHttp.LoadCodeAssistUrl, """{"metadata":{"ideType":"ANTIGRAVITY"}}""", accessToken);
        using var response = await AntigravityHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
            throw AntigravityHttp.Failure(response);
        var root = response.Body!.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new AntigravityException(ProviderFailureKind.InvalidResponse);
        var project = AntigravityAuthClient.Text(AntigravityAuthClient.Property(root, "cloudaicompanionProject"));
        // An ineligible or unprovisioned account is a distinct outcome, not an unavailable provider.
        if (!AntigravityAuthClient.SafeIdentity(project))
            throw new AntigravityException(ProviderFailureKind.ProjectUnavailable, response.StatusCode);
        var tier = AntigravityAuthClient.Text(AntigravityAuthClient.Property(
            AntigravityAuthClient.Property(root, "currentTier"), "id"));
        return new(project!, tier is { Length: > 0 and <= 128 } ? tier : null);
    }

    /// <summary>Reads subscription quota for the discovered project without inference or retries.</summary>
    public async Task<QuotaSnapshot> GetQuotaAsync(AntigravityCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (credentials.AccountId == throttledIdentity && clock.GetUtcNow() < retryAt)
                throw new AntigravityException(ProviderFailureKind.RateLimited, retryAfter: retryAt - clock.GetUtcNow());
        }
        // JsonEncodedText escapes the opaque project identifier without a reflection serializer.
        var body = $$"""{"project":"{{JsonEncodedText.Encode(credentials.ProjectId)}}"}""";
        using var request = Post(AntigravityHttp.QuotaSummaryUrl, body, credentials.AccessToken);
        using var response = await AntigravityHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            var error = AntigravityHttp.Failure(response);
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
        return AntigravityQuotaParser.Parse(response.Body!.RootElement, clock.GetUtcNow(), credentials.Tier);
    }

    private static HttpRequestMessage Post(string url, string json, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
