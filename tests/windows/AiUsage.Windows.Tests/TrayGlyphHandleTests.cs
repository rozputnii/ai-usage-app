using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using AiUsage.Platform;
using Xunit;

namespace AiUsage.Windows.Tests;

/// <summary>
/// T-056: the tray mark is redrawn on every card change for days of uptime. Each redraw must release the icon it replaces,
/// or the process runs out of GDI/USER handles after a few thousand redraws and every later redraw fails. No desktop needed.
/// </summary>
public sealed class TrayGlyphHandleTests
{
    private const uint GdiObjects = 0;
    private const uint UserObjects = 1;

    [Fact]
    public void ThousandsOfTrayGlyphUpdatesKeepTheHandleCountFlat()
    {
        global::Windows.UI.Color[] colors =
        [
            global::Windows.UI.Color.FromArgb(255, 0xC4, 0x2B, 0x1C),
            global::Windows.UI.Color.FromArgb(255, 0xB8, 0x6E, 0x00),
            global::Windows.UI.Color.FromArgb(255, 0x0F, 0x7B, 0x0F)
        ];
        // The window keeps the icon it shows and disposes the one it replaces; the same sequence runs here.
        Icon? shown = TrayGlyph.Create(colors[0]);
        shown.Dispose();
        var process = Process.GetCurrentProcess().Handle;
        var gdiBefore = NativeMethods.GetGuiResources(process, GdiObjects);
        var userBefore = NativeMethods.GetGuiResources(process, UserObjects);
        shown = null;
        for (var i = 0; i < 3000; i++)
        {
            var next = TrayGlyph.Create(colors[i % colors.Length]);
            shown?.Dispose();
            shown = next;
        }
        shown?.Dispose();
        var gdiGrowth = (long)NativeMethods.GetGuiResources(process, GdiObjects) - gdiBefore;
        var userGrowth = (long)NativeMethods.GetGuiResources(process, UserObjects) - userBefore;

        Assert.True(gdiGrowth <= 20 && userGrowth <= 20,
            $"Over 3000 tray glyph updates GDI objects grew by {gdiGrowth} (from {gdiBefore}) and USER objects by {userGrowth} (from {userBefore}).");
    }

    [Fact]
    public void TheTrayGlyphKeepsItsColourAndTransparentBackground()
    {
        using var icon = TrayGlyph.Create(global::Windows.UI.Color.FromArgb(255, 0xC4, 0x2B, 0x1C));
        using var pixels = icon.ToBitmap();

        Assert.Equal(128, pixels.Width);
        Assert.Equal(0, pixels.GetPixel(0, 0).A);
        var inked = Enumerable.Range(0, pixels.Width).SelectMany(x => Enumerable.Range(0, pixels.Height).Select(y => pixels.GetPixel(x, y)))
            .Where(p => p.A == 255).ToArray();
        Assert.NotEmpty(inked);
        Assert.All(inked, p => Assert.Equal((0xC4, 0x2B, 0x1C), (p.R, p.G, p.B)));
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern uint GetGuiResources(IntPtr process, uint flags);
    }
}
