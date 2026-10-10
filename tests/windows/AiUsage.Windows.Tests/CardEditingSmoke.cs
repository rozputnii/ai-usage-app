using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace AiUsage.Windows.Tests;

/// <summary>Editing account cards with the mouse in the demo app (T-055); isolated synthetic state, no sign-in.</summary>
public sealed partial class CardEditingSmoke
{
    /// <summary>R-09: a click on the name starts the rename; Enter, a click elsewhere and a click on another name save; Esc cancels.</summary>
    [Fact]
    public void ClickingTheNameRenamesTheAccount()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var evidence = EvidenceDirectory();
        using var app = new DemoApp("aiu-rename-smoke-");
        var passed = false;
        var stage = "launch";
        try
        {
            SmokeKit.Focus(app.Main());
            stage = "hover";
            // The hovered name shows the dotted underline and the Rename tooltip (Codex: the Refresh tooltip covers Claude at launch).
            SmokeKit.HoverOwned(app.Name("codex-week", "Codex Pro").GetClickablePoint(), app.ProcessId);
            Assert.True(Wait(app.ShowsToolTip, TimeSpan.FromSeconds(5)), "Hovering the name opened no tooltip");
            Save(app, evidence, "rename-hover.png");

            stage = "click, type, Enter";
            SmokeKit.ClickOwned(app.Name("claude-week", "Claude Pro"), app.ProcessId);
            Assert.Equal("Claude Pro", app.Box("claude-week").Text);
            Keyboard.Type("Claude Work");
            Keyboard.Press(VirtualKeyShort.RETURN);
            app.Name("claude-week", "Claude Work");
            Assert.True(Wait(app.NoBox), "Enter left the rename box open");

            stage = "focus leaving saves";
            SmokeKit.ClickOwned(app.Name("claude-week", "Claude Work"), app.ProcessId);
            app.Box("claude-week");
            Keyboard.Type("Claude Tab");
            Keyboard.Press(VirtualKeyShort.TAB);
            app.Name("claude-week", "Claude Tab");
            Assert.True(Wait(app.NoBox), "Tab left the rename box open");

            stage = "click outside saves";
            SmokeKit.ClickOwned(app.Name("claude-week", "Claude Tab"), app.ProcessId);
            app.Box("claude-week");
            Keyboard.Type("Claude Home");
            var codex = app.Card("codex-week").BoundingRectangle;
            // The card's top padding, clear of the name, the icons and the pill.
            SmokeKit.ClickOwned(new System.Drawing.Point(codex.Left + codex.Width / 2, codex.Top + 4), app.ProcessId);
            app.Name("claude-week", "Claude Home");
            Assert.True(Wait(app.NoBox), "A click outside left the rename box open");

            stage = "another name saves the first";
            SmokeKit.ClickOwned(app.Name("claude-week", "Claude Home"), app.ProcessId);
            app.Box("claude-week");
            Keyboard.Type("First");
            SmokeKit.ClickOwned(app.Name("codex-week", "Codex Pro"), app.ProcessId);
            app.Name("claude-week", "First");
            Assert.Equal("Codex Pro", app.Box("codex-week").Text);
            Assert.Null(app.Card("claude-week").FindFirstDescendant(cf => cf.ByAutomationId("RenameBox").And(cf.ByControlType(ControlType.Edit))));

            stage = "context menu keeps the rename";
            // Focus moves into the box's own menu; that is not leaving the box.
            SmokeKit.RightClickOwned(app.Box("codex-week"), app.ProcessId);
            Assert.True(Wait(app.ShowsMenu, TimeSpan.FromSeconds(5)), "The rename box opened no context menu");
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Assert.Equal("Codex Pro", app.Box("codex-week").Text);

            stage = "Esc cancels";
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Assert.True(Wait(app.NoBox), "Esc left the rename box open");
            app.Name("codex-week", "Codex Pro");
            Save(app, evidence, "rename.png");

            stage = "exit";
            app.Exit();
            passed = true;
        }
        finally
        {
            app.Finish(evidence, "rename", passed, stage);
        }
    }

    /// <summary>R-10: dragging a card's grip moves the account; an open rename is saved first, and Esc cancels a drag.</summary>
    [Fact]
    public void DraggingTheGripReordersAccounts()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var evidence = EvidenceDirectory();
        using var app = new DemoApp("aiu-drag-smoke-");
        var passed = false;
        var stage = "launch";
        var held = false;
        // The account names in the order the cards show them: tree order, since a card scrolled out of view has no bounds.
        string[] Order()
        {
            string[] names = ["Claude Pro", "Codex Pro", "Codex Work", "Copilot Free", "Antigravity AI Plus"];
            return [.. app.Main().FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                .Select(text => text.Properties.Name.ValueOrDefault).OfType<string>().Where(names.Contains)];
        }
        try
        {
            SmokeKit.Focus(app.Main());
            Assert.True(Wait(() => Order() is ["Claude Pro", "Codex Pro", ..]), "Unexpected demo order: " + string.Join(", ", Order()));

            stage = "a drag saves the open rename";
            SmokeKit.ClickOwned(app.Name("codex-week", "Codex Pro"), app.ProcessId);
            app.Box("codex-week");
            Keyboard.Type("Codex Work");
            var grip = PointAtGrip(app, "claude-week", "Reorder Claude Pro");
            Mouse.Down(MouseButton.Left);
            held = true;
            // A few pixels start the drag.
            var started = new System.Drawing.Point(grip.X, grip.Y + 10);
            MoveInSteps(grip, started, 3);
            app.Name("codex-week", "Codex Work");
            Assert.True(Wait(app.NoBox), "The drag left the rename box open");

            stage = "drag below Codex";
            // Past the middle of the Codex card, read again now that its rename hint is gone.
            var codex = app.Card("codex-week").BoundingRectangle;
            var below = new System.Drawing.Point(grip.X, codex.Top + codex.Height / 2 + 20);
            MoveInSteps(started, below);
            Thread.Sleep(300);
            Save(app, evidence, "drag.png");
            Mouse.Up(MouseButton.Left);
            held = false;
            Assert.True(Wait(() => Order() is ["Codex Work", "Claude Pro", ..]), "The drop did not move Claude Pro below Codex: " + string.Join(", ", Order()));

            stage = "Esc cancels a drag";
            grip = PointAtGrip(app, "claude-week", "Reorder Claude Pro");
            Mouse.Down(MouseButton.Left);
            held = true;
            codex = app.Card("codex-week").BoundingRectangle;
            var claudeTop = app.Card("claude-week").BoundingRectangle.Top;
            var above = new System.Drawing.Point(grip.X, codex.Top + codex.Height / 2 - 20);
            MoveInSteps(grip, above);
            // The card is lifted and follows the pointer before Esc puts it back.
            Assert.True(Wait(() => app.Card("claude-week").BoundingRectangle.Top < claudeTop - 20), "The second drag did not lift Claude Pro");
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Assert.True(Wait(() => app.Card("claude-week").BoundingRectangle.Top == claudeTop), "Esc did not put Claude Pro back");
            MoveInSteps(above, new System.Drawing.Point(above.X, above.Y - 10), 3);
            Mouse.Up(MouseButton.Left);
            held = false;
            Thread.Sleep(1000);
            Assert.True(Order() is ["Codex Work", "Claude Pro", ..], "A drag released after Esc changed the order: " + string.Join(", ", Order()));
            Save(app, evidence, "drag-cancelled.png");

            stage = "exit";
            app.Exit();
            passed = true;
        }
        finally
        {
            // A failed run must not leave the button held for the desktop.
            if (held)
                Mouse.Up(MouseButton.Left);
            app.Finish(evidence, "drag", passed, stage);
        }
    }

    /// <summary>Tooltips open only under the pointer: Tab through the window opens none, hovering the refresh icon opens its tooltip.</summary>
    [Fact]
    public void TooltipsOpenOnHoverOnly()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var evidence = EvidenceDirectory();
        using var app = new DemoApp("aiu-tooltip-smoke-");
        var passed = false;
        var stage = "launch";
        try
        {
            SmokeKit.Focus(app.Main());
            // The pointer rests in the Codex card's top padding, over no control.
            var codex = app.Card("codex-week").BoundingRectangle;
            var empty = new System.Drawing.Point(codex.Left + codex.Width / 2, codex.Top + 4);
            SmokeKit.HoverOwned(empty, app.ProcessId);
            Assert.True(Wait(() => !app.ShowsToolTip()), "A tooltip stayed open before Tab");

            stage = "Tab";
            for (var i = 0; i < 12; i++)
            {
                Keyboard.Press(VirtualKeyShort.TAB);
                // WinUI opens a keyboard-focus tooltip after about the hover delay; the app closes it at once, though UI
                // Automation may still list it while its popup goes.
                Thread.Sleep(1200);
                Assert.True(Wait(() => !app.ShowsToolTip(), TimeSpan.FromSeconds(1)), $"Tab {i + 1} left a tooltip open");
            }
            Save(app, evidence, "tooltip-tab.png");

            stage = "hover";
            var refresh = app.Main().FindFirstDescendant(cf => cf.ByAutomationId("RefreshButton"))!.BoundingRectangle;
            SmokeKit.HoverOwned(new System.Drawing.Point(refresh.Left + refresh.Width / 2, refresh.Top + refresh.Height / 2), app.ProcessId);
            Assert.True(Wait(app.ShowsToolTip, TimeSpan.FromSeconds(5)), "Hovering the refresh icon opened no tooltip");
            Save(app, evidence, "tooltip-hover.png");

            stage = "exit";
            SmokeKit.HoverOwned(empty, app.ProcessId);
            app.Exit();
            passed = true;
        }
        finally
        {
            app.Finish(evidence, "tooltip", passed, stage);
        }
    }

    private static string EvidenceDirectory()
    {
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence!);
        return evidence!;
    }

    private static void Save(DemoApp app, string evidence, string file)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        using var capture = app.Main().Capture();
        capture.Save(Path.Combine(evidence, file), ImageFormat.Png);
    }

    /// <summary>Points at the card so its grip shows, then onto the grip; returns the grip's point.</summary>
    private static System.Drawing.Point PointAtGrip(DemoApp app, string cardId, string gripName)
    {
        var card = app.Card(cardId).BoundingRectangle;
        SmokeKit.HoverOwned(new System.Drawing.Point(card.Left + card.Width / 2, card.Top + 4), app.ProcessId);
        AutomationElement? grip = null;
        Assert.True(Wait(() => (grip = app.Card(cardId).FindFirstDescendant(cf => cf.ByAutomationId("Grip"))) is not null), "Missing " + gripName);
        var bounds = grip!.BoundingRectangle;
        var point = new System.Drawing.Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        SmokeKit.HoverOwned(point, app.ProcessId);
        return point;
    }

    /// <summary>The demo app with isolated state. A published snapshot can replace a cached UIA element, so each query resolves afresh.</summary>
    private sealed class DemoApp : IDisposable
    {
        private readonly Application app;
        private readonly Process process;
        private readonly UIA3Automation automation = new();

        public DemoApp(string statePrefix)
        {
            var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
            Assert.False(string.IsNullOrWhiteSpace(exe));
            var start = new ProcessStartInfo(exe!, "--demo") { UseShellExecute = false };
            start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), statePrefix + Guid.NewGuid().ToString("N"));
            app = Application.Launch(start);
            process = Process.GetProcessById(app.ProcessId);
            _ = process.Handle;
        }

        public int ProcessId => app.ProcessId;

        public Window Main()
        {
            Window? current = null;
            Assert.True(Wait(() => (current = OwnedWindow(automation, app.ProcessId)) is not null), "Ledger window is not available");
            return current!;
        }

        public AutomationElement Card(string id)
        {
            AutomationElement? card = null;
            Assert.True(Wait(() => (card = Main().FindFirstDescendant(cf => cf.ByAutomationId(id))) is not null), "Missing card " + id);
            return card!;
        }

        public AutomationElement Name(string cardId, string text)
        {
            AutomationElement? name = null;
            Assert.True(Wait(() => (name = Card(cardId).FindFirstDescendant(cf => cf.ByName(text).And(cf.ByControlType(ControlType.Text)))) is not null),
                $"Card {cardId} does not show the name {text}");
            return name!;
        }

        /// <summary>The open rename box, once it holds keyboard focus with its text selected.</summary>
        public TextBox Box(string cardId)
        {
            AutomationElement? box = null;
            Assert.True(Wait(() => (box = Card(cardId).FindFirstDescendant(cf => cf.ByAutomationId("RenameBox").And(cf.ByControlType(ControlType.Edit)))) is not null &&
                box.Properties.HasKeyboardFocus.ValueOrDefault), $"No focused rename box in {cardId}");
            return box!.AsTextBox();
        }

        /// <summary>No card shows a rename box.</summary>
        public bool NoBox() => Main().FindFirstDescendant(cf => cf.ByAutomationId("RenameBox").And(cf.ByControlType(ControlType.Edit))) is null;

        /// <summary>An open tooltip, inside the window or in a popup window of the app.</summary>
        public bool ShowsToolTip() => Shows(ControlType.ToolTip);

        /// <summary>An open context menu, inside the window or in a popup window of the app.</summary>
        public bool ShowsMenu() => Shows(ControlType.Menu);

        private bool Shows(ControlType type) => SmokeKit.OwnedWindows(automation.GetDesktop(), app.ProcessId).Any(w =>
            w.Properties.ControlType.ValueOrDefault == type || w.FindFirstDescendant(cf => cf.ByControlType(type)) is not null);

        public void Exit()
        {
            SmokeKit.Focus(Main());
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000), "Exit did not terminate the launched process");
            Assert.Equal(0, process.ExitCode);
        }

        /// <summary>Records the outcome; a run that did not exit leaves a failure capture and is stopped.</summary>
        public void Finish(string evidence, string name, bool passed, string stage)
        {
            File.WriteAllText(Path.Combine(evidence, name + "-smoke.json"), JsonSerializer.Serialize(new { passed, stage, exited = process.HasExited }));
            if (process.HasExited)
                return;
            try
            {
                if (OwnedWindow(automation, app.ProcessId) is { } failed)
                    using (var capture = failed.Capture()) capture.Save(Path.Combine(evidence, name + "-failure.png"), ImageFormat.Png);
            }
            catch (System.Runtime.InteropServices.COMException) { }
            process.Kill();
            process.WaitForExit(5000);
        }

        public void Dispose()
        {
            automation.Dispose();
            process.Dispose();
            app.Dispose();
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

    /// <summary>Moves the cursor in SendInput steps, which WinUI takes as pointer movement (FlaUI's Mouse.MoveTo is not).</summary>
    private static void MoveInSteps(System.Drawing.Point from, System.Drawing.Point to, int steps = 12)
    {
        for (var i = 1; i <= steps; i++)
        {
            SmokeKit.MoveTo(new System.Drawing.Point(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps));
            Thread.Sleep(20);
        }
        Thread.Sleep(100);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
