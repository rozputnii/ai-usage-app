using AiUsage.Features.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// The tray's five-hour indicator (T-055 R-04): a 16 px rail circle with an arc over it that starts at 12 o'clock and runs
/// clockwise for <see cref="Fraction"/> of the circle in <see cref="ArcBrush"/>. The caller decides what the share is and what
/// colour says; the control only draws. The rail and the arc are paths on the same circle (stroke centred on radius 7), so
/// they line up without relying on how a shape's stroke sits inside its box. The whole box is the hover target for a tooltip.
/// </summary>
internal sealed partial class FiveHourRing : Grid
{
    // Below this the arc is too short to draw and only the rail shows; from 1 - Epsilon the arc is a full circle, because an
    // arc segment whose end point is its start point draws nothing.
    private const double Epsilon = 0.001;

    public static readonly DependencyProperty FractionProperty = DependencyProperty.Register(nameof(Fraction), typeof(double), typeof(FiveHourRing), new PropertyMetadata(0.0, (d, _) => ((FiveHourRing)d).Update()));
    public static readonly DependencyProperty ArcBrushProperty = DependencyProperty.Register(nameof(ArcBrush), typeof(Brush), typeof(FiveHourRing), new PropertyMetadata(null, (d, _) => ((FiveHourRing)d).Update()));

    private readonly Path arc = new() { StrokeThickness = RingGeometry.Stroke, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };

    public FiveHourRing()
    {
        Width = Height = RingGeometry.Size;
        Background = LedgerTheme.Solid(Paint.Transparent);
        Children.Add(new Path { Data = Circle(), Stroke = LedgerTheme.Solid("Rail"), StrokeThickness = RingGeometry.Stroke });
        Children.Add(arc);
        Update();
    }

    /// <summary>The share of the circle the arc spans, 0 to 1. The setter clamps, and the drawing clamps again, so a value that arrives through a binding is drawn within the range too.</summary>
    public double Fraction { get => (double)GetValue(FractionProperty); set => SetValue(FractionProperty, RingGeometry.Clamp(value)); }

    public Brush? ArcBrush { get => (Brush?)GetValue(ArcBrushProperty); set => SetValue(ArcBrushProperty, value); }

    private void Update()
    {
        var fraction = RingGeometry.Clamp(Fraction);
        arc.Stroke = ArcBrush;
        arc.Visibility = fraction > Epsilon && ArcBrush is not null ? Visibility.Visible : Visibility.Collapsed;
        arc.Data = fraction >= 1 - Epsilon ? Circle() : Arc(fraction);
    }

    private static EllipseGeometry Circle() =>
        new() { Center = new Point(RingGeometry.Size / 2, RingGeometry.Size / 2), RadiusX = RingGeometry.Radius, RadiusY = RingGeometry.Radius };

    private static PathGeometry Arc(double fraction)
    {
        var (startX, startY) = RingGeometry.ArcEnd(0);
        var (endX, endY) = RingGeometry.ArcEnd(fraction);
        var figure = new PathFigure { StartPoint = new Point(startX, startY), IsClosed = false, IsFilled = false };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(RingGeometry.Radius, RingGeometry.Radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = RingGeometry.IsLargeArc(fraction),
        });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
