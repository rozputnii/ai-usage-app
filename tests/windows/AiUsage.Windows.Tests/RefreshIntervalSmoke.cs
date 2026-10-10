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

/// <summary>The refresh interval stepper in Settings (T-055 R-11), in the demo app; isolated synthetic state, no sign-in.</summary>
public sealed class RefreshIntervalSmoke
{
    /// <summary>Clicking + shows 6; typing 1 and Enter shows 1 and stops the − button; letters are not accepted.</summary>
    [Fact]
    public void StepperChangesTheInterval()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe)); Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence!);
        var start = new ProcessStartInfo(exe!, "--demo") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-refresh-smoke-" + Guid.NewGuid().ToString("N"));
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        _ = process.Handle;
        using var automation = new UIA3Automation();
        var passed = false;
        var stage = "launch";
        // A cached UIA element can turn invalid when a snapshot replaces it; resolve the window afresh for each query.
        Window Main()
        {
            Window? current = null;
            Assert.True(Wait(() => (current = OwnedWindow(automation, app.ProcessId)) is not null), "Ledger window is not available");
            return current!;
        }
        AutomationElement Named(string name, ControlType type)
        {
            AutomationElement? element = null;
            Assert.True(Wait(() => (element = Main().FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(type)))) is not null), "Missing " + name);
            return element!;
        }
        AutomationElement Button(string name) => Named(name, ControlType.Button);
        TextBox Box() => SmokeKit.Find(() => Main().FindFirstDescendant(cf => cf.ByAutomationId("RefreshBox").And(cf.ByControlType(ControlType.Edit))),
            "Refresh interval in minutes").AsTextBox();
        // The box's context menu, in the window or in a popup window of the app.
        bool ShowsMenu() => SmokeKit.OwnedWindows(automation.GetDesktop(), app.ProcessId).Any(w =>
            w.Properties.ControlType.ValueOrDefault == ControlType.Menu || w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Menu)) is not null);
        try
        {
            SmokeKit.Focus(Main());
            stage = "open settings";
            Button("Settings").AsButton().Invoke();
            Assert.Equal("5", Box().Text);

            stage = "click +";
            SmokeKit.ClickOwned(Button("Longer refresh interval"), app.ProcessId);
            Assert.True(Wait(() => Box().Text == "6"), "+ left the box at " + Box().Text);

            stage = "type 1, Enter";
            SmokeKit.ClickOwned(Box(), app.ProcessId);
            Assert.True(Wait(() => Box().Properties.HasKeyboardFocus.ValueOrDefault), "The click did not focus the box");
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type("1a");
            Keyboard.Press(VirtualKeyShort.RETURN);
            Assert.True(Wait(() => Box().Text == "1"), "Typing 1a and Enter left the box at " + Box().Text);
            Assert.True(Wait(() => !Button("Shorter refresh interval").IsEnabled), "− must stop at 1");
            Assert.True(Button("Longer refresh interval").IsEnabled);
            // The footer no longer carries the interval.
            Assert.True(Wait(() => !Main().FindAllDescendants().Any(e => (e.Properties.Name.ValueOrDefault ?? "").StartsWith("every ", StringComparison.Ordinal))),
                "The footer still shows the interval");

            DesktopTestEnvironment.RequireUnlockedDesktop();
            using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "settings-refresh.png"), System.Drawing.Imaging.ImageFormat.Png);

            stage = "context menu keeps the typed text";
            // Focus moves into the box's own menu; that is not leaving the box, so an emptied box is not put back to the saved value.
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Press(VirtualKeyShort.DELETE);
            Assert.True(Wait(() => Box().Text == ""), "The box was not emptied");
            SmokeKit.RightClickOwned(Box(), app.ProcessId);
            Assert.True(Wait(ShowsMenu, TimeSpan.FromSeconds(5)), "The box opened no context menu");
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Assert.True(Wait(() => !ShowsMenu(), TimeSpan.FromSeconds(5)), "Esc left the context menu open");
            Assert.Equal("", Box().Text);

            stage = "Tab saves";
            Keyboard.Type("2");
            Keyboard.Press(VirtualKeyShort.TAB);
            Assert.True(Wait(() => Box().Text == "2" && !Box().Properties.HasKeyboardFocus.ValueOrDefault && Button("Shorter refresh interval").IsEnabled), "Tab did not save 2");

            stage = "exit";
            // Ctrl+Q does not reach the window while any text box (the rename box too) holds the focus; here it has left the box.
            SmokeKit.Focus(Main());
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000), "Exit did not terminate the launched process");
            Assert.Equal(0, process.ExitCode);
            passed = true;
        }
        finally
        {
            File.WriteAllText(Path.Combine(evidence!, "refresh-smoke.json"), JsonSerializer.Serialize(new { passed, stage, exited = process.HasExited }));
            if (!process.HasExited)
            {
                try
                {
                    if (OwnedWindow(automation, app.ProcessId) is { } failed)
                        using (var capture = failed.Capture()) capture.Save(Path.Combine(evidence!, "refresh-failure.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (System.Runtime.InteropServices.COMException) { }
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

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
