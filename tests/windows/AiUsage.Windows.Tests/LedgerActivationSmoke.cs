using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class LedgerSmoke
{
    [Fact]
    public void WorkBudgetShowsMonthlyAndDailyBarsBesideSubscriptionWithoutCredits()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe));
        Assert.False(string.IsNullOrWhiteSpace(evidence));
        var start = new ProcessStartInfo(exe!, "--demo --scenario=work-budget") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-work-budget-" + Guid.NewGuid().ToString("N"));
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        using var automation = new UIA3Automation();
        _ = process.Handle;
        try
        {
            Assert.True(Wait(() => OwnedWindow(automation, app.ProcessId) is not null));
            var window = OwnedWindow(automation, app.ProcessId)!;
            SmokeKit.Focus(window);
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("Claude Work")));
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("Codex subscription")));
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("month")));
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("today")));
            Assert.Null(window.FindFirstDescendant(cf => cf.ByName("Credits")));
            Directory.CreateDirectory(evidence!);
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = window.Capture()) capture.Save(Path.Combine(evidence!, "work-budget-used.png"), System.Drawing.Imaging.ImageFormat.Png);
            // This test is about the work-budget bars, not mouse input, so the toggle is invoked.
            SmokeKit.Find(() => OwnedWindow(automation, app.ProcessId)?.FindFirstDescendant(cf => cf.ByAutomationId("LeftButton")), "Left").AsButton().Invoke();
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = window.Capture()) capture.Save(Path.Combine(evidence!, "work-budget-left.png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }

    /// <summary>R-199: the Copilot Business popover shows credits in dollars and stores today's use, driven through UI Automation patterns.</summary>
    [Fact]
    public void WorkBudgetPopoverShowsCreditsInDollarsAndSetsToday()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe));
        Assert.False(string.IsNullOrWhiteSpace(evidence));
        var start = new ProcessStartInfo(exe!, "--demo --scenario=work-budget") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-credits-" + Guid.NewGuid().ToString("N"));
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        using var automation = new UIA3Automation();
        _ = process.Handle;
        try
        {
            Assert.True(Wait(() => OwnedWindow(automation, app.ProcessId) is not null));
            // The popover replaces elements, so every query resolves the window afresh.
            Window Main() => (Window)SmokeKit.Find(() => OwnedWindow(automation, app.ProcessId), "Ledger window");
            SmokeKit.Focus(Main());
            Directory.CreateDirectory(evidence!);
            void Capture(string name) { DesktopTestEnvironment.RequireUnlockedDesktop(); using var image = Main().Capture(); image.Save(Path.Combine(evidence!, name), System.Drawing.Imaging.ImageFormat.Png); }
            // A WinUI flyout can be its own popup window, so names are looked up in all of this app's windows, and only there.
            AutomationElement? Named(string name) => SmokeKit.FindOwned(automation.GetDesktop(), app.ProcessId, cf => cf.ByName(name));
            bool Shows(string text) => SmokeKit.OwnedWindows(automation.GetDesktop(), app.ProcessId).SelectMany(w => w.FindAllDescendants().Prepend(w))
                .Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains(text, StringComparison.Ordinal));
            void Open() { Named("Limit settings, Copilot Business Premium requests")!.AsButton().Invoke(); Assert.True(Wait(() => Named("Show as US dollars") is not null)); }

            Assert.True(Wait(() => Shows("3,240 of 17,500 used")));
            // R-202: a weekly percent window takes a percent cap, and the bar then spans the cap.
            // R-203: the card has no cap editor or cap caption of its own; C opens the limit settings at the cap field.
            Assert.DoesNotContain(Main().FindAllDescendants(), e => e.Properties.ControlType.ValueOrDefault == FlaUI.Core.Definitions.ControlType.Button &&
                ((e.Properties.Name.ValueOrDefault ?? "").Contains("edit the cap", StringComparison.Ordinal) || e.Properties.Name.ValueOrDefault == "Set cap"));
            Named("Limit settings, Codex subscription 7 day")!.Focus();
            Keyboard.Type(VirtualKeyShort.KEY_C);
            Assert.True(Wait(() => Named("Cap amount in %")?.Properties.HasKeyboardFocus.ValueOrDefault == true));
            Named("Cap amount in %")!.Patterns.Value.Pattern.SetValue("90");
            Capture("percent-cap-popover.png");
            Named("Save")!.AsButton().Invoke();
            Assert.True(Wait(() => Shows("of 90 % cap")));
            Capture("percent-cap.png");
            Open();
            Capture("credits-popover.png");
            Named("Show as US dollars")!.AsButton().Invoke();
            Assert.True(Wait(() => Shows("$32.40 of $175.00 used")));
            Assert.True(Wait(() => Named("Today’s use in USD") is not null));
            var today = Named("Today’s use in USD")!;
            Assert.Equal("1.20", today.Patterns.Value.Pattern.Value.Value);
            today.Patterns.Value.Pattern.SetValue("9.00");
            Capture("credits-usd-popover.png");
            Named("Save")!.AsButton().Invoke();
            Assert.True(Wait(() => Named("Show as US dollars") is null));
            Open();
            Assert.True(Wait(() => Named("Today’s use in USD")?.Patterns.Value.Pattern.Value.Value == "9.00"));
            Assert.True(Shows("set by you"));
            Capture("credits-today-set.png");
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeTrayExitTerminatesTheOwningProcess(bool hidden)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        Assert.False(string.IsNullOrWhiteSpace(exe));
        var start = new ProcessStartInfo(exe!, "--demo") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-tray-exit-" + Guid.NewGuid().ToString("N"));
        // OD-23: this launch's tray icon is "AI Usage <id>", so no other AI Usage instance can match.
        var trayId = SmokeKit.NewTrayId();
        start.Environment[SmokeKit.TrayIdVariable] = trayId;
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        using var automation = new UIA3Automation();
        _ = process.Handle;
        try
        {
            Assert.True(Wait(() => OwnedWindow(automation, app.ProcessId) is not null));
            var window = OwnedWindow(automation, app.ProcessId)!;
            SmokeKit.Focus(window);
            if (hidden) window.TitleBar!.CloseButton!.Invoke();
            var desktop = automation.GetDesktop();
            AutomationElement? taskbar = null;
            Assert.True(Wait(() => (taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"))) is not null), "Windows taskbar must be available");
            // OD-23: only this launch's icon carries its name; it shows in the taskbar or in the hidden-icons overflow.
            var trayName = SmokeKit.TrayName(trayId);
            AutomationElement? Icon(AutomationElement? parent) => parent?
                .FindAllDescendants(cf => cf.ByName(trayName).And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))
                .FirstOrDefault(e => !e.IsOffscreen);
            AutomationElement? Overflow() => desktop.FindFirstChild(cf => cf.ByClassName("TopLevelWindowForOverflowXamlIsland"));
            AutomationElement? icon = null;
            if (!Wait(() => (icon = Icon(taskbar)) is not null, TimeSpan.FromSeconds(2)))
            {
                if (Overflow() is not { IsOffscreen: false })
                {
                    var toggle = taskbar!.FindAllDescendants().FirstOrDefault(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Hidden Icons", StringComparison.Ordinal));
                    Assert.NotNull(toggle);
                    toggle.AsButton().Invoke();
                }
                Assert.True(Wait(() => (icon = Icon(Overflow())) is not null), "No tray icon named " + trayName);
            }
            // Explorer (the taskbar or its overflow host) owns the icon, so the point must belong to the icon's own process.
            SmokeKit.RightClickOwned(icon!, icon!.Properties.ProcessId.Value);
            AutomationElement? exitItem = null;
            Assert.True(Wait(() =>
            {
                exitItem = desktop.FindAllChildren().Where(w => w.Properties.ProcessId.ValueOrDefault == app.ProcessId)
                    .Select(w => w.FindFirstDescendant(cf => cf.ByName("Exit"))).FirstOrDefault(e => e is not null);
                return exitItem is not null;
            }, TimeSpan.FromSeconds(5)), "The tray menu of " + trayName + " did not open");
            SmokeKit.ClickOwned(exitItem!, app.ProcessId);
            Assert.True(process.WaitForExit(10000), "Clicking the native tray Exit item must drain and exit");
            Assert.Equal(0, process.ExitCode);
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }

    [Theory]
    [InlineData("Claude")]
    [InlineData("Codex")]
    [InlineData("GitHub Copilot")]
    [InlineData("Antigravity")]
    public void FirstRunSignInButtonsRespondToMouseClicksAfterSettingsCloses(string provider)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe));
        Assert.False(string.IsNullOrWhiteSpace(evidence));
        var start = new ProcessStartInfo(exe!, "--demo --scenario=first-run") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-buttons-" + Guid.NewGuid().ToString("N"));
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        using var automation = new UIA3Automation();
        _ = process.Handle;
        Window? Current() => OwnedWindow(automation, app.ProcessId);
        AutomationElement Named(string name) => SmokeKit.Find(() => Current()?.FindFirstDescendant(cf => cf.ByName(name)), name);
        try
        {
            SmokeKit.Focus((Window)SmokeKit.Find(Current, "Ledger window"));
            SmokeKit.ClickOwned(SmokeKit.Find(() => Current()?.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button))), "Settings"), app.ProcessId);
            // R-197: the settings sheet covers the whole body, and the first-run buttons behind it leave the tab order.
            Assert.True(Wait(() => Current()?.FindFirstDescendant(cf => cf.ByName("Sign in to " + provider)) is { IsEnabled: false }),
                "Open settings must cover the first-run sign-in buttons");
            SmokeKit.ClickOwned(Named("Close settings"), app.ProcessId);
            Assert.True(Wait(() => Current() is { } current && current.FindFirstDescendant(cf => cf.ByName("Close settings")) is null), "The settings sheet did not roll up");
            var button = Named("Sign in to " + provider).AsButton();
            button.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
            Assert.True(button.IsEnabled);
            Assert.False(button.IsOffscreen);
            Assert.True(Current()!.BoundingRectangle.Contains(button.BoundingRectangle));
            SmokeKit.ClickOwned(button, app.ProcessId);
            // The one-line sign-in strip names the account and then says "added · N limits" for a few seconds.
            Assert.True(Wait(() => Current()?.FindAllDescendants().Any(e =>
                (e.Properties.Name.ValueOrDefault ?? "").StartsWith("added", StringComparison.Ordinal)) == true), "The sign-in click must add a synthetic account");
            Directory.CreateDirectory(evidence!);
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = Current()!.Capture()) capture.Save(Path.Combine(evidence!, "sign-in-" + provider.Replace(' ', '-') + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
        }
        finally
        {
            if (!process.HasExited)
            {
                Directory.CreateDirectory(evidence!);
                SmokeKit.SaveFailure(evidence!, "sign-in-" + provider.Replace(' ', '-'), () => OwnedWindow(automation, app.ProcessId));
                process.Kill(); process.WaitForExit(5000);
            }
        }
    }

    [Fact]
    public void BusyStorageExplainsTheBlockAndRetryEnablesSignIn()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe));
        Assert.False(string.IsNullOrWhiteSpace(evidence));
        var root = Path.Combine(Path.GetTempPath(), "aiu-busy-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "state.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var start = new ProcessStartInfo(exe!) { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = root;
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        using var automation = new UIA3Automation();
        _ = process.Handle;
        Window? Current() => OwnedWindow(automation, app.ProcessId);
        AutomationElement Named(string name) => SmokeKit.Find(() => Current()?.FindFirstDescendant(cf => cf.ByName(name)), name);
        try
        {
            _ = Named("Recovery and diagnostics");
            Assert.Null(Current()!.FindFirstDescendant(cf => cf.ByName("Sign in to Codex")));
            Assert.False(SmokeKit.Find(() => Current()?.FindFirstDescendant(cf => cf.ByAutomationId("AddButton")), "Add account").IsEnabled);
            SmokeKit.Focus((Window)SmokeKit.Find(Current, "Ledger window"));
            SmokeKit.ClickOwned(Named("Recovery and diagnostics"), app.ProcessId);
            Assert.True(Wait(() => Current()?.FindFirstDescendant(cf => cf.ByName("Retry recovery")) is { IsOffscreen: false }));
            Assert.False(Named("Restore legacy preferences").IsEnabled);
            lease.Dispose();
            SmokeKit.ClickOwned(Named("Retry recovery"), app.ProcessId);
            // The first-run sign-in replaces the recovery notice behind the settings sheet (R-197), and is usable once the sheet closes.
            Assert.True(Wait(() => Current()?.FindFirstDescendant(cf => cf.ByName("Sign in to Codex")) is not null), "Retry did not finish recovery");
            Assert.Null(Current()!.FindFirstDescendant(cf => cf.ByName("Recovery and diagnostics")));
            SmokeKit.ClickOwned(Named("Close settings"), app.ProcessId);
            Assert.True(Wait(() => Current()?.FindFirstDescendant(cf => cf.ByName("Sign in to Codex")) is { IsEnabled: true }), "Sign-in stayed disabled after recovery");
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
        }
        finally
        {
            if (!process.HasExited)
            {
                Directory.CreateDirectory(evidence!);
                SmokeKit.SaveFailure(evidence!, "busy-storage", () => OwnedWindow(automation, app.ProcessId));
                process.Kill(); process.WaitForExit(5000);
            }
        }
    }

    [Fact]
    public void RepeatedLaunchRestoresTheExistingWindowWithoutRecovery()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        Assert.False(string.IsNullOrWhiteSpace(exe));
        var start = new ProcessStartInfo(exe!) { UseShellExecute = false };
        var root = Path.Combine(Path.GetTempPath(), "aiu-activation-" + Guid.NewGuid().ToString("N"));
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = root;
        using var first = Application.Launch(start);
        using var process = Process.GetProcessById(first.ProcessId);
        using var automation = new UIA3Automation();
        _ = process.Handle;
        try
        {
            Assert.True(Wait(() => OwnedWindow(automation, first.ProcessId) is not null));
            var window = OwnedWindow(automation, first.ProcessId)!;
            Assert.True(Wait(() => Directory.Exists(Path.Combine(root, "preferences"))));
            var handle = window.Properties.NativeWindowHandle.Value;
            window.TitleBar!.CloseButton!.Invoke();
            Assert.True(Wait(() => !IsWindowVisible(handle)));
            using var second = Application.Launch(start);
            using var secondProcess = Process.GetProcessById(second.ProcessId);
            _ = secondProcess.Handle;
            try
            {
                Assert.True(secondProcess.WaitForExit(10000), "Repeated launch must redirect and exit, not open recovery");
                Assert.Equal(0, secondProcess.ExitCode);
                Assert.True(Wait(() => IsWindowVisible(handle)), "Repeated launch must restore the hidden window");
                // The element held across hide and restore may be stale, so the restored window is looked up afresh.
                Window Restored() => (Window)SmokeKit.Find(() => OwnedWindow(automation, first.ProcessId), "restored Ledger window");
                Assert.Null(Restored().FindFirstDescendant(cf => cf.ByName("Recovery and diagnostics")));
                Assert.True(Restored().FindFirstDescendant(cf => cf.ByName("Sign in to Codex"))!.IsEnabled);
                SmokeKit.Focus(Restored());
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
                Assert.True(process.WaitForExit(10000));
                Assert.Equal(0, process.ExitCode);
            }
            finally { if (!secondProcess.HasExited) { secondProcess.Kill(); secondProcess.WaitForExit(5000); } }
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }
}
