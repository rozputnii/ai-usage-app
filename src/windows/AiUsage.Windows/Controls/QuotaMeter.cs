using AiUsage.Features.Accounts;
using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace AiUsage.Controls;

/// <summary>
/// 4 px quota meter: ink/status fill with threshold ticks (Known), dotted hatch (Unknown/Unavailable) or dashed pattern
/// (Unlimited). Never draws a zero fill for a missing value. Width animates over 320 ms only after a new observation.
/// Visuals are hidden from UI Automation; the owning row carries the text value and state word.
/// </summary>
internal sealed partial class QuotaMeter : UserControl
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(nameof(Kind), typeof(MeterKind), typeof(QuotaMeter), new PropertyMetadata(MeterKind.Hatch, OnVisualChanged));
    public static readonly DependencyProperty FractionProperty = DependencyProperty.Register(nameof(Fraction), typeof(double), typeof(QuotaMeter), new PropertyMetadata(0d, OnFractionChanged));
    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(nameof(Tone), typeof(ValueTone), typeof(QuotaMeter), new PropertyMetadata(ValueTone.Normal, OnVisualChanged));
    public static readonly DependencyProperty TicksProperty = DependencyProperty.Register(nameof(Ticks), typeof(object), typeof(QuotaMeter), new PropertyMetadata(null, OnVisualChanged));
    public static readonly DependencyProperty ObservationProperty = DependencyProperty.Register(nameof(Observation), typeof(long), typeof(QuotaMeter), new PropertyMetadata(0L, OnObservationChanged));
    public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(nameof(BarHeight), typeof(double), typeof(QuotaMeter), new PropertyMetadata(4d, OnVisualChanged));

    private readonly Canvas canvas = new() { IsHitTestVisible = false };
    private readonly Border track = new() { CornerRadius = new CornerRadius(2) };
    private readonly Border fill = new() { CornerRadius = new CornerRadius(2) };
    private readonly Canvas pattern = new();
    private readonly Canvas ticks = new();
    private bool observationChanged;
    private Storyboard? running;

    public QuotaMeter()
    {
        Height = 8;
        IsTabStop = false;
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        canvas.Children.Add(track);
        canvas.Children.Add(fill);
        canvas.Children.Add(pattern);
        canvas.Children.Add(ticks);
        Content = canvas;
        SizeChanged += (_, _) => Redraw(animate: false);
        ActualThemeChanged += (_, _) => Redraw(animate: false);
        Loaded += (_, _) => Redraw(animate: false);
    }

    public MeterKind Kind { get => (MeterKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public double Fraction { get => (double)GetValue(FractionProperty); set => SetValue(FractionProperty, value); }
    public ValueTone Tone { get => (ValueTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public object? Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }
    public long Observation { get => (long)GetValue(ObservationProperty); set => SetValue(ObservationProperty, value); }
    public double BarHeight { get => (double)GetValue(BarHeightProperty); set => SetValue(BarHeightProperty, value); }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((QuotaMeter)d).Redraw(animate: false);

    private static void OnObservationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((QuotaMeter)d).observationChanged = true;

    private static void OnFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var meter = (QuotaMeter)d;
        // x:Bind may apply the observation after the value; either order counts as a new reading.
        meter.Redraw(animate: meter.IsLoaded);
        meter.observationChanged = false;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);

    private void Redraw(bool animate)
    {
        var width = ActualWidth;
        var bar = BarHeight;
        Height = bar + 4;
        if (width <= 0)
            return;
        var top = 2d;
        track.Width = width;
        track.Height = bar;
        Canvas.SetTop(track, top);
        Canvas.SetTop(fill, top);
        fill.Height = bar;
        pattern.Children.Clear();
        ticks.Children.Clear();

        switch (Kind)
        {
            case MeterKind.Bar:
                track.Background = Bind.Token(this, "TrackBrush");
                fill.Visibility = Visibility.Visible;
                fill.Background = Bind.Token(this, Bind.FillKey(Tone));
                SetFillWidth(Math.Clamp(Fraction, 0, 1) * width, animate && observationChanged && MotionSettings.Allowed);
                if (Ticks is IEnumerable<double> values)
                {
                    var tickBrush = Bind.Token(this, "Text2Brush");
                    foreach (var value in values)
                    {
                        var tick = new Rectangle { Width = 1, Height = bar + 4, Fill = tickBrush, Opacity = 0.55 };
                        Canvas.SetLeft(tick, Math.Round(Math.Clamp(value, 0, 1) * width));
                        ticks.Children.Add(tick);
                    }
                }
                break;
            case MeterKind.Unlimited:
                track.Background = null;
                fill.Visibility = Visibility.Collapsed;
                DrawPattern(width, top, bar, dash: 14, gap: 4, Bind.Token(this, "FillBrush"), 0.6);
                break;
            default:
                track.Background = null;
                fill.Visibility = Visibility.Collapsed;
                DrawPattern(width, top, bar, dash: 3, gap: 4, Bind.Token(this, "HatchBrush"), 1);
                break;
        }
    }

    private void DrawPattern(double width, double top, double bar, double dash, double gap, Brush brush, double opacity)
    {
        for (double x = 0; x < width; x += dash + gap)
        {
            var mark = new Rectangle { Width = Math.Min(dash, width - x), Height = bar, Fill = brush, Opacity = opacity, RadiusX = 1, RadiusY = 1 };
            Canvas.SetLeft(mark, x);
            Canvas.SetTop(mark, top);
            pattern.Children.Add(mark);
        }
    }

    private void SetFillWidth(double target, bool animate)
    {
        running?.Stop();
        running = null;
        if (!animate || double.IsNaN(fill.Width))
        {
            fill.Width = target;
            return;
        }
        var from = fill.Width;
        fill.Width = target;
        if (Math.Abs(from - target) < 0.5)
            return;
        var animation = new DoubleAnimationUsingKeyFrames { EnableDependentAnimation = true };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = from });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(320),
            Value = target,
            KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0), ControlPoint2 = new Windows.Foundation.Point(0, 1) },
        });
        Storyboard.SetTarget(animation, fill);
        Storyboard.SetTargetProperty(animation, "Width");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Completed += (_, _) => fill.Width = target;
        running = storyboard;
        storyboard.Begin();
    }
}
