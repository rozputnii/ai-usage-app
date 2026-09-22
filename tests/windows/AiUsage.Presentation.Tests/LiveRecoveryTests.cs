using AiUsage.Adapters.Live;
using AiUsage.Core.Persistence;
using AiUsage.Core.Usage;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class LiveRecoveryTests
{
    [Fact]
    public async Task InterruptedStartupBlocksProvidersAndPreferencesUntilExplicitRetry()
    {
        var session = new FakeSession { HasStoredGrant = true };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        var reads = 0;
        var preferences = new LivePreferenceStore(source, _ => { reads++; return Task.FromResult<string?>(null); }, (_, _) => Task.CompletedTask);
        var maintenance = new Maintenance();
        var recovery = new LiveRecoveryService(maintenance, source, "owned data", () => Task.CompletedTask);
        var lifecycle = new ProductLifecycle(source, preferences, recovery);
        await lifecycle.InitializeAsync();
        Assert.Equal(0, reads);
        Assert.Equal(0, session.Calls);
        Assert.Equal(RecoveryState.Interrupted, source.Current.System.Recovery);
        Assert.Equal(CommandStatus.Failed, (await source.ConnectAsync("codex", _ => { }, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(CommandStatus.Failed, (await preferences.ResetSettingsAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(CommandStatus.Succeeded, (await recovery.RetryAsync(new Progress<double>(), TestContext.Current.CancellationToken)).Status);
        Assert.Equal(1, reads);
        Assert.Equal(1, session.Calls);
        Assert.Equal(RecoveryState.None, source.Current.System.Recovery);
        // Recovery actions cannot rewrite durable state once normal services have started.
        Assert.Equal(CommandStatus.Unsupported, (await recovery.RestoreCheckpointAsync("last-good", new Progress<double>(), TestContext.Current.CancellationToken)).Status);
        Assert.Equal(0, maintenance.Restores);
        await lifecycle.StopAsync();
    }

    [Fact]
    public async Task ExitWaitsForMaintenanceAndNeverStartsProvidersAfterStop()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<MaintenanceReport>(TaskCreationOptions.RunContinuationsAsynchronously);
        var maintenance = new Maintenance { Initialize = () => { started.SetResult(); return release.Task; } };
        var session = new FakeSession { HasStoredGrant = true };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        var reads = 0;
        var preferences = new LivePreferenceStore(source, _ => { reads++; return Task.FromResult<string?>(null); }, (_, _) => Task.CompletedTask);
        var recovery = new LiveRecoveryService(maintenance, source, "owned data", () => Task.CompletedTask);
        var lifecycle = new ProductLifecycle(source, preferences, recovery);
        var initialization = lifecycle.InitializeAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        var stopping = lifecycle.StopAsync();
        Assert.False(stopping.IsCompleted);
        release.SetResult(new(MaintenanceCondition.Ready, 1, null));
        await stopping;
        await initialization;
        Assert.Equal(0, reads);
        Assert.Equal(0, session.Calls);
    }

    [Fact]
    public async Task NewerSchemaOffersNoCheckpointAndNeverRunsRestore()
    {
        var maintenance = new Maintenance { Initialize = () => Task.FromResult(new MaintenanceReport(MaintenanceCondition.NewerSchema, 9, null)) };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession>());
        var recovery = new LiveRecoveryService(maintenance, source, "owned data", () => Task.CompletedTask);
        var preferences = new LivePreferenceStore(source, _ => Task.FromResult<string?>(null), (_, _) => Task.CompletedTask);
        await new ProductLifecycle(source, preferences, recovery).InitializeAsync();
        Assert.Equal(RecoveryState.NewerSchema, source.Current.System.Recovery);
        Assert.Empty(await recovery.ListCheckpointsAsync(TestContext.Current.CancellationToken));
        Assert.Equal(CommandStatus.Unsupported, (await recovery.RestoreCheckpointAsync("last-good", new Progress<double>(), TestContext.Current.CancellationToken)).Status);
        Assert.Equal(0, maintenance.Restores);
    }

    private sealed class Maintenance : IStateMaintenance
    {
        public MaintenanceReport Current { get; private set; } = new(MaintenanceCondition.Interrupted, 0, new("last-good", DateTimeOffset.UtcNow, 0));
        public Func<Task<MaintenanceReport>>? Initialize { get; init; }
        public int Restores { get; private set; }
        public async Task<MaintenanceReport> InitializeAsync(CancellationToken cancellationToken) => Current = Initialize is null ? Current : await Initialize();
        public Task<MaintenanceReport> RetryAsync(CancellationToken cancellationToken) => Task.FromResult(Current = new(MaintenanceCondition.Ready, 1, Current.Checkpoint));
        public Task<MaintenanceReport> RestoreAsync(string checkpointId, CancellationToken cancellationToken) { Restores++; return RetryAsync(cancellationToken); }
    }
}
