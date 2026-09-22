using System.Net;
using System.Net.Http.Headers;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ProviderTransportOptionsTests
{
    [Fact]
    public void AllRegistrationsUseTheSameConfiguredPoolLifetime()
    {
        var options = new ProviderTransportOptions { PooledConnectionLifetime = TimeSpan.FromSeconds(42) };
        using var services = Register(new ServiceCollection().AddSingleton(options)).BuildServiceProvider();
        Assert.Same(options, Assert.Single(services.GetServices<ProviderTransportOptions>()));
        foreach (var name in new[] { "CodexAuthClient", "CodexQuotaClient", "ClaudeAuthClient", "ClaudeQuotaClient",
            "CopilotAuthClient", "CopilotQuotaClient", "AntigravityAuthClient", "AntigravityQuotaClient" })
        {
            var handler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
            while (handler is DelegatingHandler delegating) handler = delegating.InnerHandler!;
            Assert.Equal(TimeSpan.FromSeconds(42), Assert.IsType<SocketsHttpHandler>(handler).PooledConnectionLifetime);
        }
    }

    [Theory]
    [InlineData("CodexAuthClient")]
    [InlineData("CodexQuotaClient")]
    [InlineData("ClaudeAuthClient")]
    [InlineData("ClaudeQuotaClient")]
    [InlineData("CopilotAuthClient")]
    [InlineData("CopilotQuotaClient")]
    [InlineData("AntigravityAuthClient")]
    [InlineData("AntigravityQuotaClient")]
    public async Task EveryClientHonorsTheConfiguredRequestDeadline(string name)
    {
        using var server = new CodexTestServer(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            return CodexTestServer.Json("{}", HttpStatusCode.ServiceUnavailable);
        });
        var collection = Register(new ServiceCollection().AddSingleton(new ProviderTransportOptions
            { RequestDeadline = TimeSpan.FromMilliseconds(20) }));
        collection.Configure<HttpClientFactoryOptions>(name, options =>
            options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = server));
        using var services = collection.BuildServiceProvider();
        var error = await Record.ExceptionAsync(() => RequestAsync(name, services, TestContext.Current.CancellationToken));
        var kind = error switch
        {
            CodexException codex => codex.Kind,
            ProviderException provider => provider.Kind,
            _ => throw new InvalidOperationException("Expected a classified provider failure.", error)
        };
        Assert.Equal(ProviderFailureKind.Timeout, kind);
        Assert.Equal(1, server.Calls);
    }

    [Theory]
    [InlineData("ClaudeQuotaClient", null, 7)]
    [InlineData("CopilotQuotaClient", null, 7)]
    [InlineData("AntigravityQuotaClient", null, 7)]
    [InlineData("ClaudeQuotaClient", 30, 30)]
    [InlineData("CopilotQuotaClient", 30, 30)]
    [InlineData("AntigravityQuotaClient", 30, 30)]
    public async Task FallbackGatesRequestsWithoutOverridingProviderRetryAfter(string name, int? retryAfter, int expectedSeconds)
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((_, _) =>
        {
            var response = CodexTestServer.Json("{}", HttpStatusCode.TooManyRequests);
            if (retryAfter is { } seconds) response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(seconds));
            return Task.FromResult(response);
        });
        var collection = Register(new ServiceCollection().AddSingleton<TimeProvider>(clock)
            .AddSingleton(new ProviderTransportOptions { RetryAfterFallback = TimeSpan.FromSeconds(7) }));
        collection.Configure<HttpClientFactoryOptions>(name, options =>
            options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = server));
        using var services = collection.BuildServiceProvider();
        // Typed clients are transient; keep this operation bound to one instance to exercise its throttle.
        var request = QuotaRequest(name, services);
        await Assert.ThrowsAsync<ProviderException>(request);
        clock.Current += TimeSpan.FromSeconds(expectedSeconds - 1);
        var throttled = await Assert.ThrowsAsync<ProviderException>(request);
        Assert.Equal(ProviderFailureKind.RateLimited, throttled.Kind);
        Assert.Equal(TimeSpan.FromSeconds(1), throttled.RetryAfter);
        Assert.Equal(1, server.Calls);
        clock.Current += TimeSpan.FromSeconds(1);
        await Assert.ThrowsAsync<ProviderException>(request);
        Assert.Equal(2, server.Calls);
    }

    private static IServiceCollection Register(IServiceCollection services) => services.AddCodexIntegration()
        .AddClaudeIntegration().AddCopilotIntegration().AddAntigravityIntegration();

    private static ClaudeCredentials ClaudeCredentials() => new("synthetic-access", "synthetic-refresh",
        new("synthetic-account", "synthetic-org"), DateTimeOffset.MaxValue);

    private static Func<Task> QuotaRequest(string name, IServiceProvider services)
    {
        var token = TestContext.Current.CancellationToken;
        return name switch
        {
            "ClaudeQuotaClient" => Bind(services.GetRequiredService<ClaudeQuotaClient>(),
                client => client.GetQuotaAsync(ClaudeCredentials(), token)),
            "CopilotQuotaClient" => Bind(services.GetRequiredService<CopilotQuotaClient>(),
                client => client.GetQuotaAsync(new("synthetic-access", "synthetic-account", DateTimeOffset.MaxValue), token)),
            "AntigravityQuotaClient" => Bind(services.GetRequiredService<AntigravityQuotaClient>(),
                client => client.GetQuotaAsync(new("synthetic-access", "synthetic-refresh", "synthetic-account",
                    "synthetic-project", null, DateTimeOffset.MaxValue), token)),
            _ => throw new ArgumentException("Unknown quota client.", nameof(name))
        };
    }

    private static Func<Task> Bind<T>(T client, Func<T, Task> request) => () => request(client);

    private static async Task RequestAsync(string name, IServiceProvider services, CancellationToken token)
    {
        using var credentials = CodexTestServer.Credentials();
        Task request = name switch
        {
            "CodexAuthClient" => services.GetRequiredService<CodexAuthClient>().BeginDeviceLoginAsync(token),
            "CodexQuotaClient" => services.GetRequiredService<CodexQuotaClient>().GetQuotaAsync(credentials, token),
            "ClaudeAuthClient" => services.GetRequiredService<ClaudeAuthClient>().RefreshAsync(ClaudeCredentials(), token),
            "CopilotAuthClient" => services.GetRequiredService<CopilotAuthClient>().GetIdentityAsync("synthetic-access", token),
            "AntigravityAuthClient" => services.GetRequiredService<AntigravityAuthClient>().GetIdentityAsync("synthetic-access", token),
            _ => QuotaRequest(name, services)()
        };
        await request;
    }
}
