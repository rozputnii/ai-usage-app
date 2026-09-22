using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Copilot;

public static class CopilotServiceCollectionExtensions
{
    internal static IServiceCollection AddCopilotIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(ProviderTransportOptions.Default);
        services.AddHttpClient<CopilotAuthClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(p => ProviderTransport.CreateHandler(p.GetRequiredService<ProviderTransportOptions>())).RemoveAllLoggers();
        services.AddHttpClient<CopilotQuotaClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(p => ProviderTransport.CreateHandler(p.GetRequiredService<ProviderTransportOptions>())).RemoveAllLoggers();
        services.AddHttpClient<CopilotHistoryClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(p => ProviderTransport.CreateHandler(p.GetRequiredService<ProviderTransportOptions>())).RemoveAllLoggers();
        return services;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static IServiceCollection AddCopilotProductSession(this IServiceCollection services, string ownedStateDirectory)
    {
        services.AddCopilotIntegration();
        services.TryAddSingleton(new CopilotStateStore(ownedStateDirectory));
        services.TryAddSingleton(p => new CopilotSession(p.GetRequiredService<CopilotAuthClient>(), p.GetRequiredService<CopilotQuotaClient>(), p.GetRequiredService<CopilotStateStore>(), p.GetRequiredService<TimeProvider>(), p.GetRequiredService<CopilotHistoryClient>()));
        return services;
    }

}
