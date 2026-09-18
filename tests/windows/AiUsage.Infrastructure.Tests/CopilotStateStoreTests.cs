using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Copilot;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CopilotStateStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "AiUsage.Copilot.Tests", Guid.NewGuid().ToString("N"));
    internal static CopilotStoredState State(string refresh = "synthetic-refresh") => new()
    {
        AccountId = "123", AccessToken = refresh
    };

    [Fact]
    public async Task StoredGrantIsEncryptedAndDeletionPreservesCodexAndUnknownFiles()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "codex.grant"), "synthetic-existing-codex", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory, "unrelated.json"), "preserve", TestContext.Current.CancellationToken);
        var store = new CopilotStateStore(directory);
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var quota = CopilotQuotaParser.Parse("""{"quota_snapshots":{"premium_interactions":{"entitlement":300,"remaining":200,"quota_id":"opaque/id","quota_remaining":199.5,"overage_permitted":false}}}"""u8.ToArray(), DateTimeOffset.UtcNow);
            var saved = await lease.SaveAsync(State() with { CachedQuota = quota }, null, TestContext.Current.CancellationToken);
            var read = await lease.LoadAsync(TestContext.Current.CancellationToken);
            Assert.Equal(saved.Revision, read!.Revision);
            Assert.Equal("synthetic-refresh", read.AccessToken);
            Assert.Equal(new QuotaSourceDetails("opaque/id", 199.5m, null, false), read.CachedQuota!.Groups[0].Windows[0].SourceDetails);
            Assert.Equal(new QuotaAmount(200, 100, 300, "requests"), read.CachedQuota.Groups[0].Windows[0].Amount);
            var ciphertext = await File.ReadAllBytesAsync(Path.Combine(directory, "copilot.state"), TestContext.Current.CancellationToken);
            Assert.DoesNotContain("synthetic", Encoding.UTF8.GetString(ciphertext));
            Assert.False(File.Exists(Path.Combine(directory, "copilot.state.pending")));
            await lease.DeleteAsync(TestContext.Current.CancellationToken);
            Assert.Null(await lease.LoadAsync(TestContext.Current.CancellationToken));
        }
        Assert.Equal("synthetic-existing-codex", await File.ReadAllTextAsync(Path.Combine(directory, "codex.grant"), TestContext.Current.CancellationToken));
        Assert.True(File.Exists(Path.Combine(directory, "unrelated.json")));
    }

    [Fact]
    public async Task FullyStagedGrantRecoversAfterInterruptedReplacement()
    {
        var interrupt = false;
        var store = new CopilotStateStore(directory, () => { if (interrupt) throw new IOException("synthetic fault"); });
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var old = await lease.SaveAsync(State(), null, TestContext.Current.CancellationToken);
            interrupt = true;
            var error = await Assert.ThrowsAsync<CopilotException>(() => lease.SaveAsync(State("synthetic-rotated"), old.Revision, TestContext.Current.CancellationToken));
            Assert.Equal(ProviderFailureKind.StorageUnavailable, error.Kind);
        }
        await using var recovered = await new CopilotStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-rotated", (await recovered.LoadAsync(TestContext.Current.CancellationToken))!.AccessToken);
        Assert.False(File.Exists(Path.Combine(directory, "copilot.state.pending")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsupportedOrCorruptRecordsArePreservedAndCannotBeOverwritten(bool futureVersion)
    {
        Directory.CreateDirectory(directory);
        var bytes = futureVersion
            ? ProtectedData.Protect("{\"Version\":99}"u8.ToArray(), "AiUsage.Copilot.State.v1"u8.ToArray(), DataProtectionScope.CurrentUser)
            : "synthetic-corrupt-ciphertext"u8.ToArray();
        var path = Path.Combine(directory, "copilot.state");
        await File.WriteAllBytesAsync(path, bytes, TestContext.Current.CancellationToken);
        await using var lease = await new CopilotStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<CopilotException>(() => lease.LoadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
        await Assert.ThrowsAsync<CopilotException>(() => lease.SaveAsync(State(), null, TestContext.Current.CancellationToken));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TornPendingRecordRequiresRecovery()
    {
        var store = new CopilotStateStore(directory);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        await lease.SaveAsync(State(), null, TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "copilot.state.pending"), [1, 2, 3], TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.RecoveryRequired,
            (await Assert.ThrowsAsync<CopilotException>(() => lease.LoadAsync(TestContext.Current.CancellationToken))).Kind);
        Assert.True(File.Exists(Path.Combine(directory, "copilot.state.pending")));
    }

    [Fact]
    public async Task AnExclusiveLeaseAndRevisionCheckPreventLostUpdates()
    {
        var store = new CopilotStateStore(directory);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<CopilotException>(() => store.AcquireAsync(TestContext.Current.CancellationToken));
        var old = await lease.SaveAsync(State(), null, TestContext.Current.CancellationToken);
        var next = await lease.SaveAsync(State("synthetic-next"), old.Revision, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<CopilotException>(() => lease.SaveAsync(State("synthetic-obsolete"), old.Revision, TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
        Assert.Equal(next.Revision, (await lease.LoadAsync(TestContext.Current.CancellationToken))!.Revision);
    }

    [Fact]
    public async Task ReparseRootsCannotRedirectCredentialWritesOrDeletion()
    {
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, "destination");
        Directory.CreateDirectory(destination);
        var junction = Path.Combine(directory, "junction");
        // Directory symbolic links need a host privilege; an NTFS junction does not.
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/c mklink /J \"{junction}\" \"{destination}\"", CreateNoWindow = true, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true
        })!;
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, process.ExitCode);
        try
        {
            var error = await Assert.ThrowsAsync<CopilotException>(() => new CopilotStateStore(junction).AcquireAsync(TestContext.Current.CancellationToken));
            Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
            Assert.Empty(Directory.EnumerateFiles(destination));
        }
        finally { Directory.Delete(junction); }
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
