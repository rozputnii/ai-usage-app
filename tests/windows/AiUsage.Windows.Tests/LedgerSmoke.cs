using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace AiUsage.Windows.Tests;

/// <summary>Ordinary desktop interaction on isolated synthetic state; no sign-in or real credentials.</summary>
public sealed partial class LedgerSmoke
{
    [Fact]
    public void AccountSpendingUsesNestedContentHistoryCapsAndAccountActions()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe)); Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence!);
        var start = new ProcessStartInfo(exe!, "--demo") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-money-smoke-" + Guid.NewGuid().ToString("N"));
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        _ = process.Handle;
        using var automation = new UIA3Automation();
        Window? window = null;
        var passed = false;
        var stage = "launch";
        // A cached UIA element for the main window can turn invalid while the same HWND stays alive:
        // its properties throw 0x80040201 and its searches return nothing. Resolve the window afresh for each query.
        Window? Current() => OwnedWindow(automation, app.ProcessId);
        Window Main()
        {
            Window? current = null;
            Assert.True(Wait(() => (current = Current()) is not null), "Ledger window is not available");
            return current!;
        }
        try
        {
            Assert.True(Wait(() => (window = OwnedWindow(automation, app.ProcessId)) is not null));
            Focus(window!);
            stage = "select scenario";
            Main().FindFirstDescendant(cf => cf.ByName("Demo scenarios"))!.AsButton().Invoke();
            AutomationElement? scenario = null;
            Assert.True(Wait(() =>
            {
                scenario = automation.GetDesktop().FindAllChildren().Where(w =>
                    GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == app.ProcessId)
                    .Select(w => w.FindFirstDescendant(cf => cf.ByName("Account spending"))).FirstOrDefault(e => e is not null);
                return scenario is not null;
            }));
            scenario!.AsMenuItem().Invoke();
            stage = "nested content";
            AutomationElement? money = null;
            Assert.True(Wait(() => (money = Current()?.FindFirstDescendant(cf => cf.ByAutomationId("money-mixed"))) is not null));
            var parent = Main().FindFirstDescendant(cf => cf.ByAutomationId("claude-week"))!;
            Assert.NotNull(parent.FindFirstDescendant(cf => cf.ByAutomationId("money-mixed")));
            Assert.DoesNotContain(money!.FindAllDescendants(cf => cf.ByControlType(ControlType.Button)), b =>
                (b.Properties.Name.ValueOrDefault ?? "").StartsWith("Sign out", StringComparison.Ordinal));
            money.Focus();
            stage = "history";
            Keyboard.Press(VirtualKeyShort.RETURN);
            Assert.True(Wait(() => Current()?.FindAllDescendants().Any(e =>
                (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Mixed account history,", StringComparison.Ordinal)) == true));
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "money-history.png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            money = Main().FindFirstDescendant(cf => cf.ByAutomationId("money-mixed"))!;
            stage = "cap";
            money.Focus(); Keyboard.Press(VirtualKeyShort.KEY_C);
            Assert.True(Wait(() => money.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit)).Any(e => !e.IsOffscreen)));
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "money-cap.png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            parent = Main().FindFirstDescendant(cf => cf.ByAutomationId("claude-week"))!;
            parent.Focus(); Keyboard.Press(VirtualKeyShort.F2);
            stage = "rename";
            TextBox? rename = null;
            Assert.True(Wait(() => (rename = Current()?.FindFirstDescendant(cf => cf.ByName("Account name").And(cf.ByControlType(ControlType.Edit)))?.AsTextBox()) is not null));
            rename!.Text = "Renamed account";
            Keyboard.Press(VirtualKeyShort.RETURN);
            Assert.True(Wait(() => Current()?.FindFirstDescendant(cf => cf.ByName("Renamed account")) is not null));
            var only = Main().FindFirstDescendant(cf => cf.ByAutomationId("money-only"))!;
            stage = "money only";
            only.Focus();
            Assert.True(Wait(() => only.FindFirstDescendant(cf => cf.ByName("Sign out Money only")) is not null));
            Assert.Contains(Main().FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "") == "12.50 EUR");
            Assert.Contains(Main().FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "") == "200.00 EUR (provider)");
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "money-account.png"), System.Drawing.Imaging.ImageFormat.Png);
            void SelectScenario(string name)
            {
                Main().FindFirstDescendant(cf => cf.ByName("Demo scenarios"))!.AsButton().Invoke();
                AutomationElement? item = null;
                Assert.True(Wait(() =>
                {
                    item = automation.GetDesktop().FindAllChildren().Where(w =>
                        GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == app.ProcessId)
                        .Select(w => w.FindFirstDescendant(cf => cf.ByName(name))).FirstOrDefault(e => e is not null);
                    return item is not null;
                }));
                item!.AsMenuItem().Invoke();
            }
            stage = "money only gains windows";
            SelectScenario("Account spending with new windows");
            Assert.True(Wait(() => Current()?.FindFirstDescendant(cf => cf.ByAutomationId("money-window"))?
                .FindFirstDescendant(cf => cf.ByAutomationId("money-only")) is not null));
            stage = "money only loses windows";
            SelectScenario("Account spending");
            Assert.True(Wait(() => Current() is { } current && current.FindFirstDescendant(cf => cf.ByAutomationId("money-window")) is null &&
                current.FindFirstDescendant(cf => cf.ByAutomationId("money-only")) is not null));
            only = Main().FindFirstDescendant(cf => cf.ByAutomationId("money-only"))!;
            only.Focus();
            Assert.True(Wait(() => only.FindFirstDescendant(cf => cf.ByName("Sign out Money only")) is not null));
            only.FindFirstDescendant(cf => cf.ByName("Sign out Money only"))!.AsButton().Invoke();
            stage = "sign out";
            Assert.True(Wait(() => Current() is { } current && current.FindFirstDescendant(cf => cf.ByAutomationId("money-only")) is null &&
                current.FindFirstDescendant(cf => cf.ByAutomationId("money-mixed")) is not null));
            Assert.NotNull(Main().FindFirstDescendant(cf => cf.ByAutomationId("money-mixed")));
            Focus(Main());
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
            passed = true;
        }
        finally
        {
            File.WriteAllText(Path.Combine(evidence!, "money-smoke.json"), JsonSerializer.Serialize(new { passed, stage, exited = process.HasExited }));
            if (!process.HasExited)
            {
                try
                {
                    if ((Current() ?? window) is { } failed)
                        using (var capture = failed.Capture()) capture.Save(Path.Combine(evidence!, "money-failure.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (System.Runtime.InteropServices.COMException) { }
                process.Kill(); process.WaitForExit(5000);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfirmedDeletionAndInterruptedDeletionRestartCleanly(bool pending)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe)); Assert.False(string.IsNullOrWhiteSpace(evidence));
        var root = Path.Combine(Path.GetTempPath(), "aiu-ledger-delete-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "providers"));
        File.WriteAllText(Path.Combine(root, "providers", "claude.state"), "synthetic invalid grant");
        File.WriteAllText(Path.Combine(root, "preserved-export.txt"), "owner export");
        if (pending) File.WriteAllText(Path.Combine(root, "delete-local-data.v1.json"), "{\"version\":1,\"operation\":\"delete-local-data\"}");
        var existing = Process.GetProcessesByName("AiUsage").Select(p => { using (p) return p.Id; }).ToHashSet();
        var start = new ProcessStartInfo(exe!) { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = root;
        using var app = Application.Launch(start);
        using var original = Process.GetProcessById(app.ProcessId);
        _ = original.Handle;
        using var automation = new UIA3Automation();
        Process? restarted = null;
        Window? window = null;
        var passed = false;
        try
        {
            if (!pending)
            {
                Assert.True(Wait(() => (window = OwnedWindow(automation, original.Id)) is not null));
                window!.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(ControlType.Button)))!.AsButton().Invoke();
                Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("Delete stored data").And(cf.ByControlType(ControlType.Button))) is not null));
                window.FindFirstDescendant(cf => cf.ByName("Delete stored data").And(cf.ByControlType(ControlType.Button)))!.AsButton().Invoke();
                window.FindFirstDescendant(cf => cf.ByName("Confirm deleting stored data"))!.AsButton().Invoke();
            }
            Assert.True(original.WaitForExit(30000), "Deletion did not restart the original process");
            Assert.True(Wait(() =>
            {
                foreach (var candidate in Process.GetProcessesByName("AiUsage"))
                {
                    if (existing.Contains(candidate.Id) || candidate.Id == original.Id || candidate.MainModule?.FileName != exe) { candidate.Dispose(); continue; }
                    restarted = candidate;
                    return true;
                }
                return false;
            }), "Restarted process was not found");
            _ = restarted!.Handle;
            Assert.True(Wait(() => (window = OwnedWindow(automation, restarted.Id)) is not null));
            Assert.True(Wait(() => window!.FindFirstDescendant(cf => cf.ByName("Sign in to Codex").And(cf.ByControlType(ControlType.Button))) is not null));
            Assert.False(File.Exists(Path.Combine(root, "providers", "claude.state")));
            Assert.False(File.Exists(Path.Combine(root, "delete-local-data.v1.json")));
            Assert.Equal("owner export", File.ReadAllText(Path.Combine(root, "preserved-export.txt")));
            using (var layout = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "layout.v1.json"))))
                Assert.Equal(2, layout.RootElement.GetProperty("Layout").GetInt32());
            Directory.CreateDirectory(evidence!);
            using (var capture = window!.Capture()) capture.Save(Path.Combine(evidence!, $"delete-{pending}.png"), System.Drawing.Imaging.ImageFormat.Png);
            Focus(window!);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(restarted.WaitForExit(10000));
            Assert.Equal(0, restarted.ExitCode);
            passed = true;
        }
        finally
        {
            Directory.CreateDirectory(evidence!);
            File.WriteAllText(Path.Combine(evidence!, $"delete-{pending}.json"), JsonSerializer.Serialize(new { passed, root, pending }));
            if (!original.HasExited) { original.Kill(); original.WaitForExit(5000); }
            if (restarted is not null) { if (!restarted.HasExited) { restarted.Kill(); restarted.WaitForExit(5000); } restarted.Dispose(); }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LedgerLaunchSettingsHistoryAndExit(bool demo)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe)); Assert.False(string.IsNullOrWhiteSpace(evidence));
        Assert.True(Environment.UserInteractive);
        Directory.CreateDirectory(evidence!);
        var state = Path.Combine(Path.GetTempPath(), "aiu-ledger-smoke-" + Guid.NewGuid().ToString("N"));
        var start = new ProcessStartInfo(exe!, demo ? "--demo" : "") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = state;
        using var app = Application.Launch(start);
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(app.ProcessId);
        _ = process.Handle;
        Window? window = null;
        bool passed = false;
        string prefix = demo ? "ledger-demo" : "ledger-live-empty";
        try
        {
            Assert.True(Wait(() =>
            {
                window = automation.GetDesktop().FindAllChildren().FirstOrDefault(w =>
                {
                    var handle = w.Properties.NativeWindowHandle.ValueOrDefault;
                    return handle != IntPtr.Zero && GetWindowThreadProcessId(handle, out var owner) != 0 && owner == app.ProcessId &&
                        w.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(ControlType.Button))) is not null;
                })?.AsWindow();
                return window is not null;
            }), "Ledger window did not appear");
            Assert.NotNull(window);
            Focus(window);
            // Used/Left and account actions are disabled by design while startup shows "Opening local data..."
            // (CanUseAccounts is false while IsStarting); live startup takes long enough to be visible. Invoke only enabled buttons.
            Button Button(string name)
            {
                Button? button = null;
                Assert.True(Wait(() => (button = window.FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.Button)))?.AsButton()) is { IsEnabled: true }), "Missing or disabled button: " + name);
                return button!;
            }
            Assert.NotNull(Button("Settings"));
            Button("Show values: left").Invoke(); Button("Show values: used").Invoke();
            if (demo)
            {
                AutomationElement? history = null;
                Assert.True(Wait(() =>
                {
                    var card = window.FindFirstDescendant(cf => cf.ByAutomationId("claude-week"));
                    if (card is null) return false;
                    card.Focus();
                    history = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button)).FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "").StartsWith("History, Claude", StringComparison.Ordinal));
                    return history is not null;
                }));
                history!.AsButton().Invoke();
                Assert.True(Wait(() => window.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("History", StringComparison.Ordinal))), "History did not open");
                Keyboard.Press(VirtualKeyShort.ESCAPE);
                Button("Add account").Invoke();
                AutomationElement? signIn = null;
                Assert.True(Wait(() =>
                {
                    signIn = automation.GetDesktop().FindAllChildren().Where(w =>
                        GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == app.ProcessId)
                        .SelectMany(w => w.FindAllDescendants(cf => cf.ByName("Sign in to Claude").And(cf.ByControlType(ControlType.Button))))
                        .FirstOrDefault(b => b.IsEnabled && !b.IsOffscreen);
                    return signIn is not null;
                }));
                signIn!.AsButton().Invoke();
                Assert.True(Wait(() => window.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Claude Pro 2", StringComparison.Ordinal))), "A second demo account did not appear");
            }
            else
            {
                Assert.True(Wait(() => Directory.Exists(Path.Combine(state, "preferences"))), "Live maintenance did not initialize isolated state");
                Assert.NotNull(Button("Sign in to Codex"));
                Assert.DoesNotContain(window.FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "").Contains("needs recovery", StringComparison.OrdinalIgnoreCase));
            }
            Button("Settings").Invoke();
            Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("Delete stored data").And(cf.ByControlType(ControlType.Button))) is not null));
            Button("Delete stored data").Invoke();
            Assert.NotNull(Button("Cancel deleting stored data"));
            Button("Cancel deleting stored data").Invoke();
            Button("Preview diagnostics").Invoke();
            Thread.Sleep(300);
            using (var screenshot = window.Capture()) screenshot.Save(Path.Combine(evidence!, prefix + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            Button("Close settings").Invoke();
            if (demo)
            {
                var handle = window.Properties.NativeWindowHandle.Value;
                window.TitleBar!.CloseButton!.Invoke();
                Assert.True(Wait(() => !IsWindowVisible(handle)), "Close should hide the main window");
                var desktop = automation.GetDesktop();
                var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
                AutomationElement[] Icons(AutomationElement? surface) => surface?
                    .FindAllDescendants(cf => cf.ByName("AI Usage").And(cf.ByControlType(ControlType.Button)))
                    .Where(icon => !icon.IsOffscreen).ToArray() ?? [];
                AutomationElement? row = null;
                var trayAttempts = new List<string>();
                bool OpenOwnedTray(AutomationElement icon)
                {
                    icon.AsButton().Invoke();
                    trayAttempts.Add("invoked " + icon.Properties.AutomationId.ValueOrDefault);
                    // Explorer owns every tray button. Match the opened window to this test's
                    // process before inspecting accounts; another AI Usage instance may be running.
                    if (Wait(() =>
                    {
                        row = desktop.FindAllChildren().Where(w => w.Properties.NativeWindowHandle.ValueOrDefault != handle &&
                            IsWindowVisible(w.Properties.NativeWindowHandle.ValueOrDefault) &&
                            GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == app.ProcessId)
                            .SelectMany(w => w.FindAllDescendants()).FirstOrDefault(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Claude Pro 2.", StringComparison.Ordinal));
                        return row is not null;
                    }, TimeSpan.FromSeconds(5))) return true;
                    Keyboard.Press(VirtualKeyShort.ESCAPE);
                    return false;
                }
                foreach (var icon in Icons(taskbar))
                    if (OpenOwnedTray(icon)) break;
                if (row is null)
                {
                    for (var index = 0; ; index++)
                    {
                        taskbar!.FindFirstDescendant(cf => cf.ByName("Show Hidden Icons"))!.AsButton().Invoke();
                        AutomationElement? overflow = null;
                        Assert.True(Wait(() =>
                        {
                            overflow = desktop.FindFirstChild(cf => cf.ByClassName("TopLevelWindowForOverflowXamlIsland"));
                            return Icons(overflow).Length > 0;
                        }));
                        var icons = Icons(overflow);
                        trayAttempts.Add("overflow icons=" + icons.Length);
                        if (index >= icons.Length) { Keyboard.Press(VirtualKeyShort.ESCAPE); break; }
                        if (OpenOwnedTray(icons[index])) break;
                    }
                }
                Assert.True(row is not null, string.Join("; ", trayAttempts));
                row!.Focus();
                Assert.True(GetWindowThreadProcessId(GetForegroundWindow(), out var activeOwner) != 0 && activeOwner == app.ProcessId);
                Keyboard.Press(VirtualKeyShort.RETURN);
                Assert.True(Wait(() => IsWindowVisible(handle)), "Selecting the tray account should restore the main window");
                Assert.True(Wait(() => (automation.FocusedElement()?.Properties.AutomationId.ValueOrDefault ?? "").StartsWith("claude-week-", StringComparison.Ordinal)), "Tray account selection should focus that account's card");
            }
            Focus(window);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000), "Exit did not drain and terminate the launched process");
            Assert.Equal(0, process.ExitCode);
            passed = true;
        }
        finally
        {
            File.WriteAllText(Path.Combine(evidence!, prefix + ".json"), JsonSerializer.Serialize(new { passed, state, pid = app.ProcessId, exited = process.HasExited }));
            if (!process.HasExited)
            {
                if (window is not null)
                    try { DesktopTestEnvironment.RequireUnlockedDesktop(); using var screenshot = window.Capture(); screenshot.Save(Path.Combine(evidence!, prefix + "-failure.png"), System.Drawing.Imaging.ImageFormat.Png); }
                    catch (Exception) { }
                process.Kill(); process.WaitForExit(5000);
            }
        }
    }

    private static bool Wait(Func<bool> check, TimeSpan? timeout = null)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < (timeout ?? TimeSpan.FromSeconds(30)))
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            try { if (check()) return true; }
            catch (System.Runtime.InteropServices.COMException) { /* A published snapshot may replace a UIA element. */ }
            Thread.Sleep(100);
        }
        return false;
    }

    private static Window? OwnedWindow(UIA3Automation automation, int pid) => automation.GetDesktop().FindAllChildren().FirstOrDefault(w =>
    {
        var handle = w.Properties.NativeWindowHandle.ValueOrDefault;
        return handle != IntPtr.Zero && GetWindowThreadProcessId(handle, out var owner) != 0 && owner == pid &&
            w.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(ControlType.Button))) is not null;
    })?.AsWindow();

    private static void Focus(Window window)
    {
        var handle = window.Properties.NativeWindowHandle.Value;
        window.SetForeground();
        if (GetForegroundWindow() != handle)
        {
            var bounds = window.BoundingRectangle;
            Mouse.Click(new System.Drawing.Point(bounds.Left + 120, bounds.Top + 18));
        }
        Assert.True(Wait(() => GetForegroundWindow() == handle), "Test window must own keyboard input");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
