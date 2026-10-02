using AiUsage.Features.Ledger;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// Token access for code-built Ledger visuals. The tokens dictionary is loaded once; paint keys from view models
/// ("OkM", "Rail") resolve to Ledger{Key}Color. Hatched paints are drawn by HatchFill: a repeating gradient brush dimmed
/// neighbouring text on this WinUI build, so the 135° stripes (3 + 3 px) are geometry instead.
/// </summary>
internal static class LedgerTheme
{
    private static ResourceDictionary? tokens;
    private static readonly Dictionary<Paint, Brush> Brushes = [];
    private static readonly UISettings Ui = new();

    /// <summary>Styles.xaml, which merges Tokens.xaml; Find searches both.</summary>
    public static ResourceDictionary Tokens => tokens ??= new ResourceDictionary { Source = new Uri("ms-appx:///Themes/Ledger/Styles.xaml") };

    public static object? Find(string key) => Find(Tokens, key);

    private static object? Find(ResourceDictionary dictionary, string key)
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;
        foreach (var merged in dictionary.MergedDictionaries)
            if (Find(merged, key) is { } found)
                return found;
        return null;
    }

    public static bool AnimationsEnabled => Ui.AnimationsEnabled;

    public static void Apply(FrameworkElement root, AppWindow? window = null)
    {
        root.RequestedTheme = ElementTheme.Dark;
        root.HighContrastAdjustment = ElementHighContrastAdjustment.None;
        ToolTip? focusedTip = null;
        root.GotFocus += (_, args) =>
        {
            if (focusedTip is not null) focusedTip.IsOpen = false;
            focusedTip = null;
            for (var current = args.OriginalSource as DependencyObject; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is not FrameworkElement element || ToolTipService.GetToolTip(element) is not { } content)
                    continue;
                focusedTip = content as ToolTip ?? Tip([content.ToString() ?? string.Empty]);
                if (focusedTip is null) break;
                focusedTip.PlacementTarget = element;
                focusedTip.IsOpen = true;
                break;
            }
        };
        root.LostFocus += (_, _) => { if (focusedTip is not null) focusedTip.IsOpen = false; };
        if (window is null || !AppWindowTitleBar.IsCustomizationSupported()) return;
        var bar = window.TitleBar;
        bar.ButtonBackgroundColor = Color("Transparent");
        bar.ButtonInactiveBackgroundColor = Color("Transparent");
        bar.ButtonForegroundColor = Color("Ink");
        bar.ButtonInactiveForegroundColor = Color("Ink2");
        bar.ButtonHoverBackgroundColor = Color("Control");
        bar.ButtonPressedBackgroundColor = Color("ControlOn");
        bar.ButtonHoverForegroundColor = Color("Ink");
        bar.ButtonPressedForegroundColor = Color("Ink");
    }

    public static Color Color(string key) =>
        key == Paint.Transparent ? Colors.Transparent : Find("Ledger" + key + "Color") is Color color ? color : Colors.Magenta;

    public static Brush Solid(string key) => Brush(new Paint(key));

    public static Brush Brush(Paint paint)
    {
        if (Brushes.TryGetValue(paint, out var cached))
            return cached;
        Brush brush = new SolidColorBrush(Color(paint.Fill));
        Brushes[paint] = brush;
        return brush;
    }

    /// <summary>A filled element for a paint: a rectangle for a solid, a <see cref="HatchFill"/> for a hatch.</summary>
    public static FrameworkElement Surface(Paint paint) =>
        paint.Stripe is { } stripe ? new HatchFill(Color(paint.Fill), Color(stripe)) : new Microsoft.UI.Xaml.Shapes.Rectangle { Fill = Brush(paint) };

    public static string ToneKey(Tone tone) => tone switch
    {
        Tone.Ok => "Ok",
        Tone.Attention => "Att",
        Tone.Critical => "Crit",
        _ => "Neutral",
    };

    public static Brush ToneMark(Tone tone) => Solid(ToneKey(tone) + "M");

    public static Brush ToneText(Tone tone) => tone == Tone.Neutral ? Solid("Ink2") : Solid(ToneKey(tone) + "Text");

    public static Brush TonePill(Tone tone) => Solid(ToneKey(tone) + "Pill");

    /// <summary>A Ledger tooltip: first line semibold ink, the rest ink2, on the tooltip surface.</summary>
    public static ToolTip? Tip(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
            return null;
        var panel = new StackPanel { Spacing = 1 };
        for (var i = 0; i < lines.Count; i++)
            panel.Children.Add(new TextBlock
            {
                Text = lines[i],
                FontSize = 12,
                LineHeight = 17,
                FontFamily = (FontFamily)Find("LedgerSansFont")!,
                FontWeight = i == 0 ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                Foreground = Solid(i == 0 ? "Ink" : "Ink2"),
                TextWrapping = TextWrapping.NoWrap,
            });
        return new ToolTip
        {
            RequestedTheme = ElementTheme.Dark,
            HighContrastAdjustment = ElementHighContrastAdjustment.None,
            Content = panel,
            Background = Solid("Tip"),
            BorderBrush = Solid("LineWindow"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 6, 10, 6),
            MaxWidth = 320,
        };
    }

    /// <summary>Makes an element a keyboard stop that shows its tooltip and reads its lines.</summary>
    public static void AttachTip(FrameworkElement element, IReadOnlyList<string> lines)
    {
        ToolTipService.SetToolTip(element, Tip(lines));
        AutomationProperties.SetName(element, string.Join(". ", lines));
    }
}
