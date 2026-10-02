using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// A Grid whose background and border are a squircle (superellipse, exponent 4) instead of circular corners, matching the
/// reference's corner-shape: squircle (PD-038-02). The shape is redrawn on resize; children are laid out as in a Grid.
/// </summary>
internal sealed partial class SquircleSurface : Grid
{
    private const int Samples = 10;
    private readonly Path shape = new() { IsHitTestVisible = false };
    private readonly Canvas layer = new() { IsHitTestVisible = false };

    public SquircleSurface()
    {
        layer.Children.Add(shape);
        Children.Add(layer);
        SetRowSpan(layer, 64);
        SetColumnSpan(layer, 64);
        SizeChanged += (_, _) => Redraw();
    }

    public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(nameof(Radius), typeof(double), typeof(SquircleSurface), new PropertyMetadata(20.0, OnShapeChanged));
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(SquircleSurface), new PropertyMetadata(null, OnBrushChanged));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(SquircleSurface), new PropertyMetadata(null, OnBrushChanged));
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(SquircleSurface), new PropertyMetadata(1.0, OnShapeChanged));

    public double Radius { get => (double)GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }
    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public Brush? Stroke { get => (Brush?)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SquircleSurface)d).Redraw();

    private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var surface = (SquircleSurface)d;
        surface.shape.Fill = surface.Fill;
        surface.shape.Stroke = surface.Stroke;
        surface.Redraw();
    }

    private void Redraw()
    {
        shape.StrokeThickness = Stroke is null ? 0 : StrokeThickness;
        shape.Data = Geometry(ActualWidth, ActualHeight, Radius, Stroke is null ? 0 : StrokeThickness / 2);
    }

    /// <summary>A rectangle with superellipse corners, inset by half the stroke so the border stays inside the bounds.</summary>
    public static Geometry? Geometry(double width, double height, double radius, double inset)
    {
        var w = width - inset * 2;
        var h = height - inset * 2;
        if (w <= 0 || h <= 0)
            return null;
        var r = Math.Max(0, Math.Min(radius, Math.Min(w, h) / 2));
        var figure = new PathFigure { IsClosed = true, IsFilled = true, StartPoint = new Point(inset + r, inset) };
        var points = new PolyLineSegment();
        void Corner(double cx, double cy, double startAngle)
        {
            for (var i = 0; i <= Samples; i++)
            {
                var t = (startAngle + i * 90.0 / Samples) * Math.PI / 180;
                var cos = Math.Cos(t);
                var sin = Math.Sin(t);
                points.Points.Add(new Point(cx + r * Math.Sign(cos) * Math.Sqrt(Math.Abs(cos)), cy + r * Math.Sign(sin) * Math.Sqrt(Math.Abs(sin))));
            }
        }
        // Clockwise from the top edge: top-right, bottom-right, bottom-left, top-left corners.
        Corner(inset + w - r, inset + r, 270);
        Corner(inset + w - r, inset + h - r, 0);
        Corner(inset + r, inset + h - r, 90);
        Corner(inset + r, inset + r, 180);
        figure.Segments.Add(points);
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
