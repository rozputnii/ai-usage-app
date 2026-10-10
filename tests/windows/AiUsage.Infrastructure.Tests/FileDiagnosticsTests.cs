using System.Text.Json;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Diagnostics;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class FileDiagnosticsTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-log-test-" + Guid.NewGuid().ToString("N"));
    private readonly Clock clock = new();

    [Fact]
    public async Task EventsAreJsonAndExceptionValuesNeverReachDisk()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        Exception failure;
        try { throw new InvalidOperationException("canary-token\nforged-event", new IOException("private-path-canary")); }
        catch (Exception error) { failure = error; }
        failure.Data["token"] = "data-canary";
        log.Failure(DiagnosticEvent.OperationFailure, failure);
        Assert.True(await log.FlushAsync());
        var text = string.Join('\n', Directory.GetFiles(log.DirectoryPath, "application-*.jsonl").Select(ReadShared));
        Assert.DoesNotContain("canary", text);
        Assert.DoesNotContain(root, text);
        var records = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToArray();
        var item = Assert.Single(records, e => e.GetProperty("eventId").GetString() == "OperationFailure");
        Assert.Equal(1, item.GetProperty("schemaVersion").GetInt32());
        Assert.Contains("InvalidOperationException", item.ToString());
        Assert.Contains(nameof(EventsAreJsonAndExceptionValuesNeverReachDisk), item.ToString());
        Assert.Contains("IOException", item.ToString());
    }

    [Fact]
    public async Task UpdateRecordsCarryOnlyTypedFacts()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        log.Update(DiagnosticEvent.UpdateCheckFailed, new(ErrorCode: unchecked((int)0x80072EFE), Consecutive: 12));
        log.Update(DiagnosticEvent.UpdateApplied, new(From: new(2026, 10, 602, 0), To: new(2026, 10, 604, 0)));
        log.Update(DiagnosticEvent.UpdateInstallStarted, new(Automatic: true, From: new(2026, 10, 604, 0)));
        log.Update(DiagnosticEvent.OperationFailure, new(ErrorCode: 1));
        Assert.True(await log.FlushAsync());
        var records = Directory.GetFiles(log.DirectoryPath, "application-*.jsonl").SelectMany(p => ReadShared(p).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToArray();
        var failed = Assert.Single(records, e => e.GetProperty("eventId").GetString() == "UpdateCheckFailed");
        Assert.Equal("Warning", failed.GetProperty("severity").GetString());
        Assert.Equal("0x80072EFE", failed.GetProperty("context").GetProperty("errorCode").GetString());
        Assert.Equal(12, failed.GetProperty("context").GetProperty("consecutive").GetInt32());
        var applied = Assert.Single(records, e => e.GetProperty("eventId").GetString() == "UpdateApplied");
        Assert.Equal("2026.10.602.0", applied.GetProperty("context").GetProperty("fromVersion").GetString());
        Assert.Equal("2026.10.604.0", applied.GetProperty("context").GetProperty("toVersion").GetString());
        var started = Assert.Single(records, e => e.GetProperty("eventId").GetString() == "UpdateInstallStarted");
        Assert.Equal("automatic", started.GetProperty("context").GetProperty("trigger").GetString());
        Assert.DoesNotContain(records, e => e.GetProperty("eventId").GetString() == "OperationFailure");
    }

    [Fact]
    public async Task TraceIsOptInAndCriticalIsImmediatelyReadable()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        log.Record(DiagnosticEvent.DispatchCompleted, DiagnosticCategory.Unexpected, DiagnosticSeverity.Debug);
        log.Fatal(DiagnosticEvent.UnhandledFailure, new InvalidOperationException("canary"), terminating: true);
        var critical = Assert.Single(Directory.GetFiles(log.DirectoryPath, "critical-*.jsonl"));
        using var record = JsonDocument.Parse(File.ReadAllText(critical));
        Assert.True(record.RootElement.GetProperty("terminating").GetBoolean());
        Assert.DoesNotContain("canary", record.RootElement.ToString());
        await log.FlushAsync();
        Assert.Empty(Directory.GetFiles(log.DirectoryPath, "trace-*.jsonl"));
        log.Fatal(DiagnosticEvent.UnhandledFailure, null, terminating: true);
        Assert.Single(Directory.GetFiles(log.DirectoryPath, "critical-*.jsonl"));
    }

    [Fact]
    public async Task RetentionUsesOriginalTimeAndCalendarMonthAndPreservesUnknownFiles()
    {
        clock.Now = new(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);
        using (var log = new FileDiagnostics(root, new DiagnosticOptions { TraceEnabled = true }, clock))
        {
            log.Record(DiagnosticEvent.DispatchCompleted, DiagnosticCategory.Unexpected, DiagnosticSeverity.Debug);
            log.Fatal(DiagnosticEvent.UnhandledFailure, null, true);
            await log.FlushAsync();
        }
        var folder = Path.Combine(root, "logs");
        var unknown = Path.Combine(folder, "user-notes.txt");
        File.WriteAllText(unknown, "keep");
        var unknownExtension = DiagnosticFiles.NewName(folder, "application", clock.Now, "json");
        File.WriteAllText(unknownExtension, "keep");
        clock.Now += TimeSpan.FromHours(72);
        using (var log = new FileDiagnostics(root, clock: clock))
        {
            await log.FlushAsync();
            Assert.Empty(Directory.GetFiles(folder, "trace-*.jsonl"));
            Assert.Single(Directory.GetFiles(folder, "critical-*.jsonl"));
        }
        clock.Now = new(2026, 2, 28, 12, 0, 0, TimeSpan.Zero);
        using (var log = new FileDiagnostics(root, clock: clock))
        {
            await log.FlushAsync();
            Assert.Empty(Directory.GetFiles(folder, "critical-*.jsonl"));
            Assert.DoesNotContain("20260131", string.Join(',', Directory.GetFiles(folder, "application-*.jsonl")));
        }
        DiagnosticFiles.DeleteOwned(root);
        Assert.Equal("keep", File.ReadAllText(unknown));
        Assert.Equal("keep", File.ReadAllText(unknownExtension));
        Assert.Equal(2, Directory.GetFiles(folder).Length);
    }

    [Fact]
    public async Task UnavailableStorageDoesNotThrowOrHideLoss()
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "logs"), "blocked");
        using var log = new FileDiagnostics(root, clock: clock);
        log.Failure(DiagnosticEvent.OperationFailure, new IOException("canary"));
        await log.FlushAsync();
        log.Fatal(DiagnosticEvent.UnhandledFailure, null, true);
        Assert.True(log.LostRecords > 0);
    }

    [Fact]
    public async Task CriticalWriterDoesNotDependOnLockedExpiredApplicationFile()
    {
        var folder = Path.Combine(root, "logs");
        Directory.CreateDirectory(folder);
        var stale = DiagnosticFiles.NewName(folder, "application", clock.Now.AddDays(-8), "jsonl");
        using var locked = new FileStream(stale, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using var log = new FileDiagnostics(root, clock: clock);
        log.Fatal(DiagnosticEvent.UnhandledFailure, new IOException("canary"), true);
        Assert.Single(Directory.GetFiles(folder, "critical-*.jsonl"));
        await log.FlushAsync();
        Assert.True(log.LostRecords > 0);
    }

    [Fact]
    public async Task ConcurrentOperationsKeepDistinctOpaqueAccountAndParentReferences()
    {
        using var log = new FileDiagnostics(root);
        var accounts = new[] { Guid.NewGuid(), Guid.NewGuid() };
        await Task.WhenAll(accounts.Select(async account =>
        {
            using var parent = log.Begin(DiagnosticOperation.Refresh, account);
            await Task.Yield();
            using var child = log.Begin(DiagnosticOperation.Persistence);
            log.Signal(DiagnosticEvent.RecoveryCompleted);
        }));
        await log.FlushAsync();
        var records = Directory.GetFiles(log.DirectoryPath, "application-*.jsonl").SelectMany(p => ReadShared(p).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(s => JsonDocument.Parse(s).RootElement.Clone()).Where(e => e.GetProperty("eventId").GetString() == "OperationStarted").ToArray();
        Assert.Equal(4, records.Length);
        foreach (var account in accounts)
        {
            var group = records.Where(e => e.GetProperty("accountReference").GetGuid() == account).ToArray();
            Assert.Equal(2, group.Length);
            var parent = Assert.Single(group, e => e.GetProperty("operation").GetString() == "Refresh");
            var child = Assert.Single(group, e => e.GetProperty("operation").GetString() == "Persistence");
            Assert.Equal(parent.GetProperty("operationId").GetGuid(), child.GetProperty("parentOperationId").GetGuid());
        }
    }

    [Fact]
    public async Task QueueSaturationIsReportedAndRepeatedBindingWarningsAreCoalesced()
    {
        using var log = new FileDiagnostics(root, new DiagnosticOptions { QueueBytes = 4096, QueueEntries = 3 });
        for (var i = 0; i < 1000; i++) log.Signal(DiagnosticEvent.BindingFailure, DiagnosticSeverity.Warning);
        await log.FlushAsync();
        var preview = await log.PreviewAsync();
        Assert.InRange(preview.Split("BindingFailure", StringSplitOptions.None).Length - 1, 1, 2);
        for (var i = 0; i < 10000; i++) log.Signal(DiagnosticEvent.OperationStarted);
        await log.FlushAsync();
        Assert.True(log.LostRecords > 0);
        Assert.Contains("LoggerHealth", await log.PreviewAsync());
    }

    [Fact]
    public async Task RepeatedFailureWritesOneDetailedRecordAndACountedSummaryOnRecovery()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        var first = clock.Now;
        for (var i = 0; i < 5; i++)
        {
            log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
            clock.Now += TimeSpan.FromSeconds(20);
        }
        log.Recovered(DiagnosticEvent.TrayFailure);
        Assert.True(await log.FlushAsync());

        var tray = Records(log).Where(e => e.GetProperty("eventId").GetString() == "TrayFailure").ToArray();
        Assert.Equal(2, tray.Length);
        var detailed =Assert.Single(tray, e => e.TryGetProperty("exception", out var exception) && exception.ValueKind == JsonValueKind.Object);
        Assert.Equal("Error", detailed.GetProperty("severity").GetString());
        Assert.Equal(unchecked((int)0x80004005), detailed.GetProperty("exception").GetProperty("hResult").GetInt32());
        var summary = Assert.Single(tray, e => e.GetProperty("severity").GetString() == "Warning");
        Assert.Equal(4, summary.GetProperty("context").GetProperty("suppressedCount").GetInt64());
        Assert.Equal(first, summary.GetProperty("context").GetProperty("firstAt").GetDateTimeOffset());
        Assert.Equal(first + TimeSpan.FromSeconds(80), summary.GetProperty("context").GetProperty("lastAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task AFailureAfterRecoveryIsLoggedInDetailAgain()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        log.Recovered(DiagnosticEvent.TrayFailure);
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        log.Recovered(DiagnosticEvent.TrayFailure);
        log.Recovered(DiagnosticEvent.TrayFailure);
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        Assert.True(await log.FlushAsync());

        var tray = Records(log).Where(e => e.GetProperty("eventId").GetString() == "TrayFailure").ToArray();
        Assert.Equal(2, tray.Length);
        Assert.All(tray, e => Assert.Equal("Error", e.GetProperty("severity").GetString()));
    }

    [Fact]
    public async Task APendingRepeatedFailureSummaryIsWrittenAtStop()
    {
        var log = new FileDiagnostics(root, clock: clock);
        for (var i = 0; i < 3; i++) log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        await log.StopAsync();

        var tray = Records(log).Where(e => e.GetProperty("eventId").GetString() == "TrayFailure").ToArray();
        Assert.Equal(2, tray.Length);
        var summary = Assert.Single(tray, e => e.GetProperty("severity").GetString() == "Warning");
        Assert.Equal(2, summary.GetProperty("context").GetProperty("suppressedCount").GetInt64());
    }

    [Fact]
    public async Task APendingRepeatedFailureCountIsWrittenOnAFatalIncident()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        for (var i = 0; i < 3; i++) log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        Assert.True(await log.FlushAsync());
        log.Fatal(DiagnosticEvent.UnhandledFailure, null, terminating: true);

        // No stop and no further flush: the process exits right after Fatal.
        var tray = Records(log).Where(e => e.GetProperty("eventId").GetString() == "TrayFailure").ToArray();
        var detailed = Assert.Single(tray, e => e.GetProperty("severity").GetString() == "Error");
        var summary = Assert.Single(tray, e => e.GetProperty("severity").GetString() == "Warning");
        Assert.Equal(2, summary.GetProperty("context").GetProperty("suppressedCount").GetInt64());
        Assert.Equal(IncidentId(detailed), IncidentId(summary));
    }

    [Fact]
    public async Task TheHourlySweepWritesPendingCountsAndKeepsTheStreakOpen()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        var first = clock.Now;
        for (var i = 0; i < 3; i++) log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        Assert.True(await log.FlushAsync());
        clock.Now += TimeSpan.FromMinutes(61);
        Assert.True(await log.FlushAsync());

        var tray = Records(log).Where(e => e.GetProperty("eventId").GetString() == "TrayFailure").ToArray();
        var detailed = Assert.Single(tray, e => e.GetProperty("severity").GetString() == "Error");
        var hourly = Assert.Single(tray, e => e.GetProperty("severity").GetString() == "Warning");
        Assert.Equal(2, hourly.GetProperty("context").GetProperty("suppressedCount").GetInt64());
        Assert.Equal(first, hourly.GetProperty("context").GetProperty("firstAt").GetDateTimeOffset());
        Assert.Equal(IncidentId(detailed), IncidentId(hourly));

        var later = clock.Now;
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        log.Recovered(DiagnosticEvent.TrayFailure);
        Assert.True(await log.FlushAsync());

        tray = Records(log).Where(e => e.GetProperty("eventId").GetString() == "TrayFailure").ToArray();
        Assert.Single(tray, e => e.GetProperty("severity").GetString() == "Error");
        var summaries = tray.Where(e => e.GetProperty("severity").GetString() == "Warning").OrderBy(e => e.GetProperty("sequence").GetInt64()).ToArray();
        Assert.Equal(2, summaries.Length);
        var recovered = summaries[1];
        Assert.Equal(1, recovered.GetProperty("context").GetProperty("suppressedCount").GetInt64());
        Assert.Equal(later, recovered.GetProperty("context").GetProperty("firstAt").GetDateTimeOffset());
        Assert.Equal(IncidentId(detailed), IncidentId(recovered));
    }

    [Fact]
    public async Task APendingRepeatedFailureCountIsWrittenBeforeAnUpdateInstallStarts()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        for (var i = 0; i < 3; i++) log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        log.Update(DiagnosticEvent.UpdateInstallStarted, new(Automatic: true));
        Assert.True(await log.FlushAsync());

        var records = Records(log);
        var summary = Assert.Single(records, e => e.GetProperty("eventId").GetString() == "TrayFailure" && e.GetProperty("severity").GetString() == "Warning");
        Assert.Equal(2, summary.GetProperty("context").GetProperty("suppressedCount").GetInt64());
        var started = Assert.Single(records, e => e.GetProperty("eventId").GetString() == "UpdateInstallStarted");
        Assert.True(summary.GetProperty("sequence").GetInt64() < started.GetProperty("sequence").GetInt64());
    }

    [Fact]
    public async Task EachDistinctFailureInAStreakIsLoggedInDetailAndSummarizedWithItsIncident()
    {
        using var log = new FileDiagnostics(root, clock: clock);
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, PlatformFailure());
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, OtherFailure());
        log.RepeatedFailure(DiagnosticEvent.TrayFailure, OtherFailure());
        log.Recovered(DiagnosticEvent.TrayFailure);
        Assert.True(await log.FlushAsync());

        var tray = Records(log).Where(e => e.GetProperty("eventId").GetString() == "TrayFailure").ToArray();
        var detailed = tray.Where(e => e.GetProperty("severity").GetString() == "Error").ToArray();
        Assert.Equal(2, detailed.Length);
        Assert.Equal(["System.InvalidOperationException", "System.Runtime.InteropServices.COMException"],
            detailed.Select(e => e.GetProperty("exception").GetProperty("type").GetString()).Order(StringComparer.Ordinal));
        var summaries = tray.Where(e => e.GetProperty("severity").GetString() == "Warning").OrderBy(e => e.GetProperty("sequence").GetInt64()).ToArray();
        Assert.Equal(2, summaries.Length);
        Assert.All(summaries, e => Assert.Equal(1, e.GetProperty("context").GetProperty("suppressedCount").GetInt64()));
        Assert.Equal(detailed.Select(IncidentId).Order(), summaries.Select(IncidentId).Order());
        Assert.NotEqual(IncidentId(detailed[0]), IncidentId(detailed[1]));
    }

    private static Guid IncidentId(JsonElement record) => record.GetProperty("context").GetProperty("incidentId").GetGuid();

    private static Exception OtherFailure()
    {
        try { throw new InvalidOperationException("canary"); }
        catch (Exception error) { return error; }
    }

    private static Exception PlatformFailure()
    {
        // The GDI+ failure the tray saw: E_FAIL from the platform.
        try { throw System.Runtime.InteropServices.Marshal.GetExceptionForHR(unchecked((int)0x80004005))!; }
        catch (Exception error) { return error; }
    }

    private static JsonElement[] Records(FileDiagnostics log) => Directory.GetFiles(log.DirectoryPath, "application-*.jsonl")
        .SelectMany(p => ReadShared(p).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        .Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToArray();

    [Fact]
    public void CleanupRejectsRedirectedNamespace()
    {
        Directory.CreateDirectory(root);
        var target = Path.Combine(root, "target");
        Directory.CreateDirectory(target);
        var link = Path.Combine(root, "logs");
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/c mklink /J \"{link}\" \"{target}\"", UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        })!;
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        try { Assert.Throws<IOException>(() => DiagnosticFiles.DeleteOwned(root)); Assert.Empty(Directory.GetFiles(target)); }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public async Task PreviewFiltersExpiryAndSurvivesTruncatedLastRecord()
    {
        using (var log = new FileDiagnostics(root, clock: clock))
        {
            log.Signal(DiagnosticEvent.RecoveryCompleted);
            await log.FlushAsync();
        }
        var path = Assert.Single(Directory.GetFiles(Path.Combine(root, "logs"), "application-*.jsonl"));
        File.AppendAllText(path, "{\"private\":\"canary");
        using var reader = new FileDiagnostics(root, clock: clock);
        var preview = await reader.PreviewAsync();
        Assert.Contains("RecoveryCompleted", preview);
        Assert.DoesNotContain("canary", preview);
        clock.Now += TimeSpan.FromHours(168);
        Assert.DoesNotContain("RecoveryCompleted", await reader.PreviewAsync());
    }

    [Fact]
    public async Task FutureInvalidAndSizeEvictedFilesDoNotSurvivePruning()
    {
        var folder = Path.Combine(root, "logs");
        Directory.CreateDirectory(folder);
        var future = DiagnosticFiles.NewName(folder, "response", clock.Now.AddHours(1), "json");
        var invalid = Path.Combine(folder, "response-20261399T1200000000000Z-" + Guid.NewGuid().ToString("N") + ".json");
        var old = DiagnosticFiles.NewName(folder, "response", clock.Now.AddHours(-2), "json");
        var newer = DiagnosticFiles.NewName(folder, "response", clock.Now.AddHours(-1), "json");
        foreach (var path in new[] { future, invalid, old, newer }) File.WriteAllText(path, new string(' ', 1024));
        using var log = new FileDiagnostics(root, new DiagnosticOptions { ResponseBytes = 1024 }, clock);
        await log.FlushAsync();
        Assert.False(File.Exists(future)); Assert.False(File.Exists(invalid)); Assert.False(File.Exists(old));
        Assert.True(File.Exists(newer));
    }

    [Fact]
    public void EmergencyPathStillWorksAfterOrdinaryWriterDisposal()
    {
        var log = new FileDiagnostics(root, clock: clock);
        log.Dispose();
        log.Fatal(DiagnosticEvent.DisposalFailure, new InvalidOperationException("late-canary"), true);
        var critical = Assert.Single(Directory.GetFiles(log.DirectoryPath, "critical-*.jsonl"));
        Assert.Contains("DisposalFailure", File.ReadAllText(critical));
        Assert.DoesNotContain("late-canary", File.ReadAllText(critical));
    }

    [Fact]
    public async Task HourlySweepExpiresIdleClassesAndMixedAgeRollsByEarliestRecord()
    {
        using var log = new FileDiagnostics(root, new DiagnosticOptions { TraceEnabled = true }, clock);
        log.Record(DiagnosticEvent.DispatchCompleted, DiagnosticCategory.Unexpected, DiagnosticSeverity.Debug);
        Assert.True(await log.FlushAsync());
        var trace = Assert.Single(Directory.GetFiles(log.DirectoryPath, "trace-*.jsonl"));
        clock.Now += TimeSpan.FromMinutes(30);
        log.Record(DiagnosticEvent.DispatchCompleted, DiagnosticCategory.Unexpected, DiagnosticSeverity.Debug);
        Assert.True(await log.FlushAsync());
        Assert.Single(Directory.GetFiles(log.DirectoryPath, "trace-*.jsonl"));
        clock.Now += TimeSpan.FromHours(71.5);
        Assert.True(await log.FlushAsync());
        Assert.False(File.Exists(trace));
        Assert.NotEmpty(Directory.GetFiles(log.DirectoryPath, "application-*.jsonl"));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    internal static string ReadShared(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(file);
        return reader.ReadToEnd();
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public override long GetTimestamp() => Now.UtcTicks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    }
}
