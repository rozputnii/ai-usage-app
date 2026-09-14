using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AiUsage.Windows.Tests;

public sealed class PackageSmoke
{
    [Theory]
    [InlineData("exit")]
    [InlineData("title-bar")]
    [InlineData("repeated-exit")]
    public void InstalledDashboardTerminatesCleanly(string scenario)
    {
        var aumid = Environment.GetEnvironmentVariable("AIU_SMOKE_AUMID");
        Assert.False(string.IsNullOrWhiteSpace(aumid), "Prerequisite: AIU_SMOKE_AUMID must identify the installed package.");
        Assert.True(Environment.UserInteractive && Process.GetCurrentProcess().SessionId != 0,
            "Prerequisite: an unlocked interactive Windows desktop is required.");
        var desktop = OpenInputDesktop(0, false, 0x0100);
        Assert.NotEqual(IntPtr.Zero, desktop);
        CloseDesktop(desktop);
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY");
        Assert.False(string.IsNullOrWhiteSpace(evidence), "Prerequisite: AIU_SMOKE_EVIDENCE_DIRECTORY is required.");
        Directory.CreateDirectory(evidence!);
        using var automation = new UIA3Automation();
        File.WriteAllText(Path.Combine(evidence!, scenario + "-activation.json"),
            JsonSerializer.Serialize(new { scenario, phase = "activation-attempted", utc = DateTime.UtcNow }));
        using var app = Application.LaunchStoreApp(aumid!);
        var pid = app.ProcessId;
        using var process = Process.GetProcessById(pid);
        // Acquire the OS process handle before observing exit so PID reuse cannot change ownership.
        _ = process.Handle;
        Window? window = null;
        var passed = false;
        try
        {
            var startup = Stopwatch.StartNew();
            while (startup.Elapsed < TimeSpan.FromSeconds(15) && !process.HasExited)
            {
                window = FindProcessWindow(automation, pid);
                if (window?.FindFirstDescendant(cf => cf.ByAutomationId("EmptyState"))?.Name == "No accounts connected."
                    && (scenario != "title-bar" || window.TitleBar?.CloseButton is not null))
                    break;
                Thread.Sleep(100);
            }
            Assert.NotNull(window);
            Assert.Equal("Dashboard", window.FindFirstDescendant(cf => cf.ByAutomationId("DashboardHeading"))?.Name);
            Assert.Equal("No accounts connected.", window.FindFirstDescendant(cf => cf.ByAutomationId("EmptyState"))?.Name);
            // UIA can expose text before the compositor presents the corresponding frame.
            Thread.Sleep(500);
            using (var screenshot = window.Capture())
                screenshot.Save(Path.Combine(evidence!, scenario + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            var handle = window.Properties.NativeWindowHandle.Value;
            if (scenario == "title-bar")
            {
                Assert.NotNull(window.TitleBar);
                Assert.NotNull(window.TitleBar.CloseButton);
                window.TitleBar.CloseButton.Invoke();
            }
            else
            {
                var exit = window.FindFirstDescendant(cf => cf.ByAutomationId("ExitButton")).AsButton();
                Assert.NotNull(exit);
                exit.Focus();
                Assert.True(exit.Properties.IsKeyboardFocusable.Value);
                FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                if (scenario == "repeated-exit")
                {
                    // Overlap the keyboard Exit action with native close requests.
                    PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                }
            }
            Assert.True(process.WaitForExit(10000), "Launched PID did not terminate within ten seconds.");
            Assert.Equal(0, process.ExitCode);
            Assert.Null(FindProcessWindow(automation, pid));
            passed = true;
        }
        finally
        {
            if (!passed && window is not null && !process.HasExited)
            {
                try
                {
                    using var screenshot = window.Capture();
                    screenshot.Save(Path.Combine(evidence!, scenario + "-failure.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception captureError) when (captureError is not OutOfMemoryException)
                {
                    File.WriteAllText(Path.Combine(evidence!, scenario + "-capture-error.txt"), captureError.GetType().Name);
                }
            }
            File.WriteAllText(Path.Combine(evidence!, scenario + ".json"), JsonSerializer.Serialize(new
            {
                scenario,
                pid,
                passed,
                exited = process.HasExited,
                exitCode = process.HasExited ? (int?)process.ExitCode : null
            }));
            // Cleanup is restricted to the retained handle of this activation, never a process name.
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(2000))
                    process.Kill();
            }
        }
    }

    private static Window? FindProcessWindow(UIA3Automation automation, int pid)
    {
        // Sandbox UIA3 can report ProcessId=0; the native HWND owner remains authoritative.
        foreach (var element in automation.GetDesktop().FindAllChildren())
        {
            var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
            if (handle != IntPtr.Zero && GetWindowThreadProcessId(handle, out var owner) != 0 && owner == (uint)pid)
                return element.AsWindow();
        }
        return null;
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);

    [DllImport("user32.dll")]
    private static extern bool CloseDesktop(IntPtr desktop);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
