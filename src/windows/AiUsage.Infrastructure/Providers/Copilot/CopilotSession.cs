using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>One app-owned GitHub grant; OMP's direct-token path needs no inference-token exchange.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CopilotSession(CopilotAuthClient auth, CopilotQuotaClient quota, CopilotStateStore store, TimeProvider clock) : IProviderSession, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private CopilotStoredState? stored;
    private bool disposed;
    public bool HasStoredGrant { get; private set; }
    public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;
    public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) =>
        RunAsync((_, _) => Task.FromResult(Cached()), cancellationToken);
    public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);
    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        if (stored is null) return ProviderSessionState.NotConnected;
        if (stored.NeedsReauthentication || stored.ExpiresAt <= clock.GetUtcNow())
            return Cached(ProviderFailureKind.AuthenticationRequired);
        try
        {
            // Revalidate the stable identity before accepting a quota under the durable account binding.
            var identity = await auth.GetIdentityAsync(stored.AccessToken, token).ConfigureAwait(false);
            if (identity != stored.AccountId) throw new ProviderException(ProviderFailureKind.AccountMismatch);
            var reading = await quota.GetQuotaAsync(new(stored.AccessToken, stored.AccountId, stored.ExpiresAt), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            stored = await lease.SaveAsync(stored with { CachedQuota = reading }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
            return Available(reading);
        }
        catch (ProviderException error) when (error.Kind is ProviderFailureKind.AuthenticationRequired or ProviderFailureKind.AccountMismatch)
        {
            stored = await lease.SaveAsync(stored with { NeedsReauthentication = true }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
            return Cached(error.Kind);
        }
    }, cancellationToken);

    // Consumers needing device flow must display the transient challenge rather than lose its code.
    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) =>
        Task.FromException<ProviderSessionState>(new ProviderException(ProviderFailureKind.DeviceLoginUnavailable));

    public Task<ProviderSessionState> ConnectWithChallengeAsync(Action<AuthorizationChallenge> authorize, CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            var next = await auth.LoginAsync(authorize, token).ConfigureAwait(false);
            if (stored is not null && next.AccountId != stored.AccountId)
                throw new ProviderException(ProviderFailureKind.AccountMismatch);
            QuotaSnapshot? reading = null;
            ProviderFailureKind? failure = null;
            try { reading = await quota.GetQuotaAsync(next, token).ConfigureAwait(false); }
            catch (ProviderException error) when (stored is null && error.Kind is not (ProviderFailureKind.AuthenticationRequired or ProviderFailureKind.AccountMismatch))
            { failure = error.Kind; }
            // A replacement must prove quota access; failure never replaces the previous connection.
            token.ThrowIfCancellationRequested();
            stored = await lease.SaveAsync(new()
            {
                AccountId = next.AccountId, AccessToken = next.AccessToken, ExpiresAt = next.ExpiresAt, CachedQuota = reading
            }, stored?.Revision, CancellationToken.None).ConfigureAwait(false);
            HasStoredGrant = true;
            return reading is not null ? Available(reading) : Cached(failure);
        }, cancellationToken);

    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        await lease.DeleteAsync(token).ConfigureAwait(false);
        stored = null; HasStoredGrant = false;
        return ProviderSessionState.NotConnected;
    }, cancellationToken, load: false);

    private static ProviderSessionState Available(QuotaSnapshot quota) => new(ProviderSessionStatus.QuotaAvailable, quota, RetrievedAt: quota.FetchedAt);
    private ProviderSessionState Cached(ProviderFailureKind? failure = null)
    {
        var needsLogin = stored?.NeedsReauthentication == true || stored?.ExpiresAt <= clock.GetUtcNow();
        return new(needsLogin ? ProviderSessionStatus.ReauthenticationRequired : stored is null ? ProviderSessionStatus.NotConnected : ProviderSessionStatus.QuotaUnavailable,
            stored?.CachedQuota, failure ?? (needsLogin ? ProviderFailureKind.AuthenticationRequired : null),
            stored?.CachedQuota?.FetchedAt, FromCache: stored?.CachedQuota is not null);
    }
    private Task<ProviderSessionState> RunAsync(Func<ProviderStateLease<CopilotStoredState>, CancellationToken, Task<ProviderSessionState>> operation,
        CancellationToken token, bool load = true) => Task.Run(async () =>
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!await gate.WaitAsync(0, token).ConfigureAwait(false)) throw new InvalidOperationException("Copilot work is already in progress.");
        try
        {
            State = ProviderSessionState.Working;
            await using var lease = await store.AcquireAsync(token).ConfigureAwait(false);
            if (load)
            {
                stored = await lease.LoadAsync(token).ConfigureAwait(false);
                HasStoredGrant = stored is not null;
            }
            return State = await operation(lease, token).ConfigureAwait(false);
        }
        catch (ProviderException error)
        {
            if (error.Kind is ProviderFailureKind.StorageUnavailable or ProviderFailureKind.RecoveryRequired or ProviderFailureKind.GrantNotRemoved)
            {
                stored = null; HasStoredGrant = true;
                return State = new(ProviderSessionStatus.RecoveryRequired, Failure: error.Kind);
            }
            return State = Cached(error.Kind);
        }
        catch (OperationCanceledException) { State = Cached(); throw; }
        finally { gate.Release(); }
    }, CancellationToken.None);
    public void Dispose()
    {
        if (disposed) return;
        if (!gate.Wait(0)) throw new InvalidOperationException("Drain Copilot work before disposing it.");
        disposed = true; stored = null;
        gate.Release(); gate.Dispose();
    }
}
