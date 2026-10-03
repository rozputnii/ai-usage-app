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
            window.SetForeground();
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
            window.SetForeground();
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

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
