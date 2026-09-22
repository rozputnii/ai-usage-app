using AiUsage.Adapters.Live;
using AiUsage.Core.History;
using AiUsage.Core.Usage;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class LiveHistoryTests
{
    private sealed class Session : IProviderSession, IProviderHistorySession
    {
        public bool HasStoredGrant { get; private set; } = true;
        public ProviderSessionState State { get; private set; } = new(ProviderSessionStatus.QuotaUnavailable);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Reading;
        public bool Disconnected;
        public int Calls;
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            Assert.False(Reading); Disconnected = true; HasStoredGrant = false;
            return Task.FromResult(State = ProviderSessionState.NotConnected);
        }
        public async Task<ProviderHistoryResult> GetHistoryAsync(HistoryRange range, CancellationToken cancellationToken)
        {
            Calls++; Reading = true; Started.TrySetResult();
            try
            {
                await Release.Task.WaitAsync(cancellationToken);
                return new(range, DateTimeOffset.UtcNow, [new("usage", HistoryStatus.Empty, [])]);
            }
            finally { Reading = false; }
        }
    }
    private static readonly HistoryRange Range = new(new(2026, 9, 1), new(2026, 9, 20));

    [Fact]
    public async Task ConcurrentSameRangeCoalescesAndDisconnectCancelsBeforeRemovingGrant()
    {
        var session = new Session();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        await source.InitializeAsync();
        var first = source.GetHistoryAsync("codex", Range, TestContext.Current.CancellationToken);
        await session.Started.Task;
        var second = source.GetHistoryAsync("codex", Range, TestContext.Current.CancellationToken);
        Assert.Equal(1, session.Calls);
        var disconnect = await source.ExecuteAsync(new(UiCommandKind.Disconnect, "codex", null, source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.Equal(CommandStatus.Succeeded, disconnect.Status);
        Assert.True(session.Disconnected);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        await source.StopAsync();
    }

    [Fact]
    public async Task ExitDrainsHistoryAndRejectsNewWork()
    {
        var session = new Session();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        await source.InitializeAsync();
        var pending = source.GetHistoryAsync("codex", Range, TestContext.Current.CancellationToken);
        await session.Started.Task;
        await source.StopAsync();
        Assert.False(session.Reading);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetHistoryAsync("codex", Range, TestContext.Current.CancellationToken));
    }
}
