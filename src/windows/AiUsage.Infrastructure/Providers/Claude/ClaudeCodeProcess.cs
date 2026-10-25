using System.Diagnostics;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>Output and exit code of one finished Claude Code run; the output is untrusted and never logged.</summary>
internal sealed record ClaudeCodeRun(int ExitCode, byte[] Output)
{
    public override string ToString() => "ClaudeCodeRun (redacted)";
}

/// <summary>Starts the installed, unmodified Claude Code binary; the seam the session tests replace.</summary>
internal interface IClaudeCode
{
    /// <summary>The full path of `claude.exe`, or null when it is not installed where the app looks.</summary>
    string? Locate();
    Task<ClaudeCodeRun> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}

/// <summary>
/// Runs `claude.exe` directly, never through a shell: a shell can rewrite `/usage` into a path,
/// and the text then reaches the model as a prompt. Claude Code keeps its own sign-in; the app
/// passes no credential and reads none.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ClaudeCodeProcess(string workingDirectory, Func<string?>? locate = null, TimeSpan? timeout = null) : IClaudeCode
{
    private const int MaxOutputBytes = 1024 * 1024;
    private readonly TimeSpan deadline = timeout ?? TimeSpan.FromSeconds(30);

    public string? Locate() => (locate ?? LocateInstalled)();

    /// <summary>PATH first, then the native installer's default folder.</summary>
    internal static string? LocateInstalled() => Locate(Environment.GetEnvironmentVariable("PATH"),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    internal static string? Locate(string? path, string? profile)
    {
        var folders = (path ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => folder.Trim('"'))
            .Concat(string.IsNullOrEmpty(profile) ? [] : [Path.Combine(profile, ".local", "bin")]);
        foreach (var folder in folders)
        {
            try
            {
                if (!Path.IsPathFullyQualified(folder))
                    continue;
                var candidate = Path.Combine(folder, "claude.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    public async Task<ClaudeCodeRun> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var executable = Locate() ?? throw new ProviderException(ProviderFailureKind.ProviderUnavailable);
        try { Directory.CreateDirectory(workingDirectory); }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        { throw new ProviderException(ProviderFailureKind.ProviderUnavailable); }
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start())
                throw new ProviderException(ProviderFailureKind.ProviderUnavailable);
        }
        catch (System.ComponentModel.Win32Exception) { throw new ProviderException(ProviderFailureKind.ProviderUnavailable); }
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(deadline);
        Task<byte[]>? output = null;
        Task? error = null;
        try
        {
            process.StandardInput.Close();
            output = ReadCappedAsync(process.StandardOutput.BaseStream, limit.Token);
            error = process.StandardError.BaseStream.CopyToAsync(Stream.Null, limit.Token);
            var exited = process.WaitForExitAsync(limit.Token);
            // Output past the cap fails the read while the child still waits on a full pipe: stop at once.
            if (await Task.WhenAny(output, exited).ConfigureAwait(false) == output)
                await output.ConfigureAwait(false);
            await exited.ConfigureAwait(false);
            var bytes = await output.ConfigureAwait(false);
            await error.ConfigureAwait(false);
            return new(process.ExitCode, bytes);
        }
        catch (OperationCanceledException)
        {
            Stop(process, output, error);
            cancellationToken.ThrowIfCancellationRequested();
            throw new ProviderException(ProviderFailureKind.Timeout);
        }
        catch (ProviderException)
        {
            Stop(process, output, error);
            throw;
        }
        catch (IOException)
        {
            Stop(process, output, error);
            throw new ProviderException(ProviderFailureKind.ProviderUnavailable);
        }
    }

    private static async Task<byte[]> ReadCappedAsync(Stream stream, CancellationToken token)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, token).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaxOutputBytes)
                throw new ProviderException(ProviderFailureKind.InvalidResponse);
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static void Stop(Process process, params Task?[] pending)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        // The abandoned pipe reads end with the process; observe them so they never surface as unobserved faults.
        foreach (var task in pending)
            _ = task?.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
    }
}

/// <summary>Whether the Add account menu can offer the Claude Code connection on this PC.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class ClaudeCodeInstallation
{
    public static bool IsInstalled() => ClaudeCodeProcess.LocateInstalled() is not null;
}
