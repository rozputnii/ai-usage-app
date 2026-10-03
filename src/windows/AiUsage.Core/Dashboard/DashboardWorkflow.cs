using AiUsage.Core.Usage;
using AiUsage.Core.Diagnostics;

namespace AiUsage.Core.Dashboard;

/// <summary>
/// Coordinates the single dashboard's work without owning credentials or desktop objects.
/// StopAsync must finish before the session is disposed. It never retries a provider operation.
/// </summary>
public sealed class DashboardWorkflow(IProviderSession session, IDiagnosticSink? diagnostics = null) : IDisposable
{
    private readonly Guid accountReference = Guid.NewGuid();
    private readonly object sync = new();
    private readonly CancellationTokenSource lifetime = new();
    private Task<ProviderSessionState>? active;
    private Task? stopping;
    private bool stopped;
    private bool disposed;

    public bool Connected => session.HasStoredGrant;
    public ProviderSessionState State => session.State;

    public Task<ProviderSessionState> LoadAsync(Func<ProviderSessionState, Task> showCached, CancellationToken cancellationToken = default) =>
        RunAsync(async token =>
        {
            var cached = await session.ReadCachedStateAsync(token).ConfigureAwait(false);
            if (!Connected)
                return cached;
            await showCached(cached).ConfigureAwait(false);
            return await session.ResumeAsync(token).ConfigureAwait(false);
        }, DiagnosticOperation.Resume, cancellationToken);

    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openBrowser, CancellationToken cancellationToken = default) =>
        RunAsync(token => session.ConnectAsync(openBrowser, token), DiagnosticOperation.Connect, cancellationToken);

    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) =>
        RunAsync(session.RefreshAsync, DiagnosticOperation.Refresh, cancellationToken);

    public Task<ProviderSessionState> ConnectWithChallengeAsync(Action<AuthorizationChallenge> authorize, CancellationToken cancellationToken = default) =>
        RunAsync(token => session.ConnectWithChallengeAsync(authorize, token), DiagnosticOperation.Connect, cancellationToken);

    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) =>
        RunAsync(session.DisconnectAsync, DiagnosticOperation.Disconnect, cancellationToken);

    public bool TrySubmitCode(string code)
    {
        lock (sync)
            return !stopped && session.TrySubmitCode(code);
    }

    private Task<ProviderSessionState> RunAsync(Func<CancellationToken, Task<ProviderSessionState>> operation, DiagnosticOperation kind, CancellationToken token)
    {
        lock (sync)
        {
            if (stopped)
                return Task.FromCanceled<ProviderSessionState>(new CancellationToken(true));
            if (active is { IsCompleted: false })
                return Task.FromException<ProviderSessionState>(new InvalidOperationException("Dashboard work is already in progress."));
            // The lifetime token is read here, not after the yield below: disposal may release the
            // source while the operation is still starting, and a cancelled token stays usable.
            return active = ExecuteAsync(operation, kind, lifetime.Token, token);
        }
    }

    private async Task<ProviderSessionState> ExecuteAsync(Func<CancellationToken, Task<ProviderSessionState>> operation,
        DiagnosticOperation kind, CancellationToken lifetimeToken, CancellationToken token)
    {
        // Publish active before a browser callback or a reentrant shutdown can run.
        await Task.Yield();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken, token);
        using var diagnostic = diagnostics?.Begin(kind, accountReference);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            var result = await operation(linked.Token).ConfigureAwait(false);
            diagnostic?.SetOutcome(result.Status == ProviderSessionStatus.ReauthenticationRequired ? DiagnosticOutcome.Reauthentication :
                result.Status == ProviderSessionStatus.RecoveryRequired ? DiagnosticOutcome.RecoveryRequired :
                result.Failure is not null ? DiagnosticOutcome.Failed : result.FromCache ? DiagnosticOutcome.StaleFallback : DiagnosticOutcome.Completed);
            return result;
        }
        catch (OperationCanceledException)
        {
            diagnostic?.SetOutcome(lifetimeToken.IsCancellationRequested ? DiagnosticOutcome.ExitCancelled : DiagnosticOutcome.Cancelled);
            throw;
        }
        catch (Exception exception)
        {
            diagnostic?.SetOutcome(DiagnosticOutcome.Failed);
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, exception);
            throw;
        }
    }

    public Task StopAsync()
    {
        lock (sync)
        {
            stopped = true;
            return stopping ??= DrainAsync(active);
        }
    }

    private async Task DrainAsync(Task? pending)
    {
        await Task.Yield();
        // Disposal cancels before it releases the source, so an already-released lifetime is cancelled.
        try { await lifetime.CancelAsync().ConfigureAwait(false); }
        catch (ObjectDisposedException) { }
        if (pending is null)
            return;
        try { await pending.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        // Other faults remain observable to the desktop shutdown owner.
    }

    /// <summary>
    /// Releases the workflow. Callers are expected to await <see cref="StopAsync"/> first, so that
    /// outstanding work is drained; that remains a precondition, not an enforced one. Disposal is
    /// idempotent and never throws, because a shutdown path that throws here would skip the
    /// remaining cleanup of whoever owns this workflow. Work still running is cancelled and
    /// abandoned rather than awaited.
    /// </summary>
    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
                return;
            disposed = true;
            stopped = true;
        }
        // Cancellation callbacks run outside the lock: they can re-enter this workflow.
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
