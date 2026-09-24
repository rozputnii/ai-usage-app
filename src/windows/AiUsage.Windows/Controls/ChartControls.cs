using AiUsage.Features.History;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace AiUsage.Controls;

/// <summary>24 h sparkline (D10): segments only, no markers, never joined across gaps or resets.</summary>
internal sealed partial class Sparkline : UserControl
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(nameof(Model), typeof(ChartModel), typeof(Sparkline), new PropertyMetadata(ChartModel.Empty, (d, _) => ((Sparkline)d).Redraw()));

    private readonly Canvas canvas = new();

    public Sparkline()
    {
        IsTabStop = false;
        Height = 28;
        Content = canvas;
        AutomationProperties.SetAccessibilityView(this, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        SizeChanged += (_, _) => Redraw();
    }

    public ChartModel Model { get => (ChartModel)GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    private void Redraw()
    {
        canvas.Children.Clear();
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0)
            return;
        var model = Model ?? ChartModel.Empty;
        var hatch = Bind.Token(this, "HatchBrush");
        foreach (var gap in model.Gaps)
        {
            var band = new Rectangle { Width = Math.Max(1, gap.Width * width), Height = height, Fill = hatch, Opacity = 0.18 };
            Canvas.SetLeft(band, gap.X * width);
            canvas.Children.Add(band);
        }
        var accent = Bind.Token(this, "AccentBrush");
        foreach (var segment in model.Segments)
        {
            var polyline = new Polyline { Stroke = accent, StrokeThickness = 1.5, StrokeLineJoin = PenLineJoin.Round };
            foreach (var point in segment.Points)
                polyline.Points.Add(new Point(point.X * (width - 2) + 1, point.Y * (height - 4) + 2));
            canvas.Children.Add(polyline);
        }
    }
}
