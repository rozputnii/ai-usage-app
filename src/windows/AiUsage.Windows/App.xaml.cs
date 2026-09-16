using AiUsage.Composition;
using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using AiUsage.Features.Tray;
using AiUsage.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace AiUsage;

/// <summary>
/// Composition root. AIU-010 default startup is mock-only: presentation features, WinUI platform services and the
/// deterministic demo adapters. Close hides to the tray; only a confirmed Exit ends the process.
/// </summary>
public partial class App : Application
{
    private IHost? host;
    private MainWindow? window;
    private TrayPopupWindow? popup;
    private Task? stopTask;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, WindowOwnedLifetime>());
            builder.Services.AddSingleton(DispatcherQueue.GetForCurrentThread());
            builder.Services.AddPresentationFeatures().AddPlatformServices().AddDemoServices();
            host = builder.Build();
            await host.StartAsync();

            var services = host.Services;
            window = services.GetRequiredService<MainWindow>();
            services.GetRequiredService<Announcer>().Attach(window.LiveRegionElement);
            services.GetRequiredService<DialogService>().Attach(window.Root, services.GetRequiredService<PresentationFormatter>());
            services.GetRequiredService<DisplaySimulation>().Attach(window);
            services.GetRequiredService<AppLifetime>().Attach(window, StopAsync, ShowTrayPopup);
            window.Activate();

            // Initial load shows layout-matched skeletons for the prototype's load latency before the F02 seed appears.
            _ = services.GetRequiredService<DemoScenarioController>().LoadScenarioAsync(DemoScenarioCatalog.DefaultScenarioId, openEntry: false);
        }
        catch
        {
            Environment.ExitCode = 1;
            await StopAsync();
        }
    }

    private void ShowTrayPopup()
    {
        if (host is null || stopTask is not null)
            return;
        var services = host.Services;
        popup ??= new TrayPopupWindow(services.GetRequiredService<TrayViewModel>(), services.GetRequiredService<ThemeService>(),
            services.GetRequiredService<ITextResources>().Get("AppTitle"));
        popup.ShowNearTray();
    }

    private Task StopAsync() => stopTask ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        await Task.Yield();
        try
        {
            popup?.CloseForExit();
            if (host is not null)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await host.StopAsync(timeout.Token);
            }
        }
        catch
        {
            Environment.ExitCode = 1;
        }
        finally
        {
            try
            {
                window?.CloseForExit();
                host?.Dispose();
            }
            catch
            {
                Environment.ExitCode = 1;
            }
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
