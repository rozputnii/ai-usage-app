using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class OwnedDataDeletionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.Deletion.Tests", Guid.NewGuid().ToString("N"));
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private void Put(string relative, string content = "synthetic-private-data")
    { var path = Path.Combine(root, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, content); }

    [Fact]
    public async Task ConfirmedDeletionRemovesKnownGenerationsAndKeepsForeignFiles()
    {
        var slot = "accounts/" + Guid.NewGuid().ToString("N") + "/";
        string[] files = ["accounts.state", "accounts.state.pending", "providers/claude.state", "providers/claude.state.pending",
            "providers/claude.state.v1.bak", slot + "codex.grant", slot + "codex.grant.new", slot + "codex.grant.pending",
            slot + "codex.quota.json", slot + "codex.quota.json.v1.bak", "preferences/appearance.v1.json",
            "preferences/ledger/appearance.v1.json", "maintenance/checkpoint.v1.bin", "maintenance/journal.v1.json",
            "budget/configuration.v1.json", "diagnostics.v1.log"];
        foreach (var file in files) Put(file);
        Put("owner-export.json", "keep"); Put("logs/owner-note.txt", "keep");
        var deletion = new OwnedDataDeletion(root);
        Assert.False(deletion.Pending);
        await Assert.ThrowsAsync<InvalidOperationException>(() => deletion.RunAsync(false, Token));
        await deletion.RunAsync(true, Token);
        Assert.False(deletion.Pending);
        foreach (var file in files.Where(f => f != "preferences/appearance.v1.json")) Assert.False(File.Exists(Path.Combine(root, file)), file);
        Assert.Equal("{}", File.ReadAllText(Path.Combine(root, "preferences/appearance.v1.json")));
        Assert.Contains("\"Layout\":2", File.ReadAllText(Path.Combine(root, "layout.v1.json")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(root, "owner-export.json")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(root, "logs/owner-note.txt")));
    }

    [Theory]
    [InlineData("intent")]
    [InlineData("provider-data")]
    [InlineData("budget-data")]
    [InlineData("metadata")]
    public async Task InterruptedConfirmedDeletionResumesWithoutReplayingCredentials(string point)
    {
        Put("providers/codex.grant"); Put("providers/codex.grant.pending");
        Put("budget/configuration.v1.json"); Put("maintenance/checkpoint.v1.bin");
        var deletion = new OwnedDataDeletion(root, name => { if (name == point) throw new IOException("Synthetic interruption"); });
        await Assert.ThrowsAsync<IOException>(() => deletion.RunAsync(true, Token));
        Assert.True(deletion.Pending);
        await new OwnedDataDeletion(root).RunAsync(false, Token);
        Assert.False(File.Exists(Path.Combine(root, "providers/codex.grant")));
        Assert.False(File.Exists(Path.Combine(root, "providers/codex.grant.pending")));
        Assert.False(File.Exists(Path.Combine(root, "maintenance/checkpoint.v1.bin")));
        Assert.False(deletion.Pending);
    }

    [Fact]
    public async Task MalformedIntentAndUnknownProviderArtifactArePreserved()
    {
        Put("delete-local-data.v1.json", "invalid"); Put("providers/claude.state");
        await Assert.ThrowsAsync<IOException>(() => new OwnedDataDeletion(root).RunAsync(false, Token));
        Assert.True(File.Exists(Path.Combine(root, "providers/claude.state")));
        File.Delete(Path.Combine(root, "delete-local-data.v1.json"));
        Put("providers/future.secret");
        await Assert.ThrowsAsync<IOException>(() => new OwnedDataDeletion(root).RunAsync(true, Token));
        Assert.True(File.Exists(Path.Combine(root, "providers/claude.state")));
        Assert.True(File.Exists(Path.Combine(root, "providers/future.secret")));
    }

    [Fact]
    public async Task CompetingProductLeasePreventsDeletionIntentAndMutations()
    {
        Put("providers/copilot.state");
        using var lease = File.Open(Path.Combine(root, "state.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var deletion = new OwnedDataDeletion(root);
        await Assert.ThrowsAsync<IOException>(() => deletion.RunAsync(true, Token));
        Assert.False(deletion.Pending);
        Assert.True(File.Exists(Path.Combine(root, "providers/copilot.state")));
    }

    [Fact]
    public async Task OrdinaryStartupCannotReopenPartiallyDeletedAccounts()
    {
        Put("delete-local-data.v1.json", "{\"version\":1,\"operation\":\"delete-local-data\"}");
        Put("providers/claude.state.pending");
        using var maintenance = new StateMaintenance(root);
        Assert.Equal(AiUsage.Core.Persistence.MaintenanceCondition.DeletionPending, (await maintenance.InitializeAsync(Token)).Condition);
        Assert.True(File.Exists(Path.Combine(root, "providers/claude.state.pending")));
    }

    [Fact]
    public async Task GenerationPublishedBeforeProviderLeaseIsAlsoDeleted()
    {
        Put("providers/codex.quota.json");
        var deletion = new OwnedDataDeletion(root, point =>
        { if (point == "before-provider-leases") Put("providers/codex.grant.pending"); });
        await deletion.RunAsync(true, Token);
        Assert.False(File.Exists(Path.Combine(root, "providers/codex.grant.pending")));
    }

    [Fact]
    public async Task CommittedIntentSurvivesAnInterruptedStagingRewrite()
    {
        Put("delete-local-data.v1.json", "{\"version\":1,\"operation\":\"delete-local-data\"}");
        Put("delete-local-data.v1.json.new", "{"); Put("providers/claude.state.pending");
        await new OwnedDataDeletion(root).RunAsync(false, Token);
        Assert.False(File.Exists(Path.Combine(root, "providers/claude.state.pending")));
        Assert.False(new OwnedDataDeletion(root).Pending);
    }

    [Fact]
    public async Task RedirectedAccountDirectoryNeverDeletesOutsideOwnedRoot()
    {
        var outside = Path.Combine(Path.GetTempPath(), "AiUsage.Deletion.Outside", Guid.NewGuid().ToString("N"));
        var link = Path.Combine(root, "accounts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside); Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        var canary = Path.Combine(outside, "claude.state"); File.WriteAllText(canary, "keep");
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            foreach (var arg in new[] { "/c", "mklink", "/J", link, outside }) start.ArgumentList.Add(arg);
            using var process = System.Diagnostics.Process.Start(start)!;
            await process.WaitForExitAsync(Token); Assert.Equal(0, process.ExitCode);
            await Assert.ThrowsAsync<AiUsage.Infrastructure.Providers.ProviderException>(() => new OwnedDataDeletion(root).RunAsync(true, Token));
            Assert.Equal("keep", File.ReadAllText(canary));
        }
        finally { if (Directory.Exists(link)) Directory.Delete(link); Directory.Delete(outside, true); }
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
