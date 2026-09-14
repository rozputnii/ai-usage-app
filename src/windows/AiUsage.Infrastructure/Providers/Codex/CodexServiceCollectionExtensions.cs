using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Codex;

public static class CodexServiceCollectionExtensions
{
    /// <summary>Registers the Codex auth and quota clients with hardened transports.</summary>
    public static IServiceCollection AddCodexIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<CodexAuthClient>(ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
        services.AddHttpClient<CodexQuotaClient>(ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
        return services;
    }

    /// <summary>
    /// Adds the product session: the Codex clients plus a DPAPI CurrentUser grant store inside
    /// <paramref name="ownedStateDirectory"/>, which must be a directory this application owns.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static IServiceCollection AddCodexProductSession(this IServiceCollection services, string ownedStateDirectory)
    {
        services.AddCodexIntegration();
        services.TryAddSingleton(new CodexGrantStore(ownedStateDirectory));
        services.TryAddSingleton(new CodexQuotaCache(ownedStateDirectory));
        services.TryAddSingleton<CodexSession>();
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
