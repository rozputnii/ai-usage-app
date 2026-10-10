using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
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

    /// <summary>Re-runs the check until it holds or the bound expires; a replaced UIA element only repeats the check.</summary>
    internal static bool Wait(Func<bool> check, TimeSpan? bound = null)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < (bound ?? TimeSpan.FromSeconds(30)))
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            try { if (check()) return true; }
            catch (COMException) { /* A published snapshot may replace a UIA element. */ }
            Thread.Sleep(100);
        }
        return false;
    }

    /// <summary>The visible top-level windows of one process: the main window and its popups (flyouts, menus, tooltips).</summary>
    internal static IEnumerable<AutomationElement> OwnedWindows(AutomationElement desktop, int processId) =>
        desktop.FindAllChildren().Where(w => GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == processId);

    /// <summary>The first element that matches in the windows of one process, so another application's element never matches.</summary>
    internal static AutomationElement? FindOwned(AutomationElement desktop, int processId, Func<ConditionFactory, ConditionBase> condition) =>
        OwnedWindows(desktop, processId).Select(w => w.FindFirstDescendant(condition)).FirstOrDefault(e => e is not null);

    /// <summary>
    /// Gives the window keyboard input. Windows refuses SetForegroundWindow to a process that did not send the last input. A
    /// click on the window could land on another window that overlaps it, or on a title-row button; an injected Alt tap lifts
    /// the lock instead.
    /// </summary>
    internal static void Focus(Window window)
    {
        var handle = window.Properties.NativeWindowHandle.Value;
        window.SetForeground();
        if (GetForegroundWindow() != handle)
        {
            Keyboard.Type(VirtualKeyShort.ALT);
            window.SetForeground();
        }
        Assert.True(Wait(() => GetForegroundWindow() == handle), "Test window must own keyboard input");
    }

    /// <summary>Clicks the element by mouse after checking that the process owns the point, so a window that covers it
    /// reports BLOCKED instead of a misleading failure or a click into another application.</summary>
    internal static void ClickOwned(AutomationElement element, int processId) => ClickOwned(PointOf(element), processId);

    internal static void ClickOwned(Point point, int processId)
    {
        DesktopTestEnvironment.RequireOwnedPoint(processId, point);
        Mouse.Click(point);
        // As FlaUI's element Click does: the app handles the click before the next input, such as a typed key.
        FlaUI.Core.Input.Wait.UntilInputIsProcessed();
    }

    /// <summary>Right-clicks the element after the same ownership check as <see cref="ClickOwned(AutomationElement, int)"/>.</summary>
    internal static void RightClickOwned(AutomationElement element, int processId)
    {
        var point = PointOf(element);
        DesktopTestEnvironment.RequireOwnedPoint(processId, point);
        Mouse.RightClick(point);
        FlaUI.Core.Input.Wait.UntilInputIsProcessed();
    }

    /// <summary>Hovers the point after the same ownership check; see <see cref="Hover"/>.</summary>
    internal static void HoverOwned(Point point, int processId)
    {
        DesktopTestEnvironment.RequireOwnedPoint(processId, point);
        Hover(point);
    }

    /// <summary>
    /// FlaUI places the cursor with SetCursorPos, which WinUI does not take as pointer movement. SendInput moves it from a
    /// few pixels to the left onto the point, so the element there sees the pointer enter. Unguarded: callers use
    /// <see cref="HoverOwned"/>.
    /// </summary>
    private static void Hover(Point point)
    {
        MoveTo(new Point(point.X - 6, point.Y));
        MoveTo(point);
        Thread.Sleep(100);
    }

    /// <summary>Moves the cursor with one SendInput step, which WinUI takes as pointer movement (FlaUI's Mouse.MoveTo is not).</summary>
    internal static void MoveTo(Point point)
    {
        Input[] move = [new(point.X, point.Y)];
        Assert.Equal(1u, SendInput(1, move, Marshal.SizeOf<Input>()));
    }

    private static Point PointOf(AutomationElement element)
    {
        var bounds = element.BoundingRectangle;
        return element.TryGetClickablePoint(out var clickable) ? clickable : new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
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

    /// <summary>Appends this test's result to one host-wide history next to the desktop smoke lock,
    /// %LOCALAPPDATA%\AiUsage-smoke-history.csv, so it survives worktree removal; call it while the test class is disposed.</summary>
    internal static void RecordResult(TimeSpan duration)
    {
        var root = RepositoryRoot();
        var context = TestContext.Current;
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage-smoke-history.csv");
        static string Field(string? value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        var row = string.Join(',',
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            Field(context.Test?.TestDisplayName),
            context.TestState?.Result.ToString() ?? "Unknown",
            duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture),
            Commit.Value,
            Field(root is null ? null : Path.GetFileName(root)));
        if (!File.Exists(file)) File.WriteAllText(file, "utc,test,outcome,duration_s,commit,worktree" + Environment.NewLine);
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
            using var git = Process.Start(new ProcessStartInfo("git", "describe --always --dirty")
            {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true, WorkingDirectory = RepositoryRoot() ?? AppContext.BaseDirectory,
            });
            var hash = git?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
            return git is not null && git.WaitForExit(5000) && git.ExitCode == 0 ? hash : string.Empty;
        }
        catch (System.ComponentModel.Win32Exception) { return string.Empty; }
    });

    // INPUT with an absolute MOUSEINPUT move over the virtual desktop (MOVE | ABSOLUTE | VIRTUALDESK).
    private struct Input(int x, int y)
    {
        public uint Type = 0;
        public MouseInput Mouse = new(x, y);
    }

    private struct MouseInput(int x, int y)
    {
        public int Dx = (int)Math.Round((x - GetSystemMetrics(76)) * 65535.0 / (GetSystemMetrics(78) - 1));
        public int Dy = (int)Math.Round((y - GetSystemMetrics(77)) * 65535.0 / (GetSystemMetrics(79) - 1));
        public uint Data = 0;
        public uint Flags = 0x0001 | 0x8000 | 0x4000;
        public uint Time = 0;
        public IntPtr ExtraInfo = IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
