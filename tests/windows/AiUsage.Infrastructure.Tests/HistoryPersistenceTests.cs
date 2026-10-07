using AiUsage.Core.Diagnostics;
using AiUsage.Core.Persistence;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

/// <summary>AIU-047: the history root outlives the package-owned state root.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class HistoryPersistenceTests : IDisposable
{
    private readonly string temp = Path.Combine(Path.GetTempPath(), "AiUsage.History.Tests", Guid.NewGuid().ToString("N"));
    private string State => Path.Combine(temp, "state");
    private string History => Path.Combine(temp, "history");
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static AccountRecord Account(string subject, string? context = null, string provider = "claude") =>
        new(Guid.NewGuid(), provider, Guid.NewGuid(), new ProviderIdentity(subject, context));

    private static void Put(string root, string relative, string content = "synthetic")
    { var path = Path.Combine(root, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, content); }

    [Fact]
    public async Task IdentityMapFindsOnlyExactProviderAndIdentity()
    {
        var first = Account("first", "org");
        var map = new AccountIdentityMap(History);
        await map.UpsertAsync([first, Account("second")], Token);
        var reopened = new AccountIdentityMap(History);
        Assert.Equal(first.Id, await reopened.FindAsync("claude", new ProviderIdentity("first", "org"), Token));
        Assert.Null(await reopened.FindAsync("claude", new ProviderIdentity("first"), Token));
        Assert.Null(await reopened.FindAsync("codex", new ProviderIdentity("first", "org"), Token));
        Assert.Null(await reopened.FindAsync("claude", new ProviderIdentity("third"), Token));
    }

    [Fact]
    public async Task IdentityMapUpsertReplacesTheIdentitysAccountId()
    {
        var map = new AccountIdentityMap(History);
        await map.UpsertAsync([Account("first")], Token);
        var current = Account("first");
        await map.UpsertAsync([current], Token);
        Assert.Equal(current.Id, await map.FindAsync("claude", new ProviderIdentity("first"), Token));
    }

    [Fact]
    public async Task IdentityMapIsProtectedAndHoldsNoStorageReference()
    {
        var account = Account("synthetic-subject-value");
        await new AccountIdentityMap(History).UpsertAsync([account], Token);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(History, "accounts.identities"), Token);
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain("synthetic-subject-value", text, StringComparison.Ordinal);
        Assert.DoesNotContain(account.StorageId.ToString("N"), text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorruptIdentityMapIsSetAsideAndRebuilt()
    {
        Put(History, "accounts.identities", "not protected");
        var account = Account("first");
        var map = new AccountIdentityMap(History);
        Assert.Null(await map.FindAsync("claude", account.Identity, Token));
        await map.UpsertAsync([account], Token);
        Assert.Equal(account.Id, await map.FindAsync("claude", account.Identity, Token));
        Assert.Single(Directory.GetFiles(History, "accounts.identities.quarantine-*"));
    }

    [Fact]
    public async Task RelocationMovesStateBudgetAndSeedsIdentitiesFromRegistry()
    {
        Put(State, "budget/configuration.v1.json", "budget-bytes");
        Put(State, "budget/unassigned-history.json", "legacy-bytes");
        var account = Account("first");
        var registry = new AccountRegistry(State);
        await registry.UpdateAsync(s => s with { Accounts = [account] }, Token);
        var sink = new RecordingSink();
        var relocation = new HistoryRelocation(State, History, registry, new AccountIdentityMap(History), sink);
        await relocation.RunAsync(Token);
        Assert.False(Directory.Exists(Path.Combine(State, "budget")));
        Assert.Equal("budget-bytes", File.ReadAllText(Path.Combine(History, "budget", "configuration.v1.json")));
        Assert.Equal("legacy-bytes", File.ReadAllText(Path.Combine(History, "budget", "unassigned-history.json")));
        Assert.Equal(account.Id, await new AccountIdentityMap(History).FindAsync("claude", account.Identity, Token));
        Assert.Contains(DiagnosticEvent.HistoryRelocated, sink.Events);
        await relocation.RunAsync(Token);
        Assert.Equal("budget-bytes", File.ReadAllText(Path.Combine(History, "budget", "configuration.v1.json")));
    }

    [Fact]
    public async Task RelocationKeepsBothCopiesWhenHistoryAlreadyExists()
    {
        Put(State, "budget/configuration.v1.json", "older");
        Put(History, "budget/configuration.v1.json", "surviving");
        var sink = new RecordingSink();
        await new HistoryRelocation(State, History, new AccountRegistry(State), new AccountIdentityMap(History), sink).RunAsync(Token);
        Assert.Equal("older", File.ReadAllText(Path.Combine(State, "budget", "configuration.v1.json")));
        Assert.Equal("surviving", File.ReadAllText(Path.Combine(History, "budget", "configuration.v1.json")));
        Assert.Contains(DiagnosticEvent.HistoryLeftInPlace, sink.Events);
    }

    [Fact]
    public async Task RelocationWithSharedRootLeavesBudgetInPlace()
    {
        Put(State, "budget/configuration.v1.json", "budget-bytes");
        var sink = new RecordingSink();
        await new HistoryRelocation(State, State, new AccountRegistry(State), new AccountIdentityMap(State), sink).RunAsync(Token);
        Assert.Equal("budget-bytes", File.ReadAllText(Path.Combine(State, "budget", "configuration.v1.json")));
        Assert.Empty(sink.Events);
    }

    [Fact]
    public async Task MaintenanceCommitsLayoutThreeAndOlderBuildRefusesIt()
    {
        Put(State, "budget/configuration.v1.json", "budget-bytes");
        var registry = new AccountRegistry(State);
        using (var maintenance = new StateMaintenance(State, accountMigration: _ => Task.CompletedTask,
            historyMigration: new HistoryRelocation(State, History, registry, new AccountIdentityMap(History)).RunAsync))
        {
            var report = await maintenance.InitializeAsync(Token);
            Assert.Equal(MaintenanceCondition.Ready, report.Condition);
            Assert.Equal(3, report.LayoutVersion);
        }
        Assert.Contains("\"Layout\":3", File.ReadAllText(Path.Combine(State, "layout.v1.json")));
        Assert.True(File.Exists(Path.Combine(History, "budget", "configuration.v1.json")));
        using (var restarted = new StateMaintenance(State, accountMigration: _ => Task.CompletedTask,
            historyMigration: new HistoryRelocation(State, History, registry, new AccountIdentityMap(History)).RunAsync))
            Assert.Equal(MaintenanceCondition.Ready, (await restarted.InitializeAsync(Token)).Condition);
        using var older = new StateMaintenance(State, accountMigration: _ => Task.CompletedTask);
        Assert.Equal(MaintenanceCondition.NewerSchema, (await older.InitializeAsync(Token)).Condition);
    }

    [Fact]
    public async Task FailedRelocationIsInterruptedAndPreservesData()
    {
        Put(State, "budget/configuration.v1.json", "budget-bytes");
        Directory.CreateDirectory(temp);
        File.WriteAllText(History, "a file where the history directory should be");
        var registry = new AccountRegistry(State);
        using var maintenance = new StateMaintenance(State, accountMigration: _ => Task.CompletedTask,
            historyMigration: new HistoryRelocation(State, History, registry, new AccountIdentityMap(History)).RunAsync);
        Assert.Equal(MaintenanceCondition.Interrupted, (await maintenance.InitializeAsync(Token)).Condition);
        Assert.Equal("budget-bytes", File.ReadAllText(Path.Combine(State, "budget", "configuration.v1.json")));
    }

    [Fact]
    public async Task DeletionClearsHistoryStoreAndIdentityMap()
    {
        Put(State, "accounts.state");
        Put(History, "budget/configuration.v1.json");
        await new AccountIdentityMap(History).UpsertAsync([Account("first")], Token);
        Put(History, "accounts.identities.quarantine-" + Guid.NewGuid().ToString("N"));
        Put(History, "owner-note.txt", "keep");
        await new OwnedDataDeletion(State, History).RunAsync(true, Token);
        Assert.False(File.Exists(Path.Combine(History, "budget", "configuration.v1.json")));
        Assert.False(File.Exists(Path.Combine(History, "accounts.identities")));
        Assert.Empty(Directory.GetFiles(History, "accounts.identities.quarantine-*"));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(History, "owner-note.txt")));
        Assert.False(File.Exists(Path.Combine(State, "accounts.state")));
    }

    [Fact]
    public async Task InterruptedDeletionFinishesHistoryOnNextRun()
    {
        Put(History, "budget/configuration.v1.json");
        await new AccountIdentityMap(History).UpsertAsync([Account("first")], Token);
        var interrupted = new OwnedDataDeletion(State, History, name => { if (name == "provider-data") throw new IOException("Synthetic interruption"); });
        await Assert.ThrowsAsync<IOException>(() => interrupted.RunAsync(true, Token));
        Assert.True(interrupted.Pending);
        await new OwnedDataDeletion(State, History).RunAsync(false, Token);
        Assert.False(File.Exists(Path.Combine(History, "budget", "configuration.v1.json")));
        Assert.False(File.Exists(Path.Combine(History, "accounts.identities")));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
        catch (IOException) { }
    }

    private sealed class RecordingSink : IDiagnosticSink
    {
        public List<DiagnosticEvent> Events { get; } = [];
        public void Record(DiagnosticEvent eventCode, DiagnosticCategory category) => Events.Add(eventCode);
        public void Signal(DiagnosticEvent eventCode, DiagnosticSeverity severity = DiagnosticSeverity.Information) => Events.Add(eventCode);
    }
}
