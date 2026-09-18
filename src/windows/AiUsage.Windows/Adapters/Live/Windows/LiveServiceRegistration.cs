using System.Diagnostics;
using AiUsage.Adapters.Live;
using AiUsage.Core.Usage;
using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.Settings;
using AiUsage.Features.SystemStatusPage;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.Composition;

internal static class LiveServiceRegistration
{
    public static IServiceCollection AddLiveServices(this IServiceCollection services)
    {
        string root;
        try
        {
            _ = Windows.ApplicationModel.Package.Current.Id;
            root = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        catch (InvalidOperationException)
        {
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage", "Development");
            if (Environment.GetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY") is { Length: > 0 } isolated)
                root = Path.GetFullPath(isolated);
        }
        services.AddCodexProductSession(Path.Combine(root, "providers"));
        services.AddClaudeProductSession(Path.Combine(root, "providers"));
        services.AddCopilotProductSession(Path.Combine(root, "providers"));
        services.AddSingleton(p => new LiveUsageSource(new Dictionary<string, IProviderSession>
        {
            ["codex"] = p.GetRequiredService<CodexDashboardSession>(),
            ["claude"] = p.GetRequiredService<ClaudeSession>(),
            ["copilot"] = p.GetRequiredService<CopilotSession>()
        }));
        services.AddSingleton<IUsageSource>(p => p.GetRequiredService<LiveUsageSource>());
        services.AddSingleton<IConnectionFlow>(p => new LiveConnectionFlow(p.GetRequiredService<LiveUsageSource>(),
            uri => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })));
        services.AddSingleton(new PresentationPreferenceFile(root));
        services.AddSingleton(p =>
        {
            var source = p.GetRequiredService<LiveUsageSource>();
            var file = p.GetRequiredService<PresentationPreferenceFile>();
            return source.PreferenceStore = new LivePreferenceStore(source, file.ReadAsync, file.WriteAsync);
        });
        services.AddSingleton<IPreferenceStore>(p => p.GetRequiredService<LivePreferenceStore>());
        services.AddSingleton<IClock, LiveClock>();
        services.AddSingleton<UnavailableServices>();
        services.AddSingleton<IHistorySource>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<ICliImportService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IDiagnosticsService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IDataManagementService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IRecoveryService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IUpdateService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<INotificationPreview>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IProductLifecycle, ProductLifecycle>();
        return services;
    }

}
