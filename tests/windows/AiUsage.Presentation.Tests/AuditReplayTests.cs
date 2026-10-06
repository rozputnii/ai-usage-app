using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using AiUsage.Adapters.Live.Audit;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class AuditReplayTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
    private static AuditInput Empty => new(Now, "UTC", [], [], [], BudgetConfiguration.Default);

    [Fact]
    public void OpenAcceptsPageFixtureWithoutScopeAnnotations() => AssertOpensFixtureWithoutScopeAnnotations(Empty);

    [Fact]
    public void OpenAcceptsMaintenanceFixtureWithoutScopeAnnotations() =>
        AssertOpensFixtureWithoutScopeAnnotations(Empty with { UseProductMaintenance = true });

    // AUD-01: exported page and maintenance fixtures omit the former annotation property.
    private static void AssertOpensFixtureWithoutScopeAnnotations(AuditInput input)
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-omitted-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var fixture = Path.Combine(root, "input.json");
        try
        {
            var json = JsonNode.Parse(JsonSerializer.Serialize(input, AuditJson.Default.AuditInput))!.AsObject();
            json.Remove("ScopeAnnotations");
            File.WriteAllText(fixture, json.ToJsonString());
            var state = Path.Combine(root, "state");
            Assert.NotNull(AuditReplay.Open(["--demo", "--audit-input=" + fixture], state, packaged: false));
            Assert.True(File.Exists(Path.Combine(state, "synthetic-audit.marker")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void OpenIgnoresAuditInputInPackagedProcess()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-packaged-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var fixture = Path.Combine(root, "input.json");
        var state = Path.Combine(root, "state");
        try
        {
            File.WriteAllText(fixture, JsonSerializer.Serialize(Empty, AuditJson.Default.AuditInput));
            Assert.Null(AuditReplay.Open(["--demo", "--audit-input=" + fixture], state, packaged: true));
            Assert.Null(AuditReplay.Open(["--demo", "--audit-input=" + Path.Combine(root, "missing.json")], state, packaged: true));
            Assert.False(Directory.Exists(state));
            Assert.False(File.Exists(Path.Combine(state, "synthetic-audit.marker")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void MaintenanceReplayRestartsWithTheSameSyntheticSelectionAndRejectsAccountFixtures()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-maintenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var fixture = Path.Combine(root, "input with spaces.json");
        try
        {
            File.WriteAllText(fixture, JsonSerializer.Serialize(Empty with { UseProductMaintenance = true }, AuditJson.Default.AuditInput));
            var replay = AuditReplay.Open(["--demo", "--audit-input=" + fixture], Path.Combine(root, "state"), packaged: false)!;
            Assert.Equal("--demo --audit-input=\"" + fixture + "\"", replay.RestartArguments);
            replay.RecordProcess();
            var receipt = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(replay.Root, "process-" + Environment.ProcessId + ".json")), AuditJson.Default.AuditProcessReceipt)!;
            Assert.Equal(Environment.ProcessId, receipt.ProcessId);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fixture))), receipt.FixtureSha256);
            Assert.Equal(Now, receipt.ControlledNow); Assert.Equal("UTC", receipt.ZoneId);
            var invalid = Empty with { UseProductMaintenance = true, NextAccounts = [] };
            File.WriteAllText(fixture, JsonSerializer.Serialize(invalid, AuditJson.Default.AuditInput));
            Assert.Throws<InvalidDataException>(() => AuditReplay.Open(["--demo", "--audit-input=" + fixture], Path.Combine(root, "rejected"), packaged: false));
            Assert.False(Directory.Exists(Path.Combine(root, "rejected")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeldInitializationReleasesOrDrainsOnShutdown(bool release)
    {
        var released = false;
        var receipts = new List<string>();
        using var accounts = new AuditAccounts(Empty with { BlockInitialization = true }, new(Now), receipts.Add, () => released);
        var initialize = accounts.InitializeAsync(CancellationToken.None);
        Assert.False(initialize.IsCompleted);
        if (release) released = true;
        else await accounts.StopAsync();
        await initialize.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Contains(release ? "InitializeCompleted" : "InitializeCancelled", receipts);
        if (release) await accounts.StopAsync();
        Assert.Contains("Stopped", receipts);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AuditSelectionRejectsMissingDemoOrStorageBeforeCreatingFiles(bool demo, bool storage)
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-reject-" + Guid.NewGuid().ToString("N"));
        var args = demo ? new[] { "--demo", "--audit-input=does-not-exist" } : ["--audit-input=does-not-exist"];
        Assert.Throws<InvalidOperationException>(() => AuditReplay.Open(args, storage ? root : null, packaged: false));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void OrdinaryLaunchDoesNotInspectOrCreateAuditStorage()
    {
        Assert.Null(AuditReplay.Open([], "not-a-valid-absolute-path", packaged: false));
        Assert.Null(AuditReplay.Open(["--demo"], null, packaged: false));
    }

    [Fact]
    public void AuditRootCanRestartButCannotAdoptUnmarkedData()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-audit-selection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var fixture = Path.Combine(root, "input.json");
        var state = Path.Combine(root, "state");
        try
        {
            File.WriteAllText(fixture, JsonSerializer.Serialize(Empty, AuditJson.Default.AuditInput));
            var args = new[] { "--demo", "--audit-input=" + fixture };
            Assert.NotNull(AuditReplay.Open(args, state, packaged: false));
            File.WriteAllText(Path.Combine(state, "preserve.txt"), "synthetic retained state");
            Assert.NotNull(AuditReplay.Open(args, state, packaged: false));
            File.WriteAllText(Path.Combine(state, "synthetic-audit.marker"), "different marker");
            Assert.Throws<InvalidOperationException>(() => AuditReplay.Open(args, state, packaged: false));
            Assert.Equal("synthetic retained state", File.ReadAllText(Path.Combine(state, "preserve.txt")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData((int)SignInFailure.Duplicate, AccountOutcome.Duplicate, null)]
    [InlineData((int)SignInFailure.WrongAccount, AccountOutcome.Failed, ProviderFailureKind.AccountMismatch)]
    [InlineData((int)SignInFailure.Storage, AccountOutcome.Failed, ProviderFailureKind.StorageUnavailable)]
    [InlineData((int)SignInFailure.AccessDenied, AccountOutcome.Failed, ProviderFailureKind.AccessDenied)]
    [InlineData((int)SignInFailure.Expired, AccountOutcome.Failed, ProviderFailureKind.LoginAttemptExpired)]
    [InlineData((int)SignInFailure.Browser, AccountOutcome.Failed, ProviderFailureKind.BrowserCallbackUnavailable)]
    [InlineData((int)SignInFailure.Registration, AccountOutcome.Failed, ProviderFailureKind.RegistrationUnavailable)]
    [InlineData((int)SignInFailure.Provider, AccountOutcome.Failed, ProviderFailureKind.ProviderUnavailable)]
    public async Task MockAuthenticationExercisesProductionFailureReasons(int failure, AccountOutcome outcome, ProviderFailureKind? kind)
    {
        var receipts = new List<string>();
        using var accounts = new AuditAccounts(Empty with { ManualCode = true, SignInFailure = (SignInFailure)failure }, new(Now), receipts.Add);
        var attempt = Guid.NewGuid();
        var connect = accounts.ConnectAsync("claude", null, attempt, challenge => Assert.Equal("synthetic.invalid", challenge.VerificationUri.Host), TestContext.Current.CancellationToken);
        Assert.False(accounts.TrySubmitCode(attempt, "invalid-synthetic-code"));
        Assert.True(accounts.TrySubmitCode(attempt, "synthetic-code"));
        var result = await connect;
        Assert.Equal(outcome, result.Outcome);
        Assert.Equal(kind, result.Failure);
        Assert.Empty(accounts.Current);
        Assert.DoesNotContain(receipts, line => line.Contains("synthetic-code", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CancelledAttemptCannotApplyALateCodeToTheNextAttempt()
    {
        using var accounts = new AuditAccounts(Empty with { ManualCode = true }, new(Now), _ => { });
        var first = Guid.NewGuid();
        var cancelled = accounts.ConnectAsync("claude", null, first, _ => { }, TestContext.Current.CancellationToken);
        await accounts.CancelConnectAsync(first);
        Assert.Equal(AccountOutcome.Cancelled, (await cancelled).Outcome);
        var next = Guid.NewGuid();
        var success = accounts.ConnectAsync("claude", null, next, _ => { }, TestContext.Current.CancellationToken);
        Assert.False(accounts.TrySubmitCode(first, "synthetic-code"));
        Assert.False(success.IsCompleted);
        Assert.True(accounts.TrySubmitCode(next, "synthetic-code"));
        Assert.Equal(AccountOutcome.Done, (await success).Outcome);
        Assert.Single(accounts.Current);
    }

    [Fact]
    public async Task MockRefreshAndSignOutOnlyChangeTheRequestedAccount()
    {
        var a = new AccountSnapshot(Guid.NewGuid(), "claude", true, ProviderSessionState.NotConnected, false, null);
        var b = a with { AccountId = Guid.NewGuid() };
        var replacement = a with { Session = a.Session with { Status = ProviderSessionStatus.QuotaAvailable } };
        var clock = new AuditClock(Now);
        var receipts = new List<string>();
        using var accounts = new AuditAccounts(Empty with { Accounts = [a, b], NextAccounts = [replacement], NextNow = Now.AddDays(1) }, clock, receipts.Add);
        await accounts.RefreshAsync(a.AccountId, TestContext.Current.CancellationToken);
        Assert.Equal(replacement, accounts.Current[0]);
        Assert.Equal(b, accounts.Current[1]);
        Assert.Equal(Now.AddDays(1), clock.Now);
        await accounts.DisconnectAsync(a.AccountId, TestContext.Current.CancellationToken);
        Assert.False(accounts.Current[0].Connected);
        Assert.Equal(b, accounts.Current[1]);
        Assert.Equal(["Refresh:" + a.AccountId.ToString("N"), "Disconnect:" + a.AccountId.ToString("N")], receipts);
    }

    [Fact]
    public async Task ShutdownCancelsAndDrainsMockedInflightWork()
    {
        var receipts = new List<string>();
        using var accounts = new AuditAccounts(Empty with { BlockRefresh = true, ManualCode = true }, new(Now), receipts.Add);
        var refresh = accounts.RefreshAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        var connect = accounts.ConnectAsync("claude", null, Guid.NewGuid(), _ => { }, TestContext.Current.CancellationToken);
        Assert.False(refresh.IsCompleted);
        Assert.False(connect.IsCompleted);
        await accounts.StopAsync().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(AccountOutcome.Cancelled, (await refresh).Outcome);
        Assert.Equal(AccountOutcome.Cancelled, (await connect).Outcome);
        Assert.Contains("Stopped", receipts);
        Assert.Empty(accounts.Current);
    }

    [Theory]
    [InlineData("claude")]
    [InlineData("codex")]
    [InlineData("copilot")]
    [InlineData("antigravity")]
    public async Task FirstRunMockLoginUsesTheSelectedProvidersSyntheticReplacement(string provider)
    {
        var template = new AccountSnapshot(Guid.NewGuid(), provider, true,
            ProviderSessionState.NotConnected with { Status = ProviderSessionStatus.QuotaAvailable }, false, null);
        using var accounts = new AuditAccounts(Empty with { ManualCode = true, NextAccounts = [template] }, new(Now), _ => { });
        var attempt = Guid.NewGuid();
        var connect = accounts.ConnectAsync(provider, null, attempt, _ => { }, TestContext.Current.CancellationToken);
        Assert.True(accounts.TrySubmitCode(attempt, "synthetic-code"));
        Assert.Equal(AccountOutcome.Done, (await connect).Outcome);
        var added = Assert.Single(accounts.Current);
        Assert.Equal(provider, added.Provider);
        Assert.True(added.Connected);
        Assert.NotEqual(template.AccountId, added.AccountId);
        Assert.Equal(template.Session, added.Session);
    }
}
