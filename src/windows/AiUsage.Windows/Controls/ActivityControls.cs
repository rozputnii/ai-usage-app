using System.ComponentModel;
using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace AiUsage.Controls;

/// <summary>Base for decorative indicators that animate only while loaded, visible and allowed by reduced-motion settings.</summary>
internal abstract partial class MotionAwareControl : UserControl
{
    private Storyboard? storyboard;
    private bool subscribed;

    protected MotionAwareControl()
    {
        IsTabStop = false;
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        Loaded += (_, _) =>
        {
            if (!subscribed && MotionSettings.Current is { } motion)
            {
                motion.PropertyChanged += OnMotionChanged;
                subscribed = true;
            }
            UpdateAnimation();
        };
        Unloaded += (_, _) =>
        {
            if (subscribed && MotionSettings.Current is { } motion)
                motion.PropertyChanged -= OnMotionChanged;
            subscribed = false;
            Stop();
        };
    }

    protected abstract bool WantsAnimation { get; }

    protected abstract Storyboard CreateStoryboard();

    protected virtual void OnStopped() { }

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);

    private void OnMotionChanged(object? sender, PropertyChangedEventArgs e) => DispatcherQueue.TryEnqueue(UpdateAnimation);

    protected void UpdateAnimation()
    {
        if (IsLoaded && WantsAnimation && MotionSettings.Allowed)
        {
            if (storyboard is null)
            {
                storyboard = CreateStoryboard();
                storyboard.Begin();
            }
        }
        else
            Stop();
    }

    protected void Restart()
    {
        Stop();
        UpdateAnimation();
    }

    private void Stop()
    {
        storyboard?.Stop();
        storyboard = null;
        OnStopped();
    }

    protected static Storyboard Spin(DependencyObject target, double seconds = 1)
    {
        var animation = new DoubleAnimation { From = 0, To = 360, Duration = TimeSpan.FromSeconds(seconds), RepeatBehavior = RepeatBehavior.Forever };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Angle");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        return storyboard;
    }
}

/// <summary>The design's circular-arrow refresh glyph; rotates 1 s linear while <see cref="IsSpinning"/> (static under reduced motion).</summary>
internal sealed partial class RefreshGlyph : MotionAwareControl
{
    public static readonly DependencyProperty IsSpinningProperty = DependencyProperty.Register(nameof(IsSpinning), typeof(bool), typeof(RefreshGlyph), new PropertyMetadata(false, (d, _) => ((RefreshGlyph)d).UpdateAnimation()));
    public static readonly DependencyProperty GlyphSizeProperty = DependencyProperty.Register(nameof(GlyphSize), typeof(double), typeof(RefreshGlyph), new PropertyMetadata(13d, (d, e) => ((RefreshGlyph)d).Resize((double)e.NewValue)));

    private readonly RotateTransform rotation = new();
    private readonly Viewbox box;

    public RefreshGlyph()
    {
        var arc = new Microsoft.UI.Xaml.Shapes.Path { StrokeThickness = 1.6, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Data = Geometry("M13.5,8 A5.5,5.5 0 1 1 11.9,4.1") };
        var head = new Microsoft.UI.Xaml.Shapes.Path { StrokeThickness = 1.6, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round, Data = Geometry("M12.2,1.6 L12.2,4.6 L9.2,4.6") };
        foreach (var path in new[] { arc, head })
            path.SetBinding(Shape.StrokeProperty, new Microsoft.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
        var canvas = new Canvas { Width = 16, Height = 16 };
        canvas.Children.Add(arc);
        canvas.Children.Add(head);
        box = new Viewbox { Width = 13, Height = 13, Child = canvas, RenderTransform = rotation, RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5) };
        Content = box;
    }

    public bool IsSpinning { get => (bool)GetValue(IsSpinningProperty); set => SetValue(IsSpinningProperty, value); }
    public double GlyphSize { get => (double)GetValue(GlyphSizeProperty); set => SetValue(GlyphSizeProperty, value); }

    protected override bool WantsAnimation => IsSpinning;
    protected override Storyboard CreateStoryboard() => Spin(rotation);
    protected override void OnStopped() => rotation.Angle = 0;

    private void Resize(double size)
    {
        box.Width = size;
        box.Height = size;
    }

    private static Geometry Geometry(string data) => (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), data);
}

/// <summary>12–16 px ring: stroke2 circle with an accent top arc, rotating while visible; static under reduced motion.</summary>
internal sealed partial class BusyRing : MotionAwareControl
{
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(double), typeof(BusyRing), new PropertyMetadata(12d, (d, _) => ((BusyRing)d).Build()));
    public static readonly DependencyProperty OnFilledProperty = DependencyProperty.Register(nameof(OnFilled), typeof(bool), typeof(BusyRing), new PropertyMetadata(false, (d, _) => ((BusyRing)d).Build()));

    private readonly RotateTransform rotation = new();

    public BusyRing()
    {
        Build();
        ActualThemeChanged += (_, _) => Build();
        Loaded += (_, _) => Build();
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateAnimation());
    }

    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    /// <summary>Inside a filled (primary/destructive) button: the ring uses the button text colour.</summary>
    public bool OnFilled { get => (bool)GetValue(OnFilledProperty); set => SetValue(OnFilledProperty, value); }

    protected override bool WantsAnimation => Visibility == Visibility.Visible;
    protected override Storyboard CreateStoryboard() => Spin(rotation);
    protected override void OnStopped() => rotation.Angle = 0;

    private void Build()
    {
        var size = Size;
        var thickness = size >= 16 ? 2 : 1.5;
        var grid = new Grid { Width = size, Height = size, RenderTransform = rotation, RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5) };
        grid.Children.Add(new Ellipse { StrokeThickness = thickness, Stroke = Bind.Token(this, "Stroke2Brush"), Opacity = OnFilled ? 0.4 : 1 });
        var radius = (size - thickness) / 2;
        var center = size / 2;
        var figure = new PathFigure { StartPoint = new(center - radius * Math.Sin(Math.PI / 4), center - radius * Math.Cos(Math.PI / 4)) };
        figure.Segments.Add(new ArcSegment { Point = new(center + radius * Math.Sin(Math.PI / 4), center - radius * Math.Cos(Math.PI / 4)), Size = new(radius, radius), SweepDirection = SweepDirection.Clockwise });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        var arc = new Microsoft.UI.Xaml.Shapes.Path { Data = geometry, StrokeThickness = thickness, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        if (OnFilled)
            arc.SetBinding(Shape.StrokeProperty, new Microsoft.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
        else
            arc.Stroke = Bind.Token(this, "AccentBrush");
        grid.Children.Add(arc);
        Content = grid;
    }
}

/// <summary>Skeleton placeholder with a 1.6 s shimmer between skel and skel2; shimmer is off under reduced motion.</summary>
internal sealed partial class SkeletonBlock : MotionAwareControl
{
    public static readonly DependencyProperty ShimmerProperty = DependencyProperty.Register(nameof(Shimmer), typeof(bool), typeof(SkeletonBlock), new PropertyMetadata(false, (d, _) => ((SkeletonBlock)d).UpdateAnimation()));

    private readonly Border block = new();
    private readonly TranslateTransform shift = new();

    public SkeletonBlock()
    {
        Content = block;
        block.CornerRadius = new CornerRadius(3);
        ActualThemeChanged += (_, _) => Restart();
        Paint();
    }

    public bool Shimmer { get => (bool)GetValue(ShimmerProperty); set => SetValue(ShimmerProperty, value); }

    public new CornerRadius CornerRadius { get => block.CornerRadius; set => block.CornerRadius = value; }

    protected override bool WantsAnimation => Shimmer;

    private void Paint() => block.Background = Bind.Token(this, "SkelBrush");

    private LinearGradientBrush Gradient()
    {
        var skel = ((SolidColorBrush)Bind.Token(this, "SkelBrush")).Color;
        // A gradient stop belongs to one brush, so every rebuild (theme change included) needs its own highlight stop.
        var highlight = new GradientStop { Color = ((SolidColorBrush)Bind.Token(this, "Skel2Brush")).Color, Offset = 0.5 };
        var brush = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 0), MappingMode = BrushMappingMode.RelativeToBoundingBox };
        brush.GradientStops.Add(new GradientStop { Color = skel, Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = skel, Offset = 0.25 });
        brush.GradientStops.Add(highlight);
        brush.GradientStops.Add(new GradientStop { Color = skel, Offset = 0.75 });
        brush.GradientStops.Add(new GradientStop { Color = skel, Offset = 1 });
        brush.RelativeTransform = shift;
        return brush;
    }
    protected override Storyboard CreateStoryboard()
    {
        block.Background = Gradient();
        var animation = new DoubleAnimation
        {
            From = 1,
            To = -1,
            Duration = TimeSpan.FromSeconds(1.6),
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(animation, shift);
        Storyboard.SetTargetProperty(animation, "X");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        return storyboard;
    }

    protected override void OnStopped()
    {
        shift.X = 0;
        Paint();
    }
}
