using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Antigravity;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ProviderStateCompatibilityTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.Compatibility.Tests", Guid.NewGuid().ToString("N"));
    private static readonly Guid Revision = Guid.Parse("18908074-7397-48ea-a9c3-c47368899910");

    // Frozen pre-extraction serialized records (cfb9ceb). Protect with the old entropy and
    // CurrentUser on the test machine; encrypted fixtures themselves would not be portable.
    [Theory]
    [InlineData("Claude", """{"Version":1,"Revision":"18908074-7397-48ea-a9c3-c47368899910","ParentRevision":null,"Identity":{"AccountId":"synthetic-account","OrganizationId":"synthetic-organization"},"RefreshToken":"synthetic-refresh","NeedsReauthentication":false,"CachedQuota":null}""")]
    [InlineData("Copilot", """{"Version":1,"Revision":"18908074-7397-48ea-a9c3-c47368899910","ParentRevision":null,"AccountId":"123","AccessToken":"synthetic-access","ExpiresAt":null,"NeedsReauthentication":false,"CachedQuota":null}""")]
    [InlineData("Antigravity", """{"Version":1,"Revision":"18908074-7397-48ea-a9c3-c47368899910","ParentRevision":null,"AccountId":"synthetic-account","RefreshToken":"synthetic-refresh","ProjectId":"synthetic-project","Tier":null,"NeedsReauthentication":false,"CachedQuota":null}""")]
    public async Task ExistingProtectedRecordLoadsAndRetainsItsExactShape(string provider, string json)
    {
        Directory.CreateDirectory(root);
        var entropy = Encoding.UTF8.GetBytes($"AiUsage.{provider}.State.v1");
        var file = Path.Combine(root, provider.ToLowerInvariant() + ".state");
        await File.WriteAllBytesAsync(file, ProtectedData.Protect(Encoding.UTF8.GetBytes(json), entropy, DataProtectionScope.CurrentUser), TestContext.Current.CancellationToken);
        switch (provider)
        {
            case "Claude":
                await using (var lease = await new ClaudeStateStore(root).AcquireAsync(TestContext.Current.CancellationToken))
                {
                    var state = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
                    Assert.Equal(Revision, state.Revision);
                    Assert.Equal("synthetic-refresh", state.RefreshToken);
                    await lease.SaveAsync(state, state.Revision, TestContext.Current.CancellationToken);
                }
                break;
            case "Copilot":
                await using (var lease = await new CopilotStateStore(root).AcquireAsync(TestContext.Current.CancellationToken))
                {
                    var state = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
                    Assert.Equal(Revision, state.Revision);
                    Assert.Equal("synthetic-access", state.AccessToken);
                    await lease.SaveAsync(state, state.Revision, TestContext.Current.CancellationToken);
                }
                break;
            case "Antigravity":
                await using (var lease = await new AntigravityStateStore(root).AcquireAsync(TestContext.Current.CancellationToken))
                {
                    var state = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
                    Assert.Equal(Revision, state.Revision);
                    Assert.Equal("synthetic-project", state.ProjectId);
                    await lease.SaveAsync(state, state.Revision, TestContext.Current.CancellationToken);
                }
                break;
        }
        var bytes = ProtectedData.Unprotect(await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken), entropy, DataProtectionScope.CurrentUser);
        try
        {
            using var expected = JsonDocument.Parse(json);
            using var actual = JsonDocument.Parse(bytes);
            Assert.Equal(expected.RootElement.EnumerateObject().Select(p => p.Name).Order(), actual.RootElement.EnumerateObject().Select(p => p.Name).Order());
            foreach (var property in expected.RootElement.EnumerateObject().Where(p => p.Name is not ("Revision" or "ParentRevision")))
                Assert.True(JsonElement.DeepEquals(property.Value, actual.RootElement.GetProperty(property.Name)), property.Name);
            Assert.Equal(Revision, actual.RootElement.GetProperty("ParentRevision").GetGuid());
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
