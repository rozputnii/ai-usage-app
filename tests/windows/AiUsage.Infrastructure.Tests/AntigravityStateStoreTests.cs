using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AntigravityStateStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "AiUsage.Antigravity.Tests", Guid.NewGuid().ToString("N"));
    internal static AntigravityStoredState State(string refresh = "synthetic-refresh") => new()
    {
        AccountId = "104729", RefreshToken = refresh, ProjectId = "synthetic-project", Tier = "free-tier"
    };

    [Fact]
    public async Task StoredGrantIsEncryptedAndDeletionPreservesOtherProviderFiles()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "claude.state"), "synthetic-existing-claude", TestContext.Current.CancellationToken);
        var store = new AntigravityStateStore(directory);
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var quota = AntigravityQuotaParser.Parse(Encoding.UTF8.GetBytes(AntigravityProtocolTests.Summary), DateTimeOffset.UtcNow, "free-tier");
            var saved = await lease.SaveAsync(State() with { CachedQuota = quota }, null, TestContext.Current.CancellationToken);
            var read = await lease.LoadAsync(TestContext.Current.CancellationToken);
            Assert.Equal(saved.Revision, read!.Revision);
            Assert.Equal("synthetic-refresh", read.RefreshToken);
            Assert.Equal("synthetic-project", read.ProjectId);
            Assert.Equal("gemini-5h", read.CachedQuota!.Groups[0].Windows[0].SourceDetails!.QuotaId);
            var ciphertext = await File.ReadAllBytesAsync(Path.Combine(directory, "antigravity.state"), TestContext.Current.CancellationToken);
            Assert.DoesNotContain("synthetic", Encoding.UTF8.GetString(ciphertext));
            Assert.False(File.Exists(Path.Combine(directory, "antigravity.state.pending")));
            await lease.DeleteAsync(TestContext.Current.CancellationToken);
            Assert.Null(await lease.LoadAsync(TestContext.Current.CancellationToken));
        }
        Assert.Equal("synthetic-existing-claude", await File.ReadAllTextAsync(Path.Combine(directory, "claude.state"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FullyStagedGrantRecoversAfterInterruptedReplacement()
    {
        var interrupt = false;
        var store = new AntigravityStateStore(directory, () => { if (interrupt) throw new IOException("synthetic fault"); });
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var old = await lease.SaveAsync(State(), null, TestContext.Current.CancellationToken);
            interrupt = true;
            var error = await Assert.ThrowsAsync<AntigravityException>(() => lease.SaveAsync(State("synthetic-rotated"), old.Revision, TestContext.Current.CancellationToken));
            Assert.Equal(ProviderFailureKind.StorageUnavailable, error.Kind);
        }
        await using var recovered = await new AntigravityStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-rotated", (await recovered.LoadAsync(TestContext.Current.CancellationToken))!.RefreshToken);
        Assert.False(File.Exists(Path.Combine(directory, "antigravity.state.pending")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsupportedOrCorruptRecordsArePreservedAndCannotBeOverwritten(bool futureVersion)
    {
        Directory.CreateDirectory(directory);
        var bytes = futureVersion
            ? ProtectedData.Protect("{\"Version\":99}"u8.ToArray(), "AiUsage.Antigravity.State.v1"u8.ToArray(), DataProtectionScope.CurrentUser)
            : "synthetic-corrupt-ciphertext"u8.ToArray();
        var path = Path.Combine(directory, "antigravity.state");
        await File.WriteAllBytesAsync(path, bytes, TestContext.Current.CancellationToken);
        await using var lease = await new AntigravityStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => lease.LoadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
        await Assert.ThrowsAsync<AntigravityException>(() => lease.SaveAsync(State(), null, TestContext.Current.CancellationToken));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ARecordWithoutAWorkspaceOrIdentityIsRejectedRatherThanStored()
    {
        await using var lease = await new AntigravityStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        foreach (var invalid in new[]
        {
            State() with { ProjectId = "" },
            State() with { AccountId = "with space" },
            State("") with { },
            State() with { Tier = new string('t', 129) }
        })
        {
            Assert.Equal(ProviderFailureKind.RecoveryRequired,
                (await Assert.ThrowsAsync<AntigravityException>(() => lease.SaveAsync(invalid, null, TestContext.Current.CancellationToken))).Kind);
        }
        Assert.False(File.Exists(Path.Combine(directory, "antigravity.state")));
    }

    [Fact]
    public async Task AnExclusiveLeaseAndRevisionCheckPreventLostUpdates()
    {
        var store = new AntigravityStateStore(directory);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<AntigravityException>(() => store.AcquireAsync(TestContext.Current.CancellationToken));
        var old = await lease.SaveAsync(State(), null, TestContext.Current.CancellationToken);
        var next = await lease.SaveAsync(State("synthetic-next"), old.Revision, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => lease.SaveAsync(State("synthetic-obsolete"), old.Revision, TestContext.Current.CancellationToken));
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
            var error = await Assert.ThrowsAsync<AntigravityException>(() => new AntigravityStateStore(junction).AcquireAsync(TestContext.Current.CancellationToken));
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
