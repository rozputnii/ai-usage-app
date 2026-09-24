using AiUsage.Adapters.Live;
using AiUsage.Core.Usage;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class CopilotPresentationTests
{
    [Fact]
    public async Task DeviceCodeIsShownDuringAttemptAndClearedOnCancellation()
    {
        using var host = new TestHost();
        var session = new DeviceSession();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["copilot"] = session });
        var launches = 0;
        var flow = new LiveConnectionFlow(source, uri => { Assert.Equal("github.com", uri.Host); launches++; });
        var context = new PresentationContext(source, host.Dispatcher, host.Clock, host.Text, host.Announcer, host.Navigation, host.Dialogs, host.Motion);
        using var sheet = new AddAccountViewModel(context, flow, new CliImportViewModel(context, host.Cli));
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sheet.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(sheet.Step) && sheet.IsWaiting) waiting.TrySetResult(); };
        var running = sheet.ConnectCommand.ExecuteAsync(sheet.ProviderOptions.Single(p => p.ProviderId == "copilot"));
        await waiting.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal("ABCD-1234", sheet.DeviceUserCode);
        Assert.Contains("github.com/login/device", sheet.StatusText);
        Assert.False(sheet.SupportsManualCode);
        Assert.Equal(1, launches);
        sheet.CancelCommand.Execute(null);
        await running;
        Assert.Equal("", sheet.DeviceUserCode);
        Assert.True(session.Cancelled);
        await source.StopAsync();
    }

    [Fact]
    public void NativeQuotaAndUnlimitedStateReachPresentationWithoutProviderRelabelling()
    {
        var quota = new QuotaSnapshot(DateTimeOffset.UtcNow, "individual", [new("premium_interactions", "Premium requests", null, null, null, false,
            [new("monthly", null, null, null, null) { Unlimited = true, Amount = new(null, null, null, "requests") }])], null, null, null, null);
        var account = LiveMapping.Map("copilot", new(ProviderSessionStatus.QuotaAvailable, quota), true);
        Assert.Equal("GitHub Copilot", account.Label);
        var window = Assert.Single(Assert.Single(Assert.Single(account.Contexts).Groups).Windows);
        Assert.Equal(ValueState.Unlimited, window.ValueState);
        Assert.Equal("requests", window.Absolute!.Unit);
        Assert.Null(window.RemainingPercent);
    }

    [Fact]
    public void ZeroEntitlementDoesNotClaimConsumedQuotaOrProviderRestriction()
    {
        var quota = new QuotaSnapshot(DateTimeOffset.UtcNow, "individual", [new("premium_interactions", "Premium requests", null, null, null, null,
            [new("monthly", 100, 0, null, null) { Amount = new(0, 0, 0, "requests") }])], null, null, null, null);
        var account = LiveMapping.Map("copilot", new(ProviderSessionStatus.QuotaAvailable, quota), true);
        var group = Assert.Single(Assert.Single(account.Contexts).Groups);
        var window = Assert.Single(group.Windows);
        Assert.Equal(ValueState.Unknown, window.ValueState);
        Assert.Equal(QuotaSeverity.None, QuotaRules.Classify(window, Preferences.DefaultRemainingThresholds));
        Assert.Null(group.LimitReached);
        Assert.Equal("0", window.Absolute!.Limit);
    }

    private sealed class DeviceSession : IProviderSession
    {
        public bool Cancelled { get; private set; }
        public bool HasStoredGrant => false;
        public ProviderSessionState State => ProviderSessionState.NotConnected;
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Code would be lost.");
        public async Task<ProviderSessionState> ConnectWithChallengeAsync(Action<AuthorizationChallenge> authorize, CancellationToken cancellationToken = default)
        {
            authorize(new(new("https://github.com/login/device"), "ABCD-1234"));
            try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
            catch (OperationCanceledException) { Cancelled = true; throw; }
            return State;
        }
    }
}
