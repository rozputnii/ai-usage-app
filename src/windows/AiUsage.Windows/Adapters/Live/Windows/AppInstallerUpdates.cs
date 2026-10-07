using System.Runtime.InteropServices;
using AiUsage.Adapters.Live;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace AiUsage.Composition;

/// <summary>
/// AIU-046 Windows App Installer calls against the feed this package was installed from. Windows downloads, replaces
/// and relaunches; this class only asks and reports HRESULTs, never feed contents, URIs or exception text.
/// </summary>
internal sealed partial class AppInstallerUpdates : IPackageUpdates
{
    // RESTART_NO_CRASH | RESTART_NO_HANG | RESTART_NO_REBOOT: relaunch only after an update shutdown.
    private const uint RestartOnUpdateOnly = 1 | 2 | 8;
    private const int Fail = unchecked((int)0x80004005);

    public UpdateSupport Support
    {
        get
        {
            try { return Package.Current.GetAppInstallerInfo() is null ? UpdateSupport.NoFeed : UpdateSupport.Supported; }
            catch (InvalidOperationException) { return UpdateSupport.NotPackaged; }
        }
    }

    public string? CurrentVersion
    {
        get
        {
            try { var v = Package.Current.Id.Version; return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}"; }
            catch (InvalidOperationException) { return null; }
        }
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct)
    {
        try
        {
            var result = await Package.Current.CheckUpdateAvailabilityAsync().AsTask(ct);
            return result.Availability switch
            {
                PackageUpdateAvailability.Available or PackageUpdateAvailability.Required => new(true, null),
                PackageUpdateAvailability.Error => new(false, result.ExtendedError?.HResult ?? Fail),
                _ => new(false, null)
            };
        }
        catch (Exception error) when (error is not OperationCanceledException) { return new(false, error.HResult); }
    }

    public async Task<int?> InstallAsync(string restartArguments, CancellationToken ct)
    {
        var registered = RegisterApplicationRestart(restartArguments, RestartOnUpdateOnly);
        if (registered != 0) return registered;
        try
        {
            var feed = Package.Current.GetAppInstallerInfo()?.Uri;
            if (feed is null) { _ = UnregisterApplicationRestart(); return Fail; }
            var manager = new PackageManager();
            var result = await manager.AddPackageByAppInstallerFileAsync(feed, AddPackageByAppInstallerOptions.ForceTargetAppShutdown,
                manager.GetDefaultPackageVolume()).AsTask(ct);
            // Reaching here means Windows did not close this process: drop the restart registration either way.
            _ = UnregisterApplicationRestart();
            return result.ExtendedErrorCode?.HResult;
        }
        catch (Exception error)
        {
            _ = UnregisterApplicationRestart();
            if (error is OperationCanceledException) throw;
            return error.HResult;
        }
    }

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int RegisterApplicationRestart(string commandLine, uint flags);

    [LibraryImport("kernel32.dll")]
    private static partial int UnregisterApplicationRestart();
}
