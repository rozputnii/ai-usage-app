using System.Diagnostics;
using System.Text.Json;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Diagnostics;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class DiagnosticCrashTests
{
    // The probe prints this line once its log exists and is flushed. The test bounds start there, so
    // Microsoft Defender's first-run scan of a freshly built probe (seconds on this host) is excluded.
    private const string ReadyLine = "AIU_PROBE_READY";
    private static readonly TimeSpan StartupBound = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task ManagedChildCrashLeavesCriticalStackBeforeTermination()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-crash-" + Guid.NewGuid().ToString("N"));
        using var child = StartProbe(root, "crash");
        var stderr = child.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        try
        {
            await WaitUntilReadyAsync(child);
            var stdout = child.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            await child.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
            await Task.WhenAll(stdout, stderr);
            Assert.NotEqual(0, child.ExitCode);
            var critical = Assert.Single(Directory.GetFiles(Path.Combine(root, "logs"), "critical-*.jsonl"));
            using var document = JsonDocument.Parse(File.ReadAllText(critical));
            Assert.True(document.RootElement.GetProperty("terminating").GetBoolean());
            Assert.Contains("Program", document.RootElement.GetRawText());
            Assert.DoesNotContain("crash-canary", document.RootElement.GetRawText());
            using var restarted = new FileDiagnostics(root);
            Assert.Contains("PreviousExitUnknown", await restarted.PreviewAsync());
        }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); child.WaitForExit(); }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static Process StartProbe(string root, string mode)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Directory.Packages.props"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var configuration = typeof(DiagnosticCrashTests).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
            .OfType<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration;
        var start = new ProcessStartInfo(Path.Combine(repository.FullName, "tools", "AiUsage.DiagnosticsProbe", "bin", configuration, "net10.0", "AiUsage.DiagnosticsProbe.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(root);
        start.ArgumentList.Add(mode);
        return Process.Start(start)!;
    }

    private static async Task WaitUntilReadyAsync(Process child)
    {
        bool ready;
        try { ready = await ReadUntilReadyAsync(child.StandardOutput).WaitAsync(StartupBound, TestContext.Current.CancellationToken); }
        catch (TimeoutException) { ready = false; }
        Assert.True(ready, $"The diagnostics probe never became ready: no {ReadyLine} line on stdout before it closed or within {StartupBound.TotalSeconds:0} s of launch.");
    }

    private static async Task<bool> ReadUntilReadyAsync(StreamReader output)
    {
        while (await output.ReadLineAsync() is { } line)
            if (line == ReadyLine) return true;
        return false;
    }

    [Fact]
    public async Task ForcedKillLeavesPriorRecordsAndUnknownExitWithoutInventedCritical()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-crash-" + Guid.NewGuid().ToString("N"));
        using var child = StartProbe(root, "wait");
        var stderr = child.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        try
        {
            await WaitUntilReadyAsync(child);
            var wait = Stopwatch.StartNew();
            var folder = Path.Combine(root, "logs");
            while (wait.Elapsed < TimeSpan.FromSeconds(10) && (!Directory.Exists(folder) ||
                !Directory.GetFiles(folder, "application-*.jsonl").Any(p => new FileInfo(p).Length > 0)))
                await Task.Delay(50, TestContext.Current.CancellationToken);
            Assert.True(Directory.Exists(folder));
            child.Kill(entireProcessTree: true);
            // WaitForExitAsync can return once the exit code is set, before Windows has closed the killed
            // probe's handles; its session lease would still read as a live process. Wait on the process object.
            child.WaitForExit();
            await stderr;
            Assert.Empty(Directory.GetFiles(folder, "critical-*.jsonl"));
            using var restarted = new FileDiagnostics(root);
            Assert.Contains("PreviousExitUnknown", await restarted.PreviewAsync());
        }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); child.WaitForExit(); }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

}
