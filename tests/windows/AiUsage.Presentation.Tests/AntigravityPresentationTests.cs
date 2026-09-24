using AiUsage.Adapters.Live;
using AiUsage.Core.Usage;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class AntigravityPresentationTests
{
    [Fact]
    public async Task BrowserSignInLaunchesGoogleAndOffersNoManualCodePath()
    {
        using var host = new TestHost();
        var session = new BrowserSession();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["antigravity"] = session });
        var launches = 0;
        var flow = new LiveConnectionFlow(source, uri => { Assert.Equal("accounts.google.com", uri.Host); launches++; });
        Assert.Contains(flow.Providers, provider => provider.ProviderId == "antigravity" &&
            provider.Methods.SequenceEqual([ConnectionMethod.BrowserSignIn]) && provider.Origin == CapabilityOrigin.Existing);
        var context = new PresentationContext(source, host.Dispatcher, host.Clock, host.Text, host.Announcer, host.Navigation, host.Dialogs, host.Motion);
        using var sheet = new AddAccountViewModel(context, flow, new CliImportViewModel(context, host.Cli));
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sheet.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(sheet.Step) && sheet.IsWaiting) waiting.TrySetResult(); };
        var running = sheet.ConnectCommand.ExecuteAsync(sheet.ProviderOptions.Single(p => p.ProviderId == "antigravity"));
        await waiting.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal("", sheet.DeviceUserCode);
        Assert.False(sheet.SupportsManualCode);
        Assert.Equal(1, launches);
        // A transient authorization URL never reaches a durable presentation field.
        Assert.DoesNotContain("code_challenge", sheet.StatusText);
        sheet.CancelCommand.Execute(null);
        await running;
        Assert.True(session.Cancelled);
        await source.StopAsync();
    }

    [Fact]
    public void ProviderGroupsWindowsAndTierReachPresentationWithoutRelabelling()
    {
        var quota = new QuotaSnapshot(DateTimeOffset.UtcNow, "free-tier",
        [
            new("Gemini Models", "Gemini Models", null, null, null, null,
            [
                new("gemini-5h", 41, 59, TimeSpan.FromHours(5), DateTimeOffset.UtcNow.AddHours(3)) { SourceDetails = new("gemini-5h", null, null, null) },
                new("gemini-weekly", 12, 88, TimeSpan.FromDays(7), DateTimeOffset.UtcNow.AddDays(4))
            ]),
            new("Claude and GPT models", "Claude and GPT models", null, null, false, null, [])
        ], null, null, null, null);
        var account = LiveMapping.Map("antigravity", new(ProviderSessionStatus.QuotaAvailable, quota), true);
        Assert.Equal("Antigravity", account.Label);
        Assert.Equal("free-tier", account.Plan);
        var groups = Assert.Single(account.Contexts).Groups;
        Assert.Equal(["Gemini Models", "Claude and GPT models"], groups.Select(group => group.Label));
        Assert.False(groups[1].Allowed);
        var fiveHour = groups[0].Windows[0];
        Assert.Equal(ValueState.Known, fiveHour.ValueState);
        Assert.Equal(59, fiveHour.RemainingPercent);
        Assert.Equal(TimeSpan.FromHours(5).TotalSeconds, fiveHour.DurationSeconds);
        Assert.Null(fiveHour.Absolute);
    }

    [Fact]
    public void AnUnknownWindowIsNeitherExhaustedNorUnlimited()
    {
        var quota = new QuotaSnapshot(DateTimeOffset.UtcNow, null,
            [new("Gemini Models", "Gemini Models", null, null, null, null,
                [new("no-fraction", null, null, null, DateTimeOffset.UtcNow.AddDays(3))])], null, null, null, null);
        var account = LiveMapping.Map("antigravity", new(ProviderSessionStatus.QuotaAvailable, quota), true);
        var window = Assert.Single(Assert.Single(Assert.Single(account.Contexts).Groups).Windows);
        Assert.Equal(ValueState.Unknown, window.ValueState);
        Assert.Equal(QuotaSeverity.None, QuotaRules.Classify(window, Preferences.DefaultRemainingThresholds));
    }

    [Fact]
    public void AMissingWorkspaceIsRetryableAndKeepsItsOwnMessage()
    {
        var failure = LiveMapping.Failure(ProviderFailureKind.ProjectUnavailable);
        Assert.Equal("ProjectUnavailable", failure!.Kind);
        Assert.Equal("Failure_ProjectUnavailable", failure.MessageKey);
        Assert.True(failure.Recoverable);
    }

    [Fact]
    public void AnUnconfiguredRegistrationOffersNoRetryAndIsNotAnInvalidGrant()
    {
        var failure = LiveMapping.Failure(ProviderFailureKind.RegistrationUnavailable);
        Assert.Equal("RegistrationUnavailable", failure!.Kind);
        Assert.Equal("Failure_RegistrationUnavailable", failure.MessageKey);
        Assert.False(failure.Recoverable);
    }

    private sealed class BrowserSession : IProviderSession
    {
        public bool Cancelled { get; private set; }
        public bool HasStoredGrant => false;
        public ProviderSessionState State => ProviderSessionState.NotConnected;
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public async Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default)
        {
            openAuthorizationUrl(new("https://accounts.google.com/o/oauth2/v2/auth?client_id=synthetic&code_challenge=synthetic"));
            try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
            catch (OperationCanceledException) { Cancelled = true; throw; }
            return State;
        }
    }
}
