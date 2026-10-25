using System.Diagnostics;
using System.Text;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Claude;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ClaudeCodeProcessTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.ClaudeCode.Process.Tests", Guid.NewGuid().ToString("N"));
    private static readonly string Cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");

    private string Folder(string name, bool withClaude)
    {
        var folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        if (withClaude)
            File.WriteAllBytes(Path.Combine(folder, "claude.exe"), []);
        return folder;
    }

    [Fact]
    public void LocateSearchesPathInOrderThenTheInstallerFolder()
    {
        var empty = Folder("empty", false);
        var first = Folder("first", true);
        var second = Folder("second", true);
        var profile = Folder("profile", false);
        Folder(Path.Combine("profile", ".local", "bin"), true);
        Assert.Equal(Path.Combine(first, "claude.exe"),
            ClaudeCodeProcess.Locate(string.Join(Path.PathSeparator, empty, "\"" + first + "\"", second), profile));
        Assert.Equal(Path.Combine(profile, ".local", "bin", "claude.exe"), ClaudeCodeProcess.Locate(empty, profile));
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("")]
    [InlineData(null)]
    public void LocateIgnoresRelativeOrMissingEntries(string? path)
    {
        Folder("relative", true);
        Assert.Null(ClaudeCodeProcess.Locate(path, null));
    }

    [Fact]
    public async Task AMissingExecutableIsProviderUnavailable()
    {
        var process = new ClaudeCodeProcess(Path.Combine(root, "work"), () => null);
        var error = await Assert.ThrowsAsync<ProviderException>(() => process.RunAsync(["auth", "status"], TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.ProviderUnavailable, error.Kind);
    }

    [Fact]
    public async Task ArgumentsReachTheProcessLiterallyInTheOwnedFolder()
    {
        var work = Path.Combine(root, "work");
        var process = new ClaudeCodeProcess(work, () => Cmd);
        var run = await process.RunAsync(["/d", "/c", "echo", "/usage", "&&", "cd"], TestContext.Current.CancellationToken);
        var output = Encoding.UTF8.GetString(run.Output);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("/usage", output, StringComparison.Ordinal);
        Assert.Contains(work, output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TheExitCodeIsReported()
    {
        var run = await new ClaudeCodeProcess(Path.Combine(root, "work"), () => Cmd).RunAsync(["/d", "/c", "exit", "3"], TestContext.Current.CancellationToken);
        Assert.Equal(3, run.ExitCode);
    }

    [Fact]
    public async Task ARunPastTheDeadlineIsKilledAndReportedAsTimeout()
    {
        var process = new ClaudeCodeProcess(Path.Combine(root, "work"), () => Cmd, TimeSpan.FromSeconds(1));
        var watch = Stopwatch.StartNew();
        var error = await Assert.ThrowsAsync<ProviderException>(() =>
            process.RunAsync(["/d", "/c", "ping", "-n", "30", "127.0.0.1"], TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.Timeout, error.Kind);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task OutputPastTheCapIsRejectedAtOnceAndNotReportedAsTimeout()
    {
        var large = Path.Combine(Folder("large", false), "output.txt");
        File.WriteAllText(large, new string('x', 3 * 1024 * 1024));
        var process = new ClaudeCodeProcess(Path.Combine(root, "work"), () => Cmd, TimeSpan.FromSeconds(20));
        var watch = Stopwatch.StartNew();
        var error = await Assert.ThrowsAsync<ProviderException>(() => process.RunAsync(["/d", "/c", "type", large], TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.InvalidResponse, error.Kind);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task AWorkingFolderThatCannotBeCreatedIsAProviderFailure()
    {
        Directory.CreateDirectory(root);
        var blocked = Path.Combine(root, "blocked");
        File.WriteAllBytes(blocked, []);
        var process = new ClaudeCodeProcess(blocked, () => Cmd);
        var error = await Assert.ThrowsAsync<ProviderException>(() => process.RunAsync(["/d", "/c", "exit", "0"], TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.ProviderUnavailable, error.Kind);
    }

    [Fact]
    public async Task CancellationKillsTheRunAndIsNotATimeout()
    {
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var process = new ClaudeCodeProcess(Path.Combine(root, "work"), () => Cmd);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => process.RunAsync(["/d", "/c", "ping", "-n", "30", "127.0.0.1"], cancel.Token));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
