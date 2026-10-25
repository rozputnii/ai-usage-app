using AiUsage.Core.Diagnostics;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>
/// A Claude account read through the installed Claude Code binary, which keeps its own sign-in.
/// The app stores only who was signed in and the last reading; no credential exists here.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ClaudeCodeSession : IProviderSession, IDisposable
{
    internal static readonly string[] AuthArguments = ["auth", "status", "--json"];
    // `--safe-mode` skips the owner's hooks, plugins and MCP servers; the sign-in still works.
    internal static readonly string[] UsageArguments = ["-p", "/usage", "--output-format", "stream-json", "--verbose", "--no-session-persistence", "--safe-mode"];

    private readonly IClaudeCode claude;
    private readonly ClaudeCodeStateStore store;
    private readonly TimeProvider clock;
    private readonly IDiagnosticSink? diagnostics;
    private readonly SemaphoreSlim gate = new(1, 1);
    private ClaudeCodeStoredState? stored;
    private bool disposed;

    internal ClaudeCodeSession(IClaudeCode claude, ClaudeCodeStateStore store, TimeProvider clock, IDiagnosticSink? diagnostics = null)
    {
        this.claude = claude;
        this.store = store;
        this.clock = clock;
        this.diagnostics = diagnostics;
    }

    public bool HasStoredGrant { get; private set; }
    internal Accounts.ProviderIdentity? Identity => stored is null ? null : new(stored.Identity.Email, stored.Identity.OrganizationId);
    public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;

    public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) =>
        RunAsync((_, _) => Task.FromResult(Cached()), cancellationToken);

    public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);

    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) =>
        RunAsync(async (lease, token) =>
        {
            var account = await ReadAccountAsync(token).ConfigureAwait(false);
            var reading = await ReadUsageAsync(account, token).ConfigureAwait(false);
            stored = await lease.SaveAsync(new ClaudeCodeStoredState { Identity = account.Identity, CachedQuota = reading },
                stored?.Revision, CancellationToken.None).ConfigureAwait(false);
            HasStoredGrant = true;
            return Available(reading);
        }, cancellationToken);

    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        if (stored is null)
            return ProviderSessionState.NotConnected;
        var account = await ReadAccountAsync(token).ConfigureAwait(false);
        if (account.Identity != stored.Identity)
            return Cached(ProviderFailureKind.AccountMismatch);
        var reading = await ReadUsageAsync(account, token).ConfigureAwait(false);
        stored = await lease.SaveAsync(stored with { CachedQuota = reading }, stored.Revision, CancellationToken.None).ConfigureAwait(false);
        return Available(reading);
    }, cancellationToken);

    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => RunAsync(async (lease, token) =>
    {
        await lease.DeleteAsync(token).ConfigureAwait(false);
        stored = null;
        HasStoredGrant = false;
        return ProviderSessionState.NotConnected;
    }, cancellationToken, load: false);

    private async Task<ClaudeCodeAccount> ReadAccountAsync(CancellationToken token) =>
        ClaudeCodeAccount.Read(await claude.RunAsync(AuthArguments, token).ConfigureAwait(false));

    private async Task<ClaudeQuotaReading> ReadUsageAsync(ClaudeCodeAccount account, CancellationToken token)
    {
        var run = await claude.RunAsync(UsageArguments, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (run.ExitCode != 0)
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        if (ClaudeCodeReport.Read(run.Output, clock.GetUtcNow(), out var reading) is { } failure)
            throw new ProviderException(failure);
        return reading! with { Quota = reading!.Quota with { PlanType = account.Plan } };
    }

    private static ProviderSessionState Available(ClaudeQuotaReading reading) =>
        new(ProviderSessionStatus.QuotaAvailable, reading.Quota, RetrievedAt: reading.Quota.FetchedAt, ExtraUsage: reading.ExtraUsage);

    private ProviderSessionState Cached(ProviderFailureKind? failure = null)
    {
        var reading = stored?.CachedQuota;
        var status = stored is null ? ProviderSessionStatus.NotConnected : ProviderSessionStatus.QuotaUnavailable;
        return new(status, reading?.Quota, failure, reading?.Quota.FetchedAt, FromCache: reading is not null, ExtraUsage: reading?.ExtraUsage);
    }

    private Task<ProviderSessionState> RunAsync(Func<ProviderStateLease<ClaudeCodeStoredState>, CancellationToken, Task<ProviderSessionState>> operation,
        CancellationToken cancellationToken, bool load = true) => Task.Run(async () =>
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!await gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Claude Code work is already in progress.");
        try
        {
            State = ProviderSessionState.Working;
            await using var lease = await store.AcquireAsync(cancellationToken).ConfigureAwait(false);
            if (load)
            {
                stored = await lease.LoadAsync(cancellationToken).ConfigureAwait(false);
                HasStoredGrant = stored is not null;
            }
            return State = await operation(lease, cancellationToken).ConfigureAwait(false);
        }
        catch (ProviderException error)
        {
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
            if (error.Kind is ProviderFailureKind.StorageUnavailable or ProviderFailureKind.RecoveryRequired or ProviderFailureKind.GrantNotRemoved)
            {
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
            throw new InvalidOperationException("Drain Claude Code work before disposing it.");
        disposed = true;
        stored = null;
        gate.Release();
        gate.Dispose();
    }
}
