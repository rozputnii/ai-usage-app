using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Xunit;

namespace AiUsage.Windows.Tests;

internal static class DesktopTestEnvironment
{
    // Keep this test process's input/capture coordinates aligned with WinUI's physical window.
    // This does not change the user's display or accessibility settings.
    [ModuleInitializer]
    internal static void Initialize() => SetProcessDpiAwarenessContext(new IntPtr(-4));

    // UserInteractive is true on a locked console. Fail before any input or capture.
    internal static void RequireUnlockedDesktop()
    {
        var desktop = OpenInputDesktop(0, false, 1);
        Assert.True(desktop != IntPtr.Zero, "BLOCKED: an unlocked interactive Windows desktop is required");
        try
        {
            var name = new char[256];
            Assert.True(GetUserObjectInformation(desktop, 2, name, name.Length * 2, out _));
            Assert.Equal("Default", new string(name).TrimEnd('\0'), ignoreCase: true);
        }
        finally { CloseDesktop(desktop); }
        // Windows 11 can leave the named input desktop as Default while LockApp/LogonUI owns input.
        var foreground = GetForegroundWindow();
        uint processId = 0;
        Assert.True(foreground != IntPtr.Zero && GetWindowThreadProcessId(foreground, out processId) != 0,
            "BLOCKED: foreground desktop input is unavailable");
        using var process = Process.GetProcessById((int)processId);
        Assert.True(process.ProcessName is not ("LockApp" or "LogonUI"),
            "BLOCKED: unlock the Windows desktop before application input or capture");
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr context);

    [DllImport("user32.dll")]
    private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetUserObjectInformation(IntPtr handle, int index, [Out] char[] information, int length, out int needed);
    [DllImport("user32.dll")]
    private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
