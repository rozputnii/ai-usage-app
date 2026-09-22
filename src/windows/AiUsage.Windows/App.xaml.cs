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
/// Product composition by default; --demo selects isolated synthetic adapters.
/// Close hides to the tray; confirmed Exit drains provider work before disposing sessions.
/// </summary>
public partial class App : Application
{
    private IHost? host;
    private MainWindow? window;
    private TrayPopupWindow? popup;
    private Task? stopTask;
    private readonly ApplicationDiagnostics diagnostics = new();
    internal ProviderCatalog Providers { get; private set; } = ProviderCatalog.Default;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var demo = Environment.GetCommandLineArgs().Contains("--demo", StringComparer.Ordinal);
            diagnostics.Initialize(demo);
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            diagnostics.Register(builder.Services);
            builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, WindowOwnedLifetime>());
            builder.Services.AddSingleton(DispatcherQueue.GetForCurrentThread());
            builder.Services.AddPresentationFeatures().AddPlatformServices();
            if (demo) builder.Services.AddDemoServices();
            else builder.Services.AddLiveServices();
            host = builder.Build();
            await host.StartAsync();

            var services = host.Services;
            Providers = services.GetRequiredService<ProviderCatalog>();
            Controls.CapabilityGate.Source = services.GetRequiredService<Features.Accounts.IUsageSource>();
            window = services.GetRequiredService<MainWindow>();
            services.GetRequiredService<Announcer>().Attach(window.LiveRegionElement);
            services.GetRequiredService<DialogService>().Attach(window.Root, services.GetRequiredService<PresentationFormatter>());
            services.GetRequiredService<DisplaySimulation>().Attach(window);
            services.GetRequiredService<AppLifetime>().Attach(window, StopAsync, ShowTrayPopup);
            window.Activate();

            // Both paths publish through the same presentation boundary; only product loads app-owned state.
            if (demo)
                await services.GetRequiredService<DemoScenarioController>().LoadScenarioAsync(DemoScenarioCatalog.DefaultScenarioId, openEntry: false);
            else
                await services.GetRequiredService<IProductLifecycle>().InitializeAsync();
        }
        catch (Exception error)
        {
            diagnostics.StartupFailure(error);
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
                if (host.Services.GetService<IProductLifecycle>() is { } product)
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
                window?.CloseForExit();
                host?.Dispose();
            }
            catch (Exception error)
            {
                diagnostics.DisposalFailure(error);
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
