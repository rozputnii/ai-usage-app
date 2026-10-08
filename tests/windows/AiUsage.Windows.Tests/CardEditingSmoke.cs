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

/// <summary>Editing account cards with the mouse in the demo app (AIU-055); isolated synthetic state, no sign-in.</summary>
public sealed partial class CardEditingSmoke
{
    /// <summary>R-09: a click on the name starts the rename; Enter, a click elsewhere and a click on another name save; Esc cancels.</summary>
    [Fact]
    public void ClickingTheNameRenamesTheAccount()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe)); Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence!);
        var start = new ProcessStartInfo(exe!, "--demo") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-rename-smoke-" + Guid.NewGuid().ToString("N"));
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
        AutomationElement Card(string id)
        {
            AutomationElement? card = null;
            Assert.True(Wait(() => (card = Main().FindFirstDescendant(cf => cf.ByAutomationId(id))) is not null), "Missing card " + id);
            return card!;
        }
        AutomationElement Name(string cardId, string text)
        {
            AutomationElement? name = null;
            Assert.True(Wait(() => (name = Card(cardId).FindFirstDescendant(cf => cf.ByName(text).And(cf.ByControlType(ControlType.Text)))) is not null),
                $"Card {cardId} does not show the name {text}");
            return name!;
        }
        // The open rename box, once it holds keyboard focus with its text selected.
        TextBox Box(string cardId)
        {
            AutomationElement? box = null;
            Assert.True(Wait(() => (box = Card(cardId).FindFirstDescendant(cf => cf.ByName("Account name").And(cf.ByControlType(ControlType.Edit)))) is not null &&
                box.Properties.HasKeyboardFocus.ValueOrDefault), $"No focused rename box in {cardId}");
            return box!.AsTextBox();
        }
        bool NoBox() => Main().FindFirstDescendant(cf => cf.ByName("Account name").And(cf.ByControlType(ControlType.Edit))) is null;
        try
        {
            Focus(Main());
            stage = "hover";
            // The hovered name shows the dotted underline and the Rename tooltip (Codex: the Refresh tooltip covers Claude at launch).
            Hover(Name("codex-week", "Codex Pro").GetClickablePoint());
            Thread.Sleep(1500);
            DesktopTestEnvironment.RequireUnlockedDesktop();
            using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "rename-hover.png"), System.Drawing.Imaging.ImageFormat.Png);

            stage = "click, type, Enter";
            Name("claude-week", "Claude Pro").Click();
            Assert.Equal("Claude Pro", Box("claude-week").Text);
            Keyboard.Type("Claude Work");
            Keyboard.Press(VirtualKeyShort.RETURN);
            Name("claude-week", "Claude Work");
            Assert.True(Wait(NoBox), "Enter left the rename box open");

            stage = "focus leaving saves";
            Name("claude-week", "Claude Work").Click();
            Box("claude-week");
            Keyboard.Type("Claude Tab");
            Keyboard.Press(VirtualKeyShort.TAB);
            Name("claude-week", "Claude Tab");
            Assert.True(Wait(NoBox), "Tab left the rename box open");

            stage = "click outside saves";
            Name("claude-week", "Claude Tab").Click();
            Box("claude-week");
            Keyboard.Type("Claude Home");
            var codex = Card("codex-week").BoundingRectangle;
            // The card's top padding, clear of the name, the icons and the pill.
            var empty = new System.Drawing.Point(codex.Left + codex.Width / 2, codex.Top + 4);
            DesktopTestEnvironment.RequireOwnedPoint(app.ProcessId, empty);
            Mouse.Click(empty);
            Name("claude-week", "Claude Home");
            Assert.True(Wait(NoBox), "A click outside left the rename box open");

            stage = "another name saves the first";
            Name("claude-week", "Claude Home").Click();
            Box("claude-week");
            Keyboard.Type("First");
            Name("codex-week", "Codex Pro").Click();
            Name("claude-week", "First");
            Assert.Equal("Codex Pro", Box("codex-week").Text);
            Assert.Null(Card("claude-week").FindFirstDescendant(cf => cf.ByName("Account name").And(cf.ByControlType(ControlType.Edit))));

            stage = "context menu keeps the rename";
            // Focus moves into the box's own menu; that is not leaving the box.
            Box("codex-week").RightClick();
            Thread.Sleep(800);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Assert.Equal("Codex Pro", Box("codex-week").Text);

            stage = "Esc cancels";
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Assert.True(Wait(NoBox), "Esc left the rename box open");
            Name("codex-week", "Codex Pro");

            DesktopTestEnvironment.RequireUnlockedDesktop();
            using (var capture = Main().Capture()) capture.Save(Path.Combine(evidence!, "rename.png"), System.Drawing.Imaging.ImageFormat.Png);

            stage = "exit";
            Focus(Main());
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000), "Exit did not terminate the launched process");
            Assert.Equal(0, process.ExitCode);
            passed = true;
        }
        finally
        {
            File.WriteAllText(Path.Combine(evidence!, "rename-smoke.json"), JsonSerializer.Serialize(new { passed, stage, exited = process.HasExited }));
            if (!process.HasExited)
            {
                try
                {
                    if (OwnedWindow(automation, app.ProcessId) is { } failed)
                        using (var capture = failed.Capture()) capture.Save(Path.Combine(evidence!, "rename-failure.png"), System.Drawing.Imaging.ImageFormat.Png);
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

    /// <summary>
    /// FlaUI places the cursor with SetCursorPos, which WinUI does not take as pointer movement. SendInput moves it from a
    /// few pixels to the left onto the point, so the element there sees the pointer enter.
    /// </summary>
    private static void Hover(System.Drawing.Point point)
    {
        Input[] moves = [new(point.X - 6, point.Y), new(point.X, point.Y)];
        Assert.Equal((uint)moves.Length, SendInput((uint)moves.Length, moves, System.Runtime.InteropServices.Marshal.SizeOf<Input>()));
        Thread.Sleep(100);
    }

    // INPUT with an absolute MOUSEINPUT move over the virtual desktop (MOVE | ABSOLUTE | VIRTUALDESK).
    private struct Input(int x, int y)
    {
        public uint Type = 0;
        public MouseInput Mouse = new(x, y);
    }

    private struct MouseInput(int x, int y)
    {
        public int Dx = (int)Math.Round((x - GetSystemMetrics(76)) * 65535.0 / (GetSystemMetrics(78) - 1));
        public int Dy = (int)Math.Round((y - GetSystemMetrics(77)) * 65535.0 / (GetSystemMetrics(79) - 1));
        public uint Data = 0;
        public uint Flags = 0x0001 | 0x8000 | 0x4000;
        public uint Time = 0;
        public IntPtr ExtraInfo = IntPtr.Zero;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
