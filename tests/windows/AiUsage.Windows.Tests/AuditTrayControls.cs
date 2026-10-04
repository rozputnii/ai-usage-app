using System.Diagnostics;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void NativeTrayClicksRestoreRefreshAndDrainExit(bool hidden, bool inFlight)
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        Assert.Equal("WDAGUtilityAccount", Environment.UserName); // Explorer input is restricted to the disposable guest.
        var fixture = OverviewFixture();
        fixture["BlockRefresh"] = inFlight;
        fixture["ManualCode"] = inFlight;
        using var session = Session.FromFixture(fixture, "tray-" + hidden + "-" + inFlight);
        if (hidden) session.HideToTray();
        session.TrayMenu("Open AI Usage");
        Assert.True(Session.Wait(() => session.IsVisible));
        session.RequireForeground();
        session.Capture("tray-open-" + hidden + "-" + inFlight);
        session.HideToTray();
        session.TrayIconClick(false);
        var popup = session.Find(e => e.Properties.Name.ValueOrDefault == "AI Usage tray");
        session.CaptureSurface(popup, "tray-miniature-" + hidden + "-" + inFlight);
        var rows = popup.FindAllDescendants().Where(e => e.Properties.IsKeyboardFocusable.ValueOrDefault &&
            (e.Properties.Name.ValueOrDefault ?? "").Contains("SYNTHETIC", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(rows);
        var focusedRow = rows.Single(e => e.Properties.HasKeyboardFocus.ValueOrDefault);
        session.Key(VirtualKeyShort.DOWN);
        Assert.True(Session.Wait(() => !focusedRow.Properties.HasKeyboardFocus.ValueOrDefault && rows.Any(e =>
            e.Properties.HasKeyboardFocus.ValueOrDefault && (e.Properties.Name.ValueOrDefault ?? "").Contains("Codex", StringComparison.Ordinal))));
        session.Key(VirtualKeyShort.UP);
        Assert.True(Session.Wait(() => focusedRow.Properties.HasKeyboardFocus.ValueOrDefault));
        session.Key(VirtualKeyShort.RIGHT);
        Assert.True(Session.Wait(() => !focusedRow.Properties.HasKeyboardFocus.ValueOrDefault && focusedRow.FindAllDescendants().Any(e =>
            e.Properties.IsKeyboardFocusable.ValueOrDefault && e.Properties.HasKeyboardFocus.ValueOrDefault)));
        session.Key(VirtualKeyShort.LEFT);
        Assert.False(focusedRow.Properties.HasKeyboardFocus.ValueOrDefault); // One strip clamps at its first position.
        session.Key(VirtualKeyShort.RETURN);
        Assert.True(Session.Wait(() => session.IsVisible)); session.RequireForeground();
        session.HideToTray(); session.TrayIconClick(false);
        popup = session.Find(e => e.Properties.Name.ValueOrDefault == "AI Usage tray" && !e.Properties.IsOffscreen.ValueOrDefault);
        session.Key(VirtualKeyShort.ESCAPE);
        Assert.True(Session.Wait(() => popup.Properties.IsOffscreen.ValueOrDefault));
        session.TrayIconClick(false);
        popup = session.Find(e => e.Properties.Name.ValueOrDefault == "AI Usage tray" && !e.Properties.IsOffscreen.ValueOrDefault);
        var firstRow = popup.FindAllDescendants().First(e => e.Properties.IsKeyboardFocusable.ValueOrDefault &&
            (e.Properties.Name.ValueOrDefault ?? "").Contains("Claude", StringComparison.Ordinal));
        session.Click(firstRow);
        Assert.True(Session.Wait(() => session.IsVisible)); session.RequireForeground();
        Assert.Contains(session.Window.FindAllDescendants(), e => e.Properties.HasKeyboardFocus.ValueOrDefault &&
            (e.Properties.AutomationId.ValueOrDefault ?? "").StartsWith(fixture["Accounts"]![0]!["AccountId"]!.GetValue<string>().Replace("-", "", StringComparison.Ordinal), StringComparison.Ordinal));
        if (hidden) session.HideToTray();
        session.TrayMenu("Refresh");
        var ids = fixture["Accounts"]!.AsArray().Select(a => a!["AccountId"]!.GetValue<string>().Replace("-", "", StringComparison.Ordinal)).ToArray();
        Assert.True(Session.Wait(() => ids.All(id => session.Receipts.Split('\n').Count(l => l.Trim() == "Refresh:" + id) == 1)));
        if (inFlight)
        {
            session.TrayMenu("Open AI Usage"); session.RequireForeground();
            session.Click("Add account"); session.Click("Sign in to Claude");
            Assert.True(Session.Wait(() => session.Receipts.Contains("BrowserRequested:synthetic", StringComparison.Ordinal)));
            Assert.NotNull(session.Find(e => e.Properties.Name.ValueOrDefault == "Cancel"));
            if (hidden) session.HideToTray();
        }
        session.TrayMenu("Exit");
        session.AssertExited();
        if (inFlight) foreach (var id in ids) Assert.Contains("RefreshCancelled:" + id, session.Receipts, StringComparison.Ordinal);
    }

    private sealed partial class Session
    {
        public bool IsVisible => IsWindowVisible(windowHandle);
        public void RequireForeground() => Assert.True(Wait(OwnsForeground));
        public void HideToTray()
        {
            Click(Find(e => e.Properties.Name.ValueOrDefault == "Close" && !e.Properties.IsOffscreen.ValueOrDefault &&
                e.Properties.ControlType.ValueOrDefault == ControlType.Button));
            Assert.True(Wait(() => !IsVisible));
            Assert.False(process.HasExited);
        }
        public void AssertExited()
        {
            Assert.True(process.WaitForExit(10000)); Assert.Equal(0, process.ExitCode);
            Assert.Contains("Stopped", Receipts, StringComparison.Ordinal);
        }
        public void TrayMenu(string name)
        {
            TrayIconClick(true);
            var menu = Find(e => e.Properties.Name.ValueOrDefault == name &&
                e.Properties.ControlType.ValueOrDefault == ControlType.MenuItem && !e.Properties.IsOffscreen.ValueOrDefault);
            Click(menu);
        }
        public void TrayIconClick(bool right)
        {
            Assert.Equal("WDAGUtilityAccount", Environment.UserName);
            var desktop = automation.GetDesktop();
            var taskbar = desktop.FindFirstChild(cf => cf.ByClassName("Shell_TrayWnd"));
            Assert.NotNull(taskbar);
            var handle = taskbar.Properties.NativeWindowHandle.Value;
            _ = GetWindowThreadProcessId(handle, out var shellId);
            using var explorer = Process.GetProcessById((int)shellId);
            Assert.Equal("explorer", explorer.ProcessName, ignoreCase: true);
            AutomationElement? Icon(AutomationElement parent) => parent.FindAllDescendants().FirstOrDefault(e =>
                e.Properties.Name.ValueOrDefault == "AI Usage" && e.Properties.ControlType.ValueOrDefault == ControlType.Button &&
                !e.Properties.IsOffscreen.ValueOrDefault);
            var icon = Icon(taskbar);
            if (icon is null)
            {
                var toggle = taskbar.FindAllDescendants().FirstOrDefault(e =>
                    (e.Properties.Name.ValueOrDefault ?? "").Contains("Hidden Icons", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(toggle);
                ShellClick(toggle, (int)shellId, false);
                AutomationElement? overflow = null;
                Assert.True(Wait(() => (overflow = desktop.FindFirstChild(cf => cf.ByClassName("TopLevelWindowForOverflowXamlIsland"))) is not null));
                icon = Icon(overflow!);
            }
            Assert.NotNull(icon);
            ShellClick(icon, (int)shellId, right);
            Assert.True(Wait(OwnsForeground), "The clicked tray icon must open a surface owned by this audit app");
        }
        private static void ShellClick(AutomationElement element, int shellId, bool right)
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            var point = element.GetClickablePoint();
            DesktopTestEnvironment.RequireOwnedPoint(shellId, point); Mouse.MoveTo(point);
            DesktopTestEnvironment.RequireOwnedPoint(shellId, point);
            if (right) Mouse.RightClick(); else Mouse.Click();
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
        }
        public void CaptureSurface(AutomationElement surface, string name)
        {
            RequireOwnedElement(surface); DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            var bounds = surface.BoundingRectangle;
            for (var y = bounds.Top + 4; y < bounds.Bottom - 4; y += 24)
                for (var x = bounds.Left + 4; x < bounds.Right - 4; x += 24)
                    DesktopTestEnvironment.RequireOwnedPoint(app.ProcessId, new(x, y));
            using var capture = surface.Capture();
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            capture.Save(Path.Combine(Evidence, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr handle);
    }
}
