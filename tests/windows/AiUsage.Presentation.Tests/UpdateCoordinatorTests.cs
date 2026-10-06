using AiUsage.Adapters.Live;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class UpdateCoordinatorTests
{
    private const string Current = "2026.10.604.0";
    private static readonly TimeSpan Minute = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly int Offline = unchecked((int)0x80072EFE);

    [Fact]
    public void AlwaysChecksAfterSixtySecondsThenEveryFiveMinutes()
    {
        var (coordinator, package, _, _, scheduler) = Create(UpdateMode.Always);
        coordinator.Start();
        Assert.Equal(0, package.Checks);
        scheduler.Run(Minute);
        Assert.Equal(1, package.Checks);
        scheduler.Run(Interval);
        Assert.Equal(2, package.Checks);
    }

    [Fact]
    public void OnLaunchChecksOnceAfterSixtySeconds()
    {
        var (coordinator, package, _, _, scheduler) = Create(UpdateMode.OnLaunch);
        coordinator.Start();
        scheduler.Run(Minute);
        Assert.Equal(1, package.Checks);
        scheduler.Run(Interval);
        scheduler.Run(Interval);
        Assert.Equal(1, package.Checks);
    }

    [Fact]
    public async Task OffNeverChecksAutomaticallyButManualWorks()
    {
        var (coordinator, package, _, _, scheduler) = Create(UpdateMode.Off);
        coordinator.Start();
        scheduler.Run(Minute);
        scheduler.Run(Interval);
        Assert.Equal(0, package.Checks);
        await coordinator.CheckAsync(manual: true);
        Assert.Equal(1, package.Checks);
    }

    [Fact]
    public async Task ConcurrentChecksShareOneCall()
    {
        var (coordinator, package, host, _, _) = Create(UpdateMode.Off);
        coordinator.Start();
        package.Hold = new();
        var first = coordinator.CheckAsync(manual: true);
        var second = coordinator.CheckAsync(manual: false);
        Assert.Same(first, second);
        package.Hold.SetResult(new(false, null));
        await first;
        Assert.Equal(1, package.Checks);
        Assert.Equal(UpdateState.UpToDate, host.Status!.State);
        Assert.NotNull(host.Status.CheckedAt);
    }

    [Fact]
    public void SwitchingToAlwaysAfterLaunchStartsInterval()
    {
        var (coordinator, package, host, _, scheduler) = Create(UpdateMode.Off);
        coordinator.Start();
        scheduler.Run(Minute);
        Assert.Equal(0, package.Checks);
        host.Mode = UpdateMode.Always;
        coordinator.Reevaluate();
        scheduler.Run(Interval);
        Assert.Equal(1, package.Checks);
    }

    [Theory]
    [InlineData(true, false, 1, (int)UpdateState.Installing)]
    [InlineData(false, false, 0, (int)UpdateState.Ready)]
    [InlineData(true, true, 0, (int)UpdateState.Ready)]
    public void AvailableUpdateInstallsOnlyWhenHiddenAndNoSignIn(bool hidden, bool signIn, int installs, int state)
    {
        var (coordinator, package, host, _, scheduler) = Create(UpdateMode.Always);
        host.WindowHidden = hidden; host.SignInActive = signIn;
        package.Result = new(true, null);
        coordinator.Start();
        scheduler.Run(Minute);
        Assert.Equal(installs, package.Installs);
        Assert.Equal((UpdateState)state, host.Status!.State);
    }

    [Fact]
    public void ReadyInstallsWhenWindowHides()
    {
        var (coordinator, package, host, log, scheduler) = Create(UpdateMode.Always);
        package.Result = new(true, null);
        coordinator.Start();
        scheduler.Run(Minute);
        Assert.Equal(UpdateState.Ready, host.Status!.State);
        host.WindowHidden = true;
        coordinator.Reevaluate();
        Assert.Equal(1, package.Installs);
        Assert.Equal("--updated-from=2026.10.604.0 --background", package.LastArguments);
        Assert.Equal([(true, Current)], log.Starts);
    }

    [Fact]
    public async Task ManualInstallFromOpenWindowRelaunchesWithWindow()
    {
        var (coordinator, package, host, log, scheduler) = Create(UpdateMode.Off);
        package.Result = new(true, null);
        coordinator.Start();
        scheduler.Run(Minute);
        await coordinator.CheckAsync(manual: true);
        Assert.Equal(UpdateState.Available, host.Status!.State);
        await coordinator.InstallAsync(automatic: false);
        Assert.Equal("--updated-from=2026.10.604.0", package.LastArguments);
        Assert.Equal([(false, Current)], log.Starts);
    }

    [Fact]
    public async Task InstallBeforeSixtySecondsWaitsForUptime()
    {
        var (coordinator, package, host, _, scheduler) = Create(UpdateMode.Off);
        package.Result = new(true, null);
        coordinator.Start();
        await coordinator.CheckAsync(manual: true);
        await coordinator.InstallAsync(automatic: false);
        Assert.Equal(0, package.Installs);
        Assert.Equal(UpdateState.Installing, host.Status!.State);
        scheduler.Run(Minute);
        Assert.Equal(1, package.Installs);
    }

    [Fact]
    public async Task FailedInstallResumesRefreshAndOffersRetry()
    {
        var (coordinator, package, host, log, scheduler) = Create(UpdateMode.Off);
        var busy = unchecked((int)0x80073D02);
        package.Result = new(true, null);
        package.InstallResult = busy;
        coordinator.Start();
        scheduler.Run(Minute);
        await coordinator.CheckAsync(manual: true);
        await coordinator.InstallAsync(automatic: false);
        Assert.Equal(1, host.Resumed);
        Assert.Equal(new UpdateStatus(UpdateState.InstallFailed, Current, host.Status!.CheckedAt, busy), host.Status);
        Assert.Equal([(busy, 1)], log.InstallFailures);
    }

    [Fact]
    public async Task DrainTimeoutReturnsToAvailableWithoutInstalling()
    {
        var (coordinator, package, host, log, scheduler) = Create(UpdateMode.Off);
        package.Result = new(true, null);
        host.PauseResult = false;
        coordinator.Start();
        scheduler.Run(Minute);
        await coordinator.CheckAsync(manual: true);
        await coordinator.InstallAsync(automatic: false);
        Assert.Equal(0, package.Installs);
        Assert.Equal(1, host.Resumed);
        Assert.Equal(UpdateState.Available, host.Status!.State);
        Assert.Empty(log.Starts);
    }

    [Fact]
    public async Task CheckFailuresShowCodeOnlyForManualAndCoalesceLogs()
    {
        var (coordinator, package, host, log, scheduler) = Create(UpdateMode.Always);
        package.Result = new(false, Offline);
        coordinator.Start();
        scheduler.Run(Minute);
        Assert.Equal(UpdateState.CheckFailed, host.Status!.State);
        Assert.Null(host.Status.ErrorCode);
        for (var i = 0; i < 12; i++) scheduler.Run(Interval);
        Assert.Equal([(Offline, 1), (Offline, 12)], log.CheckFailures);
        package.Result = new(false, null);
        scheduler.Run(Interval);
        package.Result = new(false, Offline);
        await coordinator.CheckAsync(manual: true);
        Assert.Equal([(Offline, 1), (Offline, 12), (Offline, 1)], log.CheckFailures);
        Assert.Equal(Offline, host.Status!.ErrorCode);
    }

    [Fact]
    public void SwitchingToOffCancelsPendingAutomaticInstall()
    {
        var (coordinator, package, host, _, scheduler) = Create(UpdateMode.Always);
        package.Result = new(true, null);
        coordinator.Start();
        scheduler.Run(Minute);
        Assert.Equal(UpdateState.Ready, host.Status!.State);
        host.Mode = UpdateMode.Off;
        host.WindowHidden = true;
        coordinator.Reevaluate();
        Assert.Equal(0, package.Installs);
        Assert.Equal(UpdateState.Available, host.Status!.State);
    }

    [Fact]
    public async Task LoopGuardSuspendsAutomaticInstall()
    {
        var (coordinator, package, host, log, scheduler) = Create(UpdateMode.Always, new(false, Current));
        host.WindowHidden = true;
        package.Result = new(true, null);
        coordinator.Start();
        Assert.Equal(UpdateState.NotApplied, host.Status!.State);
        scheduler.Run(Minute);
        Assert.Equal(0, package.Installs);
        Assert.Equal(UpdateState.NotApplied, host.Status!.State);
        Assert.Empty(log.Applications);
        await coordinator.InstallAsync(automatic: false);
        Assert.Equal(1, package.Installs);
    }

    [Fact]
    public void RelaunchAfterUpdateLogsApplied()
    {
        var (coordinator, _, host, log, _) = Create(UpdateMode.Always, new(true, "2026.10.602.0"));
        coordinator.Start();
        Assert.Equal([("2026.10.602.0", Current)], log.Applications);
        Assert.Equal(UpdateState.Idle, host.Status!.State);
    }

    [Theory]
    [InlineData((int)UpdateSupport.NotPackaged, (int)UpdateState.NotPackaged)]
    [InlineData((int)UpdateSupport.NoFeed, (int)UpdateState.NoFeed)]
    public async Task UnsupportedPackageNeverChecks(int support, int state)
    {
        var (coordinator, package, host, _, scheduler) = Create(UpdateMode.Always);
        package.Support = (UpdateSupport)support;
        coordinator.Start();
        scheduler.Run(Minute);
        scheduler.Run(Interval);
        await coordinator.CheckAsync(manual: true);
        Assert.Equal(0, package.Checks);
        Assert.Equal((UpdateState)state, host.Status!.State);
    }

    [Fact]
    public void LaunchArgumentsParse()
    {
        Assert.Equal(new UpdateLaunch(true, "2026.10.602.0"), UpdateLaunch.Parse(["AiUsage.exe", "--background", "--updated-from=2026.10.602.0"]));
        Assert.Equal(new UpdateLaunch(false, null), UpdateLaunch.Parse(["AiUsage.exe", "--updated-from=..\\x"]));
    }

    [Theory]
    [InlineData((int)UpdateState.NotPackaged, null, "Updates unavailable in development build")]
    [InlineData((int)UpdateState.NoFeed, null, "Updates unavailable without an update feed")]
    [InlineData((int)UpdateState.Idle, null, "Not checked yet")]
    [InlineData((int)UpdateState.Checking, null, "Checking…")]
    [InlineData((int)UpdateState.Available, null, "New version available")]
    [InlineData((int)UpdateState.Ready, null, "Update ready")]
    [InlineData((int)UpdateState.Installing, null, "Installing…")]
    [InlineData((int)UpdateState.CheckFailed, null, "Check failed · will retry")]
    [InlineData((int)UpdateState.CheckFailed, unchecked((int)0x80072EFE), "Check failed · 0x80072EFE")]
    [InlineData((int)UpdateState.InstallFailed, unchecked((int)0x80073D02), "Install failed · 0x80073D02")]
    [InlineData((int)UpdateState.NotApplied, null, "Automatic update didn't apply · install manually")]
    public void StatusTextMatchesSpec(int state, int? code, string text) =>
        Assert.Equal(text, LedgerFormat.UpdateText(new((UpdateState)state, Current, null, code)));

    [Fact]
    public void UpToDateTextShowsCheckTime() =>
        Assert.Matches(@"^Up to date · checked \d\d:\d\d$", LedgerFormat.UpdateText(new(UpdateState.UpToDate, Current, DateTimeOffset.UtcNow)));

    private static (UpdateCoordinator, FakePackage, FakeHost, FakeLog, ManualScheduler) Create(UpdateMode mode, UpdateLaunch? launch = null)
    {
        var package = new FakePackage(); var host = new FakeHost { Mode = mode }; var log = new FakeLog(); var scheduler = new ManualScheduler();
        return (new UpdateCoordinator(package, host, log, scheduler, TimeProvider.System, launch ?? new(false, null)), package, host, log, scheduler);
    }

    private sealed class FakePackage : IPackageUpdates
    {
        public UpdateSupport Support { get; set; } = UpdateSupport.Supported;
        public string? CurrentVersion => Current;
        public UpdateCheckResult Result { get; set; } = new(false, null);
        public TaskCompletionSource<UpdateCheckResult>? Hold { get; set; }
        public int? InstallResult { get; set; }
        public int Checks { get; private set; }
        public int Installs { get; private set; }
        public string? LastArguments { get; private set; }
        public Task<UpdateCheckResult> CheckAsync(CancellationToken ct) { Checks++; return Hold?.Task ?? Task.FromResult(Result); }
        public Task<int?> InstallAsync(string restartArguments, CancellationToken ct) { Installs++; LastArguments = restartArguments; return Task.FromResult(InstallResult); }
    }

    private sealed class FakeHost : IUpdateHost
    {
        public UpdateMode Mode { get; set; }
        public bool WindowHidden { get; set; }
        public bool SignInActive { get; set; }
        public bool PauseResult { get; set; } = true;
        public int Resumed { get; private set; }
        public UpdateStatus? Status { get; private set; }
        public Task<bool> PauseRefreshAsync(CancellationToken ct) => Task.FromResult(PauseResult);
        public void ResumeRefresh() => Resumed++;
        public Task PublishAsync(UpdateStatus status) { Status = status; return Task.CompletedTask; }
    }

    private sealed class FakeLog : IUpdateLog
    {
        public List<(int, int)> CheckFailures { get; } = [];
        public List<(bool, string)> Starts { get; } = [];
        public List<(int, int)> InstallFailures { get; } = [];
        public List<(string, string)> Applications { get; } = [];
        public void CheckFailed(int errorCode, int consecutive) => CheckFailures.Add((errorCode, consecutive));
        public void InstallStarted(bool automatic, string version) => Starts.Add((automatic, version));
        public void InstallFailed(int errorCode, int consecutive) => InstallFailures.Add((errorCode, consecutive));
        public void Applied(string fromVersion, string toVersion) => Applications.Add((fromVersion, toVersion));
    }
}
