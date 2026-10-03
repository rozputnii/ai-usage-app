using AiUsage.Adapters.Live;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using AiUsage.Infrastructure.Diagnostics;
using Microsoft.UI.Dispatching;

namespace AiUsage.Composition;

/// <summary>Desktop fault projection, available before host creation and after host disposal.</summary>
internal sealed class ApplicationDiagnostics : IDiagnosticSink, IDisposable
{
    private FileDiagnostics? sink;
    private DispatcherWatchdog? watchdog;
    internal static ApplicationDiagnostics? Current { get; private set; }
    internal FileDiagnostics? Files => sink;

    public void Initialize(bool demo)
    {
        Current = this;
        try
        {
            var root = ApplicationStateDirectory.Get(demo);
            if (!new OwnedDataDeletion(root).Pending)
                sink = new FileDiagnostics(root, mode: demo ? "demo" : Packaged() ? "packaged" : "development");
        }
        catch (Exception) { /* An unavailable state root must not prevent desktop startup or exit. */ }
        AppDomain.CurrentDomain.UnhandledException += ManagedFailure;
        TaskScheduler.UnobservedTaskException += UnobservedFailure;
    }

    public void Register(IServiceCollection services)
    {
        if (sink is not null) services.AddFileDiagnostics(sink);
        services.AddSingleton(this).AddSingleton<IDiagnosticSink>(this);
    }
    public void Watch(DispatcherQueue queue) => watchdog = new(queue, this);
    public void RegisterSurface(IServiceCollection services, bool demo)
    {
        if (sink is { } files)
            services.AddSingleton<Features.SystemStatusPage.IDiagnosticsService>(p => new FileDiagnosticsService(
                demo ? p.GetRequiredService<Features.Demo.DemoDiagnosticsService>() : p.GetRequiredService<UnavailableServices>(), files));
    }
    public void BindingFailure() => Signal(DiagnosticEvent.BindingFailure, DiagnosticSeverity.Warning);
    public void DispatchRejected() => Signal(DiagnosticEvent.DispatchRejected, DiagnosticSeverity.Warning);
    public void CommandFailure(Exception exception) => sink?.Fatal(DiagnosticEvent.OperationFailure, exception, true);
    public void WindowVisibility(bool hidden)
    {
        if (watchdog is not null) watchdog.Active = !hidden;
        Signal(hidden ? DiagnosticEvent.WindowHidden : DiagnosticEvent.WindowShown);
    }
    public void DispatcherRecovered(double durationMs) => sink?.RecordDuration(DiagnosticEvent.DispatcherRecovered, durationMs);
    public void NavigationCompleted() => Signal(DiagnosticEvent.NavigationCompleted);
    public bool TryRunProbe(Action exit) => DiagnosticProbe.TryStart(this, exit);
    public static void RunAnimation(Action action)
    {
        try { action(); }
        catch (Exception exception) { Current?.sink?.Fatal(DiagnosticEvent.AnimationFailure, exception, true); throw; }
    }
    public void StartupFailure(Exception error) => sink?.Fatal(DiagnosticEvent.StartupFailure, error, true);
    public void ShutdownFailure(Exception error) => sink?.Fatal(DiagnosticEvent.ShutdownFailure, error, true);
    public void DisposalFailure(Exception error) => sink?.Fatal(DiagnosticEvent.DisposalFailure, error, true);
    public void TrayFailure(Exception error) => sink?.Failure(DiagnosticEvent.TrayFailure, error);
    public void UnhandledFailure(Exception? error) => sink?.Fatal(DiagnosticEvent.UnhandledFailure, error, true);
    public void Record(DiagnosticEvent eventCode, DiagnosticCategory category) => sink?.Record(eventCode, category);
    public void Failure(DiagnosticEvent eventCode, Exception exception) => sink?.Failure(eventCode, exception);
    public IDiagnosticOperation? Begin(DiagnosticOperation operation, Guid? accountReference = null) => sink?.Begin(operation, accountReference);
    public void Signal(DiagnosticEvent eventCode, DiagnosticSeverity severity = DiagnosticSeverity.Information) => sink?.Signal(eventCode, severity);
    private void ManagedFailure(object sender, System.UnhandledExceptionEventArgs args) => sink?.Fatal(DiagnosticEvent.UnhandledFailure, args.ExceptionObject as Exception, args.IsTerminating);
    private void UnobservedFailure(object? sender, UnobservedTaskExceptionEventArgs args) => sink?.Failure(DiagnosticEvent.BackgroundFailure, args.Exception);
    public void Dispose()
    {
        watchdog?.Dispose();
        sink?.Dispose();
        // Global hooks stay available through the final host/window disposal; FileDiagnostics.Fatal remains usable.
    }
    public void StopForDeletion()
    {
        watchdog?.Dispose(); watchdog = null;
        sink?.Dispose(); sink = null;
    }
    private static bool Packaged()
    {
        try { _ = Windows.ApplicationModel.Package.Current.Id; return true; }
        catch (InvalidOperationException) { return false; }
    }
}

internal static class ApplicationStateDirectory
{
    public static string Get(bool demo = false)
    {
        if (!demo)
        {
            try
            {
                _ = Windows.ApplicationModel.Package.Current.Id;
                return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            }
            catch (InvalidOperationException) { }
        }
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage", "Development");
        if (Environment.GetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY") is { Length: > 0 } isolated)
            root = Path.GetFullPath(isolated);
        return demo ? Path.Combine(root, "Demo") : root;
    }
}
