using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace AiUsage.Controls;

/// <summary>
/// Attached token keys for state-dependent colours (tone, outcome, pill). The brush is re-resolved when the element's
/// theme changes, so bound state colours follow System/Light/Dark exactly like static ThemeResource references.
/// </summary>
public sealed partial class Tokens : DependencyObject
{
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.RegisterAttached(
        "Foreground", typeof(string), typeof(Tokens), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.RegisterAttached(
        "Background", typeof(string), typeof(Tokens), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty BorderProperty = DependencyProperty.RegisterAttached(
        "Border", typeof(string), typeof(Tokens), new PropertyMetadata(null, OnChanged));
    private static readonly DependencyProperty HookedProperty = DependencyProperty.RegisterAttached(
        "Hooked", typeof(bool), typeof(Tokens), new PropertyMetadata(false));

    public static string GetForeground(DependencyObject element) => (string)element.GetValue(ForegroundProperty);
    public static void SetForeground(DependencyObject element, string value) => element.SetValue(ForegroundProperty, value);
    public static string GetBackground(DependencyObject element) => (string)element.GetValue(BackgroundProperty);
    public static void SetBackground(DependencyObject element, string value) => element.SetValue(BackgroundProperty, value);
    public static string GetBorder(DependencyObject element) => (string)element.GetValue(BorderProperty);
    public static void SetBorder(DependencyObject element, string value) => element.SetValue(BorderProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;
        if (!(bool)element.GetValue(HookedProperty))
        {
            element.SetValue(HookedProperty, true);
            element.ActualThemeChanged += (sender, _) => Apply(sender);
            element.Loaded += (sender, _) => Apply((FrameworkElement)sender);
        }
        Apply(element);
    }

    private static void Apply(FrameworkElement element)
    {
        if (GetForeground(element) is { Length: > 0 } foreground)
        {
            var brush = Bind.Token(element, foreground);
            switch (element)
            {
                case TextBlock text: text.Foreground = brush; break;
                case Control control: control.Foreground = brush; break;
                case Shape shape: shape.Fill = brush; break;
                case ContentPresenter presenter: presenter.Foreground = brush; break;
            }
        }
        if (GetBackground(element) is { Length: > 0 } background)
        {
            var brush = Bind.Token(element, background);
            switch (element)
            {
                case Border border: border.Background = brush; break;
                case Panel panel: panel.Background = brush; break;
                case Control control: control.Background = brush; break;
            }
        }
        if (GetBorder(element) is { Length: > 0 } stroke)
        {
            var brush = Bind.Token(element, stroke);
            switch (element)
            {
                case Border border: border.BorderBrush = brush; break;
                case Control control: control.BorderBrush = brush; break;
                case Shape shape: shape.Stroke = brush; break;
            }
        }
    }
}

/// <summary>Uppercases section labels (11 px, .06em) without storing uppercase copy in resources.</summary>
public sealed partial class TextCase : DependencyObject
{
    public static readonly DependencyProperty UpperProperty = DependencyProperty.RegisterAttached(
        "Upper", typeof(bool), typeof(TextCase), new PropertyMetadata(false, OnUpperChanged));

    public static bool GetUpper(DependencyObject element) => (bool)element.GetValue(UpperProperty);
    public static void SetUpper(DependencyObject element, bool value) => element.SetValue(UpperProperty, value);

    private static void OnUpperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock text || e.NewValue is not true)
            return;
        text.RegisterPropertyChangedCallback(TextBlock.TextProperty, (sender, _) => Transform((TextBlock)sender));
        text.Loaded += (sender, _) => Transform((TextBlock)sender);
        Transform(text);
    }

    private static void Transform(TextBlock text)
    {
        var upper = Bind.Upper(text.Text);
        if (!string.Equals(upper, text.Text, StringComparison.Ordinal))
            text.Text = upper;
    }
}
