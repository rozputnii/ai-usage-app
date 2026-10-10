using System.Diagnostics;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class BudgetStorageSafetyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-budget-safety-" + Guid.NewGuid().ToString("N"));
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly ReadingSeriesKey Key = new("target", new("provider", "family", "opaque"));
    private static readonly DateTimeOffset Start = new(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
    private static ReadingObservation Observation => new(Key, new CountQuantity(40, "percent"), Start);

    [Fact]
    public async Task ReplaceFailureKeepsCommittedFileAndCleansStage()
    {
        using var store = new LocalBudgetStore(root);
        await store.AppendAsync([Observation], Token);
        var path = Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "series-*.json"));
        var original = await File.ReadAllBytesAsync(path, Token);
        using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var error = await Record.ExceptionAsync(() => store.AppendAsync([Observation with { Value = new CountQuantity(45, "percent"), FetchedAt = Start.AddMinutes(5) }], Token));
            Assert.True(error is IOException or UnauthorizedAccessException);
        }
        Assert.Equal(original, await File.ReadAllBytesAsync(path, Token));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "budget"), "*.stage-*"));
    }

    [Fact]
    public async Task CorruptConfigurationAndFutureVersionArePreservedWithBoundedRecovery()
    {
        using var store = new LocalBudgetStore(root);
        await store.SaveConfigurationAsync(BudgetConfiguration.Default, Token);
        var path = Path.Combine(root, "budget", "configuration.v1.json");
        for (int i = 0; i < 3; i++)
        {
            await File.WriteAllTextAsync(path, "{\"Version\":99,\"WorkDays\":[],\"Caps\":[]}", Token);
            var read = await store.LoadConfigurationAsync(Token);
            Assert.True(read.Recovered);
            Assert.Equal(5, read.Value.WorkDays.Count);
        }
        await File.WriteAllTextAsync(path, "fourth-invalid", Token);
        await Assert.ThrowsAsync<IOException>(() => store.LoadConfigurationAsync(Token));
        Assert.Equal("fourth-invalid", await File.ReadAllTextAsync(path, Token));
        Assert.Equal(3, Directory.GetFiles(Path.Combine(root, "budget"), "*.quarantine-*").Length);
        await store.DeleteAllAsync(Token);
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "budget"), "*.quarantine-*"));
    }

    [Theory]
    [InlineData("root")]
    [InlineData("budget.lock")]
    [InlineData("configuration.v1.json")]
    public async Task RedirectedPathsNeverReadWriteOrDeleteOutsideOwnedStorage(string location)
    {
        var outside = Path.Combine(root, "outside");
        var owned = Path.Combine(root, "app");
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(Path.Combine(owned, "budget"));
        var sentinel = Path.Combine(outside, "preserve.txt");
        await File.WriteAllTextAsync(sentinel, "synthetic owner data", Token);
        var link = location == "root" ? Path.Combine(owned, "budget") : Path.Combine(owned, "budget", location);
        if (location == "root") Directory.Delete(link);
        await Junction(link, outside);
        try
        {
            using var store = new LocalBudgetStore(owned);
            await Assert.ThrowsAsync<ProviderException>(() => store.LoadConfigurationAsync(Token));
            await Assert.ThrowsAsync<ProviderException>(() => store.SaveConfigurationAsync(BudgetConfiguration.Default, Token));
            await Assert.ThrowsAsync<ProviderException>(() => store.DeleteAllAsync(Token));
            Assert.Equal("synthetic owner data", await File.ReadAllTextAsync(sentinel, Token));
            Assert.Single(Directory.GetFileSystemEntries(outside));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public async Task RedirectedExactSeriesRefusesReadAppendAndSelectiveCleanup()
    {
        using var store = new LocalBudgetStore(root);
        await store.AppendAsync([Observation], Token);
        var path = Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "series-*.json"));
        File.Delete(path);
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "sentinel"), "preserve", Token);
        await Junction(path, outside);
        try
        {
            await Assert.ThrowsAsync<ProviderException>(() => store.ReadAsync(Key, Token));
            await Assert.ThrowsAsync<ProviderException>(() => store.AppendAsync([Observation], Token));
            await Assert.ThrowsAsync<ProviderException>(() => store.DeleteAccountAsync(Key.AccountTarget, Token));
            Assert.Single(Directory.GetFileSystemEntries(outside));
        }
        finally { Directory.Delete(path); }
    }

    [Fact]
    public async Task RedirectedEntryCountedByTheCapRefusesWritesToOtherSeries()
    {
        using var store = new LocalBudgetStore(root);
        await store.AppendAsync([Observation], Token);
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "sentinel"), "preserve", Token);
        var link = Path.Combine(root, "budget", "series-redirected.v1.json");
        await Junction(link, outside);
        try
        {
            var other = Observation with { Series = Key with { AccountTarget = "other" } };
            await Assert.ThrowsAsync<ProviderException>(() => store.AppendAsync([other], Token));
            await Assert.ThrowsAsync<ProviderException>(() => store.SaveConfigurationAsync(BudgetConfiguration.Default, Token));
            Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "series-*.json"));
            Assert.Empty(Directory.GetFiles(Path.Combine(root, "budget"), "*.stage-*"));
            Assert.False(File.Exists(Path.Combine(root, "budget", "configuration.v1.json")));
            Assert.Single(Directory.GetFileSystemEntries(outside));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public async Task RedirectedAncestorOfTheOwnedDirectoryRefusesWrites()
    {
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(Path.Combine(outside, "budget"));
        var link = Path.Combine(root, "app");
        await Junction(link, outside);
        try
        {
            var files = new BudgetJsonFile(Path.Combine(link, "budget"));
            await Assert.ThrowsAsync<ProviderException>(() => files.WriteAsync("configuration.v1.json", "value", 100, Token));
            Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(outside, "budget")));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public async Task OversizedWriteAndConcurrentProcessLeaseCannotReplaceCommittedState()
    {
        Directory.CreateDirectory(Path.Combine(root, "budget"));
        var files = new BudgetJsonFile(Path.Combine(root, "budget"));
        await files.WriteAsync("configuration.v1.json", "first", 100, Token);
        await Assert.ThrowsAsync<IOException>(() => files.WriteAsync("configuration.v1.json", new string('x', 101), 100, Token));
        Assert.Equal("\"first\"", await File.ReadAllTextAsync(Path.Combine(root, "budget", "configuration.v1.json"), Token));
        using var lease = new FileStream(Path.Combine(root, "budget", "budget.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var store = new LocalBudgetStore(root);
        await Assert.ThrowsAsync<IOException>(() => store.AppendAsync([Observation], Token));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "budget"), "series-*"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectiveCleanupCannotClaimSuccessWhileUnparsedConfigurationMayRetainCaps(bool staged)
    {
        using var store = new LocalBudgetStore(root);
        await store.AppendAsync([Observation], Token);
        await store.SaveConfigurationAsync(new([DayOfWeek.Monday], [new(Key, new(new CountQuantity(10, "requests"), Start))]), Token);
        var path = Path.Combine(root, "budget", "configuration.v1.json");
        if (staged) path += ".stage-" + Guid.NewGuid().ToString("N");
        await File.WriteAllTextAsync(path, "unreadable prior cap data", Token);
        await Assert.ThrowsAsync<IOException>(() => store.DeleteAccountAsync(Key.AccountTarget, Token));
        Assert.Single((await store.ReadAsync(Key, Token)).Value);
        await store.DeleteAllAsync(Token);
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "budget"), "*.json*"));
    }

    [Fact]
    public async Task ProductionRecorderReportsRecoveryWithoutLoggingPrivateData()
    {
        var diagnostics = new LocalDiagnosticSink(root);
        using var store = new LocalBudgetStore(root, diagnostics);
        var recorder = new QuotaObservationRecorder(store);
        var quota = new QuotaSnapshot(Start, "private-plan", [new("private-group", null, null, null, null, null,
            [new("weekly", 40, 60, TimeSpan.FromDays(7), Start.AddDays(3))])], null, null, null, null);
        var state = new ProviderSessionState(ProviderSessionStatus.QuotaAvailable, quota);
        await recorder.RecordAsync("private-target", "codex", state, Token);
        var path = Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "series-*.json"));
        await File.WriteAllTextAsync(path, "private-corrupt-data", Token);
        await recorder.RecordAsync("private-target", "codex", state with { Quota = quota with { FetchedAt = Start.AddMinutes(5) } }, Token);
        var log = await File.ReadAllTextAsync(Path.Combine(root, "diagnostics.v1.log"), Token);
        Assert.Contains("BudgetStoreRecovered\tInvalidData", log);
        Assert.DoesNotContain("private", log);
        Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "*.quarantine-*"));
    }

    [Fact]
    public async Task ConfigurationAndSeriesWritesReturnExplicitRecoveryResults()
    {
        using var store = new LocalBudgetStore(root);
        await store.AppendAsync([Observation], Token);
        var series = Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "series-*.json"));
        await File.WriteAllTextAsync(series, "corrupt", Token);
        Assert.True((await store.AppendAsync([Observation with { FetchedAt = Start.AddMinutes(5) }], Token)).Recovered);
        Assert.False((await store.AppendAsync([Observation with { FetchedAt = Start.AddMinutes(10) }], Token)).Recovered);
        await store.SaveConfigurationAsync(BudgetConfiguration.Default, Token);
        await File.WriteAllTextAsync(Path.Combine(root, "budget", "configuration.v1.json"), "{\"Version\":99,\"WorkDays\":[],\"Caps\":[]}", Token);
        Assert.True((await store.SaveConfigurationAsync(BudgetConfiguration.Default, Token)).Recovered);
        Assert.False((await store.SaveConfigurationAsync(BudgetConfiguration.Default, Token)).Recovered);
    }

    private static async Task Junction(string link, string destination)
    {
        var start = new ProcessStartInfo("cmd.exe") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "/c", "mklink", "/J", link, destination }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync(Token);
        Assert.Equal(0, process.ExitCode);
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
