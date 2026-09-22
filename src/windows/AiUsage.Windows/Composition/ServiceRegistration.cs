using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.Demo;
using AiUsage.Features.History;
using AiUsage.Features.Overview;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.Settings;
using AiUsage.Features.Settings.Appearance;
using AiUsage.Features.Settings.DataPrivacy;
using AiUsage.Features.Settings.Monitoring;
using AiUsage.Features.Settings.Updates;
using AiUsage.Features.Shell;
using AiUsage.Features.SystemStatusPage;
using AiUsage.Features.Tray;
using AiUsage.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.Composition;

/// <summary>
/// Shared presentation and platform composition, plus isolated synthetic demo adapters.
/// Product adapters are registered separately by AddLiveServices.
/// </summary>
internal static class ServiceRegistration
{
    public static IServiceCollection AddPresentationFeatures(this IServiceCollection collection)
    {
        collection.AddSingleton(ProviderCatalog.Default);
        collection.AddSingleton(provider => new PresentationContext(
            provider.GetRequiredService<IUsageSource>(),
            provider.GetRequiredService<IUiDispatcher>(),
            provider.GetRequiredService<IClock>(),
            provider.GetRequiredService<ITextResources>(),
            provider.GetRequiredService<IAnnouncer>(),
            provider.GetRequiredService<INavigationService>(),
            provider.GetRequiredService<IDialogService>(),
            provider.GetRequiredService<IMotionSettings>(),
            provider.GetRequiredService<ProviderCatalog>()));
        collection.AddSingleton(provider => provider.GetRequiredService<PresentationContext>().Format);
        collection.AddSingleton(provider => new ToastViewModel(provider.GetRequiredService<PresentationContext>()));
        collection.AddSingleton(provider => new ShellViewModel(
            provider.GetRequiredService<PresentationContext>(),
            provider.GetRequiredService<IThemeService>(),
            provider.GetRequiredService<IAppLifetime>(),
            provider.GetRequiredService<ToastViewModel>(),
            isDemo: provider.GetService<DemoScenarioController>() is not null));
        collection.AddSingleton(provider => new OverviewViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IHistorySource>(), provider.GetRequiredService<IPreferenceStore>()));
        collection.AddSingleton(provider => new AccountsViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IHistorySource>(),
            provider.GetRequiredService<IDataManagementService>(), provider.GetRequiredService<IPreferenceStore>()));
        collection.AddSingleton(provider => new HistoryViewModel(provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IHistorySource>()));
        collection.AddSingleton(provider => new AppearanceSettingsViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IPreferenceStore>(), provider.GetRequiredService<IThemeService>()));
        collection.AddSingleton(provider => new MonitoringSettingsViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IPreferenceStore>(),
            provider.GetRequiredService<INotificationPreview>(), provider.GetRequiredService<ToastViewModel>(), provider.GetService<DemoScenarioController>()));
        collection.AddSingleton(provider => new DataPrivacyViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IPreferenceStore>(), provider.GetRequiredService<IDataManagementService>()));
        collection.AddSingleton(provider => new UpdatesViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IUpdateService>(), provider.GetService<DemoScenarioController>()));
        collection.AddSingleton(provider => new SettingsViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<AppearanceSettingsViewModel>(),
            provider.GetRequiredService<MonitoringSettingsViewModel>(), provider.GetRequiredService<DataPrivacyViewModel>(), provider.GetRequiredService<UpdatesViewModel>()));
        collection.AddSingleton(provider => new SystemStatusViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IDiagnosticsService>(),
            () => provider.GetRequiredService<ShellViewModel>().ExitCommand.ExecuteAsync(null)));
        collection.AddSingleton(provider => new RecoveryViewModel(provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IRecoveryService>()));
        collection.AddSingleton(provider => new TrayViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IAppLifetime>(),
            () => provider.GetRequiredService<ShellViewModel>().RefreshAllFromTrayAsync(),
            () => provider.GetRequiredService<ShellViewModel>().ExitCommand.ExecuteAsync(null)));
        collection.AddSingleton(provider => new AddAccountViewModel(
            provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<IConnectionFlow>(),
            new CliImportViewModel(provider.GetRequiredService<PresentationContext>(), provider.GetRequiredService<ICliImportService>()),
            provider.GetService<DemoScenarioController>()));
        return collection;
    }

    public static IServiceCollection AddPlatformServices(this IServiceCollection collection)
    {
        collection.AddSingleton<UiDispatcher>();
        collection.AddSingleton<IUiDispatcher>(provider => provider.GetRequiredService<UiDispatcher>());
        collection.AddSingleton<ResourceText>();
        collection.AddSingleton<ITextResources>(provider => provider.GetRequiredService<ResourceText>());
        collection.AddSingleton<NavigationService>();
        collection.AddSingleton<INavigationService>(provider => provider.GetRequiredService<NavigationService>());
        collection.AddSingleton<Announcer>();
        collection.AddSingleton<IAnnouncer>(provider => provider.GetRequiredService<Announcer>());
        collection.AddSingleton<MotionSettings>();
        collection.AddSingleton<IMotionSettings>(provider => provider.GetRequiredService<MotionSettings>());
        collection.AddSingleton<ThemeService>();
        collection.AddSingleton<IThemeService>(provider => provider.GetRequiredService<ThemeService>());
        collection.AddSingleton<AppLifetime>();
        collection.AddSingleton<IAppLifetime>(provider => provider.GetRequiredService<AppLifetime>());
        collection.AddSingleton<DisplaySimulation>();
        collection.AddSingleton<IDisplaySimulation>(provider => provider.GetRequiredService<DisplaySimulation>());
        collection.AddSingleton(provider => new DialogService(provider.GetRequiredService<AddAccountViewModel>, provider.GetRequiredService<ThemeService>()));
        collection.AddSingleton<IDialogService>(provider => provider.GetRequiredService<DialogService>());
        collection.AddSingleton<MainWindow>();
        return collection;
    }

    /// <summary>Deterministic synthetic adapters (Features/Demo). The demo clock renders in UTC so exact times are checkable.</summary>
    public static IServiceCollection AddDemoServices(this IServiceCollection collection)
    {
        collection.AddSingleton(_ => new DemoClock(timeZone: TimeZoneInfo.Utc));
        collection.AddSingleton<IClock>(services => services.GetRequiredService<DemoClock>());
        collection.AddSingleton(services => new DemoState(services.GetRequiredService<DemoClock>(), DemoScenarioCatalog.DefaultScenarioId));
        collection.AddSingleton(services => new DemoUsageSource(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoConnectionFlow(services.GetRequiredService<DemoState>(), services.GetRequiredService<ProviderCatalog>()));
        collection.AddSingleton(services => new DemoHistorySource(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoPreferenceStore(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoNotificationPreview(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoCliImportService(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoDiagnosticsService(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoDataManagementService(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoRecoveryService(services.GetRequiredService<DemoState>()));
        collection.AddSingleton(services => new DemoUpdateService(services.GetRequiredService<DemoState>()));
        collection.AddSingleton<IUsageSource>(services => services.GetRequiredService<DemoUsageSource>());
        collection.AddSingleton<IConnectionFlow>(services => services.GetRequiredService<DemoConnectionFlow>());
        collection.AddSingleton<IHistorySource>(services => services.GetRequiredService<DemoHistorySource>());
        collection.AddSingleton<IPreferenceStore>(services => services.GetRequiredService<DemoPreferenceStore>());
        collection.AddSingleton<INotificationPreview>(services => services.GetRequiredService<DemoNotificationPreview>());
        collection.AddSingleton<ICliImportService>(services => services.GetRequiredService<DemoCliImportService>());
        collection.AddSingleton<IDiagnosticsService>(services => services.GetRequiredService<DemoDiagnosticsService>());
        collection.AddSingleton<IDataManagementService>(services => services.GetRequiredService<DemoDataManagementService>());
        collection.AddSingleton<IRecoveryService>(services => services.GetRequiredService<DemoRecoveryService>());
        collection.AddSingleton<IUpdateService>(services => services.GetRequiredService<DemoUpdateService>());
        collection.AddSingleton(services => new DemoScenarioController(
            services.GetRequiredService<DemoState>(), services.GetRequiredService<DemoConnectionFlow>(), services.GetRequiredService<DemoUpdateService>(),
            services.GetRequiredService<DemoCliImportService>(), services.GetRequiredService<INavigationService>(), services.GetRequiredService<IDialogService>()));
        collection.AddSingleton(services => new DemoControlViewModel(
            services.GetRequiredService<DemoScenarioController>(), services.GetRequiredService<IDisplaySimulation>(),
            services.GetRequiredService<IAppLifetime>(), services.GetRequiredService<PresentationFormatter>()));
        return collection;
    }
}
