using AiUsage.Core.Dashboard;
using AiUsage.Core.Providers.Codex;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class DashboardWorkflowTests
{
    [Fact]
    public async Task FaultedWorkRemainsObservableAndCanBeDisposedAfterDrain()
    {
        var session = new FakeSession { Operation = _ => throw new IOException("Synthetic failure") };
        using var workflow = new DashboardWorkflow(session);
        await Assert.ThrowsAsync<IOException>(() => workflow.RefreshAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => workflow.StopAsync());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => workflow.RefreshAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FirstRunDoesNotResumeOrReadCache()
    {
        var session = new FakeSession();
        using var workflow = new DashboardWorkflow(session);
        await workflow.LoadAsync(_ => throw new InvalidOperationException("No cached state expected."), TestContext.Current.CancellationToken);
        Assert.Equal(0, session.Calls);
    }

    [Fact]
    public async Task CachedStateIsDeliveredBeforeResume()
    {
        var session = new FakeSession { HasStoredGrant = true };
        using var workflow = new DashboardWorkflow(session);
        var cachedShown = false;
        session.Operation = _ =>
        {
            Assert.True(cachedShown);
            return Task.FromResult(CodexSessionState.NotConnected);
        };
        await workflow.LoadAsync(state => { cachedShown = state.FromCache; return Task.CompletedTask; }, TestContext.Current.CancellationToken);
        Assert.True(cachedShown);
        Assert.Equal(1, session.Calls);
    }

    [Fact]
    public async Task StopCancelsAndDrainsWorkAndRejectsLaterCommands()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeSession
        {
            Operation = async token =>
            {
                using var registration = token.Register(() => cancelled.SetResult());
                started.SetResult();
                await release.Task;
                token.ThrowIfCancellationRequested();
                return CodexSessionState.NotConnected;
            }
        };
        using var workflow = new DashboardWorkflow(session);
        var running = workflow.RefreshAsync(TestContext.Current.CancellationToken);
        await started.Task;
        var stopping = workflow.StopAsync();
        await cancelled.Task;
        Assert.False(stopping.IsCompleted);
        release.SetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
        await stopping;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => workflow.RefreshAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, session.Calls);
    }

    [Fact]
    public async Task OverlappingCommandsDoNotStartAnotherSessionOperation()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<CodexSessionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeSession { Operation = _ => { started.SetResult(); return release.Task; } };
        using var workflow = new DashboardWorkflow(session);
        var running = workflow.RefreshAsync(TestContext.Current.CancellationToken);
        await started.Task;
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.DisconnectAsync(TestContext.Current.CancellationToken));
        release.SetResult(CodexSessionState.NotConnected);
        await running;
        Assert.Equal(1, session.Calls);
    }
}

internal sealed class FakeSession : ICodexSession
{
    public bool HasStoredGrant { get; set; }
    public CodexSessionState State { get; set; } = CodexSessionState.NotConnected;
    public int Calls { get; private set; }
    public Func<CancellationToken, Task<CodexSessionState>> Operation { get; set; } = _ => Task.FromResult(CodexSessionState.NotConnected);
    public CodexSessionState ReadCachedState() => new(CodexSessionStatus.QuotaUnavailable, FromCache: true);
    public Task<CodexSessionState> ResumeAsync(CancellationToken cancellationToken = default) => Run(cancellationToken);
    public Task<CodexSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Run(cancellationToken);
    public Task<CodexSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => Run(cancellationToken);
    public Task<CodexSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => Run(cancellationToken);
    private Task<CodexSessionState> Run(CancellationToken token) { Calls++; return Operation(token); }
}
