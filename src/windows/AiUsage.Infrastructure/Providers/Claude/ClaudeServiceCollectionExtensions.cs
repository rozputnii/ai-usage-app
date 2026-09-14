using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Claude;

public static class ClaudeServiceCollectionExtensions
{
    public static IServiceCollection AddClaudeIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<ClaudeAuthClient>(ConfigureClient).ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
        services.AddHttpClient<ClaudeQuotaClient>(ConfigureClient).ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
        return services;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static IServiceCollection AddClaudeProductSession(this IServiceCollection services, string ownedStateDirectory)
    {
        services.AddClaudeIntegration();
        services.TryAddSingleton(new ClaudeStateStore(ownedStateDirectory));
        services.TryAddSingleton<ClaudeSession>();
        return services;
    }

    private static void ConfigureClient(HttpClient client) => client.Timeout = Timeout.InfiniteTimeSpan;
    private static HttpMessageHandler CreateHandler() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };
}
