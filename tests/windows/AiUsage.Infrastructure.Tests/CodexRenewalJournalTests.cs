using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexRenewalJournalTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage-RenewalTests-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("torn")]
    [InlineData("parent-mismatch")]
    [InlineData("missing-predecessor")]
    public async Task FreshAuthorizationCannotAdoptAnUnboundOrUnreadableIntent(string damage)
    {
        var store = new CodexGrantStore(root);
        await store.WriteAsync(new("synthetic-account", "synthetic-original"), TestContext.Current.CancellationToken);
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var state = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
            await lease.BeginExternalUpdateAsync(CodexGrantStore.Revision(state), TestContext.Current.CancellationToken);
        }
        var pending = Path.Combine(root, "codex.grant.pending");
        if (damage == "missing-predecessor") File.Delete(Path.Combine(root, "codex.grant"));
        else if (damage == "torn") await File.WriteAllBytesAsync(pending, [1, 2, 3], TestContext.Current.CancellationToken);
        else
        {
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(new ProviderPendingGeneration(1, Guid.NewGuid(), []), ProviderPendingJson.Default.ProviderPendingGeneration);
            try
            {
                var bytes = ProtectedData.Protect(plaintext, "AiUsage.Codex.Grant.v1"u8.ToArray(), DataProtectionScope.CurrentUser);
                await File.WriteAllBytesAsync(pending, bytes, TestContext.Current.CancellationToken);
            }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("No provider traffic expected."));
        using var http = new HttpClient(server);
        using var session = new CodexSession(new(http), new(http), store, new(root));
        bool opened = false;
        var result = await session.ConnectAsync(_ => opened = true, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderSessionStatus.RecoveryRequired, result.Status);
        Assert.False(opened);
        Assert.Equal(0, server.Calls);
        Assert.True(File.Exists(pending));
    }

    [Fact]
    public async Task InterruptedIntentPreservesPredecessorButPreventsReplayAndAllowsExplicitDisconnect()
    {
        var store = new CodexGrantStore(root);
        await store.WriteAsync(new("synthetic-account", "synthetic-original"), TestContext.Current.CancellationToken);
        var predecessor = await File.ReadAllBytesAsync(Path.Combine(root, "codex.grant"), TestContext.Current.CancellationToken);
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var state = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
            await lease.BeginExternalUpdateAsync(CodexGrantStore.Revision(state), TestContext.Current.CancellationToken);
        }
        Assert.Equal(predecessor, await File.ReadAllBytesAsync(Path.Combine(root, "codex.grant"), TestContext.Current.CancellationToken));
        var journal = await File.ReadAllBytesAsync(Path.Combine(root, "codex.grant.pending"), TestContext.Current.CancellationToken);
        Assert.DoesNotContain("synthetic-original", Encoding.UTF8.GetString(journal));
        var plaintext = ProtectedData.Unprotect(journal, "AiUsage.Codex.Grant.v1"u8.ToArray(), DataProtectionScope.CurrentUser);
        try
        {
            using var document = JsonDocument.Parse(plaintext);
            Assert.Equal(new[] { "Ciphertext", "ParentRevision", "Version" }, document.RootElement.EnumerateObject().Select(p => p.Name).Order());
            // The unchanged pre-AIU-042 reader requires Length > 0 before decoding a successor.
            Assert.Empty(document.RootElement.GetProperty("Ciphertext").GetBytesFromBase64());
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
        var error = await Assert.ThrowsAsync<ProviderException>(() => store.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
        await store.DeleteAsync(TestContext.Current.CancellationToken);
        Assert.Null(await store.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReturnedSuccessorRecoversAfterInterruptionBeforePromotion()
    {
        var store = new CodexGrantStore(root);
        await store.WriteAsync(new("synthetic-account", "synthetic-original"), TestContext.Current.CancellationToken);
        var interrupted = new CodexGrantStore(root, () => throw new IOException("Synthetic cutover interruption."));
        await using (var lease = await interrupted.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var state = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
            var revision = CodexGrantStore.Revision(state);
            await lease.BeginExternalUpdateAsync(revision, TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<ProviderException>(() => lease.SaveAsync(
                CodexGrantStore.Record(new("synthetic-account", "synthetic-successor")), revision, TestContext.Current.CancellationToken));
        }
        Assert.Equal("synthetic-successor", (await store.ReadAsync(TestContext.Current.CancellationToken))!.RefreshToken);
        Assert.False(File.Exists(Path.Combine(root, "codex.grant.pending")));
    }

    [Fact]
    public async Task ChangedIntentCannotBeOverwrittenByItsOriginalLease()
    {
        var store = new CodexGrantStore(root);
        await store.WriteAsync(new("synthetic-account", "synthetic-original"), TestContext.Current.CancellationToken);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        var state = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
        var revision = CodexGrantStore.Revision(state);
        await lease.BeginExternalUpdateAsync(revision, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "codex.grant.pending"), "synthetic-torn", TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<ProviderException>(() => lease.SaveAsync(
            CodexGrantStore.Record(new("synthetic-account", "synthetic-successor")), revision, TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.RecoveryRequired, error.Kind);
        Assert.Equal("synthetic-torn", await File.ReadAllTextAsync(Path.Combine(root, "codex.grant.pending"), TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
