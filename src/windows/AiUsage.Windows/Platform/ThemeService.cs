using System.ComponentModel;
using AiUsage.Features.Presentation;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace AiUsage.Platform;

/// <summary>
/// System / Light / Dark. System follows Windows immediately; Light and Dark ignore Windows changes. Real Windows contrast
/// themes are honoured by the framework's HighContrast dictionary. The demo shell can simulate the Windows app mode and a
/// contrast theme; the simulated contrast swaps the token dictionary and re-resolves every ThemeResource reference.
/// </summary>
internal sealed class ThemeService : IThemeService
{
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly DispatcherQueue queue;
    private readonly List<(FrameworkElement Root, AppWindow? Window)> roots = [];
    private ResourceDictionary? tokens;
    private ResourceDictionary? simulated;
    private EffectiveTheme? simulatedSystem;
    private bool simulatedContrast;

    public ThemeService(DispatcherQueue queue)
    {
        this.queue = queue;
        // Desktop apps cannot subscribe to AccessibilitySettings.HighContrastChanged; a contrast theme change also raises ColorValuesChanged.
        settings.ColorValuesChanged += (_, _) => queue.TryEnqueue(Update);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ThemePreference Preference { get; private set; }

    public EffectiveTheme SystemTheme => simulatedSystem ?? (settings.GetColorValue(UIColorType.Background).R < 128 ? EffectiveTheme.Dark : EffectiveTheme.Light);

    public EffectiveTheme Effective => Preference switch
    {
        ThemePreference.Light => EffectiveTheme.Light,
        ThemePreference.Dark => EffectiveTheme.Dark,
        _ => SystemTheme,
    };

    public bool HighContrast => accessibility.HighContrast || simulatedContrast;

    public ElementTheme ElementTheme => Effective == EffectiveTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

    public EffectiveTheme? SimulatedSystemTheme
    {
        get => simulatedSystem;
        set
        {
            simulatedSystem = value;
            Update();
        }
    }

    public bool SimulatedHighContrast
    {
        get => simulatedContrast;
        set
        {
            if (simulatedContrast == value)
                return;
            simulatedContrast = value;
            ApplySimulatedContrast(value);
            Update();
        }
    }

    public void Attach(FrameworkElement root, AppWindow? window)
    {
        roots.Add((root, window));
        root.RequestedTheme = ElementTheme;
        RefreshContrast(root);
        UpdateTitleBar(window);
    }

    public void Detach(FrameworkElement root) => roots.RemoveAll(r => r.Root == root);

    public void Apply(ThemePreference preference)
    {
        Preference = preference;
        Update();
    }

    private void Update()
    {
        if (!queue.HasThreadAccess)
        {
            queue.TryEnqueue(Update);
            return;
        }
        var theme = ElementTheme;
        foreach (var (root, window) in roots.ToArray())
        {
            if (root.RequestedTheme != theme)
                root.RequestedTheme = theme;
            UpdateTitleBar(window);
        }
        PropertyChanged?.Invoke(this, new(nameof(SystemTheme)));
        PropertyChanged?.Invoke(this, new(nameof(Effective)));
        PropertyChanged?.Invoke(this, new(nameof(HighContrast)));
    }

    private void UpdateTitleBar(AppWindow? window)
    {
        if (window is null || !AppWindowTitleBar.IsCustomizationSupported())
            return;
        var dark = Effective == EffectiveTheme.Dark || simulatedContrast;
        var bar = window.TitleBar;
        bar.ButtonBackgroundColor = Colors.Transparent;
        bar.ButtonInactiveBackgroundColor = Colors.Transparent;
        bar.ButtonForegroundColor = dark ? Color.FromArgb(255, 0xF3, 0xF1, 0xEC) : Color.FromArgb(255, 0x1D, 0x1B, 0x18);
        bar.ButtonInactiveForegroundColor = dark ? Color.FromArgb(255, 0xB8, 0xB2, 0xA7) : Color.FromArgb(255, 0x6B, 0x66, 0x5E);
        bar.ButtonHoverBackgroundColor = dark ? Color.FromArgb(0x33, 0xF3, 0xF1, 0xEC) : Color.FromArgb(0x1A, 0x1D, 0x1B, 0x18);
        bar.ButtonHoverForegroundColor = bar.ButtonForegroundColor;
        bar.ButtonPressedBackgroundColor = dark ? Color.FromArgb(0x4D, 0xF3, 0xF1, 0xEC) : Color.FromArgb(0x33, 0x1D, 0x1B, 0x18);
        bar.ButtonPressedForegroundColor = bar.ButtonForegroundColor;
    }

    /// <summary>
    /// Swaps the whole token dictionary for the specification's contrast stand-ins (demo only) and lets a theme change
    /// re-resolve every ThemeResource reference, which is what a real Windows contrast switch does.
    /// </summary>
    private void ApplySimulatedContrast(bool enabled)
    {
        var merged = Application.Current.Resources.MergedDictionaries;
        var index = IndexOfTokens(merged);
        if (index < 0)
            return;
        tokens ??= merged[index];
        simulated ??= new ResourceDictionary { Source = new Uri("ms-appx:///Themes/SimulatedHighContrast.xaml") };
        merged[index] = enabled ? simulated : tokens;
        foreach (var (root, _) in roots.ToArray())
            Reresolve(root);
    }

    /// <summary>
    /// Content created while the simulation is on still resolves the colours cached at startup, so pages, dialogs and the
    /// tray popup ask for a re-resolution once they are built.
    /// </summary>
    public void RefreshContrast(FrameworkElement element)
    {
        if (simulatedContrast)
            Reresolve(element);
    }

    /// <summary>
    /// A theme change is what makes resolved ThemeResource references read the swapped dictionary again. Both themes carry
    /// the contrast stand-ins while the simulation is on, so the pass through the other theme shows no flash.
    /// </summary>
    private void Reresolve(FrameworkElement element)
    {
        element.RequestedTheme = ElementTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        // Low priority runs after the framework has processed the first change, so code-drawn controls see both of them.
        queue.TryEnqueue(DispatcherQueuePriority.Low, () => element.RequestedTheme = ElementTheme);
    }

    private int IndexOfTokens(IList<ResourceDictionary> merged)
    {
        for (var i = 0; i < merged.Count; i++)
            if (ReferenceEquals(merged[i], tokens) || ReferenceEquals(merged[i], simulated)
                || (merged[i].ThemeDictionaries.TryGetValue("Light", out var light) && light is ResourceDictionary theme && theme.ContainsKey("AppBgBrush")))
                return i;
        return -1;
    }
}
