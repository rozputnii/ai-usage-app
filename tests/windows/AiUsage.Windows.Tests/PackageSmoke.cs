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
    private static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < timeout)
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            if (condition()) return true;
            Thread.Sleep(50);
        }
        return condition();
    }

    [Theory]
    [InlineData("exit")]
    [InlineData("title-bar")]
    [InlineData("repeated-exit")]
    [InlineData("minimize")]
    [InlineData("tray-exit")]
    [InlineData("claude-controls")]
    [InlineData("copilot-controls")]
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
                    && (scenario is not ("title-bar" or "tray-exit") || window.TitleBar?.CloseButton is not null))
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
            if (scenario == "claude-controls")
            {
                window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
                var picker = window.FindFirstDescendant(cf => cf.ByAutomationId("ProviderPicker")).AsComboBox();
                Assert.NotNull(picker);
                picker.Select("Claude");
                Assert.True(WaitUntil(() => connect.Name == "Connect Claude", TimeSpan.FromSeconds(5)));
                Assert.Contains("Anthropic prohibits", window.FindFirstDescendant(cf => cf.ByAutomationId("UnsupportedNotice"))?.Name);
                Assert.False(refresh.IsEnabled);
                connect.Invoke();
                Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("ManualCode")) is { IsOffscreen: false }, TimeSpan.FromSeconds(5)));
                var manual = window.FindFirstDescendant(cf => cf.ByAutomationId("ManualCode"));
                Assert.NotNull(manual);
                FocusForKeyboard(window, manual, evidence!, "claude-manual");
                FlaUI.Core.Input.Keyboard.Type("synthetic-unused-code");
                Thread.Sleep(500);
                using (var screenshot = window.Capture())
                    screenshot.Save(Path.Combine(evidence!, "claude-manual-entered.png"), System.Drawing.Imaging.ImageFormat.Png);
                var cancel = window.FindFirstDescendant(cf => cf.ByAutomationId("CancelButton")).AsButton();
                Assert.NotNull(cancel);
                Assert.True(cancel.IsEnabled);
                cancel.Invoke();
                Assert.True(WaitUntil(() => connect.IsEnabled && window.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"))?.Name == "No accounts connected.", TimeSpan.FromSeconds(5)));
                connect.Invoke();
                Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("ManualCode")) is { IsOffscreen: false }, TimeSpan.FromSeconds(5)));
                window.SetForeground();
                Thread.Sleep(500);
                using (var screenshot = window.Capture())
                    screenshot.Save(Path.Combine(evidence!, "claude-cancel-reopened.png"), System.Drawing.Imaging.ImageFormat.Png);
                cancel.Invoke();
                Assert.True(WaitUntil(() => connect.IsEnabled, TimeSpan.FromSeconds(5)));
                picker.Select("Codex");
                Assert.True(WaitUntil(() => connect.Name == "Connect Codex", TimeSpan.FromSeconds(5)));
                Assert.False(cancel.IsEnabled);
                Assert.Equal("No accounts connected.", window.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"))?.Name);
            }
            if (scenario == "copilot-controls")
            {
                window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
                var picker = window.FindFirstDescendant(cf => cf.ByAutomationId("ProviderPicker")).AsComboBox();
                Assert.NotNull(picker);
                picker.Select("GitHub Copilot");
                Assert.True(WaitUntil(() => connect.Name == "Connect GitHub Copilot", TimeSpan.FromSeconds(5)));
                Assert.Contains("OpenCode GitHub OAuth App", window.FindFirstDescendant(cf => cf.ByAutomationId("UnsupportedNotice"))?.Name);
                Assert.False(refresh.IsEnabled);
                Assert.False(disconnect.IsEnabled);
                // The disposable guest has no network: the device-code request must fail without a stored grant.
                connect.Invoke();
                Assert.True(WaitUntil(() => connect.IsEnabled
                    && !string.IsNullOrEmpty(window.FindFirstDescendant(cf => cf.ByAutomationId("ProviderFailureText"))?.Name), TimeSpan.FromSeconds(30)));
                Assert.Equal("No accounts connected.", window.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"))?.Name);
                Assert.True(string.IsNullOrEmpty(window.FindFirstDescendant(cf => cf.ByAutomationId("DeviceCodeText"))?.Name));
                Assert.Null(window.FindFirstDescendant(cf => cf.ByAutomationId("ManualCode")) is { IsOffscreen: false } ? "visible" : null);
                Assert.False(refresh.IsEnabled);
                Assert.False(disconnect.IsEnabled);
                window.SetForeground();
                Thread.Sleep(500);
                using (var screenshot = window.Capture())
                    screenshot.Save(Path.Combine(evidence!, "copilot-offline-connect.png"), System.Drawing.Imaging.ImageFormat.Png);
                picker.Select("Codex");
                Assert.True(WaitUntil(() => connect.Name == "Connect Codex", TimeSpan.FromSeconds(5)));
                Assert.True(string.IsNullOrEmpty(window.FindFirstDescendant(cf => cf.ByAutomationId("ProviderFailureText"))?.Name));
                Assert.Equal("No accounts connected.", window.FindFirstDescendant(cf => cf.ByAutomationId("StatusText"))?.Name);
            }
            // The tray icon is created by the window; without it there is no tray presence at all.
            Assert.True(TrayIconPresent(pid), "No tray icon window owned by the launched process.");
            // UIA can expose text before the compositor presents the corresponding frame.
            Thread.Sleep(500);
            using (var screenshot = window.Capture())
                screenshot.Save(Path.Combine(evidence!, scenario + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            var handle = window.Properties.NativeWindowHandle.Value;
            if (scenario is "title-bar" or "minimize" or "tray-exit")
            {
                Assert.NotNull(window.TitleBar);
                if (scenario != "minimize")
                {
                    Assert.NotNull(window.TitleBar.CloseButton);
                    window.TitleBar.CloseButton.Invoke();
                }
                else
                {
                    // WinUI's UIA title bar does not always expose its native Minimize button.
                    Assert.True(PostMessage(handle, 0x0112, new IntPtr(0xF020), IntPtr.Zero));
                }
                Thread.Sleep(500);
                Assert.False(process.HasExited, "Close and Minimize must keep the process alive.");
                Assert.True(TrayIconPresent(pid), "Tray icon must remain present.");
                if (scenario != "minimize")
                    Assert.Null(FindProcessWindow(automation, pid));
                else
                    Assert.True(IsIconic(handle), "Minimize must use the normal minimized window state.");
                // Exercise the real tray command; launching the package again creates a new instance.
                var trayButton = FindTrayButton(automation);
                Assert.NotNull(trayButton);
                if (scenario == "tray-exit")
                {
                    trayButton.RightClick();
                    AutomationElement? trayExit = null;
                    var menuWait = Stopwatch.StartNew();
                    while (menuWait.Elapsed < TimeSpan.FromSeconds(5) && trayExit is null)
                    {
                        // H.NotifyIcon's default native popup preserves menu text, not XAML automation IDs.
                        trayExit = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Exit").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)));
                        if (trayExit is null)
                            Thread.Sleep(100);
                    }
                    Assert.NotNull(trayExit);
                    using (var screenshot = trayExit.Capture())
                        screenshot.Save(Path.Combine(evidence!, scenario + "-menu.png"), System.Drawing.Imaging.ImageFormat.Png);
                    trayExit.Click();
                }
                else
                {
                    trayButton.Click();
                    window = null;
                    var reopened = Stopwatch.StartNew();
                    while (reopened.Elapsed < TimeSpan.FromSeconds(10))
                    {
                        window = FindProcessWindow(automation, pid);
                        if (window is not null && !IsIconic(handle))
                            break;
                        Thread.Sleep(100);
                    }
                    Assert.NotNull(window);
                    Assert.False(IsIconic(handle));
                    Assert.Equal(handle, window.Properties.NativeWindowHandle.Value);
                    Assert.Equal("Dashboard", window.FindFirstDescendant(cf => cf.ByAutomationId("DashboardHeading"))?.Name);
                    Thread.Sleep(500);
                    using (var screenshot = window.Capture())
                        screenshot.Save(Path.Combine(evidence!, scenario + "-restored.png"), System.Drawing.Imaging.ImageFormat.Png);
                    var exit = window.FindFirstDescendant(cf => cf.ByAutomationId("ExitButton")).AsButton();
                    Assert.NotNull(exit);
                    exit.Invoke();
                }
            }
            else
            {
                var exit = window.FindFirstDescendant(cf => cf.ByAutomationId("ExitButton")).AsButton();
                Assert.NotNull(exit);
                exit.Focus();
                Assert.True(exit.Properties.IsKeyboardFocusable.Value);
                if (scenario == "repeated-exit")
                {
                    // Initiate Exit before close can hide the window and cancel pending keyboard input.
                    exit.Invoke();
                    PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    FocusForKeyboard(window, exit, evidence!, scenario + "-exit");
                    FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
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

    private static void FocusForKeyboard(Window window, AutomationElement target, string evidence, string operation)
    {
        var handle = window.Properties.NativeWindowHandle.Value;
        var foregroundBefore = GetForegroundWindow();
        window.SetForeground();
        // Windows can deny programmatic foreground activation. A real title-bar click gives
        // this keyboard scenario the same activation a user performs, without invoking Exit.
        var activatedWithClick = GetForegroundWindow() != handle;
        if (activatedWithClick)
        {
            Assert.NotNull(window.TitleBar);
            window.TitleBar.Click();
        }
        target.Focus();
        var ready = WaitUntil(() => GetForegroundWindow() == handle && target.Properties.HasKeyboardFocus.ValueOrDefault,
            TimeSpan.FromSeconds(5));
        File.WriteAllText(Path.Combine(evidence, operation + "-keyboard.json"), JsonSerializer.Serialize(new
        {
            ready,
            activatedWithClick,
            foregroundWasTarget = foregroundBefore == handle,
            foregroundIsTarget = GetForegroundWindow() == handle,
            targetHasKeyboardFocus = target.Properties.HasKeyboardFocus.ValueOrDefault
        }));
        Assert.True(ready, "The launched window must own foreground keyboard input before sending keys.");
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

    private static Button? FindTrayButton(UIA3Automation automation)
    {
        var desktop = automation.GetDesktop();
        var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
        var button = taskbar?.FindFirstDescendant(cf => cf.ByName("AI Usage").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
        if (button is not null && !button.IsOffscreen)
            return button.AsButton();
        var overflow = desktop.FindFirstChild(cf => cf.ByClassName("TopLevelWindowForOverflowXamlIsland"));
        if (overflow is null || overflow.IsOffscreen)
        {
            var showHidden = taskbar?.FindFirstDescendant(cf => cf.ByName("Show Hidden Icons"));
            Assert.NotNull(showHidden);
            showHidden.AsButton().Invoke();
        }
        var wait = Stopwatch.StartNew();
        while (wait.Elapsed < TimeSpan.FromSeconds(5))
        {
            overflow = desktop.FindFirstChild(cf => cf.ByClassName("TopLevelWindowForOverflowXamlIsland"));
            button = overflow?.FindFirstDescendant(cf => cf.ByName("AI Usage").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)));
            if (button is not null && !button.IsOffscreen)
                return button.AsButton();
            Thread.Sleep(100);
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

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
