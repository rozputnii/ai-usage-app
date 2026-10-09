using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class AuditWindows
{
    [Theory(Explicit = true)]
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
        // The flyout is a pointer-only miniature (D-204): no element of its content is a tab stop or holds focus.
        var elements = popup.FindAllDescendants();
        Assert.Contains(elements, e => (e.Properties.Name.ValueOrDefault ?? "").Contains("SYNTHETIC", StringComparison.Ordinal));
        Assert.DoesNotContain(elements, e => e.Properties.IsKeyboardFocusable.ValueOrDefault || e.Properties.HasKeyboardFocus.ValueOrDefault);
        // A click on a row, not Enter, selects the account and opens the main window.
        session.Click(elements.First(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Claude", StringComparison.Ordinal)));
        Assert.True(Session.Wait(() => session.IsVisible)); session.RequireForeground();
        session.HideToTray(); session.TrayIconClick(false);
        popup = session.Find(e => e.Properties.Name.ValueOrDefault == "AI Usage tray" && session.SurfaceIsVisible(e));
        session.Key(VirtualKeyShort.ESCAPE);
        Assert.True(Session.Wait(() => !session.SurfaceIsVisible(popup)));
        session.TrayIconClick(false);
        popup = session.Find(e => e.Properties.Name.ValueOrDefault == "AI Usage tray" && session.SurfaceIsVisible(e));
        var firstRow = popup.FindAllDescendants().First(e => (e.Properties.Name.ValueOrDefault ?? "").Contains("Claude", StringComparison.Ordinal));
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
        public bool IsVisible => IsWindowVisible(GetAncestor(windowHandle, 2)); // UIA may expose an unshown WinUI child bridge.
        public bool SurfaceIsVisible(AutomationElement surface)
        {
            RequireOwnedElement(surface);
            for (AutomationElement? parent = surface; parent is not null; parent = parent.Parent)
            {
                var handle = parent.Properties.NativeWindowHandle.ValueOrDefault;
                if (handle != IntPtr.Zero) return IsWindowVisible(GetAncestor(handle, 2));
            }
            throw new InvalidOperationException("An owned tray surface needs a native window");
        }
        public void RequireForeground() => Assert.True(Wait(() => GetAncestor(GetForegroundWindow(), 2) == windowHandle));
        public void HideToTray()
        {
            ClickCaption("Close");
            Assert.True(Wait(() => !IsVisible));
            Assert.False(process.HasExited);
        }
        public void ClickCaption(string name)
        {
            DesktopTestEnvironment.RequireOwnedForeground(app.ProcessId);
            AutomationElement? button = null;
            Assert.True(Wait(() => (button = Window.FindAllDescendants().FirstOrDefault(e => e.Properties.Name.ValueOrDefault == name &&
                e.Properties.ControlType.ValueOrDefault == ControlType.Button && e.BoundingRectangle.Width > 0 && e.BoundingRectangle.Height > 0)) is not null),
                "The restored main window must expose its native caption button before a click");
            Assert.NotNull(button);
            Assert.True(button.Properties.IsEnabled.ValueOrDefault);
            var point = button.GetClickablePoint();
            RequireMouseTarget(point); Mouse.MoveTo(point); RequireMouseTarget(point); Mouse.Click();
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
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
                !e.Properties.IsOffscreen.ValueOrDefault && e.BoundingRectangle.Width > 0 && e.BoundingRectangle.Height > 0);
            var icon = Icon(taskbar);
            if (icon is null)
            {
                AutomationElement? toggle = null;
                bool open = false;
                // Observe the toggle and overflow together. The closing animation can leave the
                // old label/rectangle briefly present after a menu or owned window takes focus.
                var settled = Stopwatch.StartNew();
                bool? previousOpen = null;
                Assert.True(Wait(() =>
                {
                    var roots = automation.GetDesktop().FindAllChildren();
                    var currentTaskbar = roots.First(e => e.Properties.ClassName.ValueOrDefault == "Shell_TrayWnd");
                    toggle = currentTaskbar.FindAllDescendants().FirstOrDefault(e =>
                        (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Show Hidden Icons", StringComparison.Ordinal));
                    if (toggle is null) return false;
                    var overflow = roots.FirstOrDefault(e => e.Properties.ClassName.ValueOrDefault == "TopLevelWindowForOverflowXamlIsland" &&
                        IsWindowVisible(e.Properties.NativeWindowHandle.ValueOrDefault));
                    open = (toggle.Properties.Name.ValueOrDefault ?? "").EndsWith(" Hide", StringComparison.Ordinal);
                    var ready = open ? overflow is not null && Icon(overflow) is not null : overflow is null;
                    if (!ready || previousOpen != open) { previousOpen = ready ? open : null; settled.Restart(); return false; }
                    return settled.ElapsedMilliseconds >= 300;
                }), "The shell overflow must settle into an observed open or closed state");
                Assert.NotNull(toggle);
                if (!open)
                {
                    ShellClick(toggle, (int)shellId, false);
                }
                Assert.True(Wait(() =>
                {
                    var current = automation.GetDesktop().FindAllChildren().FirstOrDefault(e =>
                        e.Properties.ClassName.ValueOrDefault == "TopLevelWindowForOverflowXamlIsland" &&
                        IsWindowVisible(e.Properties.NativeWindowHandle.ValueOrDefault));
                    return current is not null && (icon = Icon(current)) is not null;
                }), "The opened shell overflow must finish rendering the synthetic app icon");
            }
            Assert.NotNull(icon);
            ShellClick(icon, (int)shellId, right);
            Assert.True(Wait(OwnsForeground), "The clicked tray icon must open a surface owned by this audit app");
        }
        private int shellStep;
        private void ShellClick(AutomationElement element, int shellId, bool right)
        {
            DesktopTestEnvironment.RequireUnlockedDesktop();
            var name = element.Properties.Name.Value;
            RecordShell("before");
            var point = FreshShellPoint(name);
            DesktopTestEnvironment.RequireOwnedPoint(shellId, point); Mouse.MoveTo(point);
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
            Thread.Sleep(150); // Let the guest shell finish its ordinary overflow/hover layout.
            RecordShell("hover");
            // Overflow layout can replace UIA elements. Rebind both before pointer movement
            // and before input; retain only a point from the freshly observed positive bounds.
            point = FreshShellPoint(name);
            DesktopTestEnvironment.RequireOwnedPoint(shellId, point); Mouse.MoveTo(point);
            DesktopTestEnvironment.RequireOwnedPoint(shellId, point);
            if (right) Mouse.RightClick(); else Mouse.Click();
            FlaUI.Core.Input.Wait.UntilInputIsProcessed();
            RecordShell("clicked");
        }
        private System.Drawing.Point FreshShellPoint(string name)
        {
            System.Drawing.Point point = default;
            System.Drawing.Point? previous = null;
            var settled = Stopwatch.StartNew();
            Assert.True(Wait(() =>
            {
                var button = automation.GetDesktop().FindAllChildren().Where(e =>
                    e.Properties.ClassName.ValueOrDefault is "Shell_TrayWnd" or "TopLevelWindowForOverflowXamlIsland" &&
                    IsWindowVisible(e.Properties.NativeWindowHandle.ValueOrDefault))
                    .SelectMany(e => e.FindAllDescendants()).FirstOrDefault(e =>
                        (e.Properties.Name.ValueOrDefault == name || name.StartsWith("Show Hidden Icons", StringComparison.Ordinal) &&
                            (e.Properties.Name.ValueOrDefault ?? "").StartsWith("Show Hidden Icons", StringComparison.Ordinal)) &&
                        e.Properties.ControlType.ValueOrDefault == ControlType.Button &&
                        !e.Properties.IsOffscreen.ValueOrDefault && e.BoundingRectangle.Width > 0 && e.BoundingRectangle.Height > 0);
                if (button is null) { previous = null; settled.Restart(); return false; }
                var bounds = button.BoundingRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0) return false;
                point = new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                if (point != previous) { previous = point; settled.Restart(); return false; }
                return settled.ElapsedMilliseconds >= 200;
            }), "The shell click needs a freshly observed visible button");
            return point;
        }
        private void RecordShell(string stage)
        {
            Assert.Equal("WDAGUtilityAccount", Environment.UserName);
            var surfaces = automation.GetDesktop().FindAllChildren().Where(e =>
                e.Properties.ClassName.ValueOrDefault is "Shell_TrayWnd" or "TopLevelWindowForOverflowXamlIsland");
            var foreground = GetForegroundWindow();
            _ = GetWindowThreadProcessId(foreground, out var foregroundOwner);
            File.WriteAllText(Path.Combine(Evidence, "shell-" + Path.GetFileNameWithoutExtension(Input) + "-" + shellStep++ + "-" + stage + ".json"),
                JsonSerializer.Serialize(new { Foreground = foreground.ToInt64(), ForegroundOwner = foregroundOwner,
                    AppProcessId = app.ProcessId, MainWindow = windowHandle.ToInt64(), MainVisible = IsVisible,
                    Surfaces = surfaces.Select(e => new {
                    Class = e.Properties.ClassName.ValueOrDefault, Bounds = e.BoundingRectangle,
                    Visible = IsWindowVisible(e.Properties.NativeWindowHandle.ValueOrDefault),
                    Children = e.FindAllDescendants().Take(150).Select(child => new {
                        Name = child.Properties.Name.ValueOrDefault, Type = child.Properties.ControlType.ValueOrDefault.ToString(),
                        Bounds = child.BoundingRectangle, Offscreen = child.Properties.IsOffscreen.ValueOrDefault }) }).ToArray() }));
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
        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr handle, uint flags);
    }
}
