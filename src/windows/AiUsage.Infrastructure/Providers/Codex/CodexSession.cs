using AiUsage.Core.Providers.Codex;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>
/// Owns one Codex session for the product: the stored grant, the in-memory credentials and the
/// last observed quota. All provider traffic goes through the shared clients; this type adds no
/// endpoint, header or parsing of its own.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexSession(CodexAuthClient auth, CodexQuotaClient quota, CodexGrantStore store, CodexQuotaCache cache) : ICodexSession, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private CodexCredentials? credentials;
    private CodexGrantStore.StoredRecord? stored;
    private bool disposed;

    public CodexSessionState State { get; private set; } = CodexSessionState.NotConnected;

    /// <summary>True when a grant is stored, whether or not it still works.</summary>
    public bool HasStoredGrant { get; private set; }

    /// <summary>
    /// The last cached reading for a connected account, marked stale. Used to render something
    /// truthful before the first provider request of a session completes.
    /// </summary>
    public CodexSessionState ReadCachedState() =>
        RunAsync((_, _) => Task.FromResult(HasStoredGrant ? Stale(CodexSessionStatus.QuotaUnavailable, null) : CodexSessionState.NotConnected), CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Restores the stored grant, if any, and reads quota once. Never starts a browser sign-in.</summary>
    public Task<CodexSessionState> ResumeAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            if (stored is not { } record)
                return CodexSessionState.NotConnected;
            var restored = await auth.ResumeAsync(new CodexStoredGrant(record.AccountId!, record.RefreshToken!), token).ConfigureAwait(false);
            Adopt(restored);
            // The provider rotates the refresh token, so the stored record must follow it.
            await PersistAsync(lease).ConfigureAwait(false);
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>
    /// Runs the browser authorization-code flow. The caller opens <paramref name="openAuthorizationUrl"/>
    /// so the user performs consent themselves; nothing is persisted until the exchange succeeds.
    /// </summary>
    public Task<CodexSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openAuthorizationUrl);
        return RunAsync(async (lease, token) =>
        {
            using var authorization = auth.BeginBrowserLogin();
            openAuthorizationUrl(authorization.AuthorizationUrl);
            var signedIn = await auth.CompleteBrowserLoginAsync(authorization, token).ConfigureAwait(false);
            Adopt(signedIn);
            await PersistAsync(lease).ConfigureAwait(false);
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <summary>Reads quota again, refreshing the grant first when the access token has expired.</summary>
    public Task<CodexSessionState> RefreshAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            if (credentials is null)
                return await ResumeCoreAsync(lease, token).ConfigureAwait(false);
            if (credentials.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                await auth.RefreshAsync(credentials, token).ConfigureAwait(false);
                await PersistAsync(lease).ConfigureAwait(false);
            }
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>
    /// Forgets the local session and removes the stored grant. This does not revoke anything at the
    /// provider; the account must be disconnected there separately if that is wanted.
    /// </summary>
    public Task<CodexSessionState> DisconnectAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            await lease.DeleteAsync(token).ConfigureAwait(false);
            stored = null;
            HasStoredGrant = false;
            await cache.DeleteAsync(token).ConfigureAwait(false);
            credentials?.Dispose();
            credentials = null;
            return CodexSessionState.NotConnected;
        }, cancellationToken, load: false);

    private async Task<CodexSessionState> ResumeCoreAsync(ProviderStateLease<CodexGrantStore.StoredRecord> lease, CancellationToken token)
    {
        if (stored is not { } record)
            return CodexSessionState.NotConnected;
        Adopt(await auth.ResumeAsync(new CodexStoredGrant(record.AccountId!, record.RefreshToken!), token).ConfigureAwait(false));
        await PersistAsync(lease).ConfigureAwait(false);
        return await ReadQuotaAsync(token).ConfigureAwait(false);
    }

    private async Task<CodexSessionState> ReadQuotaAsync(CancellationToken token)
    {
        try
        {
            var snapshot = await quota.GetQuotaAsync(credentials!, token).ConfigureAwait(false);
            await cache.WriteAsync(new CachedQuota(snapshot, snapshot.FetchedAt), token).ConfigureAwait(false);
            return new CodexSessionState(CodexSessionStatus.QuotaAvailable, snapshot, RetrievedAt: snapshot.FetchedAt);
        }
        catch (CodexException error) when (error.Kind != CodexFailureKind.AuthenticationRequired)
        {
            // A quota failure is not a lost session: the grant stays usable, and the last known
            // reading is offered as explicitly stale rather than replaced by nothing.
            return Stale(CodexSessionStatus.QuotaUnavailable, error.Kind);
        }
    }

    private CodexSessionState Stale(CodexSessionStatus status, CodexFailureKind? failure)
    {
        try
        {
            return cache.Read() is { } cached
                ? new CodexSessionState(status, cached.Quota, failure, cached.RetrievedAt, FromCache: true)
                : new CodexSessionState(status, Failure: failure);
        }
        catch (ProviderException error) { return StorageFailure(error.Kind); }
    }

    private CodexSessionState StorageFailure(ProviderFailureKind failure)
    {
        credentials?.Dispose();
        credentials = null;
        HasStoredGrant = true; // Retain a recovery/disconnect surface when storage is unreadable.
        return new(CodexSessionStatus.RecoveryRequired, Failure: failure switch
        {
            ProviderFailureKind.RecoveryRequired => CodexFailureKind.RecoveryRequired,
            ProviderFailureKind.GrantNotRemoved => CodexFailureKind.GrantNotRemoved,
            ProviderFailureKind.StorageUnavailable => CodexFailureKind.StorageUnavailable,
            _ => throw new InvalidOperationException("Unexpected storage classification.")
        });
    }

    private void Adopt(CodexCredentials restored)
    {
        credentials?.Dispose();
        credentials = restored;
    }

    private async Task PersistAsync(ProviderStateLease<CodexGrantStore.StoredRecord> lease)
    {
        var next = CodexGrantStore.Record(new CodexStoredGrant(credentials!.AccountId, credentials.RefreshToken));
        stored = await lease.SaveAsync(next, stored is null ? null : CodexGrantStore.Revision(stored), CancellationToken.None).ConfigureAwait(false);
        HasStoredGrant = true;
    }

    private Task<CodexSessionState> RunAsync(Func<ProviderStateLease<CodexGrantStore.StoredRecord>, CancellationToken, Task<CodexSessionState>> operation,
        CancellationToken cancellationToken, bool load = true) => Task.Run(async () =>
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            State = CodexSessionState.Working;
            await using var lease = await store.AcquireAsync(cancellationToken).ConfigureAwait(false);
            if (load)
            {
                var loaded = await lease.LoadAsync(cancellationToken).ConfigureAwait(false);
                if ((loaded is null ? (Guid?)null : CodexGrantStore.Revision(loaded)) !=
                    (stored is null ? (Guid?)null : CodexGrantStore.Revision(stored)))
                {
                    credentials?.Dispose();
                    credentials = null;
                }
                stored = loaded;
                HasStoredGrant = stored is not null;
            }
            return State = await operation(lease, cancellationToken).ConfigureAwait(false);
        }
        catch (CodexException error) when (error.Kind == CodexFailureKind.AuthenticationRequired)
        {
            // The stored grant is gone or refused; keep the record so the user can retry a sign-in knowingly.
            credentials?.Dispose();
            credentials = null;
            return State = Stale(CodexSessionStatus.ReauthenticationRequired, error.Kind);
        }
        catch (CodexException error)
        {
            return State = Stale(
                credentials is null ? CodexSessionStatus.NotConnected : CodexSessionStatus.QuotaUnavailable, error.Kind);
        }
        catch (ProviderException error)
        {
            return State = StorageFailure(error.Kind);
        }
        catch (IOException) { return State = StorageFailure(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { return State = StorageFailure(ProviderFailureKind.StorageUnavailable); }
        catch (OperationCanceledException)
        {
            State = credentials is null ? CodexSessionState.NotConnected : new CodexSessionState(CodexSessionStatus.QuotaUnavailable);
            throw;
        }
        finally { gate.Release(); }
    }, CancellationToken.None);

    public void Dispose()
    {
        disposed = true;
        credentials?.Dispose();
        gate.Dispose();
    }
}
