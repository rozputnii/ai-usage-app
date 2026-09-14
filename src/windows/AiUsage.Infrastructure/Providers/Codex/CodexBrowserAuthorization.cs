using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>
/// One pending browser sign-in. The loopback listener, PKCE verifier and state stay
/// process-local; only the authorization URL is shown to the user.
/// </summary>
public sealed class CodexBrowserAuthorization : IDisposable
{
    internal CodexBrowserAuthorization(LoopbackCallback callback, Uri authorizationUrl, string redirectUri, string state, string codeVerifier, DateTimeOffset expiresAt)
    {
        Callback = callback;
        AuthorizationUrl = authorizationUrl;
        RedirectUri = redirectUri;
        State = state;
        CodeVerifier = codeVerifier;
        ExpiresAt = expiresAt;
    }

    internal LoopbackCallback Callback { get; }
    internal string RedirectUri { get; }
    internal string State { get; }
    internal string CodeVerifier { get; }
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal bool Completed { get; set; }

    /// <summary>Provider-hosted consent URL for this attempt; safe to open in the user's browser.</summary>
    [JsonIgnore] public Uri AuthorizationUrl { get; }
    public DateTimeOffset ExpiresAt { get; }

    public override string ToString() => "CodexBrowserAuthorization (redacted)";

    public void Dispose()
    {
        Callback.Dispose();
        Gate.Dispose();
    }
}
