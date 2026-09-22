using System.Text.Json;
using AiUsage.Core.Diagnostics;
using AiUsage.Adapters.Live;
using AiUsage.Core.Usage;
using AiUsage.Features.Presentation;
using AiUsage.Features.Connection;
using AiUsage.Features.Settings;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class LiveAdapterTests
{
    [Theory]
    [InlineData(ProviderFailureKind.ProviderUnavailable)]
    [InlineData(ProviderFailureKind.NetworkFailure)]
    [InlineData(ProviderFailureKind.InvalidResponse)]
    public async Task ClassifiedProviderFailureKeepsItsMeaningWithoutAnInternalDiagnostic(ProviderFailureKind kind)
    {
        var diagnostics = new DiagnosticCapture();
        var session = new Session { RefreshFailure = kind };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session }, diagnostics);
        await source.InitializeAsync();
        var result = await source.ExecuteAsync(new(UiCommandKind.RefreshAccount, "codex", null,
            source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.Equal(CommandStatus.Failed, result.Status);
        Assert.Equal(kind.ToString(), result.Failure?.Kind);
        Assert.True(result.Failure?.Recoverable);
        Assert.Empty(diagnostics.Records);
        await source.StopAsync();
    }

    [Theory]
    [InlineData(ConnectionState.Connected)]
    [InlineData(ConnectionState.ReauthRequired)]
    public void InternalErrorDoesNotOfferRetryOrReconnection(ConnectionState connection)
    {
        using var host = new TestHost();
        var account = host.Usage.Current.Accounts[0] with
        {
            Connection = connection,
            Failure = new("InternalError", "Failure_InternalError", null, false)
        };
        var failure = new FailureViewModel();
        failure.Update(account, host.Format);
        Assert.True(failure.IsVisible);
        Assert.Equal(FailureAction.None, failure.Action);
        Assert.False(failure.ActionEnabled);
        Assert.False(failure.HasAction);
    }

    [Fact]
    public async Task UnexpectedOperationFailureHasDistinctMessageAndNoRetry()
    {
        var session = new Session { RefreshError = new InvalidOperationException("synthetic-private-value") };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        await source.InitializeAsync();
        var result = await source.ExecuteAsync(new(UiCommandKind.RefreshAccount, "codex", null,
            source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.Equal(CommandStatus.Failed, result.Status);
        Assert.Equal("InternalError", result.Failure?.Kind);
        Assert.Equal("Failure_InternalError", result.Failure?.MessageKey);
        Assert.False(result.Failure?.Recoverable);
        Assert.Equal(result.Failure, source.Current.Accounts.Single().Failure);
        _ = new TestText().Get("Failure_InternalError");
        _ = new TestText().Get("Failure_Short_InternalError");
    }

    [Fact]
    public async Task UnknownExceptionDataNeverCrossesDiagnosticBoundary()
    {
        const string token = "sk-synthetic-secret-value";
        const string opaque = "opaque-provider-account-id";
        var error = new InvalidOperationException(token, new Exception(opaque));
        error.Data[opaque] = new { future = new { credential = token } };
        var diagnostics = new DiagnosticCapture();
        var session = new Session { RefreshError = error };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { [opaque] = session }, diagnostics);
        await source.InitializeAsync();
        var result = await source.ExecuteAsync(new(UiCommandKind.RefreshAccount, opaque, null,
            source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.Equal(CommandStatus.Failed, result.Status);
        var record = Assert.Single(diagnostics.Records);
        Assert.Equal("OperationFailure/InvalidOperation", record);
        Assert.DoesNotContain(token, record);
        Assert.DoesNotContain(opaque, record);
    }

    private sealed class DiagnosticCapture : IDiagnosticSink
    {
        public List<string> Records { get; } = [];
        public void Record(DiagnosticEvent eventCode, DiagnosticCategory category) => Records.Add($"{eventCode}/{category}");
    }

    [Fact]
    public async Task ExitDuringPreferenceLoadDrainsStartupBeforeReturning()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession>());
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var preferences = new LivePreferenceStore(source, _ => { reading.TrySetResult(); return release.Task; }, (_, _) => Task.CompletedTask);
        var lifecycle = new ProductLifecycle(source, preferences);
        var startup = lifecycle.InitializeAsync();
        await reading.Task;
        var stopping = lifecycle.StopAsync();
        Assert.False(stopping.IsCompleted);
        release.SetResult(null);
        await stopping;
        Assert.True(startup.IsCompletedSuccessfully);
        Assert.Equal(CommandStatus.Cancelled, (await preferences.ResetSettingsAsync(TestContext.Current.CancellationToken)).Status);
    }
    [Fact]
    public async Task FirstRunHasNoInventedAccounts()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["claude"] = new LoginSession() });
        await source.InitializeAsync();
        Assert.True(source.Current.Loaded);
        Assert.Empty(source.Current.Accounts);
        await source.StopAsync();
    }

    [Theory]
    [InlineData("claude")]
    [InlineData("codex")]
    [InlineData("Fifth/opaque:ID")]
    public async Task ManualCodeCompletesOriginalAuthorizationAndDisconnectRemovesConnection(string providerId)
    {
        var session = new LoginSession();
        var catalog = new ProviderCatalog([.. ProviderCatalog.Default.All,
            new("Fifth/opaque:ID", "Fifth Provider", "★", [ConnectionMethod.BrowserSignIn, ConnectionMethod.ManualCode], CapabilityOrigin.Existing)]);
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { [providerId] = session }, providers: catalog);
        var flow = new LiveConnectionFlow(source, _ => { });
        var request = new ConnectRequest(providerId, ConnectionMethod.BrowserSignIn, null);
        var stages = new List<ConnectionStage>();
        await foreach (var stage in flow.ConnectAsync(request, TestContext.Current.CancellationToken))
        {
            stages.Add(stage);
            if (stage.Kind == ConnectionStageKind.WaitingForAuthorization)
                Assert.True(flow.TrySubmitCode(request, "synthetic-code"));
        }
        Assert.Equal(ConnectionStageKind.Connected, stages.Last().Kind);
        Assert.Equal(1, session.Connects);
        Assert.Equal(ConnectionState.Connected, source.Current.Accounts.Single().Connection);
        Assert.Equal(CommandStatus.Succeeded, (await source.ExecuteAsync(new(UiCommandKind.Disconnect, providerId, null,
            source.Current.Revision), TestContext.Current.CancellationToken)).Status);
        Assert.Equal(ConnectionState.NotConnected, source.Current.Accounts.Single().Connection);
        await source.StopAsync();
    }

    [Fact]
    public async Task LeavingConnectionStreamCancelsAndDrainsAuthorization()
    {
        var session = new LoginSession();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["claude"] = session });
        var flow = new LiveConnectionFlow(source, _ => { });
        await foreach (var stage in flow.ConnectAsync(new("claude", ConnectionMethod.BrowserSignIn, null), TestContext.Current.CancellationToken))
            if (stage.Kind == ConnectionStageKind.WaitingForAuthorization) break;
        Assert.True(session.Cancelled);
        Assert.Empty(source.Current.Accounts);
        await source.StopAsync();
    }

    private sealed class LoginSession : IProviderSession
    {
        private TaskCompletionSource code = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Connects { get; private set; }
        public bool Cancelled { get; private set; }
        public bool HasStoredGrant { get; private set; }
        public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public async Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default)
        {
            code = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Connects++;
            openAuthorizationUrl(new Uri("https://example.test/authorize"));
            try { await code.Task.WaitAsync(cancellationToken); }
            catch (OperationCanceledException) { Cancelled = true; throw; }
            HasStoredGrant = true;
            return State = new(ProviderSessionStatus.QuotaAvailable, new(DateTimeOffset.UtcNow, null, [], null, null, null, null));
        }
        public bool TrySubmitCode(string value) => code.TrySetResult();
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            HasStoredGrant = false;
            return Task.FromResult(State = ProviderSessionState.NotConnected);
        }
    }
    [Fact]
    public async Task PreferencesPersistOnlyAfterSuccessfulWriteAndFutureSettingsStayUnavailable()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession>());
        string? saved = null;
        var fail = false;
        var preferences = new LivePreferenceStore(source, _ => Task.FromResult(saved), (json, _) =>
        { if (fail) throw new IOException(); saved = json; return Task.CompletedTask; });
        var token = TestContext.Current.CancellationToken;
        await preferences.LoadAsync(token);
        Assert.Equal(CommandStatus.Succeeded, (await preferences.SetPreferenceAsync(new(PreferenceKey.Theme, ThemePreference.Dark), token)).Status);
        fail = true;
        Assert.Equal(CommandStatus.Failed, (await preferences.SetPreferenceAsync(new(PreferenceKey.Theme, ThemePreference.Light), token)).Status);
        Assert.Equal(ThemePreference.Dark, source.Current.Preferences.Theme);
        Assert.Equal(CommandStatus.Unsupported, (await preferences.SetPreferenceAsync(new(PreferenceKey.HistoryEnabled, true), token)).Status);
        var restored = new LivePreferenceStore(source, _ => Task.FromResult(saved), (_, _) => Task.CompletedTask);
        await restored.LoadAsync(token);
        Assert.Equal(ThemePreference.Dark, source.Current.Preferences.Theme);
        await source.StopAsync();
    }

    [Fact]
    public async Task ReentrantSubscriberIsInvokedWithoutTheSourceLockAndSeesIncreasingRevisions()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = new Session() });
        var revisions = new List<long>();
        var reentered = false;
        var observedOutsideLock = false;
        using var subscription = source.Subscribe(snapshot =>
        {
            lock (revisions) revisions.Add(snapshot.Revision);
            if (reentered) return;
            reentered = true;
            // A thread that is not publishing must be able to enter the source while a subscriber runs.
            observedOutsideLock = Task.Run(() => source.Current).Wait(TimeSpan.FromSeconds(10));
            // Re-entering the source from a subscriber must publish a later revision, not deadlock.
            source.SetPreferences(source.Current.Preferences with { AlwaysOnTop = true },
                new Dictionary<string, string>(), new Dictionary<string, ExpansionPreference>());
        });
        await source.InitializeAsync();
        Assert.True(reentered);
        Assert.True(observedOutsideLock);
        Assert.True(source.Current.Preferences.AlwaysOnTop);
        Assert.Equal(revisions.Count, revisions.Distinct().Count());
        Assert.Equal(revisions.OrderBy(revision => revision), revisions);
        await source.StopAsync();
    }

    [Fact]
    public async Task PreferenceFileWithUnknownMembersRoundTripsUnchanged()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession>());
        // A file written by a newer build: known members plus members this build has never heard of.
        const string stored = """
            {"Version":1,"Theme":2,"Density":1,"UsageDisplay":1,"AlwaysOnTop":false,"ShowDisconnected":true,
             "ShowHidden":false,"Order":["codex"],"Hidden":[],"Labels":{"codex":"Work"},"Expansion":{"codex":1},
             "FutureSetting":{"nested":[1,2],"flag":true},"AnotherUnknown":"kept"}
            """;
        string? saved = null;
        var preferences = new LivePreferenceStore(source, _ => Task.FromResult<string?>(stored), (json, _) => { saved = json; return Task.CompletedTask; });
        var token = TestContext.Current.CancellationToken;
        await preferences.LoadAsync(token);
        Assert.Equal(ThemePreference.Dark, source.Current.Preferences.Theme);
        Assert.Equal(CommandStatus.Succeeded, (await preferences.SetPreferenceAsync(new(PreferenceKey.AlwaysOnTop, true), token)).Status);
        using var written = JsonDocument.Parse(saved!);
        var root = written.RootElement;
        Assert.Equal("kept", root.GetProperty("AnotherUnknown").GetString());
        Assert.Equal("[1,2]", root.GetProperty("FutureSetting").GetProperty("nested").GetRawText());
        Assert.True(root.GetProperty("FutureSetting").GetProperty("flag").GetBoolean());
        Assert.Equal(1, root.GetProperty("Version").GetInt32());
        Assert.Equal((int)ThemePreference.Dark, root.GetProperty("Theme").GetInt32());
        Assert.Equal((int)Density.Compact, root.GetProperty("Density").GetInt32());
        Assert.Equal((int)UsageDisplay.Used, root.GetProperty("UsageDisplay").GetInt32());
        Assert.True(root.GetProperty("ShowDisconnected").GetBoolean());
        Assert.Equal("Work", root.GetProperty("Labels").GetProperty("codex").GetString());
        Assert.Equal((int)ExpansionPreference.Expanded, root.GetProperty("Expansion").GetProperty("codex").GetInt32());
        Assert.Equal(["codex"], root.GetProperty("Order").EnumerateArray().Select(value => value.GetString()));
        // Only the changed preference differs from the stored file.
        Assert.True(root.GetProperty("AlwaysOnTop").GetBoolean());
        var reloaded = new LivePreferenceStore(source, _ => Task.FromResult(saved), (_, _) => Task.CompletedTask);
        await reloaded.LoadAsync(token);
        Assert.Equal(ThemePreference.Dark, source.Current.Preferences.Theme);
        Assert.True(source.Current.Preferences.AlwaysOnTop);
        await source.StopAsync();
    }

    [Fact]
    public async Task InvalidPreferenceFileIsPreserved()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession>());
        var writes = 0;
        var preferences = new LivePreferenceStore(source, _ => Task.FromResult<string?>("{\"Version\":99}"), (_, _) => { writes++; return Task.CompletedTask; });
        var token = TestContext.Current.CancellationToken;
        await preferences.LoadAsync(token);
        Assert.Equal(CommandStatus.Failed, (await preferences.ResetSettingsAsync(token)).Status);
        Assert.Equal(0, writes);
        await source.StopAsync();
    }

    [Fact]
    public async Task OccupiedProviderExplainsLimitWithoutClaimingIdentityVerification()
    {
        var session = new Session();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        await source.InitializeAsync();
        var launches = 0;
        var flow = new LiveConnectionFlow(source, _ => launches++);
        var token = TestContext.Current.CancellationToken;
        var stages = new List<ConnectionStage>();
        // A provider the live flow does not offer is refused before any browser launch.
        await foreach (var stage in flow.ConnectAsync(new("unregistered-provider", ConnectionMethod.BrowserSignIn, null), token)) stages.Add(stage);
        Assert.Equal(ConnectionStageKind.Failed, stages.Single().Kind);
        stages.Clear();
        await foreach (var stage in flow.ConnectAsync(new("codex", ConnectionMethod.BrowserSignIn, null), token)) stages.Add(stage);
        Assert.Equal(ConnectionStageKind.ProviderSlotOccupied, stages.Single().Kind);
        using var host = new TestHost();
        var sheet = new AddAccountViewModel(host.Context, flow, host.CliImport());
        sheet.Open(new(AddAccountTab.SignIn, "codex"));
        await sheet.StartCommand.ExecuteAsync(null);
        Assert.Equal("One account per provider", sheet.ResultTitle);
        Assert.Contains("Disconnect", sheet.ResultBody);
        Assert.DoesNotContain("identity", sheet.ResultBody);
        Assert.Equal(0, launches);
        await source.StopAsync();
    }

    [Theory]
    [InlineData("codex", false)]
    [InlineData("claude", false)]
    [InlineData("codex", true)]
    [InlineData("claude", true)]
    public async Task DisconnectAllowsAnotherBrowserAuthorization(string provider, bool useAccountAction)
    {
        var session = new LoginSession();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { [provider] = session });
        var launches = 0;
        var flow = new LiveConnectionFlow(source, _ => launches++);
        var token = TestContext.Current.CancellationToken;
        await source.InitializeAsync();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var stages = new List<ConnectionStage>();
            var reconnect = attempt > 0 && useAccountAction ? provider : null;
            await foreach (var stage in flow.ConnectAsync(new(provider, ConnectionMethod.BrowserSignIn, reconnect), token))
            {
                stages.Add(stage);
                if (stage.Kind == ConnectionStageKind.WaitingForAuthorization) session.TrySubmitCode("synthetic-code");
            }
            Assert.Equal(reconnect is null ? ConnectionStageKind.Connected : ConnectionStageKind.Reconnected, stages.Last().Kind);
            Assert.Equal(attempt + 1, launches);
            Assert.Equal(CommandStatus.Succeeded, (await source.ExecuteAsync(new(UiCommandKind.Disconnect, provider, null,
                source.Current.Revision), token)).Status);
            Assert.Equal(ConnectionState.NotConnected, source.Current.Accounts.Single().Connection);
        }
        await source.StopAsync();
    }

    [Fact]
    public async Task RefreshWithoutNewQuotaDoesNotReportUpdated()
    {
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = new Session() });
        await source.InitializeAsync();
        await source.ExecuteAsync(new(UiCommandKind.RefreshAll, null, null, source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.Equal(0, source.Current.LastRefreshAll!.Updated);
        Assert.Equal(["codex"], source.Current.LastRefreshAll.FailedAccountIds);
        await source.StopAsync();
    }
    [Fact]
    public void MappingPreservesUnknownAndCachedObservation()
    {
        var at = DateTimeOffset.Parse("2026-09-15T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var quota = new QuotaSnapshot(at, "plan", [new("opaque", "Name", null, null, null, null,
            [new("unknown", null, null, null, at), new("empty", 100, 0, TimeSpan.FromHours(5), at)])],
            new(null, true, null), 2, null, null);
        var account = LiveMapping.Map("codex", new(ProviderSessionStatus.ReauthenticationRequired, quota,
            ProviderFailureKind.AuthenticationRequired, at, true), true);
        Assert.Equal(ConnectionState.ReauthRequired, account.Connection);
        Assert.Equal(Freshness.Stale, account.Freshness);
        Assert.Equal(at, account.FetchedAt);
        var windows = account.Contexts.Single().Groups.Single().Windows;
        Assert.Null(windows[0].RemainingPercent);
        Assert.Equal(ValueState.Unknown, windows[0].ValueState);
        Assert.Equal(ValueState.Exhausted, windows[1].ValueState);
        Assert.True(account.Extensions.Single().Unlimited);
    }

    [Fact]
    public void RestrictionFlagsRemainIndependentOfPositiveQuotaAndAreVisibleInDetails()
    {
        using var host = new TestHost();
        var quota = new QuotaSnapshot(DateTimeOffset.UtcNow, null,
            [new("opaque", "Provider pool", null, null, false, true, [new("5h", 20, 80, null, null)])],
            null, null, false, "provider-reason");
        var mapped = LiveMapping.Map("codex", new(ProviderSessionStatus.QuotaAvailable, quota), true);
        var group = mapped.Contexts.Single().Groups.Single();
        Assert.False(group.Allowed);
        Assert.True(group.LimitReached);
        Assert.Equal(80, group.Windows.Single().RemainingPercent);
        var detail = host.Accounts().Detail;
        detail.Update(mapped, host.Usage.Current with { Accounts = [mapped] });
        Assert.Contains(detail.InfoLines, line => line.Contains("Access not allowed") && line.Contains("Provider pool"));
        Assert.Contains(detail.InfoLines, line => line.Contains("limit reached") && line.Contains("Provider pool"));
        Assert.Contains(detail.InfoLines, line => line.Contains("provider-reason"));
    }

    [Fact]
    public async Task ResumePublishesCacheAndUnsupportedNeverCallsSession()
    {
        var session = new Session();
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        var snapshots = new List<UiSnapshot>();
        using var subscription = source.Subscribe(snapshots.Add);
        await source.InitializeAsync();
        Assert.Equal(1, session.Resumes);
        Assert.Contains(snapshots, s => s.Accounts.Any(a => a.Freshness == Freshness.Cached));
        var result = await source.ExecuteAsync(new(UiCommandKind.FactoryReset, null, null, source.Current.Revision), TestContext.Current.CancellationToken);
        Assert.Equal(CommandStatus.Unsupported, result.Status);
        Assert.Equal(0, session.Disconnects);
        await source.StopAsync();
    }

    [Fact]
    public async Task CancellationPublishesSessionReauthenticationStateAndStopDrains()
    {
        var session = new Session { BlockRefresh = true };
        using var source = new LiveUsageSource(new Dictionary<string, IProviderSession> { ["codex"] = session });
        await source.InitializeAsync();
        var refresh = source.ExecuteAsync(new(UiCommandKind.RefreshAccount, "codex", null, source.Current.Revision), TestContext.Current.CancellationToken);
        await session.Started.Task;
        await source.StopAsync();
        Assert.Equal(CommandStatus.Cancelled, (await refresh).Status);
        Assert.Equal(ConnectionState.ReauthRequired, source.Current.Accounts.Single().Connection);
        Assert.Equal(AccountOperation.Idle, source.Current.Accounts.Single().Operation);
    }

    private sealed class Session : IProviderSession
    {
        public bool HasStoredGrant => true;
        public ProviderSessionState State { get; private set; } = new(ProviderSessionStatus.QuotaUnavailable, FromCache: true);
        public int Resumes { get; private set; }
        public int Disconnects { get; private set; }
        public bool BlockRefresh { get; init; }
        public Exception? RefreshError { get; init; }
        public ProviderFailureKind? RefreshFailure { get; init; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) { Resumes++; return Task.FromResult(State); }
        public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) => Task.FromResult(State);
        public async Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (RefreshError is not null) throw RefreshError;
            if (RefreshFailure is not null) return State = State with { Failure = RefreshFailure };
            Started.TrySetResult();
            try { if (BlockRefresh) await Task.Delay(Timeout.Infinite, cancellationToken); }
            catch (OperationCanceledException) { State = new(ProviderSessionStatus.ReauthenticationRequired); throw; }
            return State;
        }
        public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) { Disconnects++; return Task.FromResult(State); }
    }
}
