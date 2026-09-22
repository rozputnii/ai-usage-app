using AiUsage.Core.Usage;

namespace AiUsage.Core.Dashboard;

/// <summary>
/// Coordinates the single dashboard's work without owning credentials or desktop objects.
/// StopAsync must finish before the session is disposed. It never retries a provider operation.
/// </summary>
public sealed class DashboardWorkflow(IProviderSession session) : IDisposable
{
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
        }, cancellationToken);

    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openBrowser, CancellationToken cancellationToken = default) =>
        RunAsync(token => session.ConnectAsync(openBrowser, token), cancellationToken);

    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) =>
        RunAsync(session.RefreshAsync, cancellationToken);

    public Task<ProviderSessionState> ConnectWithChallengeAsync(Action<AuthorizationChallenge> authorize, CancellationToken cancellationToken = default) =>
        RunAsync(token => session.ConnectWithChallengeAsync(authorize, token), cancellationToken);

    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) =>
        RunAsync(session.DisconnectAsync, cancellationToken);

    public bool TrySubmitCode(string code)
    {
        lock (sync)
            return !stopped && session.TrySubmitCode(code);
    }

    private Task<ProviderSessionState> RunAsync(Func<CancellationToken, Task<ProviderSessionState>> operation, CancellationToken token)
    {
        lock (sync)
        {
            if (stopped)
                return Task.FromCanceled<ProviderSessionState>(new CancellationToken(true));
            if (active is { IsCompleted: false })
                return Task.FromException<ProviderSessionState>(new InvalidOperationException("Dashboard work is already in progress."));
            // The lifetime token is read here, not after the yield below: disposal may release the
            // source while the operation is still starting, and a cancelled token stays usable.
            return active = ExecuteAsync(operation, lifetime.Token, token);
        }
    }

    private static async Task<ProviderSessionState> ExecuteAsync(Func<CancellationToken, Task<ProviderSessionState>> operation,
        CancellationToken lifetimeToken, CancellationToken token)
    {
        // Publish active before a browser callback or a reentrant shutdown can run.
        await Task.Yield();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken, token);
        linked.Token.ThrowIfCancellationRequested();
        return await operation(linked.Token).ConfigureAwait(false);
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
