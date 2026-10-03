using System.Security.Cryptography;
using System.Text;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Providers;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AccountRegistryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.AccountRegistry.Tests", Guid.NewGuid().ToString("N"));
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RegistryProtectsBindingsAndRetainsSignedOutIdentity()
    {
        var account = Account("synthetic-private-identity");
        var registry = new AccountRegistry(root);
        await registry.UpdateAsync(s => s with { Accounts = [account] }, Token);
        await registry.UpdateAsync(s => s with { Accounts = [s.Accounts[0] with { Connected = false }] }, Token);
        var restored = Assert.Single((await new AccountRegistry(root).ReadAsync(Token)).Accounts);
        Assert.Equal(account.Id, restored.Id);
        Assert.Equal(account.Identity, restored.Identity);
        Assert.False(restored.Connected);
        Assert.DoesNotContain("synthetic-private", Encoding.UTF8.GetString(await File.ReadAllBytesAsync(Path.Combine(root, "accounts.state"), Token)));
    }

    [Fact]
    public async Task InterruptedPublicationRecoversExactAccountAndStorageReferences()
    {
        var original = Account("first");
        var next = Account("second");
        var registry = new AccountRegistry(root);
        await registry.UpdateAsync(s => s with { Accounts = [original] }, Token);
        var interrupted = new AccountRegistry(root, afterStage: () => throw new IOException("synthetic fault"));
        await Assert.ThrowsAsync<ProviderException>(() => interrupted.UpdateAsync(s => s with { Accounts = [.. s.Accounts, next] }, Token));
        var recovered = await registry.ReadAsync(Token);
        Assert.Equal(new[] { original, next }, recovered.Accounts);
        Assert.False(File.Exists(Path.Combine(root, "accounts.state.pending")));
    }

    [Theory]
    [InlineData("duplicate-id")]
    [InlineData("duplicate-storage")]
    [InlineData("duplicate-identity")]
    [InlineData("pending-active")]
    public async Task ConflictingReferencesCannotReplaceTheCommittedRegistry(string conflict)
    {
        var first = Account("first");
        var second = Account("second");
        var registry = new AccountRegistry(root);
        await registry.UpdateAsync(s => s with { Accounts = [first] }, Token);
        second = conflict switch
        {
            "duplicate-id" => second with { Id = first.Id },
            "duplicate-storage" => second with { StorageId = first.StorageId },
            "duplicate-identity" => second with { Identity = first.Identity },
            _ => second
        };
        await Assert.ThrowsAsync<ProviderException>(() => registry.UpdateAsync(s => s with
        {
            Accounts = [first, second],
            Pending = conflict == "pending-active" ? [new(first.Provider, first.StorageId)] : []
        }, Token));
        Assert.Equal(first, Assert.Single((await registry.ReadAsync(Token)).Accounts));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CorruptAndNewerRegistryBytesArePreserved(bool newer)
    {
        Directory.CreateDirectory(root);
        var bytes = newer ? ProtectedData.Protect("{\"Version\":99}"u8.ToArray(),
            "AiUsage.AccountRegistry.v1"u8.ToArray(), DataProtectionScope.CurrentUser) : new byte[] { 1, 2, 3 };
        var path = Path.Combine(root, "accounts.state");
        await File.WriteAllBytesAsync(path, bytes, Token);
        var registry = new AccountRegistry(root);
        await Assert.ThrowsAsync<ProviderException>(() => registry.ReadAsync(Token));
        await Assert.ThrowsAsync<ProviderException>(() => registry.UpdateAsync(_ => new(), Token));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path, Token));
    }

    internal static AccountRecord Account(string identity) => new(Guid.NewGuid(), "claude", Guid.NewGuid(), new(identity, "synthetic-org"));
    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
