using System.Globalization;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class LocalDiagnosticSinkTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-diagnostics-" + Guid.NewGuid().ToString("N"));
    private string Log => Path.Combine(root, "diagnostics.v1.log");
    private readonly Clock clock = new();

    [Fact]
    public void RecordsOnlyDefinedCodesAndDropsUnknownNestedFieldsOnRewrite()
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Log, "{\"future\":{\"token\":\"sk-synthetic-secret\",\"provider\":\"opaque-provider-id\"}}\n" +
            clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture) + "\tOperationFailure\tUnexpected\topaque-provider-id\n");
        var sink = new LocalDiagnosticSink(root, clock);
        sink.Record(DiagnosticEvent.OperationFailure, DiagnosticCategory.InvalidOperation);
        sink.Record((DiagnosticEvent)999, DiagnosticCategory.Unexpected);
        sink.Record(DiagnosticEvent.StartupFailure, (DiagnosticCategory)999);
        var record = Assert.Single(File.ReadAllLines(Log));
        Assert.Equal(clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture) + "\tOperationFailure\tInvalidOperation", record);
        Assert.DoesNotContain("synthetic-secret", record);
        Assert.DoesNotContain("opaque-provider-id", record);
    }

    [Fact]
    public void RestartPrunesExpiredRecordsAndWritesStayWithinSizeBound()
    {
        var sink = new LocalDiagnosticSink(root, clock);
        sink.Record(DiagnosticEvent.StartupFailure, DiagnosticCategory.Unexpected);
        clock.Now += TimeSpan.FromDays(6);
        sink.Record(DiagnosticEvent.ShutdownFailure, DiagnosticCategory.Io);
        clock.Now += TimeSpan.FromDays(2);
        sink = new LocalDiagnosticSink(root, clock);
        Assert.DoesNotContain("StartupFailure", File.ReadAllText(Log));
        Assert.Contains("ShutdownFailure", File.ReadAllText(Log));
        // Seed a near-capacity log to exercise eviction without thousands of disk writes.
        var line = clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture) + "\tShutdownFailure\tIo\n";
        File.WriteAllText(Log, string.Concat(Enumerable.Repeat(line, 65536 / line.Length)));
        sink.Record(DiagnosticEvent.DisposalFailure, DiagnosticCategory.AccessDenied);
        Assert.InRange(new FileInfo(Log).Length, 1, 65536);
        Assert.Contains("DisposalFailure\tAccessDenied", File.ReadLines(Log).Last());
    }

    [Fact]
    public void LockedOrInvalidStorageDoesNotReplaceOriginalFailure()
    {
        var sink = new LocalDiagnosticSink(root, clock);
        sink.Record(DiagnosticEvent.StartupFailure, DiagnosticCategory.Unexpected);
        using (var locked = File.Open(Log, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.Null(Record.Exception(() => sink.Record(DiagnosticEvent.ShutdownFailure, DiagnosticCategory.Io)));
        var blocked = new LocalDiagnosticSink(Log, clock);
        Assert.Null(Record.Exception(() => blocked.Record(DiagnosticEvent.DisposalFailure, DiagnosticCategory.Io)));
        Assert.Single(File.ReadAllLines(Log));
    }

    [Fact]
    public void RedirectedDirectoryIsRefusedWithoutChangingTarget()
    {
        Directory.CreateDirectory(root);
        var target = Path.Combine(root, "target");
        var link = Path.Combine(root, "link");
        Directory.CreateDirectory(target);
        // Junction creation needs no developer-mode or elevation on Windows.
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe")
        {
            Arguments = $"/c mklink /J \"{link}\" \"{target}\"", UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        })!;
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        try
        {
            new LocalDiagnosticSink(link, clock).Record(DiagnosticEvent.StartupFailure, DiagnosticCategory.Unexpected);
            Assert.Empty(Directory.GetFileSystemEntries(target));
        }
        finally { Directory.Delete(link); }
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
