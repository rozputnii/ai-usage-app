using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>
/// Owns one Codex session for the product: the stored grant, the in-memory credentials and the
/// last observed quota. All provider traffic goes through the shared clients; this type adds no
/// endpoint, header or parsing of its own.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class CodexSession : IProviderSession, IDisposable
{
    private readonly IDiagnosticSink? diagnostics;
    private readonly CodexAuthClient auth;
    private readonly CodexQuotaClient quota;
    private readonly CodexGrantStore store;
    private readonly CodexQuotaCache cache;
    private readonly CodexHistoryClient history;

    internal CodexSession(CodexAuthClient auth, CodexQuotaClient quota, CodexGrantStore store, CodexQuotaCache cache, CodexHistoryClient history, IDiagnosticSink? diagnostics = null)
    {
        this.diagnostics = diagnostics;
        this.auth = auth;
        this.quota = quota;
        this.store = store;
        this.cache = cache;
        this.history = history;
    }

    private readonly SemaphoreSlim gate = new(1, 1);
    private CodexCredentials? credentials;
    private CodexGrantStore.StoredRecord? stored;
    private bool disposed;

    public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;

    /// <summary>True when a grant is stored, whether or not it still works.</summary>
    public bool HasStoredGrant { get; private set; }
    internal Accounts.ProviderIdentity? Identity => stored?.AccountId is { } id ? new(id) : null;

    /// <summary>
    /// The last cached reading for a connected account, marked stale. Used to render something
    /// truthful before the first provider request of a session completes.
    /// </summary>
    public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) =>
        RunAsync((_, _) => Task.FromResult(HasStoredGrant ? Stale(ProviderSessionStatus.QuotaUnavailable, null) : ProviderSessionState.NotConnected), cancellationToken);

    /// <summary>Restores the stored grant, if any, and reads quota once. Never starts a browser sign-in.</summary>
    public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) =>
        RunAsync(ResumeCoreAsync, cancellationToken);

    /// <summary>
    /// Runs the browser authorization-code flow. The caller opens <paramref name="openAuthorizationUrl"/>
    /// so the user performs consent themselves; nothing is persisted until the exchange succeeds.
    /// </summary>
    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openAuthorizationUrl);
        return RunAsync(async (lease, token) =>
        {
            using var authorization = auth.BeginBrowserLogin();
            try { openAuthorizationUrl(authorization.AuthorizationUrl); }
            catch (InvalidOperationException) { throw new CodexException(ProviderFailureKind.BrowserCallbackUnavailable); }
            catch (System.ComponentModel.Win32Exception) { throw new CodexException(ProviderFailureKind.BrowserCallbackUnavailable); }
            var signedIn = await auth.CompleteBrowserLoginAsync(authorization, token).ConfigureAwait(false);
            Adopt(signedIn);
            await PersistAsync(lease).ConfigureAwait(false);
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken, forFreshAuthorization: true);
    }

    /// <summary>Reads quota again, refreshing the grant first when the access token has expired.</summary>
    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            if (credentials is null)
                return await ResumeCoreAsync(lease, token).ConfigureAwait(false);
            if (credentials.ExpiresAt <= DateTimeOffset.UtcNow)
                await RenewAsync(lease, token).ConfigureAwait(false);
            return await ReadQuotaAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    /// <summary>
    /// Forgets the local session and removes the stored grant. This does not revoke anything at the
    /// provider; the account must be disconnected there separately if that is wanted.
    /// </summary>
    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            await lease.DeleteAsync(token).ConfigureAwait(false);
            stored = null;
            HasStoredGrant = false;
            credentials?.Dispose();
            credentials = null;
            // Deleting the durable grant commits disconnect. Finish local cleanup even when
            // the original request is cancelled, and never retain a usable in-memory grant.
            await cache.DeleteAsync(CancellationToken.None).ConfigureAwait(false);
            return ProviderSessionState.NotConnected;
        }, cancellationToken, load: false);

    private async Task<ProviderSessionState> ResumeCoreAsync(ProviderStateLease<CodexGrantStore.StoredRecord> lease, CancellationToken token)
    {
        if (stored is null)
            return ProviderSessionState.NotConnected;
        credentials?.Dispose();
        credentials = null;
        await RenewAsync(lease, token).ConfigureAwait(false);
        return await ReadQuotaAsync(token).ConfigureAwait(false);
    }

    private async Task RenewAsync(ProviderStateLease<CodexGrantStore.StoredRecord> lease, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var record = stored!;
        var revision = CodexGrantStore.Revision(record);
        await lease.BeginExternalUpdateAsync(revision, token).ConfigureAwait(false);
        try
        {
            if (credentials is null)
                Adopt(await auth.ResumeAsync(new(record.AccountId!, record.RefreshToken!), token).ConfigureAwait(false));
            else
                await auth.RefreshAsync(credentials, token).ConfigureAwait(false);
        }
        catch (CodexException error) when (error.Kind == ProviderFailureKind.RateLimited)
        {
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
            // A known rejection preserves the existing retry semantics. Unknown outcomes
            // leave the intent in place, including across process termination/relaunch.
            stored = await lease.SaveAsync(record, revision, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (CodexException error) when (error.Kind != ProviderFailureKind.AuthenticationRequired)
        {
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        }
        catch (OperationCanceledException)
        {
            State = StorageFailure(ProviderFailureKind.RecoveryRequired);
            throw;
        }
        await PersistAsync(lease).ConfigureAwait(false);
    }

    private async Task<ProviderSessionState> ReadQuotaAsync(CancellationToken token)
    {
        try
        {
            var snapshot = await quota.GetQuotaAsync(credentials!, token).ConfigureAwait(false);
            await cache.WriteAsync(new CachedQuota(snapshot, snapshot.FetchedAt), token).ConfigureAwait(false);
            return new ProviderSessionState(ProviderSessionStatus.QuotaAvailable, snapshot, RetrievedAt: snapshot.FetchedAt);
        }
        catch (CodexException error) when (error.Kind != ProviderFailureKind.AuthenticationRequired)
        {
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
            // A quota failure is not a lost session: the grant stays usable, and the last known
            // reading is offered as explicitly stale rather than replaced by nothing.
            return Stale(ProviderSessionStatus.QuotaUnavailable, error.Kind);
        }
    }

    private ProviderSessionState Stale(ProviderSessionStatus status, ProviderFailureKind? failure)
    {
        try
        {
            return cache.Read() is { } cached
                ? new ProviderSessionState(status, cached.Quota, failure, cached.RetrievedAt, FromCache: true)
                : new ProviderSessionState(status, Failure: failure);
        }
        catch (ProviderException error) { return StorageFailure(error.Kind); }
    }

    private ProviderSessionState StorageFailure(ProviderFailureKind failure)
    {
        credentials?.Dispose();
        credentials = null;
        HasStoredGrant = true; // Retain a recovery/disconnect surface when storage is unreadable.
        return new(ProviderSessionStatus.RecoveryRequired, Failure: failure);
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

    private Task<ProviderSessionState> RunAsync(Func<ProviderStateLease<CodexGrantStore.StoredRecord>, CancellationToken, Task<ProviderSessionState>> operation,
        CancellationToken cancellationToken, bool load = true, bool forFreshAuthorization = false) => Task.Run(async () =>
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            State = ProviderSessionState.Working;
            await using var lease = await store.AcquireAsync(cancellationToken).ConfigureAwait(false);
            if (load)
            {
                var loaded = await lease.LoadAsync(cancellationToken, forFreshAuthorization).ConfigureAwait(false);
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
        catch (CodexException error) when (error.Kind == ProviderFailureKind.AuthenticationRequired)
        {
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
            // Keep a refused grant non-replayable; explicit fresh authorization can replace its intact intent.
            credentials?.Dispose();
            credentials = null;
            return State = Stale(ProviderSessionStatus.ReauthenticationRequired, error.Kind);
        }
        catch (CodexException error)
        {
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
            return State = Stale(
                credentials is null ? ProviderSessionStatus.NotConnected : ProviderSessionStatus.QuotaUnavailable, error.Kind);
        }
        catch (ProviderException error)
        {
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
            return State = StorageFailure(error.Kind);
        }
        catch (IOException error) { diagnostics?.Failure(DiagnosticEvent.OperationFailure, error); return State = StorageFailure(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException error) { diagnostics?.Failure(DiagnosticEvent.OperationFailure, error); return State = StorageFailure(ProviderFailureKind.StorageUnavailable); }
        catch (OperationCanceledException)
        {
            if (State.Status != ProviderSessionStatus.RecoveryRequired)
                State = credentials is null ? ProviderSessionState.NotConnected : new ProviderSessionState(ProviderSessionStatus.QuotaUnavailable);
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
