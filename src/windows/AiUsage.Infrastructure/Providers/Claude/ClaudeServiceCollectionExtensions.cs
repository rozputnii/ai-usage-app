using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Claude;

public static class ClaudeServiceCollectionExtensions
{
    internal static IServiceCollection AddClaudeIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<ClaudeAuthClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(ProviderTransport.CreateHandler).RemoveAllLoggers();
        services.AddHttpClient<ClaudeQuotaClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(ProviderTransport.CreateHandler).RemoveAllLoggers();
        return services;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static IServiceCollection AddClaudeProductSession(this IServiceCollection services, string ownedStateDirectory)
    {
        services.AddClaudeIntegration();
        services.TryAddSingleton(new ClaudeStateStore(ownedStateDirectory));
        services.TryAddSingleton(p => new ClaudeSession(p.GetRequiredService<ClaudeAuthClient>(), p.GetRequiredService<ClaudeQuotaClient>(), p.GetRequiredService<ClaudeStateStore>(), p.GetRequiredService<TimeProvider>()));
        return services;
    }

}
