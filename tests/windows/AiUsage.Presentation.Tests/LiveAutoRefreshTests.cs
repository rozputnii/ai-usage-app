using AiUsage.Adapters.Live;
using AiUsage.Core.Usage;
using AiUsage.Features.Presentation;
using Xunit;
using ManualTime = AiUsage.Presentation.Tests.LiveClockTests.ManualTime;

namespace AiUsage.Presentation.Tests;

public sealed class LiveAutoRefreshTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 25, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ConnectedAccountIsReadAgainOnlyOnceItsReadingIsFiveMinutesOld()
    {
        var (time, session, source, auto) = await StartAsync();
        using var _ = source;
        time.Now = Start + TimeSpan.FromMinutes(4);
        await auto.RunOnceAsync();
        Assert.Equal(0, session.Refreshes);

        time.Now = Start + TimeSpan.FromMinutes(5);
        await auto.RunOnceAsync();
        await auto.RunOnceAsync();
        Assert.Equal(1, session.Refreshes);
        var account = source.Current.Accounts.Single();
        Assert.Equal(time.Now, account.FetchedAt);
        Assert.Equal(Freshness.Fresh, account.Freshness);
        Assert.Equal(AccountOperation.Idle, account.Operation);
        Assert.Null(source.Current.LastRefreshAll); // Background work never reports as a Refresh all.
        await source.StopAsync();
    }

    [Fact]
    public async Task ManualReadingPostponesTheNextAutomaticOne()
    {
        var (time, session, source, auto) = await StartAsync();
        using var _ = source;
        time.Now = Start + TimeSpan.FromMinutes(3);
        await source.ExecuteAsync(new(UiCommandKind.RefreshAccount, "codex", null, source.Current.Revision), TestContext.Current.CancellationToken);
        time.Now = Start + TimeSpan.FromMinutes(6);
        await auto.RunOnceAsync();
        Assert.Equal(1, session.Refreshes);
        time.Now = Start + TimeSpan.FromMinutes(8);
        await auto.RunOnceAsync();
        Assert.Equal(2, session.Refreshes);
        await source.StopAsync();
    }

    [Fact]
    public async Task FailedAttemptsBackOffAndASuccessRestoresTheNormalInterval()
    {
        var (time, session, source, auto) = await StartAsync();
        using var _ = source;
        session.NextFailure = ProviderFailureKind.NetworkFailure;
        async Task<int> At(int minutes)
        {
            time.Now = Start + TimeSpan.FromMinutes(minutes);
            await auto.RunOnceAsync();
            return session.Refreshes;
        }

        Assert.Equal(1, await At(5));
        Assert.Equal(Freshness.Stale, source.Current.Accounts.Single().Freshness);
        Assert.Equal(1, await At(14)); // 10 minutes after one failure.
        Assert.Equal(2, await At(15));
        Assert.Equal(2, await At(34)); // 20 minutes after two.
        Assert.Equal(3, await At(35));
        Assert.Equal(3, await At(64)); // Capped at 30 minutes.
        session.NextFailure = null;
        Assert.Equal(4, await At(65));
        Assert.Equal(Freshness.Fresh, source.Current.Accounts.Single().Freshness);
        Assert.Equal(4, await At(69));
        Assert.Equal(5, await At(70));
        await source.StopAsync();
    }

    [Theory]
    [InlineData(ProviderFailureKind.AuthenticationRequired)]
    [InlineData(ProviderFailureKind.InternalError)]
    public async Task SignInAndUnrecoverableFailuresAreLeftToTheUser(ProviderFailureKind failure)
    {
        var (time, session, source, auto) = await StartAsync();
        using var _ = source;
        session.NextFailure = failure;
        time.Now = Start + TimeSpan.FromMinutes(5);
        await auto.RunOnceAsync();
        Assert.Equal(1, session.Refreshes);
        time.Now = Start + TimeSpan.FromHours(3);
        await auto.RunOnceAsync();
        Assert.Equal(1, session.Refreshes);
        await source.StopAsync();
    }

    [Fact]
    public async Task UnrenewedReadingTurnsStaleUntilTheNextReadingArrives()
    {
        var (time, session, source, auto) = await StartAsync();
        using var _ = source;
        source.ExpireReadings(Start + TimeSpan.FromMinutes(14), LiveAutoRefresh.StaleAfter);
        Assert.Equal(Freshness.Fresh, source.Current.Accounts.Single().Freshness);

        // For example the first tick after the PC resumes from sleep.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Gate = gate;
        time.Now = Start + TimeSpan.FromHours(2);
        var pass = auto.RunOnceAsync();
        await session.Started.Task;
        Assert.Equal(Freshness.Stale, source.Current.Accounts.Single().Freshness);
        gate.SetResult();
        await pass;
        Assert.Equal(Freshness.Fresh, source.Current.Accounts.Single().Freshness);
        await source.StopAsync();
    }

    [Fact]
    public async Task UserOperationDuringBackgroundRefreshRunsAfterItInsteadOfConflicting()
    {
        var (time, session, source, auto) = await StartAsync();
        using var _ = source;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Gate = gate;
        time.Now = Start + TimeSpan.FromMinutes(5);
        var pass = auto.RunOnceAsync();
        await session.Started.Task;
        var all = source.ExecuteAsync(new(UiCommandKind.RefreshAll, null, null, source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.False(all.IsCompleted);
        gate.SetResult();
        await pass;
        Assert.Equal(CommandStatus.Succeeded, (await all).Status);
        Assert.Equal(2, session.Refreshes);
        Assert.Equal(1, source.Current.LastRefreshAll?.Updated);
        await source.StopAsync();
    }

    [Fact]
    public async Task TimerRunsEveryMinuteOnlyBetweenStartAndDispose()
    {
        var time = new ManualTime { Now = Start };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession>());
        var auto = new LiveAutoRefresh(source, time);
        Assert.Null(time.Timer);
        auto.Start();
        Assert.NotNull(time.Timer);
        Assert.Equal(LiveAutoRefresh.Tick, time.Timer.DueTime);
        time.Timer.Fire();
        auto.Dispose();
        Assert.True(time.Timer.Disposed);
        var retired = time.Timer;
        auto.Start();
        Assert.Same(retired, time.Timer);
        await source.StopAsync();
    }

    private static async Task<(ManualTime, QuotaSession, LiveUsageSource, LiveAutoRefresh)> StartAsync()
    {
        var time = new ManualTime { Now = Start };
        var session = new QuotaSession(time);
        var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        await source.InitializeAsync();
        Assert.Equal(Freshness.Fresh, source.Current.Accounts.Single().Freshness);
        return (time, session, source, new LiveAutoRefresh(source, time));
    }

    private sealed class QuotaSession(ManualTime time) : IProviderSession
    {
        public bool HasStoredGrant => true;
        public ProviderSessionState State { get; private set; } = new(ProviderSessionStatus.QuotaUnavailable);
        public int Refreshes { get; private set; }
        public ProviderFailureKind? NextFailure { get; set; }
        public TaskCompletionSource? Gate { get; set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => Task.FromResult(Read());
        public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => Task.FromResult(State);
        public async Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default)
        {
            Refreshes++;
            Started.TrySetResult();
            if (Gate is { } gate)
            {
                Gate = null;
                await gate.Task.WaitAsync(cancellationToken);
            }
            return Read();
        }
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);

        private ProviderSessionState Read() => State = NextFailure switch
        {
            ProviderFailureKind.AuthenticationRequired => new(ProviderSessionStatus.ReauthenticationRequired, State.Quota, NextFailure, State.RetrievedAt, FromCache: true),
            { } failure => new(ProviderSessionStatus.QuotaUnavailable, State.Quota, failure, State.RetrievedAt, FromCache: true),
            null => new(ProviderSessionStatus.QuotaAvailable, new QuotaSnapshot(time.Now, null,
                [new("codex", "Codex", null, null, true, false, [new("primary", 40, 60, TimeSpan.FromDays(7), time.Now.AddDays(3))])],
                null, null, null, null), RetrievedAt: time.Now),
        };
    }
}
