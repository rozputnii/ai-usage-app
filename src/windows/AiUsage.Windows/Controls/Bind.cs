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
    public static Visibility VisibleEither(bool first, bool second) => first || second ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility VisibleText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    public static Visibility VisibleCount(int count) => count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public static bool Not(bool value) => !value;
    public static bool And(bool first, bool second) => first && second;
    public static double Opacity(bool dimmed) => dimmed ? 0.6 : 1;
    public static double Rotation(bool expanded) => expanded ? 90 : 0;
    public static string Upper(string? value) => (value ?? string.Empty).ToUpper(System.Globalization.CultureInfo.CurrentCulture);
    public static Thickness RowPadding(bool compactDensity) => compactDensity ? new Thickness(0, 7, 0, 7) : new Thickness(0, 11, 0, 11);

    /// <summary>Token lookup that honours the element's actual theme (Application resources resolve against the app theme).</summary>
    public static Brush Token(FrameworkElement element, string key)
    {
        var dictionary = Application.Current.Resources;
        var themeKey = element.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        foreach (var merged in dictionary.MergedDictionaries)
            if (merged.ThemeDictionaries.TryGetValue(themeKey, out var theme) && theme is ResourceDictionary themeDictionary && themeDictionary.TryGetValue(key, out var value) && value is Brush brush)
            {
                if (IsSystemHighContrast() && merged.ThemeDictionaries.TryGetValue("HighContrast", out var contrast) && contrast is ResourceDictionary hc && hc.TryGetValue(key, out var hcValue) && hcValue is Brush hcBrush)
                    return hcBrush;
                return brush;
            }
        return (Brush)dictionary[key];
    }

    private static readonly Windows.UI.ViewManagement.AccessibilitySettings Accessibility = new();
    public static bool IsSystemHighContrast() => Accessibility.HighContrast;

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

    public static bool IsPage(PageKey current, int index) => (int)current == index;
    public static bool IsAddAccountTab(AddAccountTab tab, int index) => (int)tab == index;
    public static Visibility VisibleForTab(AddAccountTab tab, int index) => (int)tab == index ? Visibility.Visible : Visibility.Collapsed;
    public static string NoteBackground(NoteTone tone) => tone == NoteTone.Critical ? "CritBgBrush" : "Card2Brush";
    public static string NoteBorder(NoteTone tone) => tone == NoteTone.Critical ? "CritStrokeBrush" : "StrokeBrush";
    public static string ThemeId(ThemePreference preference) => "Theme" + preference;

    private static readonly Windows.UI.Color[] LightPreview = [Windows.UI.Color.FromArgb(255, 0xF6, 0xF4, 0xEF), Windows.UI.Color.FromArgb(255, 0xEE, 0xEB, 0xE4), Windows.UI.Color.FromArgb(255, 0x1D, 0x1B, 0x18), Windows.UI.Color.FromArgb(0x1A, 0x1D, 0x1B, 0x18)];
    private static readonly Windows.UI.Color[] DarkPreview = [Windows.UI.Color.FromArgb(255, 0x1F, 0x1E, 0x1B), Windows.UI.Color.FromArgb(255, 0x31, 0x30, 0x2C), Windows.UI.Color.FromArgb(255, 0xF3, 0xF1, 0xEC), Windows.UI.Color.FromArgb(0x1F, 0xF3, 0xF1, 0xEC)];

    /// <summary>Theme card miniatures always show the theme they represent, independent of the current app theme.</summary>
    public static Brush ThemePreview(bool dark, int part) => new SolidColorBrush((dark ? DarkPreview : LightPreview)[part]);
    public static double SelectedOpacity(bool selected) => selected ? 1 : 0;
    public static string CriticalTextKey(bool critical) => critical ? "CritBrush" : "Text2Brush";
    public static string WarningOkKey(bool warning) => warning ? "WarnBrush" : "OkBrush";

    public static double ValueSize(MeterKind kind) => kind == MeterKind.Bar ? 22 : 15;
}
