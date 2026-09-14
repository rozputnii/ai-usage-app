using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

public enum CodexSessionStatus
{
    NotConnected,
    Working,
    QuotaAvailable,
    QuotaUnavailable,
    ReauthenticationRequired
}

/// <summary>
/// What a consumer may render. `Quota` is present only for <see cref="CodexSessionStatus.QuotaAvailable"/>;
/// no status implies a numeric value, so an unavailable quota can never be displayed as zero.
/// </summary>
public sealed record CodexSessionState(CodexSessionStatus Status, QuotaSnapshot? Quota = null, CodexFailureKind? Failure = null)
{
    public static readonly CodexSessionState NotConnected = new(CodexSessionStatus.NotConnected);
    public static readonly CodexSessionState Working = new(CodexSessionStatus.Working);
}

/// <summary>
/// Owns one Codex session for the product: the stored grant, the in-memory credentials and the
/// last observed quota. All provider traffic goes through the shared clients; this type adds no
/// endpoint, header or parsing of its own.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexSession(CodexAuthClient auth, CodexQuotaClient quota, CodexGrantStore store) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private CodexCredentials? credentials;
    private bool disposed;

    public CodexSessionState State { get; private set; } = CodexSessionState.NotConnected;

    /// <summary>True when a grant is stored, whether or not it still works.</summary>
    public bool HasStoredGrant => store.Read() is not null;

    /// <summary>Restores the stored grant, if any, and reads quota once. Never starts a browser sign-in.</summary>
    public Task<CodexSessionState> ResumeAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async token =>
        {
            if (store.Read() is not { } grant)
                return CodexSessionState.NotConnected;
            var restored = await auth.ResumeAsync(grant, token).ConfigureAwait(false);
            Adopt(restored);
            // The provider rotates the refresh token, so the stored record must follow it.
            Persist();
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>
    /// Runs the browser authorization-code flow. The caller opens <paramref name="openAuthorizationUrl"/>
    /// so the user performs consent themselves; nothing is persisted until the exchange succeeds.
    /// </summary>
    public Task<CodexSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openAuthorizationUrl);
        return RunAsync(async token =>
        {
            using var authorization = auth.BeginBrowserLogin();
            openAuthorizationUrl(authorization.AuthorizationUrl);
            var signedIn = await auth.CompleteBrowserLoginAsync(authorization, token).ConfigureAwait(false);
            Adopt(signedIn);
            Persist();
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <summary>Reads quota again, refreshing the grant first when the access token has expired.</summary>
    public Task<CodexSessionState> RefreshAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async token =>
        {
            if (credentials is null)
                return await ResumeCoreAsync(token).ConfigureAwait(false);
            if (credentials.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                await auth.RefreshAsync(credentials, token).ConfigureAwait(false);
                Persist();
            }
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>
    /// Forgets the local session and removes the stored grant. This does not revoke anything at the
    /// provider; the account must be disconnected there separately if that is wanted.
    /// </summary>
    public Task<CodexSessionState> DisconnectAsync(CancellationToken cancellationToken = default) =>
        RunAsync(token =>
        {
            store.Delete();
            credentials?.Dispose();
            credentials = null;
            return Task.FromResult(CodexSessionState.NotConnected);
        }, cancellationToken);

    private async Task<CodexSessionState> ResumeCoreAsync(CancellationToken token)
    {
        if (store.Read() is not { } grant)
            return CodexSessionState.NotConnected;
        Adopt(await auth.ResumeAsync(grant, token).ConfigureAwait(false));
        Persist();
        return await ReadQuotaAsync(token).ConfigureAwait(false);
    }

    private async Task<CodexSessionState> ReadQuotaAsync(CancellationToken token)
    {
        try
        {
            return new CodexSessionState(CodexSessionStatus.QuotaAvailable, await quota.GetQuotaAsync(credentials!, token).ConfigureAwait(false));
        }
        catch (CodexException error) when (error.Kind != CodexFailureKind.AuthenticationRequired)
        {
            // A quota failure is not a lost session: the grant stays usable for the next attempt.
            return new CodexSessionState(CodexSessionStatus.QuotaUnavailable, Failure: error.Kind);
        }
    }

    private void Adopt(CodexCredentials restored)
    {
        credentials?.Dispose();
        credentials = restored;
    }

    private void Persist() => store.Write(new CodexStoredGrant(credentials!.AccountId, credentials.RefreshToken));

    private async Task<CodexSessionState> RunAsync(Func<CancellationToken, Task<CodexSessionState>> operation, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            State = CodexSessionState.Working;
            return State = await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (CodexException error) when (error.Kind == CodexFailureKind.AuthenticationRequired)
        {
            // The stored grant is gone or refused; keep the record so the user can retry a sign-in knowingly.
            credentials?.Dispose();
            credentials = null;
            return State = new CodexSessionState(CodexSessionStatus.ReauthenticationRequired, Failure: error.Kind);
        }
        catch (CodexException error)
        {
            return State = new CodexSessionState(
                credentials is null ? CodexSessionStatus.NotConnected : CodexSessionStatus.QuotaUnavailable, Failure: error.Kind);
        }
        catch (OperationCanceledException)
        {
            State = credentials is null ? CodexSessionState.NotConnected : new CodexSessionState(CodexSessionStatus.QuotaUnavailable);
            throw;
        }
        finally { gate.Release(); }
    }

    public void Dispose()
    {
        disposed = true;
        credentials?.Dispose();
        gate.Dispose();
    }
}
