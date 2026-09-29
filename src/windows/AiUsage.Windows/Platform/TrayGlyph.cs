using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AiUsage.Platform;

/// <summary>
/// Draws the "AI" tray mark. H.NotifyIcon's GeneratedIconSource sizes text in points against a bitmap that inherits the
/// display DPI, so above 100 % scaling the line outgrows the 128 px icon and GDI+ drops it, leaving a transparent slot.
/// The mark is drawn as a path in pixels instead and fitted to the icon, which keeps it identical at every scale.
/// </summary>
internal static class TrayGlyph
{
    private const int Size = 128;
    private const float Inset = 8;

    public static Icon Create(Windows.UI.Color color)
    {
        using var bitmap = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var family = new FontFamily("Segoe UI"))
        using (var path = new GraphicsPath())
        using (var brush = new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B)))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            path.AddString("AI", family, (int)FontStyle.Bold, 100, PointF.Empty, StringFormat.GenericTypographic);
            var bounds = path.GetBounds();
            var scale = (Size - 2 * Inset) / Math.Max(bounds.Width, bounds.Height);
            using var fit = new Matrix();
            fit.Translate(Size / 2f, Size / 2f);
            fit.Scale(scale, scale);
            fit.Translate(-(bounds.X + bounds.Width / 2), -(bounds.Y + bounds.Height / 2));
            path.Transform(fit);
            graphics.FillPath(brush, path);
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
