using System.Diagnostics;
using System.Text.Json;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Diagnostics;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class DiagnosticCrashTests
{
    [Fact]
    public async Task ManagedChildCrashLeavesCriticalStackBeforeTermination()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-crash-" + Guid.NewGuid().ToString("N"));
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
        start.ArgumentList.Add("crash");
        using var child = Process.Start(start)!;
        var stdout = child.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = child.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        try
        {
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
            if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(TestContext.Current.CancellationToken); }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

}
