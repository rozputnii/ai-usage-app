using AiUsage.Core.Usage;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Codex;

internal sealed class CodexCredentials : IDisposable
{
    private bool disposed;

    internal CodexCredentials(string accessToken, string refreshToken, string accountId, DateTimeOffset expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccountId = accountId;
        ExpiresAt = expiresAt;
    }

    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal string AccessToken { get; private set; }
    internal string RefreshToken { get; private set; }
    internal int Revision { get; private set; }
    [JsonIgnore] public string AccountId { get; }
    [JsonIgnore] public DateTimeOffset ExpiresAt { get; private set; }
    [JsonIgnore] public bool RequiresReauthentication { get; private set; }

    internal void EnsureUsable()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (RequiresReauthentication)
            throw new CodexException(ProviderFailureKind.AuthenticationRequired);
    }

    internal void Update(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        Revision++;
    }

    internal void RequireReauthentication() => RequiresReauthentication = true;

    public override string ToString() => "CodexCredentials (redacted)";

    public void Dispose()
    {
        Gate.Wait();
        try
        {
            disposed = true;
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
        }
        finally { Gate.Release(); }
        // Strings cannot be reliably zeroed; no durable credential material is written by this slice.
    }
}
