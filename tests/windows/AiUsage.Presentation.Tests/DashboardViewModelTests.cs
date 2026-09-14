using AiUsage.Core.Dashboard;
using AiUsage.Core.Usage;
using AiUsage.Features.Dashboard;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class DashboardViewModelTests
{
    [Fact]
    public void UnavailableCompositionDisablesProviderCommands()
    {
        var model = Create(null);
        Assert.False(model.ConnectCommand.CanExecute(null));
        Assert.False(model.RefreshCommand.CanExecute(null));
        Assert.False(model.DisconnectCommand.CanExecute(null));
        Assert.Equal("EmptyState/Text", model.StatusText);
    }

    [Fact]
    public async Task BusyCommandsAreDisabledAndUpdatesUseTheDispatcher()
    {
        var release = new TaskCompletionSource<ProviderSessionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeSession { HasStoredGrant = true, Operation = _ => { started.SetResult(); return release.Task; } };
        using var workflow = new DashboardWorkflow(session);
        var dispatches = 0;
        var model = new DashboardViewModel(workflow, _ => { }, key => key,
            action => { dispatches++; action(); return Task.CompletedTask; });
        var running = model.RefreshCommand.ExecuteAsync(null);
        await started.Task;
        Assert.True(model.Busy);
        Assert.False(model.ConnectCommand.CanExecute(null));
        Assert.False(model.DisconnectCommand.CanExecute(null));
        release.SetResult(new(ProviderSessionStatus.ReauthenticationRequired));
        await running;
        Assert.False(model.Busy);
        Assert.True(model.ConnectCommand.CanExecute(null));
        Assert.Equal("ReauthenticationRequired/Text", model.StatusText);
        Assert.True(dispatches >= 2);
    }

    [Fact]
    public async Task CachedFailureRemainsExplicitlyStale()
    {
        var session = new FakeSession
        {
            HasStoredGrant = true,
            Operation = _ => Task.FromResult(new ProviderSessionState(ProviderSessionStatus.QuotaUnavailable,
                RetrievedAt: DateTimeOffset.UtcNow, FromCache: true))
        };
        using var workflow = new DashboardWorkflow(session);
        var model = Create(workflow);
        await model.RefreshCommand.ExecuteAsync(null);
        Assert.Contains("CachedNotice/Text", model.StatusText);
        Assert.StartsWith("QuotaUnavailable/Text", model.StatusText);
        Assert.Empty(model.Windows);
    }

    private static DashboardViewModel Create(DashboardWorkflow? workflow) =>
        new(workflow, _ => { }, key => key, action => { action(); return Task.CompletedTask; });

    [Fact]
    public void UnknownAndExhaustedQuotaRemainDistinct()
    {
        var window = new QuotaWindow("primary", null, null, null, null);
        var group = new QuotaGroup("opaque-id", null, null, null, null, null, [window]);
        var unknown = new QuotaWindowItem(group, window, key => key);
        var exhausted = new QuotaWindowItem(group, window with { RemainingPercent = 0 }, key => key);
        Assert.Equal("opaque-id", unknown.Name);
        Assert.Equal("RemainingUnknown/Text", unknown.Remaining);
        Assert.Equal("ResetsUnknown/Text", unknown.Resets);
        Assert.Equal("RemainingExhausted/Text", exhausted.Remaining);
    }

    [Fact]
    public async Task ShutdownDrainsCommandPresentationAndKeepsCommandsDisabled()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeSession { HasStoredGrant = true, Operation = async token =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return ProviderSessionState.NotConnected;
        } };
        using var workflow = new DashboardWorkflow(session);
        var model = Create(workflow);
        var running = model.RefreshCommand.ExecuteAsync(null);
        await started.Task;
        await model.StopAsync();
        Assert.True(running.IsCompletedSuccessfully);
        Assert.False(model.Busy);
        Assert.False(model.ConnectCommand.CanExecute(null));
        Assert.False(model.RefreshCommand.CanExecute(null));
    }
}
