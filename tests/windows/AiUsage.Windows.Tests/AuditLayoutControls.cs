using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    [Fact]
    public void OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        using var session = new Session(Path.Combine(Required("AIU_AUDIT_PAGE_DIRECTORY"), "overview.json"));
        var fixture = OverviewFixture();
        var moneyAccount = fixture["Accounts"]!.AsArray().Single(a => a!["Provider"]!.GetValue<string>() == "claude")!["AccountId"]!.GetValue<string>().Replace("-", "", StringComparison.Ordinal);
        var id = fixture["ExpectedStates"]!.AsObject().Single(p => p.Key.StartsWith(moneyAccount + ":", StringComparison.Ordinal)).Key;
        var initial = session.Window.BoundingRectangle;
        session.ClickCaption("Maximize");
        Assert.True(Session.Wait(() => session.Window.BoundingRectangle.Width > initial.Width));
        session.Click("Add account");
        session.Capture("LIFE-04-maximized-provider-menu");
        session.Key(VirtualKeyShort.ESCAPE);
        Assert.False(Visible(session, "Sign in to Claude"));
        session.ClickCaption("Restore");
        Assert.True(Session.Wait(() => Math.Abs(session.Window.BoundingRectangle.Width - initial.Width) < 5));
        session.Focus(session.ById(id)); session.Key(VirtualKeyShort.RETURN);
        var history = session.Find(e => (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        session.Click("Settings");
        session.ShrinkWithMouse(70, 80);
        Assert.True(session.Window.BoundingRectangle.Width < initial.Width - 40);
        Assert.True(session.Window.BoundingRectangle.Height < initial.Height - 40);
        session.Capture("LIFE-04-resized-settings-history");
        session.Menu("Preview diagnostics");
        var preview = session.Find(e => e.Properties.Name.ValueOrDefault == "SYNTHETIC AUDIT · provider transport disabled · isolated temporary storage");
        session.Show(preview); session.Capture("LIFE-04-resized-diagnostic-preview");
        Assert.Equal(1, session.Receipts.Split('\n').Count(l => l.Trim() == "PreviewDiagnostics"));
        session.Show(session.ById(id)); session.Focus(session.ById(id)); session.Key(VirtualKeyShort.KEY_C);
        session.Type(CapInput(session), "260.00");
        session.Capture("LIFE-04-resized-cap-editor");
        session.Key(VirtualKeyShort.ESCAPE);
        Assert.DoesNotContain(session.Window.FindAllDescendants(), e => e.Properties.Name.ValueOrDefault == "Save" && !e.Properties.IsOffscreen.ValueOrDefault);
        Assert.Empty(JsonNode.Parse(File.ReadAllText(Path.Combine(session.Root, "budget", "configuration.v1.json")))!["Caps"]!.AsArray());
        // The settings panel is still open: its header was scrolled out of view by the diagnostic preview above,
        // so bring "Close settings" into view (a closed, collapsed panel has no such element) before requiring it visible.
        void RequireSettingsOpen()
        {
            session.Show(session.Find(e => e.Properties.Name.ValueOrDefault == "Close settings"));
            Assert.True(Visible(session, "Close settings"));
        }
        RequireSettingsOpen();
        Assert.Contains(session.Window.FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        session.Focus(session.ById(id)); session.Key(VirtualKeyShort.ESCAPE);
        Assert.DoesNotContain(session.Window.FindAllDescendants(), e => (e.Properties.Name.ValueOrDefault ?? "").Contains(" history,", StringComparison.Ordinal));
        RequireSettingsOpen();
        session.Key(VirtualKeyShort.ESCAPE); Assert.False(Visible(session, "Close settings"));
        session.Click("Add account"); session.Capture("LIFE-04-resized-provider-menu");
        session.Click("Show signed-out accounts"); session.Key(VirtualKeyShort.ESCAPE);
        RequirePreference(session, p => p["Preferences"]!["ShowSignedOut"]!.GetValue<bool>());
        session.Click("Show values: left"); session.Click("Show values: used");
        session.Key(VirtualKeyShort.F5);
        var accountIds = fixture["Accounts"]!.AsArray().Select(a => a!["AccountId"]!.GetValue<string>().Replace("-", "", StringComparison.Ordinal)).ToArray();
        Assert.True(Session.Wait(() => accountIds.All(a => session.Receipts.Split('\n').Count(l => l.Trim() == "Refresh:" + a) == 1)));
        session.Capture("LIFE-04-resized-after-refresh"); session.Exit();
    }

    private sealed partial class Session
    {
        public void ShrinkWithMouse(int width, int height)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            var bounds = Window.BoundingRectangle;
            System.Drawing.Point? start = null;
            foreach (var inset in new[] { 2, 3, 4, 5, 6 })
            {
                var point = new System.Drawing.Point(bounds.Right - inset, bounds.Bottom - inset);
                var packed = new IntPtr((point.Y << 16) | (point.X & 0xffff));
                if (SendMessage(windowHandle, 0x84, IntPtr.Zero, packed).ToInt64() == 17) { start = point; break; }
            }
            Assert.NotNull(start);
            RequireMouseTarget(start.Value); Mouse.MoveTo(start.Value); RequireMouseTarget(start.Value);
            Mouse.Down(MouseButton.Left);
            try
            {
                for (var step = 1; step <= 10; step++)
                {
                    var target = new System.Drawing.Point(start.Value.X - width * step / 10, start.Value.Y - height * step / 10);
                    RequireMouseTarget(target); Mouse.MoveTo(target); RequireMouseTarget(target);
                    Thread.Sleep(40);
                }
            }
            finally { Mouse.Up(MouseButton.Left); }
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
        }
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr parameter, IntPtr coordinates);
    }
}
