using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AiUsage.Windows.Tests;

internal static class DesktopTestEnvironment
{
    // Keep this test process's input/capture coordinates aligned with WinUI's physical window.
    // This does not change the user's display or accessibility settings.
    [ModuleInitializer]
    internal static void Initialize() => SetProcessDpiAwarenessContext(new IntPtr(-4));

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
}
