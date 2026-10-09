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
public sealed partial class LedgerSmoke : IDisposable
{
    private readonly Stopwatch started = Stopwatch.StartNew();

    /// <summary>R5: every LedgerSmoke result is appended to the local smoke history once the test has finished.</summary>
    public void Dispose() => SmokeKit.RecordResult(started.Elapsed);

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
            FocusIn(money);
            stage = "history";
            // A button clicks on key release, so type the key instead of only pressing it.
            Keyboard.Type(VirtualKeyShort.RETURN);
            Assert.True(Wait(() => Current()?.FindAllDescendants().Any(e =>
                (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Mixed account history,", StringComparison.Ordinal)) == true));
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "money-history.png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            money = Main().FindFirstDescendant(cf => cf.ByAutomationId("money-mixed"))!;
            stage = "cap";
            FocusIn(money); Keyboard.Press(VirtualKeyShort.KEY_C);
            Assert.True(Wait(() => money.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit)).Any(e => !e.IsOffscreen)));
            var amount = money.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit)).First(e => !e.IsOffscreen).AsTextBox();
            // Only digits and two decimals are typed, and never above the $500.00 provider limit.
            amount.Focus(); Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A); Keyboard.Type("6005-x.25");
            Assert.True(Wait(() => amount.Text == "60.25"), "Cap input kept " + amount.Text);
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "money-cap.png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            parent = Main().FindFirstDescendant(cf => cf.ByAutomationId("claude-week"))!;
            FocusIn(parent); Keyboard.Press(VirtualKeyShort.F2);
            stage = "rename";
            TextBox? rename = null;
            Assert.True(Wait(() => (rename = Current()?.FindFirstDescendant(cf => cf.ByName("Account name").And(cf.ByControlType(ControlType.Edit)))?.AsTextBox()) is not null));
            rename!.Text = "Renamed account";
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "money-rename.png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.Press(VirtualKeyShort.RETURN);
            Assert.True(Wait(() => Current()?.FindFirstDescendant(cf => cf.ByName("Renamed account")) is not null));
            var only = Main().FindFirstDescendant(cf => cf.ByAutomationId("money-only"))!;
            stage = "money only";
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
                Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("More settings").And(cf.ByControlType(ControlType.Button))) is not null));
                window.FindFirstDescendant(cf => cf.ByName("More settings").And(cf.ByControlType(ControlType.Button)))!.AsButton().Invoke();
                Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("Delete stored data").And(cf.ByControlType(ControlType.MenuItem))) is not null));
                window.FindFirstDescendant(cf => cf.ByName("Delete stored data").And(cf.ByControlType(ControlType.MenuItem)))!.Patterns.Invoke.Pattern.Invoke();
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
                Assert.Equal(3, layout.RootElement.GetProperty("Layout").GetInt32());
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

    // AIU-055 R-06: the comfortable run switches density in settings first, so its tray capture shows Comfortable.
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void LedgerLaunchSettingsHistoryAndExit(bool demo, bool comfortable)
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
        var trayId = SmokeKit.NewTrayId();
        start.Environment[SmokeKit.TrayIdVariable] = trayId;
        using var app = Application.Launch(start);
        using var automation = new UIA3Automation();
        using var process = Process.GetProcessById(app.ProcessId);
        _ = process.Handle;
        Window? window = null;
        bool passed = false;
        string prefix = (demo ? "ledger-demo" : "ledger-live-empty") + (comfortable ? "-comfortable" : string.Empty);
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
                Assert.True(Wait(() => (button = Current()?.FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.Button)))?.AsButton()) is { IsEnabled: true }), "Missing or disabled button: " + name);
                return button!;
            }
            // Support actions and Delete stored data sit in the settings footer menu (D-193).
            void Menu(string name)
            {
                Button("More settings").Invoke();
                AutomationElement? item = null;
                Assert.True(Wait(() => (item = Current()?.FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.MenuItem)))) is not null), "Missing menu item: " + name);
                item!.Patterns.Invoke.Pattern.Invoke();
            }
            Assert.NotNull(Button("Settings"));
            Button("Show values: left").Invoke(); Button("Show values: used").Invoke();
            if (demo)
            {
                AutomationElement? history = null;
                Assert.True(Wait(() =>
                {
                    var current = Current();
                    var card = current?.FindFirstDescendant(cf => cf.ByAutomationId("claude-week"));
                    if (card is null) return false;
                    history = current!.FindAllDescendants(cf => cf.ByControlType(ControlType.Button)).FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "").StartsWith("History, Claude", StringComparison.Ordinal));
                    return history is not null;
                }));
                history!.AsButton().Invoke();
                Assert.True(Wait(() => Current()?.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("History", StringComparison.Ordinal)) == true), "History did not open");
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
                Assert.True(Wait(() => Current()?.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Claude Pro 2", StringComparison.Ordinal)) == true), "A second demo account did not appear");
            }
            else
            {
                Assert.True(Wait(() => Directory.Exists(Path.Combine(state, "preferences"))), "Live maintenance did not initialize isolated state");
                Assert.NotNull(Button("Sign in to Codex"));
                // Live startup still replaces elements here; a scan that loses an element (COMException) is repeated, and only a complete scan counts.
                bool? recovery = null;
                Assert.True(Wait(() => (recovery = Main().FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("needs recovery", StringComparison.OrdinalIgnoreCase))) is not null), "No complete window scan");
                Assert.False(recovery, "Live-empty startup must not show recovery");
            }
            Button("Settings").Invoke();
            if (comfortable)
                Button("Density: comfortable").Invoke();
            Menu("Delete stored data");
            Assert.NotNull(Button("Cancel deleting stored data"));
            Button("Cancel deleting stored data").Invoke();
            Menu("Preview diagnostics");
            Assert.True(Wait(() => Main().FindFirstDescendant(cf => cf.ByName("Preview diagnostics").And(cf.ByControlType(ControlType.MenuItem))) is null),
                "The settings menu did not close");
            using (var screenshot = Main().Capture()) screenshot.Save(Path.Combine(evidence!, prefix + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            Button("Close settings").Invoke();
            if (demo)
            {
                var main = Main();
                var handle = main.Properties.NativeWindowHandle.Value;
                main.TitleBar!.CloseButton!.Invoke();
                Assert.True(Wait(() => !IsWindowVisible(handle)), "Close should hide the main window");
                var desktop = automation.GetDesktop();
                var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
                // OD-23: this launch names its tray icon "AI Usage <id>", so no other AI Usage instance, such as the installed
                // app, can match. The icon shows in the taskbar or, when Windows hides it, in the hidden-icons overflow.
                var trayName = SmokeKit.TrayName(trayId);
                AutomationElement? Icon(AutomationElement? surface) => surface?
                    .FindAllDescendants(cf => cf.ByName(trayName).And(cf.ByControlType(ControlType.Button)))
                    .FirstOrDefault(icon => !icon.IsOffscreen);
                AutomationElement? Overflow() => desktop.FindFirstChild(cf => cf.ByClassName("TopLevelWindowForOverflowXamlIsland"));
                AutomationElement? icon = null;
                if (!Wait(() => (icon = Icon(taskbar)) is not null, TimeSpan.FromSeconds(2)))
                {
                    // The chevron is a SystemTrayIcon button named "Show Hidden Icons", and "Show Hidden Icons Hide" while the
                    // overflow is open (invoking it then closes it), so it is invoked only while the overflow is closed.
                    if (Overflow() is null)
                        SmokeKit.Find(() => taskbar?.FindAllDescendants(cf => cf.ByAutomationId("SystemTrayIcon").And(cf.ByControlType(ControlType.Button)))
                            .FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "").StartsWith("Show Hidden Icons", StringComparison.Ordinal)),
                            "Show Hidden Icons button").AsButton().Invoke();
                    icon = SmokeKit.Find(() => Icon(Overflow()), "tray icon " + trayName);
                }
                icon!.AsButton().Invoke();
                AutomationElement? row = null;
                AutomationElement? trayWindow = null;
                // Explorer owns every tray button; the opened flyout must still belong to this test's process.
                Assert.True(Wait(() =>
                {
                    trayWindow = desktop.FindAllChildren().FirstOrDefault(w => w.Properties.NativeWindowHandle.ValueOrDefault != handle &&
                        IsWindowVisible(w.Properties.NativeWindowHandle.ValueOrDefault) &&
                        GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == app.ProcessId &&
                        w.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Claude Pro 2.", StringComparison.Ordinal)));
                    row = trayWindow?.FindAllDescendants().FirstOrDefault(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Claude Pro 2.", StringComparison.Ordinal));
                    return row is not null;
                }, TimeSpan.FromSeconds(10)), "The tray flyout of " + trayName + " did not open");
                Assert.True(GetWindowThreadProcessId(GetForegroundWindow(), out var activeOwner) != 0 && activeOwner == app.ProcessId);
                DesktopTestEnvironment.RequireUnlockedDesktop();
                // AIU-055 R-08: the flyout content is 260 logical px wide. The window itself is wider by its invisible resize borders,
                // so the width is measured on the content, not on the window.
                var trayWidth = (int)(260 * GetDpiForWindow(trayWindow!.Properties.NativeWindowHandle.Value) / 96.0);
                // The flyout content has no AutomationId, so it is found by its name.
                var trayContent = SmokeKit.Find(() => trayWindow.FindFirstDescendant(cf => cf.ByName("AI Usage tray")), "tray flyout content");
                Assert.True(Math.Abs(trayContent.BoundingRectangle.Width - trayWidth) <= 2,
                    $"Tray flyout content should be 260 px wide ({trayWidth} physical px), was {trayContent.BoundingRectangle.Width}");
                using (var capture = trayWindow.Capture()) capture.Save(Path.Combine(evidence!, comfortable ? "tray-icons-comfortable.png" : "tray-icons.png"), System.Drawing.Imaging.ImageFormat.Png);
                // The flyout is a pointer-only miniature (D-204): no element of its XAML content is a tab stop, so no focus frame
                // can show. The popup host panes of an open tooltip and the native title bar are Win32 chrome, not its content.
                var focusable = trayWindow.FindAllDescendants().Where(e => e.Properties.FrameworkId.ValueOrDefault == "XAML" && e.Properties.IsKeyboardFocusable.ValueOrDefault)
                    .Select(e => $"{e.Properties.FrameworkId.ValueOrDefault}/{e.Properties.ControlType.ValueOrDefault}/{e.Properties.Name.ValueOrDefault}").ToArray();
                Assert.True(focusable.Length == 0, "Tray flyout elements must not be keyboard-focusable: " + string.Join("; ", focusable));
                // AIU-055 R-07: a provider mark replaces the account name, so no element of the flyout shows it as text.
                Assert.DoesNotContain(trayWindow.FindAllDescendants(), e => e.Properties.Name.ValueOrDefault == "Claude Pro 2");
                row!.Click();
                Assert.True(Wait(() => IsWindowVisible(handle)), "Selecting the tray account should restore the main window");
                bool InCard(AutomationElement? element)
                {
                    for (; element is not null; element = element.Parent)
                        if ((element.Properties.AutomationId.ValueOrDefault ?? "").StartsWith("claude-week-", StringComparison.Ordinal)) return true;
                    return false;
                }
                Assert.True(Wait(() => InCard(automation.FocusedElement())), "Tray account selection should focus a control in that account's card");
            }
            Focus(Main());
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
                SmokeKit.SaveFailure(evidence!, prefix, () => Current() ?? window);
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

    /// <summary>Cards are not tab stops (D-200); their keys work from any control inside, here the History button.</summary>
    private static void FocusIn(AutomationElement card) => card.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
        .First(b => (b.Properties.Name.ValueOrDefault ?? "").StartsWith("History,", StringComparison.Ordinal)).Focus();

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

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);
}
