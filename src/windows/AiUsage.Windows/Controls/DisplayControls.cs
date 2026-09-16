using System.ComponentModel;
using AiUsage.Features.Presentation;
using AiUsage.Platform;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace AiUsage.Controls;

/// <summary>
/// Provider glyph tile. Neutral (card2 surface, ink glyph) by default per D-121/D6; the demo can show the designed hues,
/// which remain pending rights review. Decorative: the provider name is always present as text nearby.
/// </summary>
internal sealed partial class ProviderTile : UserControl
{
    public static readonly DependencyProperty ProviderIdProperty = DependencyProperty.Register(nameof(ProviderId), typeof(string), typeof(ProviderTile), new PropertyMetadata(string.Empty, (d, _) => ((ProviderTile)d).Paint()));
    public static readonly DependencyProperty TileSizeProperty = DependencyProperty.Register(nameof(TileSize), typeof(double), typeof(ProviderTile), new PropertyMetadata(18d, (d, _) => ((ProviderTile)d).Paint()));

    private readonly Border tile = new();
    private readonly TextBlock glyph = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold, TextLineBounds = TextLineBounds.Tight };

    public ProviderTile()
    {
        IsTabStop = false;
        AutomationProperties.SetAccessibilityView(this, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        tile.Child = glyph;
        Content = tile;
        glyph.FontFamily = new FontFamily("Consolas");
        ActualThemeChanged += (_, _) => Paint();
        Loaded += (_, _) =>
        {
            AppLayout.Current.PropertyChanged += OnLayoutChanged;
            Paint();
        };
        Unloaded += (_, _) => AppLayout.Current.PropertyChanged -= OnLayoutChanged;
        Paint();
    }

    public string ProviderId { get => (string)GetValue(ProviderIdProperty); set => SetValue(ProviderIdProperty, value); }
    public double TileSize { get => (double)GetValue(TileSizeProperty); set => SetValue(TileSizeProperty, value); }

    private void OnLayoutChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppLayout.ProviderHues))
            Paint();
    }

    private void Paint()
    {
        var size = TileSize;
        tile.Width = size;
        tile.Height = size;
        tile.CornerRadius = new CornerRadius(size >= 28 ? 6 : size >= 22 ? 5 : size >= 18 ? 4 : 3);
        glyph.FontSize = size >= 28 ? 13 : size >= 22 ? 10 : size >= 18 ? 10 : 9;
        glyph.Text = Providers.Get(ProviderId ?? string.Empty).Glyph;
        if (AppLayout.Current.ProviderHues && !Bind.IsSystemHighContrast())
        {
            (tile.Background, glyph.Foreground) = ProviderId switch
            {
                "codex" => (Bind.Token(this, "FillBrush"), Bind.Token(this, "AppBgBrush")),
                "claude" => (Solid(0xD7, 0x76, 0x55), new SolidColorBrush(Colors.White)),
                "copilot" => (Solid(0x5B, 0x6C, 0xFF), new SolidColorBrush(Colors.White)),
                "antigravity" => (Solid(0x1B, 0xA3, 0x9C), new SolidColorBrush(Colors.White)),
                _ => (Bind.Token(this, "Card2Brush"), Bind.Token(this, "TextBrush")),
            };
            tile.BorderThickness = new Thickness(0);
        }
        else
        {
            tile.Background = Bind.Token(this, "Card2Brush");
            glyph.Foreground = Bind.Token(this, "TextBrush");
            tile.BorderBrush = Bind.Token(this, "StrokeBrush");
            tile.BorderThickness = new Thickness(1);
        }
    }

    private static SolidColorBrush Solid(byte r, byte g, byte b) => new(Color.FromArgb(255, r, g, b));
}

/// <summary>Freshness / connection pill. Tone drives background, stroke and text; the text always names the state.</summary>
internal sealed partial class StatusPill : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusPill), new PropertyMetadata(string.Empty, (d, _) => ((StatusPill)d).Paint()));
    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(nameof(Tone), typeof(PillTone), typeof(StatusPill), new PropertyMetadata(PillTone.Neutral, (d, _) => ((StatusPill)d).Paint()));

    private readonly Border border = new() { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Padding = new Thickness(7, 0, 7, 1), HorizontalAlignment = HorizontalAlignment.Left };
    private readonly TextBlock label = new() { FontSize = 11, TextWrapping = TextWrapping.Wrap };

    public StatusPill()
    {
        IsTabStop = false;
        border.Child = label;
        Content = border;
        ActualThemeChanged += (_, _) => Paint();
        Loaded += (_, _) => Paint();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public PillTone Tone { get => (PillTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    private void Paint()
    {
        label.Text = Text;
        AutomationProperties.SetName(this, Text);
        var (background, stroke, foreground) = Tone switch
        {
            PillTone.Warning => ("WarnBgBrush", "WarnStrokeBrush", "WarnBrush"),
            PillTone.Critical => ("CritBgBrush", "CritStrokeBrush", "CritBrush"),
            PillTone.Muted => ("TransparentBrush", "StrokeBrush", "Text2Brush"),
            _ => ("TransparentBrush", "Stroke2Brush", "Text2Brush"),
        };
        border.Background = Bind.Token(this, background);
        border.BorderBrush = Bind.Token(this, stroke);
        label.Foreground = Bind.Token(this, foreground);
    }
}

/// <summary>Motion helpers: page/section entrance (6 px rise + fade, 200 ms decelerate) and the 1.5 s "✓ Updated" acknowledgement.</summary>
public sealed partial class Motion : DependencyObject
{
    public static readonly DependencyProperty EntranceProperty = DependencyProperty.RegisterAttached(
        "Entrance", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnEntranceChanged));

    public static bool GetEntrance(DependencyObject element) => (bool)element.GetValue(EntranceProperty);
    public static void SetEntrance(DependencyObject element, bool value) => element.SetValue(EntranceProperty, value);

    private static void OnEntranceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element || e.NewValue is not true)
            return;
        element.Loaded += (sender, _) => Rise((UIElement)sender);
        element.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (sender, _) =>
        {
            if (((UIElement)sender).Visibility == Visibility.Visible)
                Rise((UIElement)sender);
        });
    }

    /// <summary>Plays the entrance on demand (for example when a page is shown again from the navigation cache).</summary>
    public static void Rise(UIElement element)
    {
        if (!MotionSettings.Allowed)
        {
            element.Opacity = 1;
            return;
        }
        var transform = element.RenderTransform as TranslateTransform ?? new TranslateTransform();
        element.RenderTransform = transform;
        var ease = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 };
        var fade = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(200), EasingFunction = ease };
        var rise = new DoubleAnimation { From = 6, To = 0, Duration = TimeSpan.FromMilliseconds(200), EasingFunction = ease };
        Storyboard.SetTarget(fade, element);
        Storyboard.SetTargetProperty(fade, "Opacity");
        Storyboard.SetTarget(rise, transform);
        Storyboard.SetTargetProperty(rise, "Y");
        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(rise);
        storyboard.Begin();
    }

    public static readonly DependencyProperty AcknowledgeProperty = DependencyProperty.RegisterAttached(
        "Acknowledge", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnAcknowledgeChanged));

    public static bool GetAcknowledge(DependencyObject element) => (bool)element.GetValue(AcknowledgeProperty);
    public static void SetAcknowledge(DependencyObject element, bool value) => element.SetValue(AcknowledgeProperty, value);

    /// <summary>Visible while true; fades in quickly and out over the 1.5 s window, or switches instantly under reduced motion.</summary>
    private static void OnAcknowledgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;
        if (e.NewValue is not true)
        {
            element.Visibility = Visibility.Collapsed;
            return;
        }
        element.Visibility = Visibility.Visible;
        if (!MotionSettings.Allowed)
        {
            element.Opacity = 1;
            return;
        }
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0 });
        animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(225), Value = 1 });
        animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(1200), Value = 1 });
        animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(1500), Value = 0 });
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    public static readonly DependencyProperty RotateProperty = DependencyProperty.RegisterAttached(
        "Rotate", typeof(double), typeof(Motion), new PropertyMetadata(0d, OnRotateChanged));

    public static double GetRotate(DependencyObject element) => (double)element.GetValue(RotateProperty);
    public static void SetRotate(DependencyObject element, double value) => element.SetValue(RotateProperty, value);

    /// <summary>Chevron rotation over 200 ms decelerate; instant under reduced motion.</summary>
    private static void OnRotateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;
        var transform = element.RenderTransform as RotateTransform ?? new RotateTransform();
        element.RenderTransform = transform;
        element.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        var target = (double)e.NewValue;
        if (!MotionSettings.Allowed || element is FrameworkElement { IsLoaded: false })
        {
            transform.Angle = target;
            return;
        }
        var animation = new DoubleAnimation { To = target, Duration = TimeSpan.FromMilliseconds(200), EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5 } };
        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, "Angle");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
}

/// <summary>
/// Lays its child out at an effective-pixel scale (demo 100/150/200 % content scale): the child is measured with the
/// available size divided by the scale and rendered scaled, so wrapping and compact breakpoints behave as at that scale.
/// </summary>
internal sealed partial class ScalePanel : Panel
{
    public static readonly DependencyProperty ContentScaleProperty = DependencyProperty.Register(nameof(ContentScale), typeof(double), typeof(ScalePanel), new PropertyMetadata(1d, (d, _) => ((ScalePanel)d).InvalidateMeasure()));

    public double ContentScale { get => (double)GetValue(ContentScaleProperty); set => SetValue(ContentScaleProperty, value); }

    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        var scale = Math.Max(1, ContentScale);
        var inner = new Windows.Foundation.Size(availableSize.Width / scale, availableSize.Height / scale);
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            child.Measure(inner);
            width = Math.Max(width, child.DesiredSize.Width * scale);
            height = Math.Max(height, child.DesiredSize.Height * scale);
        }
        // A desired size is never infinite, even when the parent offers unbounded space.
        return new Windows.Foundation.Size(
            double.IsInfinity(availableSize.Width) ? width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? height : availableSize.Height);
    }

    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        var scale = Math.Max(1, ContentScale);
        var inner = new Windows.Foundation.Size(finalSize.Width / scale, finalSize.Height / scale);
        foreach (var child in Children)
        {
            child.Arrange(new Windows.Foundation.Rect(0, 0, inner.Width, inner.Height));
            child.RenderTransform = scale == 1 ? null : new ScaleTransform { ScaleX = scale, ScaleY = scale };
        }
        return finalSize;
    }
}

/// <summary>
/// Arrow-key focus movement inside a group that takes one tab stop (the shell tabs and the settings sections). Focus
/// moves; Space or Enter still activates, so nothing navigates by accident.
/// </summary>
public sealed partial class ArrowNavigation : DependencyObject
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(ArrowNavigation), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            return;
        // RadioButton marks arrow keys handled while trying to move inside its group, so the group listens for them anyway.
        panel.RemoveHandler(UIElement.KeyDownEvent, handler);
        if ((bool)e.NewValue)
            panel.AddHandler(UIElement.KeyDownEvent, handler, handledEventsToo: true);
    }

    private static readonly Microsoft.UI.Xaml.Input.KeyEventHandler handler = OnKeyDown;

    private static void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var step = e.Key switch
        {
            Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Up => -1,
            Windows.System.VirtualKey.Right or Windows.System.VirtualKey.Down => 1,
            _ => 0,
        };
        if (step == 0 || sender is not Panel panel)
            return;
        var items = panel.Children.OfType<Control>().Where(c => c.IsEnabled && c.IsTabStop).ToList();
        var current = items.FindIndex(c => c.FocusState != FocusState.Unfocused);
        if (items.Count == 0 || current < 0)
            return;
        var next = items[(current + step + items.Count) % items.Count];
        e.Handled = next.Focus(FocusState.Keyboard);
    }
}
