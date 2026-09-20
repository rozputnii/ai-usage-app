using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>One browser sign-in attempt. The verifier and state are transient and never persisted.</summary>
public sealed class AntigravityBrowserAuthorization : IDisposable
{
    internal AntigravityBrowserAuthorization(LoopbackCallback callback, Uri url, string redirect, string state,
        string verifier, AntigravityRegistration registration, DateTimeOffset expires)
    {
        Callback = callback;
        AuthorizationUrl = url;
        RedirectUri = redirect;
        State = state;
        CodeVerifier = verifier;
        Registration = registration;
        ExpiresAt = expires;
    }

    internal LoopbackCallback Callback { get; }
    /// <summary>The exchange repeats the client the authorization URL was built with.</summary>
    internal AntigravityRegistration Registration { get; }
    /// <summary>Sent again with the exchange, so a fallback port stays consistent with the authorization.</summary>
    internal string RedirectUri { get; }
    internal string State { get; }
    internal string CodeVerifier { get; }
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal bool Completed { get; set; }
    [JsonIgnore] public Uri AuthorizationUrl { get; }
    [JsonIgnore] public DateTimeOffset ExpiresAt { get; }

    internal bool MatchesState(string? value) => value is not null && CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(value), Encoding.UTF8.GetBytes(State));

    public override string ToString() => "AntigravityBrowserAuthorization (redacted)";

    /// <summary>Dispose after the completion task has drained.</summary>
    public void Dispose()
    {
        Completed = true;
        Callback.Dispose();
        Gate.Dispose();
    }
}
