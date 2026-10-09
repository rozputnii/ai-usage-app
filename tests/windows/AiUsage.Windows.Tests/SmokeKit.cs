using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core.AutomationElements;
using Xunit;

namespace AiUsage.Windows.Tests;

/// <summary>R5 smoke helpers: per-launch tray identity, polling re-find, failure evidence and the local result history.</summary>
internal static class SmokeKit
{
    /// <summary>OD-23: a development build started with this id names its tray icon "AI Usage &lt;id&gt;".</summary>
    internal const string TrayIdVariable = "AIU_SMOKE_TRAY_ID";

    /// <summary>A fresh 8-hex-digit id for one launched app, so its tray icon matches no other AI Usage instance.</summary>
    internal static string NewTrayId() => Guid.NewGuid().ToString("N")[..8];

    internal static string TrayName(string id) => "AI Usage " + id;

    /// <summary>
    /// Re-runs the query until it returns an element or the bound expires, so no UIA element is held across a wait.
    /// Queries should match an AutomationId where the element has one, and a name only otherwise.
    /// </summary>
    internal static AutomationElement Find(Func<AutomationElement?> query, string what, TimeSpan? bound = null)
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            try { if (query() is { } found) return found; }
            catch (COMException) { /* A published snapshot may replace a UIA element. */ }
            Assert.True(elapsed.Elapsed < (bound ?? TimeSpan.FromSeconds(30)), "Not found: " + what);
            Thread.Sleep(100);
        }
    }

    /// <summary>Saves a screenshot and a UI-tree dump (control type, automation id, name, bounds) of the failed window.</summary>
    internal static void SaveFailure(string evidence, string prefix, Func<AutomationElement?> window)
    {
        AutomationElement? target;
        try { target = window(); }
        catch (COMException) { return; }
        if (target is null) return;
        try
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            using var capture = target.Capture();
            capture.Save(Path.Combine(evidence, prefix + "-failure.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        catch (Exception) { /* The tree dump below still explains the failure. */ }
        var tree = new StringBuilder();
        void Dump(AutomationElement element, int depth)
        {
            var p = element.Properties;
            tree.Append(' ', depth * 2).Append(CultureInfo.InvariantCulture,
                $"{p.ControlType.ValueOrDefault}\t{p.AutomationId.ValueOrDefault}\t{p.Name.ValueOrDefault}\t{p.BoundingRectangle.ValueOrDefault}").AppendLine();
            foreach (var child in element.FindAllChildren()) Dump(child, depth + 1);
        }
        try { Dump(target, 0); }
        catch (COMException) { tree.AppendLine("(the tree changed while it was read)"); }
        File.WriteAllText(Path.Combine(evidence, prefix + "-failure-tree.txt"), tree.ToString());
    }

    /// <summary>Appends this test's result to the ignored .ai-usage-local/smoke-history.csv; call it while the test class is disposed.</summary>
    internal static void RecordResult(TimeSpan duration)
    {
        var root = RepositoryRoot();
        if (root is null) return;
        var context = TestContext.Current;
        var file = Path.Combine(root, ".ai-usage-local", "smoke-history.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        static string Field(string? value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        var row = string.Join(',',
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            Field(context.Test?.TestDisplayName),
            context.TestState?.Result.ToString() ?? "Unknown",
            duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture),
            Commit.Value);
        if (!File.Exists(file)) File.WriteAllText(file, "utc,test,outcome,duration_s,commit" + Environment.NewLine);
        File.AppendAllText(file, row + Environment.NewLine);
    }

    private static string? RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (Path.Exists(Path.Combine(directory.FullName, ".git"))) return directory.FullName;
        return null;
    }

    private static readonly Lazy<string> Commit = new(() =>
    {
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git", "rev-parse --short HEAD")
            {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true, WorkingDirectory = RepositoryRoot() ?? AppContext.BaseDirectory,
            });
            var hash = git?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
            return git is not null && git.WaitForExit(5000) && git.ExitCode == 0 ? hash : string.Empty;
        }
        catch (System.ComponentModel.Win32Exception) { return string.Empty; }
    });
}
