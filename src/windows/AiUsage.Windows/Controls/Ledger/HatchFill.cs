using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// A 135° hatch, 3 + 3 px along the stripe normal, as in the reference's repeating-linear-gradient: a solid base colour with
/// stripes of the second colour drawn as parallelograms and clipped to the element. The stripes sit in a Canvas so they
/// never take part in layout.
/// </summary>
internal sealed partial class HatchFill : Grid
{
    private const double Period = 6 * 1.4142135623730951;
    private readonly Path stripes;
    private readonly Canvas layer = new() { IsHitTestVisible = false };

    public HatchFill(Color baseColor, Color stripeColor)
    {
        Background = new SolidColorBrush(baseColor);
        stripes = new Path { Fill = new SolidColorBrush(stripeColor) };
        layer.Children.Add(stripes);
        Children.Add(layer);
        SizeChanged += (_, e) => Redraw(e.NewSize.Width, e.NewSize.Height);
    }

    private void Redraw(double width, double height)
    {
        Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        if (width <= 0 || height <= 0)
        {
            stripes.Data = null;
            return;
        }
        var geometry = new PathGeometry();
        // Stripes are the bands where (x + y) / √2 mod 6 falls in [3, 6).
        for (var k = -height; k < width + height; k += Period)
        {
            var a = k + Period / 2;
            var b = k + Period;
            var figure = new PathFigure { StartPoint = new Point(a, 0), IsClosed = true, IsFilled = true };
            var line = new PolyLineSegment();
            line.Points.Add(new Point(b, 0));
            line.Points.Add(new Point(b - height, height));
            line.Points.Add(new Point(a - height, height));
            figure.Segments.Add(line);
            geometry.Figures.Add(figure);
        }
        stripes.Data = geometry;
    }
}
