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
public sealed class LedgerSmoke
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfirmedDeletionAndInterruptedDeletionRestartCleanly(bool pending)
    {
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
            Assert.Contains("2", File.ReadAllText(Path.Combine(root, "layout.v1.json")));
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
            Button Button(string name) => window.FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.Button)))!.AsButton();
            Assert.NotNull(Button("Settings"));
            Button("Show values: left").Invoke(); Button("Show values: used").Invoke();
            if (demo)
            {
                window.FindFirstDescendant(cf => cf.ByAutomationId("claude-week"))!.Focus();
                Assert.True(Wait(() => window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button)).Any(b => (b.Properties.Name.ValueOrDefault ?? "").StartsWith("History, Claude", StringComparison.Ordinal))));
                var history = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button)).First(b => (b.Properties.Name.ValueOrDefault ?? "").StartsWith("History, Claude", StringComparison.Ordinal));
                history.AsButton().Invoke();
                Assert.True(Wait(() => window.FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("History", StringComparison.Ordinal))), "History did not open");
                Keyboard.Press(VirtualKeyShort.ESCAPE);
                Button("Add account").Invoke();
                var signIn = automation.GetDesktop().FindAllDescendants(cf => cf.ByName("Sign in to Claude").And(cf.ByControlType(ControlType.Button))).First(b => b.IsEnabled);
                signIn.AsButton().Invoke();
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
                AutomationElement? icon = taskbar?.FindFirstDescendant(cf => cf.ByName("AI Usage").And(cf.ByControlType(ControlType.Button)));
                if (icon is null || icon.IsOffscreen)
                {
                    taskbar!.FindFirstDescendant(cf => cf.ByName("Show Hidden Icons"))!.AsButton().Invoke();
                    Assert.True(Wait(() => (icon = desktop.FindFirstChild(cf => cf.ByClassName("TopLevelWindowForOverflowXamlIsland"))?
                        .FindFirstDescendant(cf => cf.ByName("AI Usage").And(cf.ByControlType(ControlType.Button)))) is not null));
                }
                icon!.AsButton().Invoke();
                AutomationElement? row = null;
                Assert.True(Wait(() =>
                {
                    row = desktop.FindAllChildren().Where(w => GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == app.ProcessId)
                        .SelectMany(w => w.FindAllDescendants()).FirstOrDefault(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Claude Pro 2.", StringComparison.Ordinal));
                    return row is not null;
                }), "The tray should expose the second account independently");
                row!.Click();
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
                    try { using var screenshot = window.Capture(); screenshot.Save(Path.Combine(evidence!, prefix + "-failure.png"), System.Drawing.Imaging.ImageFormat.Png); }
                    catch (Exception) { }
                process.Kill(); process.WaitForExit(5000);
            }
        }
    }

    private static bool Wait(Func<bool> check)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(30))
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            if (check()) return true;
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
        if (GetForegroundWindow() != handle) window.TitleBar!.Click();
        Assert.True(Wait(() => GetForegroundWindow() == handle), "Test window must own keyboard input");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}

