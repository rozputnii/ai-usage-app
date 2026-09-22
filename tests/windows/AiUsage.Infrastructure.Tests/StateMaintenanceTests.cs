using System.Security.Cryptography;
using System.Text;
using AiUsage.Core.Persistence;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class StateMaintenanceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-migration-" + Guid.NewGuid().ToString("N"));
    private const string Preferences = "{\"Version\":1,\"Theme\":2,\"Labels\":{\"opaque/provider\":\"My account\"},\"future\":{\"raw\":[null,7]}}";
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private string Legacy => Path.Combine(root, "appearance.v1.json");
    private string Target => Path.Combine(root, "preferences", "appearance.v1.json");
    private string Checkpoint => Path.Combine(root, "maintenance", "checkpoint.v1.bin");

    public StateMaintenanceTests() => Directory.CreateDirectory(root);

    [Fact]
    public async Task UpgradePreservesExactOpaquePreferencesAndExcludesCredentialsAndUnknownFiles()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        Directory.CreateDirectory(Path.Combine(root, "providers"));
        var secretPath = Path.Combine(root, "providers", "synthetic.bin");
        var secret = ProtectedData.Protect("synthetic-token-never-backed-up"u8.ToArray(), null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(secretPath, secret, Token);
        await File.WriteAllTextAsync(Path.Combine(root, "unknown.txt"), "leave alone", Token);
        using (var maintenance = new StateMaintenance(root))
        {
            Assert.Equal(MaintenanceCondition.Ready, (await maintenance.InitializeAsync(Token)).Condition);
            Assert.Equal(Preferences, await File.ReadAllTextAsync(Target, Token));
            Assert.False(File.Exists(Legacy));
            Assert.Equal(secret, await File.ReadAllBytesAsync(secretPath, Token));
            Assert.Equal("leave alone", await File.ReadAllTextAsync(Path.Combine(root, "unknown.txt"), Token));
            var plain = ProtectedData.Unprotect(await File.ReadAllBytesAsync(Checkpoint, Token), "AiUsage.Checkpoint.v1"u8.ToArray(), DataProtectionScope.CurrentUser);
            Assert.DoesNotContain("synthetic-token", Encoding.UTF8.GetString(plain), StringComparison.Ordinal);
            CryptographicOperations.ZeroMemory(plain);
        }
        // Fast startup must not require/decrypt the old checkpoint or inspect unrelated files.
        await File.WriteAllTextAsync(Checkpoint, "bad backup", Token);
        using var restarted = new StateMaintenance(root);
        Assert.Equal(MaintenanceCondition.Ready, (await restarted.InitializeAsync(Token)).Condition);
    }

    [Theory]
    [InlineData("checkpoint")]
    [InlineData("journal")]
    [InlineData("target")]
    [InlineData("commit")]
    [InlineData("cleanup")]
    public async Task InterruptedMigrationRecoversWithoutReplacingLastGoodCheckpoint(string phase)
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using (var first = new StateMaintenance(root, p => { if (p == phase) throw new IOException("simulated power loss"); }))
            Assert.Equal(MaintenanceCondition.Interrupted, (await first.InitializeAsync(Token)).Condition);
        var backup = await File.ReadAllBytesAsync(Checkpoint, Token);
        using var restarted = new StateMaintenance(root);
        var initial = await restarted.InitializeAsync(Token);
        if (phase != "checkpoint") Assert.Equal(MaintenanceCondition.Interrupted, initial.Condition);
        Assert.Equal(MaintenanceCondition.Ready, (await restarted.RetryAsync(Token)).Condition);
        Assert.Equal(Preferences, await File.ReadAllTextAsync(Target, Token));
        if (phase != "checkpoint") Assert.Equal(backup, await File.ReadAllBytesAsync(Checkpoint, Token));
    }

    [Fact]
    public async Task RestoreRecoversCheckpointAndNeverRollsBackNewerCredentialBytes()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using var maintenance = new StateMaintenance(root);
        var report = await maintenance.InitializeAsync(Token);
        Assert.NotNull(report.Checkpoint);
        await File.WriteAllTextAsync(Target, "corrupted", Token);
        Directory.CreateDirectory(Path.Combine(root, "providers"));
        var provider = Path.Combine(root, "providers", "codex.grant.bin");
        await File.WriteAllTextAsync(provider, "newer synthetic protected generation", Token);
        Assert.Equal(MaintenanceCondition.Ready, (await maintenance.RestoreAsync(report.Checkpoint.Id, Token)).Condition);
        Assert.Equal(Preferences, await File.ReadAllTextAsync(Target, Token));
        Assert.Equal("newer synthetic protected generation", await File.ReadAllTextAsync(provider, Token));
    }

    [Theory]
    [InlineData("restore-journal")]
    [InlineData("restore-target")]
    [InlineData("restore-commit")]
    public async Task InterruptedRestoreRequiresExplicitRetryAndRetainsTheCheckpoint(string phase)
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        byte[] backup;
        using (var first = new StateMaintenance(root, p => { if (p == phase) throw new IOException("fault"); }))
        {
            var ready = await first.InitializeAsync(Token);
            Assert.NotNull(ready.Checkpoint);
            backup = await File.ReadAllBytesAsync(Checkpoint, Token);
            Assert.Equal(MaintenanceCondition.RestoreFailed, (await first.RestoreAsync(ready.Checkpoint.Id, Token)).Condition);
        }
        using var restarted = new StateMaintenance(root);
        Assert.Equal(MaintenanceCondition.RestoreFailed, (await restarted.InitializeAsync(Token)).Condition);
        Assert.Equal(MaintenanceCondition.Ready, (await restarted.RetryAsync(Token)).Condition);
        Assert.Equal(Preferences, await File.ReadAllTextAsync(Target, Token));
        Assert.Equal(backup, await File.ReadAllBytesAsync(Checkpoint, Token));
    }

    [Fact]
    public async Task TamperedBackupAndUnrecognizedCheckpointCannotChangeDurableState()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using var maintenance = new StateMaintenance(root);
        var ready = await maintenance.InitializeAsync(Token);
        Assert.NotNull(ready.Checkpoint);
        Assert.Equal(MaintenanceCondition.RestoreFailed, (await maintenance.RestoreAsync("../outside", Token)).Condition);
        var backup = await File.ReadAllBytesAsync(Checkpoint, Token);
        backup[^1] ^= 1;
        await File.WriteAllBytesAsync(Checkpoint, backup, Token);
        Assert.Equal(MaintenanceCondition.RestoreFailed, (await maintenance.RestoreAsync(ready.Checkpoint.Id, Token)).Condition);
        Assert.Equal(Preferences, await File.ReadAllTextAsync(Target, Token));
    }

    [Fact]
    public async Task NewerSchemaAlwaysRefusesRetryAndRestoreEvenWithInterruptedJournal()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using (var first = new StateMaintenance(root, p => { if (p == "journal") throw new IOException("fault"); }))
            await first.InitializeAsync(Token);
        await File.WriteAllTextAsync(Path.Combine(root, "layout.v1.json"), "{\"Version\":1,\"Layout\":99}", Token);
        using var maintenance = new StateMaintenance(root);
        Assert.Equal(MaintenanceCondition.NewerSchema, (await maintenance.InitializeAsync(Token)).Condition);
        Assert.Equal(MaintenanceCondition.NewerSchema, (await maintenance.RetryAsync(Token)).Condition);
        Assert.Equal(MaintenanceCondition.NewerSchema, (await maintenance.RestoreAsync("last-good", Token)).Condition);
        Assert.Equal(Preferences, await File.ReadAllTextAsync(Legacy, Token));
        Assert.False(File.Exists(Target));
    }

    [Fact]
    public async Task ExclusiveLifetimeLeasePreventsSecondProcessFromMigratingOrRestoring()
    {
        using var first = new StateMaintenance(root);
        Assert.Equal(MaintenanceCondition.Ready, (await first.InitializeAsync(Token)).Condition);
        using var second = new StateMaintenance(root);
        Assert.Equal(MaintenanceCondition.Interrupted, (await second.InitializeAsync(Token)).Condition);
        Assert.Equal(MaintenanceCondition.Interrupted, (await second.RetryAsync(Token)).Condition);
        first.Dispose();
        Assert.Equal(MaintenanceCondition.Ready, (await second.RetryAsync(Token)).Condition);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"Version\":2}")]
    [InlineData("invalid")]
    [InlineData("{\"Version\":\"bad\"}")]
    public async Task InvalidLegacyPreferencesStayUntouchedAndDoNotCreateCheckpoint(string content)
    {
        await File.WriteAllTextAsync(Legacy, content, Token);
        using var maintenance = new StateMaintenance(root);
        Assert.Equal(MaintenanceCondition.Interrupted, (await maintenance.InitializeAsync(Token)).Condition);
        Assert.Equal(content, await File.ReadAllTextAsync(Legacy, Token));
        Assert.False(File.Exists(Checkpoint));
        Assert.False(File.Exists(Target));
    }

    [Fact]
    public async Task CorruptJournalCanBeExplicitlyRestoredFromVerifiedCheckpoint()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using (var first = new StateMaintenance(root)) await first.InitializeAsync(Token);
        await File.WriteAllTextAsync(Path.Combine(root, "maintenance", "journal.v1.json"), "corrupt", Token);
        using var restarted = new StateMaintenance(root);
        var report = await restarted.InitializeAsync(Token);
        Assert.Equal(MaintenanceCondition.Interrupted, report.Condition);
        Assert.NotNull(report.Checkpoint);
        Assert.Equal(MaintenanceCondition.Ready, (await restarted.RestoreAsync(report.Checkpoint.Id, Token)).Condition);
        Assert.Equal(Preferences, await File.ReadAllTextAsync(Target, Token));
    }

    [Fact]
    public async Task CorruptCurrentPreferencesEnterRecoveryWithAvailableCheckpoint()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using (var first = new StateMaintenance(root)) await first.InitializeAsync(Token);
        await File.WriteAllTextAsync(Target, "broken", Token);
        using var restarted = new StateMaintenance(root);
        var report = await restarted.InitializeAsync(Token);
        Assert.Equal(MaintenanceCondition.Interrupted, report.Condition);
        Assert.NotNull(report.Checkpoint);
        Assert.Equal("broken", await File.ReadAllTextAsync(Target, Token));
    }

    [Fact]
    public async Task MissingCommittedPreferencesNeverSilentlyResetsToDefaults()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using (var first = new StateMaintenance(root)) await first.InitializeAsync(Token);
        File.Delete(Target);
        using var restarted = new StateMaintenance(root);
        Assert.Equal(MaintenanceCondition.Interrupted, (await restarted.InitializeAsync(Token)).Condition);
    }

    [Fact]
    public async Task EmptyFirstRunHasCommittedDefaultsAndRestoresWithoutReplayingLaterPreferences()
    {
        using var maintenance = new StateMaintenance(root);
        var ready = await maintenance.InitializeAsync(Token);
        Assert.Equal("{}", await File.ReadAllTextAsync(Target, Token));
        await File.WriteAllTextAsync(Target, Preferences, Token);
        Assert.NotNull(ready.Checkpoint);
        await maintenance.RestoreAsync(ready.Checkpoint.Id, Token);
        Assert.Equal("{}", await File.ReadAllTextAsync(Target, Token));
    }

    [Fact]
    public async Task RedirectedTargetDirectoryNeverChangesOutsideData()
    {
        var outside = Path.Combine(Path.GetTempPath(), "aiu-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        var link = Path.Combine(root, "preferences");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(outside, "appearance.v1.json"), "outside", Token);
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            foreach (var argument in new[] { "/c", "mklink", "/J", link, outside }) start.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(start)!;
            await process.WaitForExitAsync(Token);
            Assert.Equal(0, process.ExitCode);
            using var maintenance = new StateMaintenance(root);
            Assert.Equal(MaintenanceCondition.Interrupted, (await maintenance.InitializeAsync(Token)).Condition);
            Assert.Equal("outside", await File.ReadAllTextAsync(Path.Combine(outside, "appearance.v1.json"), Token));
        }
        finally
        {
            if (Directory.Exists(link)) Directory.Delete(link);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task DiagnosticExportContainsOnlyStatusAndNormalizesUnsafePathFailure()
    {
        await File.WriteAllTextAsync(Legacy, Preferences, Token);
        using var maintenance = new StateMaintenance(root);
        await maintenance.InitializeAsync(Token);
        await maintenance.ExportDiagnosticsAsync(Token);
        var path = Path.Combine(root, "recovery-diagnostics.txt");
        var text = await File.ReadAllTextAsync(path, Token);
        Assert.Contains("Condition: Ready", text, StringComparison.Ordinal);
        Assert.DoesNotContain("opaque/provider", text, StringComparison.Ordinal);
        Assert.DoesNotContain(root, text, StringComparison.Ordinal);
        File.Delete(path);
        Directory.CreateDirectory(path);
        await Assert.ThrowsAsync<IOException>(() => maintenance.ExportDiagnosticsAsync(Token));
        Assert.True(Directory.Exists(path));
    }

    public void Dispose() => Directory.Delete(root, recursive: true);
}
