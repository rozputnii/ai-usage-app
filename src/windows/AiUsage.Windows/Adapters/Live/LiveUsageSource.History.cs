using AiUsage.Core.Dashboard;
using AiUsage.Core.History;
using AiUsage.Core.Usage;
using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

internal sealed partial class LiveUsageSource : IProviderHistorySource
{
    public Task<ProviderHistoryResult> GetHistoryAsync(string accountId, HistoryRange range, CancellationToken cancellationToken)
    {
        range.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (stopped) return Task.FromCanceled<ProviderHistoryResult>(new CancellationToken(true));
            if (maintenanceBlocked) return Task.FromResult(ProviderHistoryResult.Unavailable(range, HistoryStatus.Failed));
            if (!entries.TryGetValue(accountId, out var entry) || entry.Session is not IProviderHistorySession session)
                return Task.FromResult(ProviderHistoryResult.Unavailable(range, HistoryStatus.Unsupported));
            if (entry.Pending is { IsCompleted: false } pending)
            {
                if (entry.HistoryActive && entry.HistoryRange == range && entry.Cancellation?.IsCancellationRequested == false)
                    return entry.HistoryTask!.WaitAsync(cancellationToken);
                return AfterPendingAsync(pending, accountId, range, cancellationToken);
            }
            if (!entry.Session.HasStoredGrant) return Task.FromResult(ProviderHistoryResult.Unavailable(range, HistoryStatus.AuthenticationRequired));
            entry.HistoryActive = true;
            entry.HistoryRange = range;
            var completion = new TaskCompletionSource<ProviderHistoryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            entry.HistoryTask = completion.Task;
            entry.Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            entry.Pending = ExecuteHistoryAsync(accountId, entry, session, range, completion, entry.Cancellation);
            return completion.Task;
        }
    }

    private async Task<ProviderHistoryResult> AfterPendingAsync(Task pending, string id, HistoryRange range, CancellationToken token)
    {
        await pending.WaitAsync(token).ConfigureAwait(false);
        return await GetHistoryAsync(id, range, token).ConfigureAwait(false);
    }

    private async Task<UiCommandResult> RunAfterHistoryAsync(Task pending, string id, AccountOperation operation,
        Func<DashboardWorkflow, CancellationToken, Task<ProviderSessionState>> run, CancellationToken token)
    {
        await pending.WaitAsync(token).ConfigureAwait(false);
        return await RunAsync(id, operation, run, token).ConfigureAwait(false);
    }

    private async Task<UiCommandResult> ExecuteHistoryAsync(string accountId, Entry entry, IProviderHistorySession session, HistoryRange range,
        TaskCompletionSource<ProviderHistoryResult> completion, CancellationTokenSource cancellation)
    {
        await Task.Yield();
        try
        {
            var previous = entry.Session.State;
            var result = await session.GetHistoryAsync(range, cancellation.Token).ConfigureAwait(false);
            cancellation.Token.ThrowIfCancellationRequested();
            if (entry.Session.State != previous) Update(accountId, entry.Session.State);
            completion.TrySetResult(result);
        }
        catch (OperationCanceledException) { completion.TrySetCanceled(); }
        catch (Exception) { completion.TrySetResult(ProviderHistoryResult.Unavailable(range, HistoryStatus.Failed)); }
        finally
        {
            lock (sync) { entry.HistoryActive = false; entry.Cancellation = null; cancellation.Dispose(); }
        }
        return UiCommandResult.Succeeded;
    }
}
