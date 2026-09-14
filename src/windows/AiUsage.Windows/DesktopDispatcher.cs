using Microsoft.UI.Dispatching;

namespace AiUsage;

/// <summary>Awaitable UI dispatch; shutdown drains callers before the window is closed.</summary>
internal sealed class DesktopDispatcher(DispatcherQueue queue)
{
    public Task InvokeAsync(Action action)
    {
        if (queue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(() =>
        {
            try { action(); completion.SetResult(); }
            catch (Exception error) { completion.SetException(error); }
        }))
            completion.SetException(new InvalidOperationException("The desktop dispatcher is no longer available."));
        return completion.Task;
    }
}
