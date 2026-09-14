using AiUsage.Core.Providers.Codex;

namespace AiUsage.Core.Dashboard;

/// <summary>
/// Coordinates the single dashboard's work without owning credentials or desktop objects.
/// StopAsync must finish before the session is disposed. It never retries a provider operation.
/// </summary>
public sealed class DashboardWorkflow(ICodexSession session) : IDisposable
{
    private readonly object sync = new();
    private readonly CancellationTokenSource lifetime = new();
    private Task<CodexSessionState>? active;
    private Task? stopping;
    private bool stopped;

    public bool Connected => session.HasStoredGrant;
    public CodexSessionState State => session.State;

    public Task<CodexSessionState> LoadAsync(Func<CodexSessionState, Task> showCached, CancellationToken cancellationToken = default) =>
        RunAsync(async token =>
        {
            if (!Connected)
                return CodexSessionState.NotConnected;
            await showCached(session.ReadCachedState()).ConfigureAwait(false);
            return await session.ResumeAsync(token).ConfigureAwait(false);
        }, cancellationToken);

    public Task<CodexSessionState> ConnectAsync(Action<Uri> openBrowser, CancellationToken cancellationToken = default) =>
        RunAsync(token => session.ConnectAsync(openBrowser, token), cancellationToken);

    public Task<CodexSessionState> RefreshAsync(CancellationToken cancellationToken = default) =>
        RunAsync(session.RefreshAsync, cancellationToken);

    public Task<CodexSessionState> DisconnectAsync(CancellationToken cancellationToken = default) =>
        RunAsync(session.DisconnectAsync, cancellationToken);

    private Task<CodexSessionState> RunAsync(Func<CancellationToken, Task<CodexSessionState>> operation, CancellationToken token)
    {
        lock (sync)
        {
            if (stopped)
                return Task.FromCanceled<CodexSessionState>(new CancellationToken(true));
            if (active is { IsCompleted: false })
                return Task.FromException<CodexSessionState>(new InvalidOperationException("Dashboard work is already in progress."));
            return active = ExecuteAsync(operation, token);
        }
    }

    private async Task<CodexSessionState> ExecuteAsync(Func<CancellationToken, Task<CodexSessionState>> operation, CancellationToken token)
    {
        // Publish active before a browser callback or a reentrant shutdown can run.
        await Task.Yield();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, token);
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
        await lifetime.CancelAsync().ConfigureAwait(false);
        if (pending is null)
            return;
        try { await pending.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        // Other faults remain observable to the desktop shutdown owner.
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (active is { IsCompleted: false })
                throw new InvalidOperationException("Await StopAsync before disposing dashboard work.");
            stopped = true;
            lifetime.Dispose();
        }
    }
}
