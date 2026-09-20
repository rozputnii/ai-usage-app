using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// One app-owned connection. No source CLI credentials are read. Connecting can provision the free
/// tier on an account that has none, which is a provider-side write; refresh and resume never can.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AntigravitySession(AntigravityAuthClient auth, AntigravityQuotaClient quota,
    AntigravityStateStore store, TimeProvider clock) : IProviderSession, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private AntigravityStoredState? stored;
    private AntigravityCredentials? credentials;
    private bool disposed;

    public bool HasStoredGrant { get; private set; }
    public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;

    public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) =>
        RunAsync((_, _) => Task.FromResult(Cached()), cancellationToken);

    public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);

    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        if (stored is null)
            return ProviderSessionState.NotConnected;
        if (stored.NeedsReauthentication)
            return Cached(ProviderFailureKind.AuthenticationRequired);
        if (credentials is null || credentials.AccountId != stored.AccountId ||
            credentials.ProjectId != stored.ProjectId || credentials.RefreshAt <= clock.GetUtcNow())
        {
            token.ThrowIfCancellationRequested();
            // Renewal revalidates the account binding through userinfo before the grant is adopted.
            var renewed = await auth.RefreshAsync(stored.RefreshToken, stored.AccountId, token).ConfigureAwait(false);
            // Google normally returns no replacement refresh token. A returned one must be durable
            // before it is used, because the previous value may already have been invalidated.
            if (renewed.RefreshToken != stored.RefreshToken)
                stored = await lease.SaveAsync(stored with { RefreshToken = renewed.RefreshToken }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
            credentials = new(renewed.AccessToken, renewed.RefreshToken, renewed.AccountId, stored.ProjectId, stored.Tier, renewed.RefreshAt);
        }
        return await ReadQuotaAsync(lease, token).ConfigureAwait(false);
    }, cancellationToken);

    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            using var attempt = auth.BeginBrowserLogin();
            try { openAuthorizationUrl(attempt.AuthorizationUrl); }
            catch (InvalidOperationException) { throw new AntigravityException(ProviderFailureKind.BrowserCallbackUnavailable); }
            catch (System.ComponentModel.Win32Exception) { throw new AntigravityException(ProviderFailureKind.BrowserCallbackUnavailable); }
            var grant = await auth.CompleteBrowserLoginAsync(attempt, token).ConfigureAwait(false);
            // One slot per provider: a different account must be disconnected deliberately first.
            if (stored is not null && grant.AccountId != stored.AccountId)
                throw new AntigravityException(ProviderFailureKind.AccountMismatch);
            var workspace = await quota.DiscoverWorkspaceAsync(grant.AccessToken, token).ConfigureAwait(false);
            var connected = new AntigravityCredentials(grant.AccessToken, grant.RefreshToken, grant.AccountId,
                workspace.ProjectId, workspace.Tier, grant.RefreshAt);
            if (stored is not null)
            {
                // A replacement proves quota access before the previous grant is discarded.
                var reading = await quota.GetQuotaAsync(connected, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                stored = await lease.SaveAsync(Record(connected, reading), stored.Revision, CancellationToken.None).ConfigureAwait(false);
                credentials = connected;
                HasStoredGrant = true;
                return Available(reading);
            }
            stored = await lease.SaveAsync(Record(connected, null), null, CancellationToken.None).ConfigureAwait(false);
            credentials = connected;
            HasStoredGrant = true;
            return await ReadQuotaAsync(lease, token).ConfigureAwait(false);
        }, cancellationToken);

    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        await lease.DeleteAsync(token).ConfigureAwait(false);
        stored = null;
        credentials = null;
        HasStoredGrant = false;
        return ProviderSessionState.NotConnected;
    }, cancellationToken, load: false);

    private static AntigravityStoredState Record(AntigravityCredentials next, QuotaSnapshot? reading) => new()
    {
        AccountId = next.AccountId, RefreshToken = next.RefreshToken, ProjectId = next.ProjectId,
        Tier = next.Tier, CachedQuota = reading
    };

    private async Task<ProviderSessionState> ReadQuotaAsync(AntigravityStateLease lease, CancellationToken token)
    {
        try
        {
            var reading = await quota.GetQuotaAsync(credentials!, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            stored = await lease.SaveAsync(stored! with { CachedQuota = reading }, stored!.Revision, CancellationToken.None).ConfigureAwait(false);
            return Available(reading);
        }
        catch (AntigravityException error) when (error.Kind is ProviderFailureKind.AuthenticationRequired or ProviderFailureKind.AccountMismatch)
        {
            credentials = null;
            stored = await lease.SaveAsync(stored! with { NeedsReauthentication = true }, stored!.Revision, CancellationToken.None).ConfigureAwait(false);
            return Cached(error.Kind);
        }
    }

    private static ProviderSessionState Available(QuotaSnapshot reading) =>
        new(ProviderSessionStatus.QuotaAvailable, reading, RetrievedAt: reading.FetchedAt);

    private ProviderSessionState Cached(ProviderFailureKind? failure = null)
    {
        var reading = stored?.CachedQuota;
        var status = stored?.NeedsReauthentication == true ? ProviderSessionStatus.ReauthenticationRequired
            : stored is null ? ProviderSessionStatus.NotConnected : ProviderSessionStatus.QuotaUnavailable;
        return new(status, reading, failure, reading?.FetchedAt, FromCache: reading is not null);
    }

    private Task<ProviderSessionState> RunAsync(Func<AntigravityStateLease, CancellationToken, Task<ProviderSessionState>> operation,
        CancellationToken cancellationToken, bool load = true) => Task.Run(async () =>
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Antigravity work is already in progress.");
        try
        {
            State = ProviderSessionState.Working;
            await using var lease = await store.AcquireAsync(cancellationToken).ConfigureAwait(false);
            if (load)
            {
                var loaded = await lease.LoadAsync(cancellationToken).ConfigureAwait(false);
                if (loaded?.Revision != stored?.Revision)
                    credentials = null;
                stored = loaded;
                HasStoredGrant = stored is not null;
            }
            return State = await operation(lease, cancellationToken).ConfigureAwait(false);
        }
        catch (AntigravityException error)
        {
            if (error.Kind is ProviderFailureKind.StorageUnavailable or ProviderFailureKind.RecoveryRequired or ProviderFailureKind.GrantNotRemoved)
            {
                credentials = null;
                HasStoredGrant = true;
                return State = new(ProviderSessionStatus.RecoveryRequired, Failure: error.Kind);
            }
            return State = Cached(error.Kind);
        }
        catch (OperationCanceledException)
        {
            State = Cached();
            throw;
        }
        finally { gate.Release(); }
    }, CancellationToken.None);

    public void Dispose()
    {
        if (disposed)
            return;
        if (!gate.Wait(0))
            throw new InvalidOperationException("Drain Antigravity work before disposing it.");
        disposed = true;
        credentials = null;
        stored = null;
        gate.Release();
        gate.Dispose();
    }
}
