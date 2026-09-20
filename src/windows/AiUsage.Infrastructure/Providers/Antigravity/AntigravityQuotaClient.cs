using AiUsage.Core.Usage;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// Reads the Cloud Code Assist control plane: workspace discovery and the quota summary
/// Antigravity's own usage surface uses. No inference, model enablement, automatic retry, sandbox
/// host or legacy model-catalog endpoint is used. Connecting an account with no workspace
/// provisions the free tier, which the owner selected on 2026-09-20 to match OMP.
/// </summary>
public sealed class AntigravityQuotaClient
{
    private const string Metadata = """{"ideType":"ANTIGRAVITY"}""";
    private const string FreeTier = "free-tier";
    private static readonly TimeSpan OnboardTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan OnboardInterval = TimeSpan.FromSeconds(1);

    private readonly HttpClient client;
    private readonly TimeProvider clock;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private readonly object sync = new();
    private string? throttledIdentity;
    private DateTimeOffset retryAt;

    public AntigravityQuotaClient(HttpClient client, TimeProvider? timeProvider = null)
        : this(client, timeProvider, null) { }

    internal AntigravityQuotaClient(HttpClient client, TimeProvider? timeProvider, Func<TimeSpan, CancellationToken, Task>? delay)
    {
        this.client = client;
        clock = timeProvider ?? TimeProvider.System;
        this.delay = delay ?? ((duration, token) => Task.Delay(duration, clock, token));
    }

    /// <summary>
    /// Resolves the workspace for an access token. A provisioned account is read without any write.
    /// An account with no tier is enrolled in the free tier through onboardUser, mirroring OMP; that
    /// is a provider-side change to account entitlement and happens only while connecting, never on
    /// a refresh or resume, and only when the provider actively offers that tier.
    /// </summary>
    internal async Task<AntigravityWorkspace> DiscoverWorkspaceAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var loaded = await LoadAsync(accessToken, cancellationToken).ConfigureAwait(false);
        if (Workspace(loaded) is { } ready)
            return ready;
        EnsureFreeTierOffered(loaded);
        await OnboardAsync(accessToken, cancellationToken).ConfigureAwait(false);
        var provisioned = await LoadAsync(accessToken, cancellationToken).ConfigureAwait(false);
        // Provisioning that reports success without yielding a workspace is still no workspace.
        return Workspace(provisioned) ?? throw new AntigravityException(ProviderFailureKind.ProjectUnavailable);
    }

    private async Task<JsonElement> LoadAsync(string accessToken, CancellationToken cancellationToken)
    {
        var loaded = await PostLoadAsync(accessToken, null, cancellationToken).ConfigureAwait(false);
        // Mirrors OMP: a response without paid-tier detail is re-read against the discovered project,
        // which is how the active tier becomes visible. Both calls are reads.
        if (AntigravityAuthClient.Property(loaded, "paidTier").ValueKind is JsonValueKind.Undefined or JsonValueKind.Null &&
            Project(loaded) is { } project)
            return await PostLoadAsync(accessToken, project, cancellationToken).ConfigureAwait(false);
        return loaded;
    }

    private async Task<JsonElement> PostLoadAsync(string accessToken, string? project, CancellationToken cancellationToken)
    {
        var body = project is null
            ? $$"""{"metadata":{{Metadata}}}"""
            : $$"""{"cloudaicompanionProject":"{{JsonEncodedText.Encode(project)}}","metadata":{{Metadata}}}""";
        using var request = Post(AntigravityHttp.LoadCodeAssistUrl, body, accessToken);
        using var response = await AntigravityHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
            throw AntigravityHttp.Failure(response);
        if (response.Body!.RootElement.ValueKind != JsonValueKind.Object)
            throw new AntigravityException(ProviderFailureKind.InvalidResponse);
        // The document is disposed with the response, so the caller gets an independent copy.
        return response.Body.RootElement.Clone();
    }

    /// <summary>Provisions the free tier and waits for the operation the provider returns.</summary>
    private async Task OnboardAsync(string accessToken, CancellationToken cancellationToken)
    {
        var deadline = clock.GetUtcNow() + OnboardTimeout;
        var operation = await PostAsync(AntigravityHttp.OnboardUserUrl,
            $$"""{"tierId":"{{FreeTier}}","metadata":{{Metadata}}}""", accessToken, cancellationToken).ConfigureAwait(false);
        while (true)
        {
            if (Boolean(operation, "done") == true)
            {
                var error = AntigravityAuthClient.Property(operation, "error");
                // The provider's reason is not echoed; a failed provisioning is simply no workspace.
                if (error.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
                    throw new AntigravityException(ProviderFailureKind.ProjectUnavailable);
                if (AntigravityAuthClient.Property(operation, "response").ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                    throw new AntigravityException(ProviderFailureKind.InvalidResponse);
                return;
            }
            var remaining = deadline - clock.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
                throw new AntigravityException(ProviderFailureKind.Timeout);
            await delay(remaining < OnboardInterval ? remaining : OnboardInterval, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            // The operation name is a provider path segment, so it is constrained rather than escaped:
            // escaping would break the slash the provider uses, and an unconstrained value could
            // redirect the poll somewhere else on the host.
            var name = AntigravityAuthClient.Text(AntigravityAuthClient.Property(operation, "name"));
            if (name is not { Length: > 0 and <= 512 } || name.StartsWith('/') || name.Contains("..", StringComparison.Ordinal) ||
                !name.All(character => char.IsAsciiLetterOrDigit(character) || character is '/' or '-' or '_' or '.' or '~'))
                throw new AntigravityException(ProviderFailureKind.InvalidResponse);
            operation = await GetAsync($"{AntigravityHttp.OperationsUrl}/{name}", accessToken, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>A workspace is usable only when the provider reports both a project and a current tier.</summary>
    private static AntigravityWorkspace? Workspace(JsonElement loaded)
    {
        var tier = AntigravityAuthClient.Text(AntigravityAuthClient.Property(
            AntigravityAuthClient.Property(loaded, "currentTier"), "id"));
        return Project(loaded) is { } project && tier is { Length: > 0 and <= 128 } ? new(project, tier) : null;
    }

    private static string? Project(JsonElement loaded)
    {
        var project = AntigravityAuthClient.Text(AntigravityAuthClient.Property(loaded, "cloudaicompanionProject"));
        return AntigravityAuthClient.SafeIdentity(project) ? project : null;
    }

    /// <summary>
    /// Provisioning is permitted only by an explicit offer. The gate is positive on purpose: a write
    /// against someone's account entitlement must not proceed because a refusal was phrased in a way
    /// this code did not recognize, so an absent, changed or empty tier listing declines it.
    /// </summary>
    private static void EnsureFreeTierOffered(JsonElement loaded)
    {
        var allowed = AntigravityAuthClient.Property(loaded, "allowedTiers");
        if (allowed.ValueKind != JsonValueKind.Array || !allowed.EnumerateArray().Any(tier =>
                AntigravityAuthClient.Text(AntigravityAuthClient.Property(tier, "id")) == FreeTier))
            throw new AntigravityException(ProviderFailureKind.ProjectUnavailable);
    }

    private static bool? Boolean(JsonElement root, string key) =>
        AntigravityAuthClient.Property(root, key).ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };

    private async Task<JsonElement> PostAsync(string url, string body, string accessToken, CancellationToken cancellationToken)
    {
        using var request = Post(url, body, accessToken);
        return await ReadAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonElement> GetAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        Authorize(request, accessToken);
        return await ReadAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonElement> ReadAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await AntigravityHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
            throw AntigravityHttp.Failure(response);
        if (response.Body!.RootElement.ValueKind != JsonValueKind.Object)
            throw new AntigravityException(ProviderFailureKind.InvalidResponse);
        return response.Body.RootElement.Clone();
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
        Authorize(request, accessToken);
        return request;
    }

    /// <summary>See <see cref="AntigravityHttp.ClientIdentity"/> for why the control plane is not told who we are.</summary>
    private static void Authorize(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        // The captured string is not a well-formed product token, so it is sent verbatim rather than
        // reformatted; the value is built from constrained parts and can carry no extra header.
        request.Headers.TryAddWithoutValidation("User-Agent", AntigravityHttp.ClientIdentity);
    }
}
