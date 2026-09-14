using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>One app-owned connection. No source CLI credentials are read or imported.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ClaudeSession(ClaudeAuthClient auth, ClaudeQuotaClient quota, ClaudeStateStore store, TimeProvider clock) : IProviderSession, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private ClaudeStoredState? stored;
    private ClaudeCredentials? credentials;
    private ClaudeBrowserAuthorization? authorization;
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
        if (credentials is null || credentials.Identity != stored.Identity || credentials.RefreshAt <= clock.GetUtcNow())
        {
            token.ThrowIfCancellationRequested();
            var previous = credentials ?? new ClaudeCredentials("", stored.RefreshToken, stored.Identity, DateTimeOffset.MinValue);
            // This durable marker prevents replay after a crash, cancellation or ambiguous response.
            stored = await lease.SaveAsync(stored with { NeedsReauthentication = true }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
            credentials = null;
            var renewed = await auth.RefreshAsync(previous, token).ConfigureAwait(false);
            await PromoteAsync(lease, renewed).ConfigureAwait(false);
        }
        return await ReadQuotaAsync(lease, token).ConfigureAwait(false);
    }, cancellationToken);

    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        using var attempt = auth.BeginBrowserLogin();
        Volatile.Write(ref authorization, attempt);
        try
        {
            try { openAuthorizationUrl(attempt.AuthorizationUrl); }
            catch (InvalidOperationException) { throw new ClaudeException(ClaudeFailureKind.BrowserCallbackUnavailable); }
            catch (System.ComponentModel.Win32Exception) { throw new ClaudeException(ClaudeFailureKind.BrowserCallbackUnavailable); }
            var connected = await auth.CompleteBrowserLoginAsync(attempt, token).ConfigureAwait(false);
            if (stored is not null && connected.Identity != stored.Identity)
            {
                // A new account must prove the requested quota capability before replacing a
                // working connection. This is a new login, not a rotation of the stored grant.
                var initialQuota = await quota.GetQuotaAsync(connected, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var replacement = new ClaudeStoredState
                {
                    Identity = connected.Identity, RefreshToken = connected.RefreshToken, CachedQuota = initialQuota
                };
                stored = await lease.SaveAsync(replacement, stored.Revision, CancellationToken.None).ConfigureAwait(false);
                credentials = connected;
                HasStoredGrant = true;
                return new(ProviderSessionStatus.QuotaAvailable, initialQuota.Quota,
                    RetrievedAt: initialQuota.Quota.FetchedAt, ExtraUsage: initialQuota.ExtraUsage);
            }
            await PromoteAsync(lease, connected).ConfigureAwait(false);
            return await ReadQuotaAsync(lease, token).ConfigureAwait(false);
        }
        finally { Volatile.Write(ref authorization, null); }
    }, cancellationToken);

    public bool TrySubmitCode(string code) => Volatile.Read(ref authorization)?.TrySubmitCode(code) == true;

    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        await lease.DeleteAsync(token).ConfigureAwait(false);
        stored = null;
        credentials = null;
        HasStoredGrant = false;
        return ProviderSessionState.NotConnected;
    }, cancellationToken, load: false);

    private async Task PromoteAsync(ClaudeStateLease lease, ClaudeCredentials next)
    {
        var record = new ClaudeStoredState
        {
            Identity = next.Identity, RefreshToken = next.RefreshToken,
            CachedQuota = stored?.Identity == next.Identity ? stored.CachedQuota : null
        };
        // Cancellation after a returned rotating pair must not discard it. DPAPI and replacement
        // run on a worker; shutdown waits for this bounded cutover before disposing the session.
        stored = await lease.SaveAsync(record, stored?.Revision, CancellationToken.None).ConfigureAwait(false);
        credentials = next;
        HasStoredGrant = true;
    }

    private async Task<ProviderSessionState> ReadQuotaAsync(ClaudeStateLease lease, CancellationToken token)
    {
        try
        {
            var reading = await quota.GetQuotaAsync(credentials!, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            stored = await lease.SaveAsync(stored! with { CachedQuota = reading }, stored!.Revision, CancellationToken.None).ConfigureAwait(false);
            return new(ProviderSessionStatus.QuotaAvailable, reading.Quota, RetrievedAt: reading.Quota.FetchedAt, ExtraUsage: reading.ExtraUsage);
        }
        catch (ClaudeException error) when (error.Kind == ClaudeFailureKind.AuthenticationRequired)
        {
            credentials = null;
            stored = await lease.SaveAsync(stored! with { NeedsReauthentication = true }, stored!.Revision, CancellationToken.None).ConfigureAwait(false);
            return Cached(ProviderFailureKind.AuthenticationRequired);
        }
    }

    private ProviderSessionState Cached(ProviderFailureKind? failure = null)
    {
        var reading = stored?.CachedQuota;
        var status = stored?.NeedsReauthentication == true ? ProviderSessionStatus.ReauthenticationRequired
            : stored is null ? ProviderSessionStatus.NotConnected : ProviderSessionStatus.QuotaUnavailable;
        return new(status, reading?.Quota, failure, reading?.Quota.FetchedAt, FromCache: reading is not null, ExtraUsage: reading?.ExtraUsage);
    }

    private Task<ProviderSessionState> RunAsync(Func<ClaudeStateLease, CancellationToken, Task<ProviderSessionState>> operation,
        CancellationToken cancellationToken, bool load = true) => Task.Run(async () =>
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Claude work is already in progress.");
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
        catch (ClaudeException error)
        {
            var failure = Enum.Parse<ProviderFailureKind>(error.Kind.ToString());
            if (error.Kind is ClaudeFailureKind.StorageUnavailable or ClaudeFailureKind.RecoveryRequired or ClaudeFailureKind.GrantNotRemoved)
            {
                credentials = null;
                HasStoredGrant = true;
                return State = new(ProviderSessionStatus.RecoveryRequired, Failure: failure);
            }
            return State = Cached(failure);
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
        if (!gate.Wait(0))
            throw new InvalidOperationException("Drain Claude work before disposing it.");
        disposed = true;
        credentials = null;
        stored = null;
        gate.Release();
        gate.Dispose();
    }
}
