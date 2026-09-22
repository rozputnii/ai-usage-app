using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ProviderBoundaryTests
{
    [Fact]
    public void PublicConsumersCannotConstructAProviderWithTheirOwnHttpPipeline()
    {
        var constructors = typeof(CodexServiceCollectionExtensions).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetConstructors());
        Assert.DoesNotContain(constructors, constructor => constructor.GetParameters()
            .Any(parameter => parameter.ParameterType == typeof(HttpClient)));
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
    public void EveryRegisteredTransportDisablesRedirectsCookiesAndLogging(string name)
    {
        using var services = new ServiceCollection().AddCodexIntegration().AddClaudeIntegration()
            .AddCopilotIntegration().AddAntigravityIntegration().BuildServiceProvider();
        var handler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
        while (handler is DelegatingHandler delegating)
        {
            Assert.DoesNotContain("Logging", handler.GetType().Namespace ?? "", StringComparison.Ordinal);
            handler = Assert.IsAssignableFrom<HttpMessageHandler>(delegating.InnerHandler);
        }
        var primary = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.False(primary.AllowAutoRedirect);
        Assert.False(primary.UseCookies);
        Assert.Equal(TimeSpan.FromMinutes(5), primary.PooledConnectionLifetime);
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void ProductRegistrationsResolveSingletonSessionsWithoutSigningIn()
    {
        if (!OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(Path.GetTempPath(), "AiUsage-boundary-" + Guid.NewGuid());
        using var services = new ServiceCollection().AddCodexProductSession(directory)
            .AddClaudeProductSession(directory).AddCopilotProductSession(directory)
            .AddAntigravityProductSession(directory).BuildServiceProvider();
        foreach (var type in new[] { typeof(CodexSession), typeof(ClaudeSession), typeof(CopilotSession), typeof(AntigravitySession) })
        {
            var session = Assert.IsAssignableFrom<IProviderSession>(services.GetRequiredService(type));
            Assert.Same(session, services.GetRequiredService(type));
            Assert.Equal(ProviderSessionState.NotConnected, session.State);
            Assert.False(session.HasStoredGrant);
        }
        Assert.False(Directory.Exists(directory));
    }
}
