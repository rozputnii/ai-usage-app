using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Adapters.Live;

internal enum UpdateSupport { Supported, NotPackaged, NoFeed }

internal sealed record UpdateCheckResult(bool Available, int? ErrorCode);

/// <summary>Windows App Installer boundary; the desktop adapter owns the WinRT and kernel32 calls.</summary>
internal interface IPackageUpdates
{
    UpdateSupport Support { get; }
    string? CurrentVersion { get; }
    Task<UpdateCheckResult> CheckAsync(CancellationToken ct);
    /// <summary>Returns the failure HRESULT; on success Windows ends the process, so null only reaches fakes.</summary>
    Task<int?> InstallAsync(string restartArguments, CancellationToken ct);
}

/// <summary>The desktop lifetime as seen by updates: preferences, window, sign-in, refresh pause and the snapshot.</summary>
internal interface IUpdateHost
{
    UpdateMode Mode { get; }
    bool WindowHidden { get; }
    bool SignInActive { get; }
    Task<bool> PauseRefreshAsync(CancellationToken ct);
    void ResumeRefresh();
    Task PublishAsync(UpdateStatus status);
}

/// <summary>T-043 update records; recurring failures arrive already coalesced.</summary>
internal interface IUpdateLog
{
    void CheckFailed(int errorCode, int consecutive);
    void InstallStarted(bool automatic, string version);
    void InstallFailed(int errorCode, int consecutive);
    void Applied(string fromVersion, string toVersion);
}

/// <summary>Arguments the restart registration hands back after an update.</summary>
internal sealed record UpdateLaunch(bool Background, string? UpdatedFrom)
{
    private const string UpdatedFromPrefix = "--updated-from=";

    public static UpdateLaunch Parse(IEnumerable<string> args)
    {
        var background = false;
        string? from = null;
        foreach (var arg in args)
        {
            if (arg == "--background") background = true;
            else if (arg.StartsWith(UpdatedFromPrefix, StringComparison.Ordinal) && Version.TryParse(arg[UpdatedFromPrefix.Length..], out var version) &&
                version.Revision >= 0 && version.ToString() == arg[UpdatedFromPrefix.Length..])
                from = version.ToString();
        }
        return new(background, from);
    }
}

/// <summary>
/// T-046 rules: when to check, when an available update installs, and failure and loop handling.
/// All calls arrive on the UI dispatcher, so the state needs no lock.
/// </summary>
internal sealed class UpdateCoordinator(IPackageUpdates package, IUpdateHost host, IUpdateLog log,
    ILedgerScheduler scheduler, TimeProvider time, UpdateLaunch launch) : IDisposable
{
    internal static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(60);
    internal static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private const int LogEvery = 12;
    private readonly CancellationTokenSource stop = new();
    private IDisposable? first;
    private IDisposable? next;
    private Task? checking;
    private bool supported;
    private bool manualJoined;
    private bool uptime;
    private bool suspended;
    private bool installing;
    private bool disposed;
    private bool? pendingInstall;
    private int checkFailures;
    private int installFailures;
    private UpdateMode mode;
    private UpdateState settled;
    private string version = string.Empty;

    public UpdateStatus Status { get; private set; } = UpdateStatus.NotPackaged;

    public void Start()
    {
        mode = host.Mode;
        if (package.Support != UpdateSupport.Supported)
        {
            Publish(new(package.Support == UpdateSupport.NoFeed ? UpdateState.NoFeed : UpdateState.NotPackaged, package.CurrentVersion));
            return;
        }
        supported = true;
        version = package.CurrentVersion ?? string.Empty;
        var state = UpdateState.Idle;
        // Windows relaunched the same version: the update did not apply, so never retry it automatically in this process.
        if (launch.UpdatedFrom == version) { suspended = true; state = UpdateState.NotApplied; }
        else if (launch.UpdatedFrom is { } from) log.Applied(from, version);
        Publish(new(state, version));
        // The restart registration only relaunches a process that has run for 60 seconds.
        first = scheduler.Schedule(FirstCheckDelay, UptimeReached);
    }

    public Task CheckAsync(bool manual)
    {
        if (!supported || installing || disposed) return Task.CompletedTask;
        if (checking is { IsCompleted: false }) { manualJoined |= manual; return checking; }
        manualJoined = manual;
        return checking = CheckCoreAsync();
    }

    public Task InstallAsync(bool automatic)
    {
        if (!supported || installing || disposed || pendingInstall is not null) return Task.CompletedTask;
        if (Status.State is not (UpdateState.Available or UpdateState.Ready or UpdateState.InstallFailed or UpdateState.NotApplied)) return Task.CompletedTask;
        if (automatic && (suspended || mode == UpdateMode.Off)) return Task.CompletedTask;
        if (!uptime)
        {
            pendingInstall = automatic;
            settled = Status.State;
            Publish(Status with { State = UpdateState.Installing, ErrorCode = null });
            return Task.CompletedTask;
        }
        return InstallCoreAsync(automatic);
    }

    /// <summary>Idempotent: re-reads mode, window and sign-in; called on every source change and window show/hide.</summary>
    public void Reevaluate()
    {
        if (!supported || disposed) return;
        if (host.Mode != mode)
        {
            mode = host.Mode;
            if (uptime) ScheduleNext();
            if (mode == UpdateMode.Off && Status.State == UpdateState.Ready) Publish(Status with { State = UpdateState.Available });
            else if (mode != UpdateMode.Off && Status.State == UpdateState.Available && !suspended) Publish(Status with { State = UpdateState.Ready });
        }
        if (Status.State == UpdateState.Ready && host.WindowHidden && !host.SignInActive && !installing)
            _ = InstallAsync(automatic: true);
    }

    private void UptimeReached()
    {
        first = null;
        uptime = true;
        if (pendingInstall is { } automatic) { pendingInstall = null; _ = InstallCoreAsync(automatic); }
        if (mode != UpdateMode.Off) _ = CheckAsync(manual: false);
        if (mode == UpdateMode.Always) ScheduleNext();
    }

    private void ScheduleNext()
    {
        next?.Dispose();
        next = mode == UpdateMode.Always && !disposed ? scheduler.Schedule(CheckInterval, () => { ScheduleNext(); _ = CheckAsync(manual: false); }) : null;
    }

    private async Task CheckCoreAsync()
    {
        var before = Status;
        Publish(Status with { State = UpdateState.Checking, ErrorCode = null });
        UpdateCheckResult result;
        try { result = await package.CheckAsync(stop.Token); }
        catch (Exception error) when (error is not OperationCanceledException) { result = new(false, error.HResult); }
        if (disposed) return;
        if (result.ErrorCode is { } code)
        {
            if (++checkFailures == 1 || checkFailures % LogEvery == 0) log.CheckFailed(code, checkFailures);
            Publish(Status with { State = UpdateState.CheckFailed, ErrorCode = manualJoined ? code : null });
            return;
        }
        checkFailures = 0;
        if (result.Available && before.State == UpdateState.InstallFailed && suspended)
        {
            // Keep the failure and its code visible; Retry stays the only way to install again.
            Publish(before with { CheckedAt = time.GetUtcNow() });
            return;
        }
        var state = !result.Available ? UpdateState.UpToDate : suspended ? UpdateState.NotApplied :
            mode != UpdateMode.Off ? UpdateState.Ready : UpdateState.Available;
        Publish(new(state, version, time.GetUtcNow()));
        Reevaluate();
    }

    private async Task InstallCoreAsync(bool automatic)
    {
        installing = true;
        if (Status.State != UpdateState.Installing) settled = Status.State;
        Publish(Status with { State = UpdateState.Installing, ErrorCode = null });
        try
        {
            if (!await host.PauseRefreshAsync(stop.Token))
            {
                // Provider work did not drain in time; nothing was installed.
                host.ResumeRefresh();
                Publish(Status with { State = settled });
                return;
            }
            log.InstallStarted(automatic, version);
            int? code;
            try { code = await package.InstallAsync("--updated-from=" + version + (host.WindowHidden ? " --background" : string.Empty), stop.Token); }
            catch (Exception error) when (error is not OperationCanceledException) { code = error.HResult; }
            // A failed or returning install suspends automatic installs in this process; only the button tries again.
            // Network failures (WinINet/WinHTTP 12000-12199, such as 0x80072EFE) are retried by the next automatic check.
            if (code is not { } network || !IsNetworkFailure(network)) suspended = true;
            host.ResumeRefresh();
            if (code is not { } failure)
            {
                // Windows normally closes this process before the call returns; a return means nothing was applied here.
                Publish(Status with { State = UpdateState.NotApplied, ErrorCode = null });
                return;
            }
            if (++installFailures == 1 || installFailures % LogEvery == 0) log.InstallFailed(failure, installFailures);
            Publish(Status with { State = UpdateState.InstallFailed, ErrorCode = failure });
        }
        finally { installing = false; }
    }

    private static bool IsNetworkFailure(int code) => (code & unchecked((int)0xFFFF0000)) == unchecked((int)0x80070000) && (code & 0xFFFF) is >= 12000 and < 12200;

    private void Publish(UpdateStatus status)
    {
        Status = status;
        _ = host.PublishAsync(status);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        first?.Dispose();
        next?.Dispose();
        stop.Cancel();
        stop.Dispose();
    }
}
