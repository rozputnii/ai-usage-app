using AiUsage.Adapters.Live;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class LiveReadingRecorderTests
{
    [Fact]
    public async Task ResumeManualAndAutomaticRefreshRecordButCacheFailureAndSignOutDoNot()
    {
        var session = new Session();
        var recorder = new Recorder();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session }, recorder: recorder);
        await source.InitializeAsync();
        Assert.Single(recorder.Readings);
        Assert.Equal(CommandStatus.Succeeded, (await source.ExecuteAsync(new(UiCommandKind.RefreshAccount, "codex", null, source.Current.Revision), TestContext.Current.CancellationToken)).Status);
        Assert.Equal(CommandStatus.Succeeded, (await source.RefreshInBackgroundAsync("codex")).Status);
        session.Fail = true;
        Assert.Equal(CommandStatus.Failed, (await source.RefreshInBackgroundAsync("codex")).Status);
        await source.ExecuteAsync(new(UiCommandKind.Disconnect, "codex", null, source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.Equal(3, recorder.Readings.Count);
        Assert.All(recorder.Readings, r => Assert.False(r.FromCache));
        await source.StopAsync();
    }

    [Fact]
    public async Task ShutdownWaitsForPendingRecordingAndDoesNotStartMoreRefreshes()
    {
        var recorder = new Recorder { Block = true };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = new Session() }, recorder: recorder);
        var starting = source.InitializeAsync();
        await recorder.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var stopping = source.StopAsync();
        Assert.False(stopping.IsCompleted);
        recorder.Release.TrySetResult();
        await stopping;
        await starting;
        Assert.Equal(CommandStatus.Conflict, (await source.RefreshInBackgroundAsync("codex")).Status);
        Assert.Single(recorder.Readings);
    }

    [Fact]
    public async Task StorageFailureIsNotReportedAsSuccessfulRefreshOrAuthenticationFailure()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = new Session() }, recorder: new Recorder { Fail = true });
        await source.InitializeAsync();
        var account = Assert.Single(source.Current.Accounts);
        Assert.Equal("StorageUnavailable", account.Failure?.Kind);
        Assert.Equal(ConnectionState.Connected, account.Connection);
        await source.StopAsync();
    }

    private sealed class Recorder : IQuotaObservationRecorder
    {
        public List<ProviderSessionState> Readings { get; } = [];
        public bool Block { get; init; }
        public bool Fail { get; init; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task RecordAsync(string accountTarget, string provider, ProviderSessionState state, CancellationToken token)
        {
            Started.TrySetResult();
            if (Block) await Release.Task;
            if (Fail) throw new IOException("synthetic storage failure");
            Readings.Add(state);
        }
    }
    private sealed class Session : IProviderSession
    {
        public bool HasStoredGrant { get; private set; } = true;
        public bool Fail { get; set; }
        public ProviderSessionState State { get; private set; } = new(ProviderSessionStatus.QuotaAvailable,
            new(DateTimeOffset.UtcNow, "test", [], null, null, null, null), FromCache: true);
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);
        public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => RefreshAsync(cancellationToken);
        public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(State = State with
        { FromCache = Fail, Failure = Fail ? ProviderFailureKind.NetworkFailure : null });
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default)
        { HasStoredGrant = false; return Task.FromResult(State = ProviderSessionState.NotConnected); }
    }
}
