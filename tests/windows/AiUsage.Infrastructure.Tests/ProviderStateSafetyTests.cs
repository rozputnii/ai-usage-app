using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Antigravity;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ProviderStateSafetyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.StateSafety.Tests", Guid.NewGuid().ToString("N"));
    private static readonly byte[] Entropy = "AiUsage.Codex.Grant.v1"u8.ToArray();
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private string StoreDirectory => Path.Combine(root, "state");
    private string GrantPath => Path.Combine(StoreDirectory, "codex.grant");

    [Theory]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Antigravity")]
    [InlineData("Codex")]
    public async Task EveryProviderRefusesARedirectedStateDirectory(string provider)
    {
        var destination = Path.Combine(root, "outside");
        Directory.CreateDirectory(destination);
        await CreateJunctionAsync(StoreDirectory, destination);
        try
        {
            var error = await Assert.ThrowsAsync<ProviderException>(async () =>
            {
                await using var lease = await AcquireAsync(provider, StoreDirectory);
            });
            Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
            Assert.Empty(Directory.EnumerateFileSystemEntries(destination));
        }
        finally { Directory.Delete(StoreDirectory); }
    }

    [Theory]
    [InlineData("codex.grant")]
    [InlineData("codex.grant.pending")]
    [InlineData("codex.grant.new")]
    [InlineData("codex.grant.lock")]
    public async Task GrantPathsCannotRedirectReadWriteOrDelete(string name)
    {
        Directory.CreateDirectory(StoreDirectory);
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        var sentinel = Path.Combine(outside, "preserve.txt");
        await File.WriteAllTextAsync(sentinel, "synthetic-owner-data", Token);
        var redirected = Path.Combine(StoreDirectory, name);
        await CreateJunctionAsync(redirected, outside);
        try
        {
            var store = new CodexGrantStore(StoreDirectory);
            Assert.Equal(ProviderFailureKind.RecoveryRequired, (await Assert.ThrowsAsync<ProviderException>(() => store.ReadAsync(Token))).Kind);
            await Assert.ThrowsAsync<ProviderException>(() => store.WriteAsync(new("synthetic-account", "synthetic-refresh"), Token));
            await Assert.ThrowsAsync<ProviderException>(() => store.DeleteAsync(Token));
            Assert.Equal("synthetic-owner-data", await File.ReadAllTextAsync(sentinel, Token));
            Assert.Single(Directory.GetFileSystemEntries(outside));
        }
        finally { Directory.Delete(redirected); }
    }

    [Theory]
    [InlineData("root")]
    [InlineData("codex.quota.json")]
    [InlineData("codex.quota.json.new")]
    [InlineData("codex.quota.json.lock")]
    public async Task CachePathsCannotRedirectReadWriteOrDelete(string name)
    {
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        var sentinel = Path.Combine(outside, "preserve.txt");
        await File.WriteAllTextAsync(sentinel, "synthetic-owner-data", Token);
        if (name != "root") Directory.CreateDirectory(StoreDirectory);
        var redirected = name == "root" ? StoreDirectory : Path.Combine(StoreDirectory, name);
        await CreateJunctionAsync(redirected, outside);
        try
        {
            var cache = new CodexQuotaCache(StoreDirectory);
            await Assert.ThrowsAsync<ProviderException>(() => cache.ReadAsync(Token));
            await Assert.ThrowsAsync<ProviderException>(() => cache.WriteAsync(new(new QuotaSnapshot(DateTimeOffset.UtcNow, null, [], null, null, null, null), DateTimeOffset.UtcNow), Token));
            await Assert.ThrowsAsync<ProviderException>(() => cache.DeleteAsync(Token));
            Assert.Equal("synthetic-owner-data", await File.ReadAllTextAsync(sentinel, Token));
            Assert.Single(Directory.GetFileSystemEntries(outside));
        }
        finally { Directory.Delete(redirected); }
    }

    [Fact]
    public async Task OldCodexRecordLoadsAndNewWritesKeepTheOriginalFormatAndEntropy()
    {
        Directory.CreateDirectory(StoreDirectory);
        // Original cfb9ceb writer's serialized shape and DPAPI purpose, independent of new types.
        const string oldJson = """{"v":1,"account":"synthetic-workspace","refresh":"synthetic-old"}""";
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(oldJson), Entropy, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(GrantPath, bytes, Token);
        var store = new CodexGrantStore(StoreDirectory);
        Assert.Equal(new("synthetic-workspace", "synthetic-old"), await store.ReadAsync(Token));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(GrantPath, Token));
        await store.WriteAsync(new("synthetic-workspace", "synthetic-next"), Token);
        var plaintext = ProtectedData.Unprotect(await File.ReadAllBytesAsync(GrantPath, Token), Entropy, DataProtectionScope.CurrentUser);
        try
        {
            Assert.Equal("""{"v":1,"account":"synthetic-workspace","refresh":"synthetic-next"}""", Encoding.UTF8.GetString(plaintext));
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FullyStagedCodexGrantRecoversBeforeOrAfterPromotion(bool alreadyPromoted)
    {
        var store = new CodexGrantStore(StoreDirectory);
        await store.WriteAsync(new("synthetic-workspace", "synthetic-old"), Token);
        var faulty = new CodexGrantStore(StoreDirectory, () => throw new IOException("Synthetic interruption."));
        Assert.Equal(ProviderFailureKind.StorageUnavailable, (await Assert.ThrowsAsync<ProviderException>(() =>
            faulty.WriteAsync(new("synthetic-workspace", "synthetic-rotated"), Token))).Kind);
        Assert.True(File.Exists(GrantPath + ".pending"));
        if (alreadyPromoted)
        {
            var journal = ReadJournal();
            await File.WriteAllBytesAsync(GrantPath, journal.Ciphertext, Token);
        }
        else
        {
            // A torn replacement payload is safe to reconstruct from a fully flushed journal.
            await File.WriteAllBytesAsync(GrantPath + ".new", [1, 2, 3], Token);
        }
        Assert.Equal("synthetic-rotated", (await store.ReadAsync(Token))!.RefreshToken);
        Assert.False(File.Exists(GrantPath + ".pending"));
        Assert.False(File.Exists(GrantPath + ".new"));
    }

    [Theory]
    [InlineData("torn")]
    [InlineData("unrelated")]
    [InlineData("legacy")]
    public async Task AmbiguousGenerationBlocksOldGrantReplayAndPreservesEvidence(string failure)
    {
        var store = new CodexGrantStore(StoreDirectory);
        await store.WriteAsync(new("synthetic-workspace", "synthetic-old"), Token);
        var before = await File.ReadAllBytesAsync(GrantPath, Token);
        var pending = GrantPath + (failure == "legacy" ? ".new" : ".pending");
        byte[] bytes = [1, 2, 3];
        if (failure == "unrelated")
        {
            var faulty = new CodexGrantStore(StoreDirectory, () => throw new IOException("Synthetic interruption."));
            await Assert.ThrowsAsync<ProviderException>(() => faulty.WriteAsync(new("synthetic-workspace", "synthetic-next"), Token));
            var journal = ReadJournal() with { ParentRevision = Guid.NewGuid() };
            bytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(journal, ProviderPendingJson.Default.ProviderPendingGeneration), Entropy, DataProtectionScope.CurrentUser);
        }
        await File.WriteAllBytesAsync(pending, bytes, Token);
        Assert.Equal(ProviderFailureKind.RecoveryRequired, (await Assert.ThrowsAsync<ProviderException>(() => store.ReadAsync(Token))).Kind);
        await Assert.ThrowsAsync<ProviderException>(() => store.WriteAsync(new("synthetic-workspace", "synthetic-overwrite"), Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(GrantPath, Token));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(pending, Token));
        await store.DeleteAsync(Token);
        Assert.Null(await store.ReadAsync(Token));
    }

    [Fact]
    public async Task ExclusiveLeaseAndRevisionRejectConcurrentOrObsoleteUpdates()
    {
        var store = new CodexGrantStore(StoreDirectory);
        await using var lease = await store.AcquireAsync(Token);
        Assert.Equal(ProviderFailureKind.StorageUnavailable, (await Assert.ThrowsAsync<ProviderException>(() => store.AcquireAsync(Token))).Kind);
        var first = await lease.SaveAsync(CodexGrantStore.Record(new("synthetic-workspace", "synthetic-old")), null, Token);
        await lease.SaveAsync(CodexGrantStore.Record(new("synthetic-workspace", "synthetic-next")), CodexGrantStore.Revision(first), Token);
        Assert.Equal(ProviderFailureKind.RecoveryRequired, (await Assert.ThrowsAsync<ProviderException>(() =>
            lease.SaveAsync(CodexGrantStore.Record(new("synthetic-workspace", "synthetic-stale")), CodexGrantStore.Revision(first), Token))).Kind);
        Assert.Equal("synthetic-next", (await lease.LoadAsync(Token))!.RefreshToken);
    }

    [Fact]
    public async Task FailedDisconnectRetainsTheStagedSuccessorInsteadOfExposingTheOldGrant()
    {
        var store = new CodexGrantStore(StoreDirectory);
        await store.WriteAsync(new("synthetic-workspace", "synthetic-old"), Token);
        var faulty = new CodexGrantStore(StoreDirectory, () => throw new IOException("Synthetic interruption."));
        await Assert.ThrowsAsync<ProviderException>(() => faulty.WriteAsync(new("synthetic-workspace", "synthetic-rotated"), Token));
        await using (var held = new FileStream(GrantPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.Equal(ProviderFailureKind.GrantNotRemoved, (await Assert.ThrowsAsync<ProviderException>(() => store.DeleteAsync(Token))).Kind);
            Assert.True(File.Exists(GrantPath + ".pending"));
        }
        Assert.Equal("synthetic-rotated", (await store.ReadAsync(Token))!.RefreshToken);
    }

    private ProviderPendingGeneration ReadJournal()
    {
        var plaintext = ProtectedData.Unprotect(File.ReadAllBytes(GrantPath + ".pending"), Entropy, DataProtectionScope.CurrentUser);
        try { return JsonSerializer.Deserialize(plaintext, ProviderPendingJson.Default.ProviderPendingGeneration)!; }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private static async Task<IAsyncDisposable> AcquireAsync(string provider, string directory) => provider switch
    {
        "Claude" => await new ClaudeStateStore(directory).AcquireAsync(Token),
        "Copilot" => await new CopilotStateStore(directory).AcquireAsync(Token),
        "Antigravity" => await new AntigravityStateStore(directory).AcquireAsync(Token),
        "Codex" => await new CodexGrantStore(directory).AcquireAsync(Token),
        _ => throw new ArgumentException("Unknown synthetic provider.")
    };

    private static async Task CreateJunctionAsync(string link, string target)
    {
        var start = new ProcessStartInfo("cmd.exe") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "/c", "mklink", "/J", link, target }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync(Token);
        Assert.Equal(0, process.ExitCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
