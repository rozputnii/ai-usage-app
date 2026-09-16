using AiUsage.Core.Providers.Copilot;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>
/// One app-owned GitHub connection. No source CLI credentials are read or imported, and
/// disconnect removes only local state: the GitHub authorization is revoked by the owner on GitHub.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CopilotSession(CopilotAuthClient auth, CopilotUsageClient usage, CopilotStateStore store) : IProviderSession, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private CopilotStoredState? stored;
    private CopilotCredentials? credentials;
    private string? pendingUserCode;
    private bool disposed;

    public bool HasStoredGrant { get; private set; }
    public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;
    public string? PendingUserCode => Volatile.Read(ref pendingUserCode);

    public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) =>
        RunAsync((_, _) => Task.FromResult(Cached()), cancellationToken);

    public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);

    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        if (stored is null)
            return ProviderSessionState.NotConnected;
        if (stored.NeedsReauthentication)
            return Cached(ProviderFailureKind.AuthenticationRequired);
        try
        {
            // Re-read identity once per loaded generation: the login used in report URLs can be renamed.
            if (credentials is null)
            {
                var verified = await auth.VerifyAsync(new CopilotCredentials(stored.AccessToken, stored.Identity, stored.GrantedScope), token).ConfigureAwait(false);
                if (verified.Identity != stored.Identity)
                    stored = await lease.SaveAsync(stored with { Identity = verified.Identity }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
                credentials = verified;
            }
            var reading = await ReadUsageAsync(credentials, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            stored = await lease.SaveAsync(stored with { CachedUsage = reading }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
            return Available(reading);
        }
        catch (CopilotException error) when (error.Kind == CopilotFailureKind.AuthenticationRequired)
        {
            credentials = null;
            stored = await lease.SaveAsync(stored with { NeedsReauthentication = true }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
            return Cached(ProviderFailureKind.AuthenticationRequired);
        }
    }, cancellationToken);

    /// <summary>
    /// Device flow: the user code is published through <see cref="PendingUserCode"/> before the
    /// verification page is opened. A reconnect proves usage access before replacing the previous grant.
    /// </summary>
    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        var attempt = await auth.BeginDeviceLoginAsync(token).ConfigureAwait(false);
        Volatile.Write(ref pendingUserCode, attempt.UserCode);
        try
        {
            try { openAuthorizationUrl(attempt.VerificationUri); }
            catch (InvalidOperationException) { throw new CopilotException(CopilotFailureKind.RequestRejected); }
            catch (System.ComponentModel.Win32Exception) { throw new CopilotException(CopilotFailureKind.RequestRejected); }
            var connected = await auth.CompleteDeviceLoginAsync(attempt, token).ConfigureAwait(false);
            Volatile.Write(ref pendingUserCode, null);
            var record = new CopilotStoredState { Identity = connected.Identity, AccessToken = connected.AccessToken, GrantedScope = connected.GrantedScope };
            // A different account must prove usage access before it replaces the stored one. The same
            // numeric account is already proven by /user, so a fresh token replaces its (possibly revoked)
            // predecessor even when GitHub provides no report for it.
            if (stored is not null && stored.Identity.AccountId != connected.Identity.AccountId)
            {
                var initial = await ReadUsageAsync(connected, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                stored = await lease.SaveAsync(record with { CachedUsage = initial }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
                credentials = connected;
                HasStoredGrant = true;
                return Available(initial);
            }
            // The verified token is persisted before a usage read can be canceled or fail. A same-account
            // reconnect keeps its last cache, which remains labeled by its own retrieval time.
            stored = await lease.SaveAsync(record with { CachedUsage = stored?.CachedUsage }, stored?.Revision, CancellationToken.None).ConfigureAwait(false);
            credentials = connected;
            HasStoredGrant = true;
            try
            {
                var reading = await ReadUsageAsync(connected, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                stored = await lease.SaveAsync(stored with { CachedUsage = reading }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
                return Available(reading);
            }
            catch (CopilotException error) when (error.Kind == CopilotFailureKind.AuthenticationRequired)
            {
                credentials = null;
                stored = await lease.SaveAsync(stored with { NeedsReauthentication = true }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
                return Cached(ProviderFailureKind.AuthenticationRequired);
            }
        }
        finally { Volatile.Write(ref pendingUserCode, null); }
    }, cancellationToken);

    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        await lease.DeleteAsync(token).ConfigureAwait(false);
        stored = null;
        credentials = null;
        HasStoredGrant = false;
        return ProviderSessionState.NotConnected;
    }, cancellationToken, load: false);

    /// <summary>
    /// Reads both documented reports. Only GitHub's explicit refusal (403/404) marks one report as
    /// absent without hiding the other; any other failure fails the whole read so a transient error
    /// cannot overwrite a good cached report. If neither report is available the first refusal is reported.
    /// </summary>
    private async Task<CopilotUsageReading> ReadUsageAsync(CopilotCredentials current, CancellationToken token)
    {
        var aiCredits = await ReadReportAsync(current, CopilotUsageReportKind.AiCredits, token).ConfigureAwait(false);
        var premium = await ReadReportAsync(current, CopilotUsageReportKind.PremiumRequests, token).ConfigureAwait(false);
        if (aiCredits.Report is null || premium.Report is null)
            credentials = null; // A renamed login also reads as absent; re-read identity next time.
        if (aiCredits.Report is null && premium.Report is null)
            throw aiCredits.Error!;
        return new(aiCredits.Report, premium.Report, (aiCredits.Report ?? premium.Report)!.ObservedAt);
    }

    private async Task<CopilotReportResult> ReadReportAsync(CopilotCredentials current, CopilotUsageReportKind kind, CancellationToken token)
    {
        try { return new(await usage.GetUsageAsync(current, kind, token).ConfigureAwait(false), null); }
        catch (CopilotException error) when (error.Kind is CopilotFailureKind.ReportUnavailable or CopilotFailureKind.AccessDenied)
        {
            return new(null, error);
        }
    }

    private sealed record CopilotReportResult(CopilotUsageReport? Report, CopilotException? Error);

    private static ProviderSessionState Available(CopilotUsageReading reading) =>
        new(ProviderSessionStatus.QuotaAvailable, RetrievedAt: reading.FetchedAt, CopilotUsage: reading);

    private ProviderSessionState Cached(ProviderFailureKind? failure = null)
    {
        var reading = stored?.CachedUsage;
        var status = stored?.NeedsReauthentication == true ? ProviderSessionStatus.ReauthenticationRequired
            : stored is null ? ProviderSessionStatus.NotConnected : ProviderSessionStatus.QuotaUnavailable;
        return new(status, Failure: failure, RetrievedAt: reading?.FetchedAt, FromCache: reading is not null, CopilotUsage: reading);
    }

    private Task<ProviderSessionState> RunAsync(Func<CopilotStateLease, CancellationToken, Task<ProviderSessionState>> operation,
        CancellationToken cancellationToken, bool load = true) => Task.Run(async () =>
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("GitHub Copilot work is already in progress.");
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
        catch (CopilotException error)
        {
            var failure = Enum.Parse<ProviderFailureKind>(error.Kind.ToString());
            if (error.Kind is CopilotFailureKind.StorageUnavailable or CopilotFailureKind.RecoveryRequired or CopilotFailureKind.GrantNotRemoved)
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
        if (disposed)
            return;
        if (!gate.Wait(0))
            throw new InvalidOperationException("Drain GitHub Copilot work before disposing it.");
        disposed = true;
        credentials = null;
        stored = null;
        gate.Release();
        gate.Dispose();
    }
}
