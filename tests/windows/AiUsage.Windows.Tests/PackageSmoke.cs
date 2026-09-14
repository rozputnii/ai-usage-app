using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AiUsage.Windows.Tests;

public sealed class PackageSmoke
{
    [Theory]
    [InlineData("exit")]
    [InlineData("title-bar")]
    [InlineData("repeated-exit")]
    public void InstalledDashboardTerminatesCleanly(string scenario)
    {
        var aumid = Environment.GetEnvironmentVariable("AIU_SMOKE_AUMID");
        Assert.False(string.IsNullOrWhiteSpace(aumid), "Prerequisite: AIU_SMOKE_AUMID must identify the installed package.");
        Assert.True(Environment.UserInteractive && Process.GetCurrentProcess().SessionId != 0,
            "Prerequisite: an unlocked interactive Windows desktop is required.");
        var desktop = OpenInputDesktop(0, false, 0x0100);
        Assert.NotEqual(IntPtr.Zero, desktop);
        CloseDesktop(desktop);
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(evidence), "Prerequisite: AIU_SMOKE_EVIDENCE_DIRECTORY is required.");
        Directory.CreateDirectory(evidence!);
        using var automation = new UIA3Automation();
        File.WriteAllText(Path.Combine(evidence!, scenario + "-activation.json"),
            JsonSerializer.Serialize(new { scenario, phase = "activation-attempted", utc = DateTime.UtcNow }));
        using var app = Application.LaunchStoreApp(aumid!);
        var pid = app.ProcessId;
        using var process = Process.GetProcessById(pid);
        // Acquire the OS process handle before observing exit so PID reuse cannot change ownership.
        _ = process.Handle;
        Window? window = null;
        var passed = false;
        try
        {
            var startup = Stopwatch.StartNew();
            while (startup.Elapsed < TimeSpan.FromSeconds(15) && !process.HasExited)
            {
                window = FindProcessWindow(automation, pid);
                if (window?.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"))?.Name == "No accounts connected."
                    && window.FindFirstDescendant(cf => cf.ByAutomationId("ConnectButton")) is not null
                    && (scenario != "title-bar" || window.TitleBar?.CloseButton is not null))
                    break;
                Thread.Sleep(100);
            }
            Assert.NotNull(window);
            Assert.Equal("Dashboard", window.FindFirstDescendant(cf => cf.ByAutomationId("DashboardHeading"))?.Name);
            // A first run has no stored grant, so the dashboard must say so instead of showing quota.
            Assert.Equal("No accounts connected.", window.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"))?.Name);
            Assert.Empty(window.FindAllDescendants(cf => cf.ByAutomationId("QuotaWindows"))
                .SelectMany(list => list.FindAllChildren()));
            var connect = window.FindFirstDescendant(cf => cf.ByAutomationId("ConnectButton")).AsButton();
            Assert.NotNull(connect);
            Assert.True(connect.IsEnabled);
            connect.Focus();
            Assert.True(connect.Properties.IsKeyboardFocusable.Value);
            // Nothing is connected, so refresh and disconnect must not be offered.
            var refresh = window.FindFirstDescendant(cf => cf.ByAutomationId("RefreshButton")).AsButton();
            var disconnect = window.FindFirstDescendant(cf => cf.ByAutomationId("DisconnectButton")).AsButton();
            Assert.NotNull(refresh);
            Assert.NotNull(disconnect);
            Assert.False(refresh.IsEnabled);
            Assert.False(disconnect.IsEnabled);
            // The tray icon is created by the window; without it there is no tray presence at all.
            Assert.True(TrayIconPresent(pid), "No tray icon window owned by the launched process.");
            // UIA can expose text before the compositor presents the corresponding frame.
            Thread.Sleep(500);
            using (var screenshot = window.Capture())
                screenshot.Save(Path.Combine(evidence!, scenario + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            var handle = window.Properties.NativeWindowHandle.Value;
            if (scenario == "title-bar")
            {
                Assert.NotNull(window.TitleBar);
                Assert.NotNull(window.TitleBar.CloseButton);
                window.TitleBar.CloseButton.Invoke();
            }
            else
            {
                var exit = window.FindFirstDescendant(cf => cf.ByAutomationId("ExitButton")).AsButton();
                Assert.NotNull(exit);
                exit.Focus();
                Assert.True(exit.Properties.IsKeyboardFocusable.Value);
                FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                if (scenario == "repeated-exit")
                {
                    // Overlap the keyboard Exit action with native close requests.
                    PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                }
            }
            Assert.True(process.WaitForExit(10000), "Launched PID did not terminate within ten seconds.");
            Assert.Equal(0, process.ExitCode);
            Assert.Null(FindProcessWindow(automation, pid));
            passed = true;
        }
        finally
        {
            if (!passed && window is not null && !process.HasExited)
            {
                try
                {
                    using var screenshot = window.Capture();
                    screenshot.Save(Path.Combine(evidence!, scenario + "-failure.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception captureError) when (captureError is not OutOfMemoryException)
                {
                    File.WriteAllText(Path.Combine(evidence!, scenario + "-capture-error.txt"), captureError.GetType().Name);
                }
            }
            File.WriteAllText(Path.Combine(evidence!, scenario + ".json"), JsonSerializer.Serialize(new
            {
                scenario,
                pid,
                passed,
                exited = process.HasExited,
                exitCode = process.HasExited ? (int?)process.ExitCode : null
            }));
            // Cleanup is restricted to the retained handle of this activation, never a process name.
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(2000))
                    process.Kill();
            }
        }
    }

    private static Window? FindProcessWindow(UIA3Automation automation, int pid)
    {
        // Sandbox UIA3 can report ProcessId=0; the native HWND owner remains authoritative.
        foreach (var element in automation.GetDesktop().FindAllChildren())
        {
            var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
            if (handle != IntPtr.Zero && GetWindowThreadProcessId(handle, out var owner) != 0 && owner == (uint)pid)
                return element.AsWindow();
        }
        return null;
    }

    /// <summary>
    /// H.NotifyIcon hosts the tray icon in a hidden message-only window owned by the app, so the
    /// presence of that class under the launched process is the observable tray evidence available
    /// without automating the notification area itself.
    /// </summary>
    private static bool TrayIconPresent(int pid)
    {
        var found = false;
        EnumWindows((handle, _) =>
        {
            if (GetWindowThreadProcessId(handle, out var owner) == 0 || owner != (uint)pid)
                return true;
            var className = new System.Text.StringBuilder(256);
            if (GetClassName(handle, className, className.Capacity) > 0 &&
                className.ToString().Contains("NotifyIcon", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, System.Text.StringBuilder className, int capacity);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);

    [DllImport("user32.dll")]
    private static extern bool CloseDesktop(IntPtr desktop);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
