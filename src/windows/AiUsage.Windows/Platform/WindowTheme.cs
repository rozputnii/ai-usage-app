using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace AiUsage.Platform;

/// <summary>
/// The app has one appearance, the designed dark palette (D-182). Window roots request the dark theme so framework controls
/// match the tokens, and the title bar buttons use the dark colours.
/// </summary>
internal static class WindowTheme
{
    public static void Apply(FrameworkElement root, AppWindow? window)
    {
        root.RequestedTheme = ElementTheme.Dark;
        if (window is null || !AppWindowTitleBar.IsCustomizationSupported())
            return;
        var bar = window.TitleBar;
        bar.ButtonBackgroundColor = Colors.Transparent;
        bar.ButtonInactiveBackgroundColor = Colors.Transparent;
        bar.ButtonForegroundColor = Color.FromArgb(255, 0xF3, 0xF1, 0xEC);
        bar.ButtonInactiveForegroundColor = Color.FromArgb(255, 0xB8, 0xB2, 0xA7);
        bar.ButtonHoverBackgroundColor = Color.FromArgb(0x33, 0xF3, 0xF1, 0xEC);
        bar.ButtonHoverForegroundColor = bar.ButtonForegroundColor;
        bar.ButtonPressedBackgroundColor = Color.FromArgb(0x4D, 0xF3, 0xF1, 0xEC);
        bar.ButtonPressedForegroundColor = bar.ButtonForegroundColor;
    }
}
