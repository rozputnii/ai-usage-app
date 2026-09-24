using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Exceptions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AiUsage.Windows.Tests;

/// <summary>
/// Interactive smoke checks for the AIU-010 product or explicit demo shell. It launches unpackaged
/// from <c>AIU_SMOKE_EXE</c>, or from an installed package when <c>AIU_SMOKE_AUMID</c> is set instead. Nothing here
/// installs, registers or uninstalls anything.
/// </summary>
public sealed partial class ShellSmoke
{
    /// <summary>The single-window header: no navigation tabs, only these actions.</summary>
    private static readonly string[] HeaderIds = ["RefreshAllButton", "AddAccountButton", "SettingsButton"];

    /// <summary>Automation IDs of the removed top navigation and settings tabs; none may appear.</summary>
    private static readonly string[] RemovedTabIds =
        ["MainNavigation", "NavOverview", "NavAccounts", "NavHistory", "NavSettings", "NavSystemStatus", "SettingsTabs", "TabAppearance"];

    [Theory]
    [InlineData("launch")]
    [InlineData("navigation")]
    [InlineData("theme")]
    [InlineData("tray-exit")]
    [InlineData("repeated-exit")]
    [InlineData("capabilities")]
    public void ShellLaunchesNavigatesAndExits(string scenario)
    {
        var aumid = Environment.GetEnvironmentVariable("AIU_SMOKE_AUMID");
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        Assert.False(string.IsNullOrWhiteSpace(aumid) && string.IsNullOrWhiteSpace(exe),
            "Prerequisite: AIU_SMOKE_EXE must point at the built AiUsage.exe (or AIU_SMOKE_AUMID at an installed package).");
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
        var demo = Environment.GetEnvironmentVariable("AIU_SMOKE_MODE") == "demo";
        if (!demo)
        {
            Assert.False(string.IsNullOrWhiteSpace(exe), "Product smoke currently requires the unpackaged executable; packaged acceptance is separate.");
            Assert.False(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY")),
                "Product smoke requires an explicitly isolated development state directory with no stored grants.");
        }
        using var app = string.IsNullOrWhiteSpace(exe) ? Application.LaunchStoreApp(aumid!) : Application.Launch(exe!, demo ? "--demo" : "");
        var pid = app.ProcessId;
        using var process = Process.GetProcessById(pid);
        // Acquire the OS process handle before observing exit so PID reuse cannot change ownership.
        _ = process.Handle;
        Window? window = null;
        var passed = false;
        try
        {
            var startup = Stopwatch.StartNew();
            while (startup.Elapsed < TimeSpan.FromSeconds(30) && !process.HasExited)
            {
                window = FindProcessWindow(automation, pid);
                if (window?.FindFirstDescendant(cf => cf.ByAutomationId(demo ? "Row_demo-codex-1" : "AddProvider_codex")) is not null)
                    break;
                Thread.Sleep(200);
            }
            Assert.NotNull(window);
            window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
            var marker = window.FindFirstDescendant(cf => cf.ByAutomationId("DemoMarker"));
            if (demo) Assert.NotNull(marker);
            else Assert.Null(marker);
            foreach (var id in HeaderIds)
                Assert.NotNull(window.FindFirstDescendant(cf => cf.ByAutomationId(id)));
            foreach (var id in RemovedTabIds)
                Assert.Null(window.FindFirstDescendant(cf => cf.ByAutomationId(id)));
            // The tray icon is created by the window; without it there is no tray presence at all.
            Assert.True(TrayIconPresent(pid), "No tray icon window owned by the launched process.");

            if (scenario is "navigation" or "theme")
                Navigate(window, evidence!, scenario);
            if (scenario == "theme")
                SwitchTheme(window, evidence!);
            if (scenario == "capabilities")
            {
                // The Add account menu replaces the dialog; CLI import is listed only where the capability exists.
                Required(window, "AddAccountButton").AsButton().Invoke();
                Assert.True(WaitUntil(() => FindAllInProcess(automation, pid, "AddImportCli").Count > 0 || FindAllInProcess(automation, pid, "AddProvider_claude").Count > (demo ? 0 : 1),
                    TimeSpan.FromSeconds(5)), "Add account must open the provider menu.");
                Assert.Equal(demo, FindAllInProcess(automation, pid, "AddImportCli").Count == 1);
                Capture(window, evidence!, "capabilities-connect");
                VerifyProviderChoices(automation, pid, demo);
                Keyboard.Press(VirtualKeyShort.ESCAPE);
                Assert.True(WaitUntil(() => FindAllInProcess(automation, pid, "AddImportCli").Count == 0 && FindAllInProcess(automation, pid, "AddProvider_claude").Count <= (demo ? 0 : 1),
                    TimeSpan.FromSeconds(5)), "Escape must close the provider menu without starting a sign-in.");
                Required(window, "SettingsButton").AsButton().Invoke();
                Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("HistoryEnabledSwitch")) is not null, TimeSpan.FromSeconds(5)));
                foreach (var id in new[] { "HistoryEnabledSwitch", "RetentionSelector", "PrepareExport" })
                    Assert.Equal(demo, Required(window, id).IsEnabled);
                Capture(window, evidence!, "capabilities-data");
            }

            // UIA can expose text before the compositor presents the corresponding frame.
            Thread.Sleep(500);
            Capture(window, evidence!, scenario);
            var handle = window.Properties.NativeWindowHandle.Value;

            if (scenario == "close-to-tray")
            {
                Assert.NotNull(window.TitleBar);
                Assert.NotNull(window.TitleBar.CloseButton);
                window.TitleBar.CloseButton.Invoke();
                Assert.True(WaitUntil(() => FindProcessWindow(automation, pid) is null, TimeSpan.FromSeconds(5)),
                    "Close must hide the window to the tray.");
                Assert.False(process.HasExited, "Close must keep the process alive.");
                Assert.True(TrayIconPresent(pid), "Tray icon must remain present while hidden.");
                var trayButton = FindTrayButton(automation);
                Assert.NotNull(trayButton);
                // Invoke the tray icon's primary action; the popup link brings the window back.
                trayButton.Invoke();
                AutomationElement? openDashboard = null;
                Assert.True(WaitUntil(() =>
                {
                    openDashboard = FindAllInProcess(automation, pid, "TrayOpenDashboard").FirstOrDefault();
                    return openDashboard is not null;
                }, TimeSpan.FromSeconds(10)), "The tray icon must open the tray dashboard.");
                openDashboard!.AsButton().Invoke();
                Assert.True(WaitUntil(() => (window = FindProcessWindow(automation, pid)) is not null, TimeSpan.FromSeconds(10)),
                    "The tray dashboard must restore the window.");
                Assert.NotNull(window);
                Assert.Equal(handle, window!.Properties.NativeWindowHandle.Value);
                Capture(window, evidence!, scenario + "-restored");
            }

            if (scenario == "tray-exit")
            {
                var trayButton = FindTrayButton(automation);
                Assert.NotNull(trayButton);
                trayButton.RightClick();
                AutomationElement? trayExit = null;
                Assert.True(WaitUntil(() =>
                {
                    // The native tray popup preserves menu text, not XAML automation IDs.
                    trayExit = automation.GetDesktop().FindFirstDescendant(cf =>
                        cf.ByName("Exit").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)));
                    return trayExit is not null;
                }, TimeSpan.FromSeconds(5)), "The tray menu must offer Exit.");
                trayExit!.Click();
                // Exit shows the window first so the confirmation is visible even from the tray.
                Assert.True(WaitUntil(() => (window = FindProcessWindow(automation, pid)) is not null, TimeSpan.FromSeconds(10)));
            }
            else
            {
                FocusForKeyboard(window!, window!, evidence!, scenario + "-exit");
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
                if (scenario == "repeated-exit")
                {
                    // A second request while the confirmation is open must not open a second dialog or exit twice.
                    Thread.Sleep(300);
                    Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
                }
            }

            var confirm = ConfirmDialog(automation, pid, window!, evidence!, scenario);
            if (scenario == "repeated-exit")
                // A second request while the confirmation is open must not open a second dialog.
                Assert.Single(FindAllInProcess(automation, pid, "ConfirmAccept"));
            confirm.Invoke();

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
                    Capture(window, evidence!, scenario + "-failure");
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

    [Fact]
    public void CloseToTrayRestoresDashboard() => ShellLaunchesNavigatesAndExits("close-to-tray");

    /// <summary>
    /// One window without tabs: the header gear shows every settings section (System status included) in place of the
    /// usage view, and Back returns. In demo, account detail opens from its compact row and History from the detail;
    /// history loads without an initial load action and Back walks the same path home.
    /// </summary>
    private static void Navigate(Window window, string evidence, string scenario)
    {
        var demo = Environment.GetEnvironmentVariable("AIU_SMOKE_MODE") == "demo";
        var home = demo ? "Row_demo-codex-1" : "AddProvider_codex";
        void Show(string id, string marker, string capture)
        {
            var entry = Required(window, id);
            Assert.True(entry.IsEnabled);
            entry.AsButton().Invoke();
            Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId(marker)) is not null, TimeSpan.FromSeconds(10)),
                $"{id} must show the view carrying {marker}.");
            if (scenario == "navigation")
                Capture(window, evidence, "view-" + capture);
        }
        void Back(string? to = null)
        {
            Required(window, "BackButton").AsButton().Invoke();
            Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId(to ?? home)) is not null, TimeSpan.FromSeconds(10)),
                $"Back must return to the view carrying {to ?? home}.");
            if (to is null)
                Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("BackButton")) is null, TimeSpan.FromSeconds(5)));
        }

        Assert.Null(window.FindFirstDescendant(cf => cf.ByAutomationId("BackButton")));
        Show("SettingsButton", "ThemeNote", "settings");
        // Every former settings tab and the former System status page are sections of this one view.
        foreach (var id in new[] { "HistoryEnabledSwitch", "UpdateStatus", "StatusBuild" })
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByAutomationId(id)));
        Back();
        if (!demo)
            return;
        Show("RowOpen", "DetailTitle", "account");
        Show("DetailHistory", "HistoryAccount", "history");
        Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("ProviderHistoryRows")) is not null,
            TimeSpan.FromSeconds(10)), "Provider history must load automatically.");
        Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByName("12000 tokens")) is not null,
            TimeSpan.FromSeconds(5)), "A native provider value must be visible without a load action.");
        Back("DetailTitle");
        Back();
    }

    /// <summary>Appearance settings must switch the app theme without a restart.</summary>
    private static void SwitchTheme(Window window, string evidence)
    {
        var settings = SettingsEntry(window);
        Assert.NotNull(settings);
        settings.Click();
        Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("ThemeNote")) is not null, TimeSpan.FromSeconds(10)));
        foreach (var (id, expected) in new[] { ("ThemeDark", "Dark"), ("ThemeLight", "Light"), ("ThemeSystem", "Windows") })
        {
            var card = window.FindFirstDescendant(cf => cf.ByAutomationId(id));
            Assert.NotNull(card);
            card.Click();
            Assert.True(WaitUntil(() => window.FindFirstDescendant(cf => cf.ByAutomationId("ThemeNote"))?.Name?.Contains(expected, StringComparison.Ordinal) == true,
                TimeSpan.FromSeconds(5)), $"{id} must be reflected by the appearance note.");
            Thread.Sleep(400);
            Capture(window, evidence, "theme-" + id);
        }
    }

    /// <summary>Exit always asks first; the dialog is the only way the process ends.</summary>
    private static Button ConfirmDialog(UIA3Automation automation, int pid, Window window, string evidence, string scenario)
    {
        AutomationElement? accept = null;
        Assert.True(WaitUntil(() =>
        {
            // The dialog is hosted in its own popup window, so it is found through the process, not the shell window.
            accept = FindAllInProcess(automation, pid, "ConfirmAccept").FirstOrDefault();
            return accept is not null;
        }, TimeSpan.FromSeconds(10)), "Exit must ask for confirmation.");
        Assert.NotEmpty(FindAllInProcess(automation, pid, "ConfirmCancel"));
        Thread.Sleep(400);
        Capture(window, evidence, scenario + "-confirm");
        return accept!.AsButton();
    }

    /// <summary>
    /// Inspects the provider menu only. One click on a provider starts browser sign-in, so no entry is invoked. With
    /// isolated empty product state every provider is available; demo marks its planned providers.
    /// </summary>
    private static void VerifyProviderChoices(UIA3Automation automation, int pid, bool demo)
    {
        var ids = new[] { "codex", "claude", "copilot", "antigravity" };
        var names = new[] { "Codex", "Claude", demo ? "Copilot" : "GitHub Copilot", "Antigravity" };
        for (var i = 0; i < ids.Length; i++)
        {
            // Product first run lists the same providers in the page; every listed entry must agree.
            var buttons = FindAllInProcess(automation, pid, "AddProvider_" + ids[i]);
            Assert.NotEmpty(buttons);
            foreach (var button in buttons)
            {
                Assert.Equal(names[i], button.Name);
                Assert.True(button.IsEnabled);
                Assert.Equal(demo && ids[i] is "copilot" or "antigravity" ? "Planned · demo only" : "", button.Properties.HelpText.ValueOrDefault ?? "");
            }
        }
    }

    /// <summary>
    /// The settings entry: the header gear of the single-window shell, or the Settings tab of older packages that the
    /// upgrade harnesses still launch as their old side.
    /// </summary>
    private static AutomationElement? SettingsEntry(AutomationElement window) =>
        window.FindFirstDescendant(cf => cf.ByAutomationId("SettingsButton")) ?? window.FindFirstDescendant(cf => cf.ByAutomationId("NavSettings"));

    private static List<AutomationElement> FindAllInProcess(UIA3Automation automation, int pid, string automationId)
        => FindAllInProcess(automation, pid, cf => cf.ByAutomationId(automationId));

    private static List<AutomationElement> FindAllInProcess(UIA3Automation automation, int pid,
        Func<FlaUI.Core.Conditions.ConditionFactory, FlaUI.Core.Conditions.ConditionBase> condition)
    {
        var found = new List<AutomationElement>();
        foreach (var element in automation.GetDesktop().FindAllChildren())
        {
            var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
            if (handle == IntPtr.Zero || GetWindowThreadProcessId(handle, out var owner) == 0 || owner != (uint)pid)
                continue;
            found.AddRange(element.FindAllDescendants(condition));
        }
        return found;
    }

    private static AutomationElement Required(Window window, string id)
    {
        var element = window.FindFirstDescendant(cf => cf.ByAutomationId(id));
        Assert.NotNull(element);
        return element;
    }

    private static void Capture(Window window, string evidence, string name)
    {
        using var screenshot = window.Capture();
        screenshot.Save(Path.Combine(evidence, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
    }

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

    private static void FocusForKeyboard(Window window, AutomationElement target, string evidence, string operation)
    {
        var handle = window.Properties.NativeWindowHandle.Value;
        var foregroundBefore = GetForegroundWindow();
        window.SetForeground();
        // Windows can deny programmatic foreground activation. A real title-bar click gives
        // this keyboard scenario the same activation a user performs, without invoking a command.
        var activatedWithClick = GetForegroundWindow() != handle;
        if (activatedWithClick)
        {
            Assert.NotNull(window.TitleBar);
            window.TitleBar.Click();
        }
        target.Focus();
        var ready = WaitUntil(() => GetForegroundWindow() == handle, TimeSpan.FromSeconds(5));
        File.WriteAllText(Path.Combine(evidence, operation + "-keyboard.json"), JsonSerializer.Serialize(new
        {
            ready,
            activatedWithClick,
            foregroundWasTarget = foregroundBefore == handle,
            foregroundIsTarget = GetForegroundWindow() == handle
        }));
        Assert.True(ready, "The launched window must own foreground keyboard input before sending keys.");
    }

    private static Window? FindProcessWindow(UIA3Automation automation, int pid)
    {
        // Sandbox UIA3 can report ProcessId=0; the native HWND owner remains authoritative.
        foreach (var element in automation.GetDesktop().FindAllChildren())
        {
            try
            {
                var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
                if (handle != IntPtr.Zero && GetWindowThreadProcessId(handle, out var owner) != 0 && owner == (uint)pid
                    && SettingsEntry(element) is not null)
                    return element.AsWindow();
            }
            catch (COMException) { /* Another desktop window can disappear during enumeration. Retry the owned PID. */ }
        }
        return null;
    }

    private static Button? FindTrayButton(UIA3Automation automation)
    {
        var desktop = automation.GetDesktop();
        var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
        // Match the tooltip's summary separator as well as the title: the taskbar's
        // application button also starts with "AI Usage" while the main window is visible.
        var button = TrayIcon(taskbar);
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
            button = TrayIcon(overflow);
            if (button is not null && !button.IsOffscreen)
                return button.AsButton();
            Thread.Sleep(100);
        }
        return null;
    }

    private static AutomationElement? TrayIcon(AutomationElement? host) =>
        host?.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button))
            .FirstOrDefault(element => TryReadAutomationName(element)?.StartsWith("AI Usage · ", StringComparison.Ordinal) == true);

    private static string? TryReadAutomationName(AutomationElement element)
    {
        try
        {
            return element.Name;
        }
        catch (PropertyNotSupportedException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
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
    private static extern IntPtr GetForegroundWindow();
}
