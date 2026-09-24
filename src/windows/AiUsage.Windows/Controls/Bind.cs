using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.Presentation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AiUsage.Controls;

/// <summary>x:Bind function helpers: visibility, token brushes by tone, and small text transforms.</summary>
internal static class Bind
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility Collapsed(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
    public static Visibility VisibleBoth(bool first, bool second) => first && second ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility VisibleText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    public static int AtLeastOne(int count) => Math.Max(1, count);
    public static bool Not(bool value) => !value;
    public static bool And(bool first, bool second) => first && second;
    public static double Opacity(bool dimmed) => dimmed ? 0.6 : 1;
    public static double Rotation(bool expanded) => expanded ? 90 : 0;
    public static string Upper(string? value) => (value ?? string.Empty).ToUpper(System.Globalization.CultureInfo.CurrentCulture);
    public static Thickness RowPadding(bool compactDensity) => compactDensity ? new Thickness(0, 7, 0, 7) : new Thickness(0, 11, 0, 11);

    /// <summary>Token brush for code-drawn controls; the app has one (dark) token dictionary (D-182).</summary>
    public static Brush Token(FrameworkElement element, string key) => (Brush)Application.Current.Resources[key];

    public static string ToneKey(ValueTone tone) => tone switch
    {
        ValueTone.Critical => "CritBrush",
        ValueTone.Warning => "WarnBrush",
        ValueTone.Muted => "Text2Brush",
        _ => "TextBrush",
    };

    public static string FillKey(ValueTone tone) => tone switch
    {
        ValueTone.Critical => "CritBrush",
        ValueTone.Warning => "WarnFillBrush",
        ValueTone.Ok => "OkFillBrush",
        _ => "FillBrush",
    };

    public static string OutcomeKey(OutcomeTone tone) => tone switch
    {
        OutcomeTone.Positive => "OkBrush",
        OutcomeTone.Critical => "CritBrush",
        _ => "Text2Brush",
    };

    public static string CriticalTextKey(bool critical) => critical ? "CritBrush" : "Text2Brush";
    public static string WarningOkKey(bool warning) => warning ? "WarnBrush" : "OkBrush";
}
