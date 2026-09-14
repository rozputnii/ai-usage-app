using AiUsage.Features.Dashboard;
using AiUsage.Core.Dashboard;
using AiUsage.Core.Providers.Codex;
using AiUsage.Infrastructure.Providers.Codex;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AiUsage;

public partial class App : Application
{
    private static readonly ResourceLoader TextResources = new();
    private IHost? host;
    private MainWindow? window;
    private Task? startTask;
    private Task? stopTask;
    private bool finalClose;

    public App() => InitializeComponent();

    internal static string Resource(string name) => TextResources.GetString(name);

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            await StartAsync();
            if (stopTask is null)
            {
                ShowWindow();
                // Restoring a stored account must not block activation or fail startup.
                await window!.ViewModel.LoadAsync();
            }
        }
        catch
        {
            Environment.ExitCode = 1;
            try
            {
                try
                {
                    if (window is not null)
                        await window.ViewModel.StopAsync();
                }
                finally { DisposeHost(); }
            }
            catch { Environment.ExitCode = 1; }
            try
            {
                if (!finalClose)
                {
                    window ??= CreateFailureWindow();
                    window.ShowFailure("StartupFailure/Text");
                    window.Activate();
                }
            }
            catch
            {
                // Resource/XAML failure can also prevent recovery UI. Exit without exposing it.
                finalClose = true;
                Exit();
            }
        }
    }

    private Task StartAsync() => startTask ??= StartCoreAsync();

    private async Task StartCoreAsync()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, WindowOwnedLifetime>());
        // LocalState is the app-owned root; the DPAPI-protected Codex grant lives only there.
        builder.Services.AddCodexProductSession(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "providers"));
        var dispatcher = new DesktopDispatcher(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        builder.Services.AddSingleton(services => new DashboardViewModel(
            services.GetRequiredService<DashboardWorkflow>(), OpenInBrowser, StopAsync, Resource, dispatcher.InvokeAsync, ShowWindow));
        builder.Services.AddSingleton<MainWindow>();
        host = builder.Build();
        window = host.Services.GetRequiredService<MainWindow>();
        window.AppWindow.Closing += OnClosing;
        await host.StartAsync();
    }

    private MainWindow CreateFailureWindow()
    {
        var dispatcher = new DesktopDispatcher(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        var failureWindow = new MainWindow(new DashboardViewModel(null, OpenInBrowser, StopAsync, Resource, dispatcher.InvokeAsync, ShowWindow));
        failureWindow.AppWindow.Closing += (_, _) => _ = StopAsync();
        return failureWindow;
    }

    private void ShowWindow()
    {
        if (stopTask is not null || finalClose || window is null)
            return;
        window.AppWindow.Show();
        if (window.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();
        window.Activate();
    }

    private static void OpenInBrowser(Uri url)
    {
        try
        {
            using var browser = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
            if (browser is null)
                throw new CodexException(CodexFailureKind.BrowserCallbackUnavailable);
        }
        catch (System.ComponentModel.Win32Exception) { throw new CodexException(CodexFailureKind.BrowserCallbackUnavailable); }
        catch (InvalidOperationException) { throw new CodexException(CodexFailureKind.BrowserCallbackUnavailable); }
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (finalClose)
            return;
        args.Cancel = true;
        if (stopTask is not null)
            return;
        if (host is not null)
            sender.Hide();
        else
            _ = StopAsync();
    }

    private Task StopAsync() => stopTask ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        // Yield before final close so the cached task is visible to reentrant requests.
        await Task.Yield();
        window?.DisableExit();
        try
        {
            if (startTask is not null)
                await startTask;
            if (window is not null)
                await window.ViewModel.StopAsync();
            if (host is not null)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await host.StopAsync(timeout.Token);
            }
        }
        catch
        {
            Environment.ExitCode = 1;
            window?.ShowFailure("ShutdownFailure/Text");
        }
        finally
        {
            try
            {
                DisposeHost();
            }
            catch
            {
                Environment.ExitCode = 1;
                window?.ShowFailure("ShutdownFailure/Text");
            }
            finalClose = true;
            window?.Close();
        }
    }

    private void DisposeHost()
    {
        var ownedHost = host;
        host = null;
        ownedHost?.Dispose();
    }

    // WinUI owns process signals. Host callbacks must not register ConsoleLifetime handlers
    // or recursively call the App stop operation which is already awaiting Host.StopAsync.
    private sealed class WindowOwnedLifetime : IHostLifetime
    {
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
