using System.Windows.Input;
using AiUsage.Features.History;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace AiUsage.Controls;

/// <summary>
/// S04 detailed chart drawn from <see cref="ChartModel"/>: axis lines, gap bands, reset lines, one polyline per observed
/// segment (never joined across gaps or resets) and focusable points whose tooltip appears on hover and keyboard focus.
/// </summary>
internal sealed partial class HistoryChart : UserControl
{
    private const double PlotLeft = 44;
    private const double PlotTop = 14;
    private const double PlotHeight = 190;
    private const double ChartHeight = 232;

    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(nameof(Model), typeof(ChartModel), typeof(HistoryChart), new PropertyMetadata(ChartModel.Empty, (d, _) => ((HistoryChart)d).Redraw()));
    public static readonly DependencyProperty AxisTopProperty = DependencyProperty.Register(nameof(AxisTop), typeof(string), typeof(HistoryChart), new PropertyMetadata("100 %", (d, _) => ((HistoryChart)d).Redraw()));
    public static readonly DependencyProperty AxisMiddleProperty = DependencyProperty.Register(nameof(AxisMiddle), typeof(string), typeof(HistoryChart), new PropertyMetadata("50 %", (d, _) => ((HistoryChart)d).Redraw()));
    public static readonly DependencyProperty AxisBottomProperty = DependencyProperty.Register(nameof(AxisBottom), typeof(string), typeof(HistoryChart), new PropertyMetadata("0 %", (d, _) => ((HistoryChart)d).Redraw()));
    public static readonly DependencyProperty GapLabelProperty = DependencyProperty.Register(nameof(GapLabel), typeof(string), typeof(HistoryChart), new PropertyMetadata("gap", (d, _) => ((HistoryChart)d).Redraw()));
    public static readonly DependencyProperty ResetLabelProperty = DependencyProperty.Register(nameof(ResetLabel), typeof(string), typeof(HistoryChart), new PropertyMetadata("reset", (d, _) => ((HistoryChart)d).Redraw()));
    public static readonly DependencyProperty PointFocusedCommandProperty = DependencyProperty.Register(nameof(PointFocusedCommand), typeof(ICommand), typeof(HistoryChart), new PropertyMetadata(null));
    public static readonly DependencyProperty PointLeftCommandProperty = DependencyProperty.Register(nameof(PointLeftCommand), typeof(ICommand), typeof(HistoryChart), new PropertyMetadata(null));

    private readonly Canvas canvas = new() { Height = ChartHeight };
    private readonly Border tip = new() { Visibility = Visibility.Collapsed, IsHitTestVisible = false, CornerRadius = new CornerRadius(5), BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6) };
    private readonly TextBlock tipText = new() { FontSize = 12, TextWrapping = TextWrapping.NoWrap };

    public HistoryChart()
    {
        IsTabStop = false;
        Height = ChartHeight;
        tip.Child = tipText;
        var root = new Grid();
        root.Children.Add(canvas);
        root.Children.Add(new Canvas { Children = { tip } });
        Content = root;
        SizeChanged += (_, _) => Redraw();
        Loaded += (_, _) => Redraw();
    }

    public ChartModel Model { get => (ChartModel)GetValue(ModelProperty); set => SetValue(ModelProperty, value); }
    public string AxisTop { get => (string)GetValue(AxisTopProperty); set => SetValue(AxisTopProperty, value); }
    public string AxisMiddle { get => (string)GetValue(AxisMiddleProperty); set => SetValue(AxisMiddleProperty, value); }
    public string AxisBottom { get => (string)GetValue(AxisBottomProperty); set => SetValue(AxisBottomProperty, value); }
    public string GapLabel { get => (string)GetValue(GapLabelProperty); set => SetValue(GapLabelProperty, value); }
    public string ResetLabel { get => (string)GetValue(ResetLabelProperty); set => SetValue(ResetLabelProperty, value); }
    public ICommand? PointFocusedCommand { get => (ICommand?)GetValue(PointFocusedCommandProperty); set => SetValue(PointFocusedCommandProperty, value); }
    public ICommand? PointLeftCommand { get => (ICommand?)GetValue(PointLeftCommandProperty); set => SetValue(PointLeftCommandProperty, value); }

    private void Redraw()
    {
        canvas.Children.Clear();
        tip.Visibility = Visibility.Collapsed;
        var width = ActualWidth;
        if (width <= PlotLeft + 20)
            return;
        var plotWidth = width - PlotLeft - 8;
        var model = Model ?? ChartModel.Empty;
        var stroke = Bind.Token(this, "StrokeBrush");
        var stroke2 = Bind.Token(this, "Stroke2Brush");
        var text2 = Bind.Token(this, "Text2Brush");
        var accent = Bind.Token(this, "AccentBrush");
        tip.Background = Bind.Token(this, "CardBrush");
        tip.BorderBrush = stroke2;
        tipText.Foreground = Bind.Token(this, "TextBrush");

        double X(double fraction) => PlotLeft + fraction * plotWidth;
        double Y(double fraction) => PlotTop + fraction * PlotHeight;

        AddLine(PlotLeft, Y(0), width - 8, Y(0), stroke);
        AddLine(PlotLeft, Y(0.5), width - 8, Y(0.5), stroke);
        AddLine(PlotLeft, Y(1), width - 8, Y(1), stroke2);
        AddText(AxisTop, 0, Y(0) - 7, text2, 11);
        AddText(AxisMiddle, 0, Y(0.5) - 7, text2, 11);
        AddText(AxisBottom, 0, Y(1) - 7, text2, 11);

        var hatch = Bind.Token(this, "HatchBrush");
        foreach (var gap in model.Gaps)
        {
            var band = new Rectangle { Width = Math.Max(2, gap.Width * plotWidth), Height = PlotHeight, Fill = hatch, Opacity = 0.22 };
            Canvas.SetLeft(band, X(gap.X));
            Canvas.SetTop(band, Y(0));
            canvas.Children.Add(band);
            AddCentered(GapLabel, X(gap.X + gap.Width / 2), Y(0.5) + 4, text2, 10);
        }

        foreach (var reset in model.Resets)
        {
            var line = new Line { X1 = X(reset), X2 = X(reset), Y1 = Y(0), Y2 = Y(1), Stroke = text2, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } };
            canvas.Children.Add(line);
            AddCentered(ResetLabel, X(reset), 0, text2, 10);
        }

        foreach (var segment in model.Segments)
        {
            var polyline = new Polyline { Stroke = accent, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
            foreach (var point in segment.Points)
                polyline.Points.Add(new Point(X(point.X), Y(point.Y)));
            canvas.Children.Add(polyline);
        }

        var background = Bind.Token(this, "AppBgBrush");
        var focus = Bind.Token(this, "FocusBrush");
        foreach (var marker in model.Markers)
        {
            var dot = new PointMarker(marker, accent, background, focus);
            Canvas.SetLeft(dot, X(marker.X) - PointMarker.Size / 2);
            Canvas.SetTop(dot, Y(marker.Y) - PointMarker.Size / 2);
            dot.Shown += (_, _) => ShowTip(dot.Marker, X(dot.Marker.X), Y(dot.Marker.Y), width);
            dot.Hidden += (_, _) => HideTip();
            canvas.Children.Add(dot);
        }

        foreach (var tick in model.Ticks)
        {
            var label = AddText(tick.Label, 0, Y(1) + 10, text2, 10);
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var left = Math.Clamp(X(tick.X) - label.DesiredSize.Width / 2, PlotLeft - 4, width - label.DesiredSize.Width);
            Canvas.SetLeft(label, left);
        }
    }

    private void ShowTip(ChartMarker marker, double x, double y, double width)
    {
        tipText.Text = marker.Tooltip;
        tip.Visibility = Visibility.Visible;
        tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = tip.DesiredSize;
        Canvas.SetLeft(tip, Math.Clamp(x - size.Width / 2, 0, Math.Max(0, width - size.Width)));
        Canvas.SetTop(tip, Math.Max(0, y - size.Height - 10));
        if (PointFocusedCommand?.CanExecute(marker) == true)
            PointFocusedCommand.Execute(marker);
    }

    private void HideTip()
    {
        tip.Visibility = Visibility.Collapsed;
        if (PointLeftCommand?.CanExecute(null) == true)
            PointLeftCommand.Execute(null);
    }

    private void AddLine(double x1, double y1, double x2, double y2, Brush brush) =>
        canvas.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush, StrokeThickness = 1 });

    private TextBlock AddText(string text, double x, double y, Brush brush, double size)
    {
        var block = new TextBlock { Text = text, FontSize = size, Foreground = brush };
        AutomationProperties.SetAccessibilityView(block, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        canvas.Children.Add(block);
        return block;
    }

    private void AddCentered(string text, double centerX, double y, Brush brush, double size)
    {
        var block = AddText(text, 0, y, brush, size);
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(block, centerX - block.DesiredSize.Width / 2);
    }

    /// <summary>A focusable 8 px point with a 2 px accent ring and an accessible name.</summary>
    private sealed partial class PointMarker : ContentControl
    {
        public const double Size = 16;

        public PointMarker(ChartMarker marker, Brush accent, Brush background, Brush focus)
        {
            Marker = marker;
            Width = Size;
            Height = Size;
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            FocusVisualPrimaryBrush = focus;
            FocusVisualPrimaryThickness = new Thickness(2);
            FocusVisualSecondaryThickness = new Thickness(0);
            AutomationProperties.SetName(this, marker.AccessibleName);
            Content = new Grid
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Children = { new Ellipse { Width = 9, Height = 9, Fill = background, Stroke = accent, StrokeThickness = 2, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } },
            };
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            PointerEntered += (_, _) => Shown?.Invoke(this, EventArgs.Empty);
            PointerExited += (_, _) => Hidden?.Invoke(this, EventArgs.Empty);
            GotFocus += (_, _) => Shown?.Invoke(this, EventArgs.Empty);
            LostFocus += (_, _) => Hidden?.Invoke(this, EventArgs.Empty);
        }

        public ChartMarker Marker { get; }
        public event EventHandler? Shown;
        public event EventHandler? Hidden;

        protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer(this);
    }
}

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
