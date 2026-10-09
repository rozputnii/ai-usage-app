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
            Focus(window);
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("Claude Work")));
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("Codex subscription")));
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("month")));
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByName("today")));
            Assert.Null(window.FindFirstDescendant(cf => cf.ByName("Credits")));
            Directory.CreateDirectory(evidence!);
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = window.Capture()) capture.Save(Path.Combine(evidence!, "work-budget-used.png"), System.Drawing.Imaging.ImageFormat.Png);
            window.FindFirstDescendant(cf => cf.ByName("Show values: left"))!.AsButton().Click();
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
            var window = OwnedWindow(automation, app.ProcessId)!;
            Focus(window);
            Directory.CreateDirectory(evidence!);
            void Capture(string name) { DesktopTestEnvironment.RequireUnlockedDesktop(); using var image = window.Capture(); image.Save(Path.Combine(evidence!, name), System.Drawing.Imaging.ImageFormat.Png); }
            // A WinUI flyout can be its own popup window, so names are looked up in the window and then on the desktop.
            AutomationElement? Named(string name) => window.FindFirstDescendant(cf => cf.ByName(name)) ?? automation.GetDesktop().FindFirstDescendant(cf => cf.ByName(name));
            bool Shows(string text) => window.FindAllDescendants().Concat(automation.GetDesktop().FindAllChildren())
                .Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains(text, StringComparison.Ordinal));
            void Open() { Named("Limit settings, Copilot Business Premium requests")!.AsButton().Invoke(); Assert.True(Wait(() => Named("Show as US dollars") is not null)); }

            Assert.True(Wait(() => Shows("3,240 of 17,500 used")));
            // R-202: a weekly percent window takes a percent cap, and the bar then spans the cap.
            // R-203: the card has no cap editor or cap caption of its own; C opens the limit settings at the cap field.
            Assert.DoesNotContain(window.FindAllDescendants(), e => e.Properties.ControlType.ValueOrDefault == FlaUI.Core.Definitions.ControlType.Button &&
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
            Focus(window);
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
            icon!.RightClick();
            AutomationElement? exitItem = null;
            Assert.True(Wait(() =>
            {
                exitItem = desktop.FindAllChildren().Where(w => w.Properties.ProcessId.ValueOrDefault == app.ProcessId)
                    .Select(w => w.FindFirstDescendant(cf => cf.ByName("Exit"))).FirstOrDefault(e => e is not null);
                return exitItem is not null;
            }, TimeSpan.FromSeconds(5)), "The tray menu of " + trayName + " did not open");
            exitItem!.Click();
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
    public void FirstRunSignInButtonsRespondToMouseClicksWithSettingsOpen(string provider)
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
        try
        {
            Assert.True(Wait(() => OwnedWindow(automation, app.ProcessId) is not null));
            var window = OwnedWindow(automation, app.ProcessId)!;
            Focus(window);
            window.FindFirstDescendant(cf => cf.ByName("Settings").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))!.AsButton().Click();
            Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("Close settings")) is not null));
            var button = window.FindFirstDescendant(cf => cf.ByName("Sign in to " + provider))!.AsButton();
            button.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
            Assert.True(button.IsEnabled);
            Assert.False(button.IsOffscreen);
            Assert.True(window.BoundingRectangle.Contains(button.BoundingRectangle));
            button.Click();
            Assert.True(Wait(() => window.FindAllDescendants().Any(e =>
                (e.Properties.Name.ValueOrDefault ?? "").EndsWith(" added", StringComparison.Ordinal))), "The sign-in click must add a synthetic account");
            Directory.CreateDirectory(evidence!);
            DesktopTestEnvironment.RequireUnlockedDesktop(); using (var capture = window.Capture()) capture.Save(Path.Combine(evidence!, "sign-in-" + provider.Replace(' ', '-') + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }

    [Fact]
    public void BusyStorageExplainsTheBlockAndRetryEnablesSignIn()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        Assert.False(string.IsNullOrWhiteSpace(exe));
        var root = Path.Combine(Path.GetTempPath(), "aiu-busy-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "state.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var start = new ProcessStartInfo(exe!) { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = root;
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        using var automation = new UIA3Automation();
        _ = process.Handle;
        try
        {
            Assert.True(Wait(() => OwnedWindow(automation, app.ProcessId) is not null));
            var window = OwnedWindow(automation, app.ProcessId)!;
            Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("Recovery and diagnostics")) is not null));
            Assert.Null(window.FindFirstDescendant(cf => cf.ByName("Sign in to Codex")));
            Assert.False(window.FindFirstDescendant(cf => cf.ByName("Add account"))!.IsEnabled);
            Focus(window);
            window.FindFirstDescendant(cf => cf.ByName("Recovery and diagnostics"))!.AsButton().Click();
            Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("Retry recovery")) is { IsOffscreen: false }));
            Assert.False(window.FindFirstDescendant(cf => cf.ByName("Restore legacy preferences"))!.IsEnabled);
            lease.Dispose();
            window.FindFirstDescendant(cf => cf.ByName("Retry recovery"))!.AsButton().Click();
            Assert.True(Wait(() => window.FindFirstDescendant(cf => cf.ByName("Sign in to Codex")) is { IsEnabled: true }));
            Assert.Null(window.FindFirstDescendant(cf => cf.ByName("Recovery and diagnostics")));
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
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
                Assert.Null(window.FindFirstDescendant(cf => cf.ByName("Recovery and diagnostics")));
                Assert.True(window.FindFirstDescendant(cf => cf.ByName("Sign in to Codex"))!.IsEnabled);
                Focus(window);
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
                Assert.True(process.WaitForExit(10000));
                Assert.Equal(0, process.ExitCode);
            }
            finally { if (!secondProcess.HasExited) { secondProcess.Kill(); secondProcess.WaitForExit(5000); } }
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }
}
