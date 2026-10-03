using AiUsage.Composition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace AiUsage;

/// <summary>
/// Product composition by default; --demo selects isolated synthetic adapters.
/// Close hides to the tray; confirmed Exit drains provider work before disposing sessions.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "WinUI owns Application; StopCoreAsync and Ledger exit dispose diagnostics after host disposal.")]
public partial class App : Application
{
    private IHost? host;
    private LedgerShell? shell;
    private Task? stopTask;
    private readonly ApplicationDiagnostics diagnostics = new();

    public App()
    {
        diagnostics.Initialize(Environment.GetCommandLineArgs().Contains("--demo", StringComparer.Ordinal));
        UnhandledException += OnUnhandledException;
        try { DiagnosticProbe.BeforeXaml(); InitializeComponent(); }
        catch (Exception exception) { diagnostics.StartupFailure(exception); throw; }
        DebugSettings.BindingFailed += (_, _) => diagnostics.BindingFailure();
        DebugSettings.IsBindingTracingEnabled = true;
    }

    /// <summary>
    /// Unknown UI failures remain fatal. Only local, explicitly recoverable boundaries may continue.
    /// </summary>
    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = false;
        diagnostics.UnhandledFailure(e.Exception);
        // The pinned WinUI projection can report async-void faults and then continue even when
        // Handled is false. The isolated probe verifies this exit occurs only after forced capture.
        Environment.Exit(1);
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            diagnostics.Watch(DispatcherQueue.GetForCurrentThread());
            if (diagnostics.TryRunProbe(Exit)) return;
            var demo = Environment.GetCommandLineArgs().Contains("--demo", StringComparer.Ordinal);
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            diagnostics.Register(builder.Services);
            builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, WindowOwnedLifetime>());
            builder.Services.AddLedger();
            if (demo) builder.Services.AddLedgerDemo();
            else builder.Services.AddLiveLedgerServices();
            var disposalProbe = DiagnosticProbe.ConfigureHost(builder.Services);
            host = builder.Build();
            await host.StartAsync();
            if (disposalProbe) { await StopAsync(); return; }

            shell = LedgerRegistration.Start(host.Services, StopAsync);
            if (!demo) await host.Services.GetRequiredService<LedgerProductLifetime>().InitializeAsync();
        }
        catch (Exception error)
        {
            diagnostics.StartupFailure(error);
            Environment.ExitCode = 1;
            await StopAsync();
        }
    }

    private Task StopAsync() => stopTask ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        await Task.Yield();
        try
        {
            if (host is not null)
            {
                if (host.Services.GetService<LedgerProductLifetime>() is { } product)
                    await product.StopAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await host.StopAsync(timeout.Token);
            }
        }
        catch (Exception error)
        {
            diagnostics.ShutdownFailure(error);
            Environment.ExitCode = 1;
        }
        finally
        {
            try
            {
                shell?.Dispose();
                host?.Dispose();
            }
            catch (Exception error)
            {
                diagnostics.DisposalFailure(error);
                Environment.ExitCode = 1;
            }
            diagnostics.Dispose();
            Exit();
        }
    }

    // WinUI owns process signals; the host must not register console lifetime handlers.
    private sealed class WindowOwnedLifetime : IHostLifetime
    {
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
