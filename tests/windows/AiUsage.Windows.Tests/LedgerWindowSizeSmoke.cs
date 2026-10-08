using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class LedgerSmoke
{
    /// <summary>
    /// A window squeezed below its minimum keeps the title controls clear of each other and of the caption buttons, and
    /// grows again when Work today widens the title row.
    /// </summary>
    [Fact]
    public void SqueezedWindowKeepsTheTitleRowClearOfTheCaptionButtons()
    {
        DesktopTestEnvironment.RequireUnlockedDesktop();
        var exe = Environment.GetEnvironmentVariable("AIU_SMOKE_EXE");
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(exe)); Assert.False(string.IsNullOrWhiteSpace(evidence));
        Directory.CreateDirectory(evidence!);
        var start = new ProcessStartInfo(exe!, "--demo") { UseShellExecute = false };
        start.Environment["AIU_DEVELOPMENT_STATE_DIRECTORY"] = Path.Combine(Path.GetTempPath(), "aiu-size-smoke-" + Guid.NewGuid().ToString("N"));
        using var app = Application.Launch(start);
        using var process = Process.GetProcessById(app.ProcessId);
        using var automation = new UIA3Automation();
        var passed = false;
        var stage = "launch";
        var launched = System.Drawing.Rectangle.Empty;
        Window Main()
        {
            Window? current = null;
            Assert.True(Wait(() => (current = OwnedWindow(automation, app.ProcessId)) is not null), "Ledger window is not available");
            return current!;
        }
        AutomationElement Button(string name)
        {
            AutomationElement? button = null;
            Assert.True(Wait(() => (button = Main().FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.Button)))) is not null), name + " is missing");
            return button!;
        }
        AutomationElement Prefixed(string prefix)
        {
            AutomationElement? button = null;
            Assert.True(Wait(() => (button = Main().FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                .FirstOrDefault(b => (b.Properties.Name.ValueOrDefault ?? "").StartsWith(prefix, StringComparison.Ordinal))) is not null), prefix + " is missing");
            return button!;
        }
        void Squeeze(string shot, bool narrower = true)
        {
            var handle = Main().Properties.NativeWindowHandle.Value;
            Assert.True(SetWindowPos(handle, IntPtr.Zero, 0, 0, 200, 120, 0x0002 | 0x0004), "SetWindowPos failed");
            Thread.Sleep(500);
            var bounds = Main().BoundingRectangle;
            Assert.True((!narrower || bounds.Width < launched.Width) && bounds.Height < launched.Height, $"The window did not shrink: {bounds} from {launched}");
            Assert.True(bounds.Height > 120, $"No minimum height: {bounds}");
            AssertClear(shot);
        }
        void AssertClear(string shot)
        {
            var main = Main();
            using (var capture = main.Capture()) capture.Save(Path.Combine(evidence!, shot + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            // UIA reports the caption buttons without bounds here; DWM gives them relative to the window rectangle.
            var handle = main.Properties.NativeWindowHandle.Value;
            Assert.Equal(0, DwmGetWindowAttribute(handle, 5, out var buttons, 16));
            Assert.True(GetWindowRect(handle, out var frame));
            var captions = frame.Left + buttons.Left;
            var settings = Button("Settings").BoundingRectangle;
            Assert.True(settings.Right <= captions, $"Settings {settings} overlaps the caption buttons from {captions}");
            var refresh = Prefixed("Refresh. ").BoundingRectangle;
            var used = Button("Show values: used").BoundingRectangle;
            Assert.True(refresh.Right <= used.Left, $"Refresh {refresh} overlaps Used {used}");
        }
        try
        {
            Assert.True(Wait(() => Main().BoundingRectangle.Width > 0));
            launched = Main().BoundingRectangle;
            stage = "brief";
            Squeeze("size-brief");
            stage = "day off";
            Button("Demo scenarios").AsButton().Invoke();
            AutomationElement? scenario = null;
            Assert.True(Wait(() =>
            {
                scenario = automation.GetDesktop().FindAllChildren().Where(w =>
                    GetWindowThreadProcessId(w.Properties.NativeWindowHandle.ValueOrDefault, out var owner) != 0 && owner == app.ProcessId)
                    .Select(w => w.FindFirstDescendant(cf => cf.ByName("Day off · Sat 17 Oct"))).FirstOrDefault(e => e is not null);
                return scenario is not null;
            }));
            scenario!.AsMenuItem().Invoke();
            Squeeze("size-day-off");
            stage = "work today";
            Assert.True(Button("Work today, off").BoundingRectangle.Right <= Button("Show values: used").BoundingRectangle.Left, "Work today overlaps Used");
            Button("Work today, off").AsButton().Invoke();
            Assert.True(Wait(() => Main().FindFirstDescendant(cf => cf.ByName("Work today, on until midnight")) is not null), "Work today did not turn on");
            Thread.Sleep(500);
            AssertClear("size-work-today");
            // Work today and the demo button make this title row wider than the launch width.
            Squeeze("size-work-today-squeezed", narrower: false);
            passed = true;
        }
        finally
        {
            File.WriteAllText(Path.Combine(evidence!, "size.json"), JsonSerializer.Serialize(new { passed, stage, pid = app.ProcessId }));
            if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); }
        }
    }

    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out NativeRect value, int size);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
