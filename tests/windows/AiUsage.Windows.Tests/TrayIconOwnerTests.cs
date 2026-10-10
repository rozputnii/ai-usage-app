using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using AiUsage.Platform;
using Xunit;

namespace AiUsage.Windows.Tests;

/// <summary>
/// T-062 R-01: the window owns the shown tray icon. A failed shell update must leave the shown icon alive and release the
/// new one; the shown icon is released only after a successful update. H.NotifyIcon's own order (dispose the shown icon,
/// then update and ignore the result) leaves the tray holding a destroyed handle. No desktop needed.
/// </summary>
public sealed class TrayMarkTests
{
    private const uint GdiObjects = 0;
    private const uint UserObjects = 1;

    private static readonly global::Windows.UI.Color[] Colors =
    [
        global::Windows.UI.Color.FromArgb(255, 0xC4, 0x2B, 0x1C),
        global::Windows.UI.Color.FromArgb(255, 0xB8, 0x6E, 0x00),
        global::Windows.UI.Color.FromArgb(255, 0x0F, 0x7B, 0x0F)
    ];

    [Fact]
    public void AFailedUpdateKeepsTheShownIconAndReleasesTheNewOne()
    {
        using var mark = new TrayMark();
        var shown = TrayGlyph.Create(Colors[0]);
        var shownHandle = shown.Handle;
        Assert.True(mark.Show(shown, _ => true));

        var rejected = TrayGlyph.Create(Colors[1]);
        Assert.False(mark.Show(rejected, _ => false));

        Assert.True(IsLiveIcon(shownHandle), "The shown icon's handle was destroyed by a failed update.");
        Assert.Throws<ObjectDisposedException>(() => rejected.Handle);

        var broken = TrayGlyph.Create(Colors[2]);
        Assert.Throws<InvalidOperationException>(() => mark.Show(broken, _ => throw new InvalidOperationException("shell")));
        Assert.True(IsLiveIcon(shownHandle), "The shown icon's handle was destroyed by an update that threw.");
        Assert.Throws<ObjectDisposedException>(() => broken.Handle);

        var next = TrayGlyph.Create(Colors[2]);
        Assert.True(mark.Show(next, _ => true));
        Assert.Throws<ObjectDisposedException>(() => shown.Handle);
        Assert.True(IsLiveIcon(next.Handle));
    }

    [Fact]
    public void ThousandsOfFailedAndSuccessfulUpdatesKeepTheHandleCountFlat()
    {
        TrayGlyph.Create(Colors[0]).Dispose();
        var process = Process.GetCurrentProcess().Handle;
        var gdiBefore = NativeMethods.GetGuiResources(process, GdiObjects);
        var userBefore = NativeMethods.GetGuiResources(process, UserObjects);
        using (var mark = new TrayMark())
        {
            for (var i = 0; i < 3000; i++)
            {
                var succeed = i % 2 == 0;
                mark.Show(TrayGlyph.Create(Colors[i % Colors.Length]), _ => succeed);
            }
        }
        var gdiGrowth = (long)NativeMethods.GetGuiResources(process, GdiObjects) - gdiBefore;
        var userGrowth = (long)NativeMethods.GetGuiResources(process, UserObjects) - userBefore;

        Assert.True(gdiGrowth <= 20 && userGrowth <= 20,
            $"Over 3000 alternating tray updates GDI objects grew by {gdiGrowth} (from {gdiBefore}) and USER objects by {userGrowth} (from {userBefore}).");
    }

    /// <summary>GetIconInfo succeeds only for a live icon handle; the bitmaps it returns are the caller's to free.</summary>
    private static bool IsLiveIcon(IntPtr handle)
    {
        if (!NativeMethods.GetIconInfo(handle, out var info))
            return false;
        if (info.hbmMask != IntPtr.Zero) NativeMethods.DeleteObject(info.hbmMask);
        if (info.hbmColor != IntPtr.Zero) NativeMethods.DeleteObject(info.hbmColor);
        return true;
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct IconInfo
        {
            public int fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        public static extern uint GetGuiResources(IntPtr process, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetIconInfo(IntPtr icon, out IconInfo info);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr gdiObject);
    }
}
