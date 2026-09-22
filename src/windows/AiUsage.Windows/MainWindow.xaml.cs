using AiUsage.Features.Accounts;
using AiUsage.Features.Demo;
using AiUsage.Features.History;
using AiUsage.Features.Overview;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.Settings;
using AiUsage.Features.Shell;
using AiUsage.Features.SystemStatusPage;
using AiUsage.Features.Tray;
using AiUsage.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using Windows.UI;

namespace AiUsage;

/// <summary>Native window: title bar, top navigation shell, page frame, tray icon and close-to-tray. State lives in view models.</summary>
internal sealed partial class MainWindow : Window
{
    private readonly NavigationService navigation;
    private readonly ThemeService theme;
    private readonly DisplaySimulation display;
    private readonly IUsageSource usage;
    private readonly IServiceProvider services;
    private readonly Dictionary<PageKey, (Type Page, object ViewModel)> pages;
    private IDisposable? densitySubscription;
    private RectInt32? passthrough;
    private bool finalClose;

    public MainWindow(ShellViewModel shell, TrayViewModel tray, RecoveryViewModel recovery, OverviewViewModel overview, AccountsViewModel accounts,
        ProviderHistoryViewModel history, SettingsViewModel settings, SystemStatusViewModel systemStatus, NavigationService navigation, ThemeService theme, DisplaySimulation display,
        IUsageSource usage, IServiceProvider services)
    {
        Shell = shell;
        Tray = tray;
        Recovery = recovery;
        Demo = services.GetService<DemoControlViewModel>();
        this.navigation = navigation;
        this.theme = theme;
        this.display = display;
        this.usage = usage;
        this.services = services;
        pages = new()
        {
            [PageKey.Overview] = (typeof(OverviewPage), overview),
            [PageKey.Accounts] = (typeof(AccountsPage), accounts),
            [PageKey.History] = (typeof(HistoryPage), history),
            [PageKey.Settings] = (typeof(SettingsPage), settings),
            [PageKey.SystemStatus] = (typeof(SystemStatusPage), systemStatus),
        };
        InitializeComponent();

        Title = services.GetRequiredService<ITextResources>().Get("AppTitle");
        AppWindow.SetIcon((string?)null);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        AppWindow.Resize(new SizeInt32(1100, 860));
        theme.Attach(RootGrid, AppWindow);

        navigation.Navigated += (_, request) => DispatcherQueue.TryEnqueue(() => ShowPage(request));
        ShowPage(new NavigationRequest(navigation.Current));
        display.ContentScaleChanged += (_, _) => ApplyScale();
        AppLayout.Current.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppLayout.IsCompact))
                ApplyCompactHeader();
        };
        RootGrid.SizeChanged += (_, _) => UpdateLayoutState();
        AppWindow.Changed += (_, _) => UpdateTitleBarRegions();
        AppWindow.Closing += OnClosing;
        tray.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrayViewModel.WorstAttention))
                UpdateTrayGlyph();
        };
        densitySubscription = usage.Subscribe(snapshot => DispatcherQueue.TryEnqueue(() => AppLayout.Current.CompactDensity = snapshot.Preferences.Density == Density.Compact));
        AppLayout.Current.CompactDensity = usage.Current.Preferences.Density == Density.Compact;
        UpdateTrayGlyph();
        Closed += (_, _) => densitySubscription?.Dispose();
    }

    public ShellViewModel Shell { get; }
    public TrayViewModel Tray { get; }
    public RecoveryViewModel Recovery { get; }
    public DemoControlViewModel? Demo { get; }

    public TextBlock LiveRegionElement => LiveRegion;
    public FrameworkElement Root => RootGrid;

    private string BannerBackground(bool security) => security ? "CritBgBrush" : "WarnBgBrush";
    private string BannerBorder(bool security) => security ? "CritStrokeBrush" : "WarnStrokeBrush";
    private string BannerGlyph(bool security) => security ? "CritBrush" : "WarnBrush";
    private string ResultGlyph(bool failures) => failures ? "▲" : "✓";
    private string ResultBrush(bool failures) => failures ? "WarnBrush" : "OkBrush";

    private void ShowPage(NavigationRequest request)
    {
        if (!pages.TryGetValue(request.Page, out var target))
            return;
        if (ContentFrame.Content?.GetType() != target.Page)
        {
            ContentFrame.Navigate(target.Page, target.ViewModel, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
            // A page built while the demo simulates a contrast theme resolved the ordinary colours; ask for a re-resolution.
            theme.RefreshContrast(RootGrid);
        }
        else if (ContentFrame.Content is UIElement page && request.Tab is null && request.AccountId is null)
            Controls.Motion.Rise(page);
    }

    private void OnNavClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && Enum.TryParse<PageKey>(tag, out var page))
            Shell.NavigateCommand.Execute(page);
    }

    private void OnExitAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        _ = Shell.ExitCommand.ExecuteAsync(null);
    }

    private void OnRefreshAllAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (Shell.RefreshAllCommand.CanExecute(null))
            _ = Shell.RefreshAllCommand.ExecuteAsync(null);
    }

    private void OnBackAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (Shell.GoBackCommand.CanExecute(null))
            Shell.GoBackCommand.Execute(null);
    }

    /// <summary>Compact layout below 720 effective pixels of content width, measured after the demo content scale.</summary>
    private void UpdateLayoutState()
    {
        var width = RootGrid.ActualWidth / Math.Max(1, Scaler.ContentScale);
        AppLayout.Current.IsCompact = width < 720;
        UpdateTitleBarRegions();
    }

    private void ApplyScale()
    {
        Scaler.ContentScale = display.ContentScale;
        UpdateLayoutState();
    }

    private void ApplyCompactHeader()
    {
        var compact = AppLayout.Current.IsCompact;
        var gutter = compact ? 18 : 40;
        HeaderHost.Margin = new Thickness(gutter, 0, gutter, 0);
        BannerHost.Margin = new Thickness(gutter, 0, gutter, 0);
        Grid.SetRow(HeaderActions, compact ? 1 : 0);
        Grid.SetColumn(HeaderActions, compact ? 0 : 1);
        HeaderActions.Padding = compact ? new Thickness(0, 2, 0, 10) : new Thickness(0, 6, 0, 10);
    }

    /// <summary>The demo marker in the title bar stays clickable; the rest of the bar drags the window.</summary>
    private void UpdateTitleBarRegions()
    {
        if (AppWindow is null || Content?.XamlRoot is null)
            return;
        var scale = Content.XamlRoot.RasterizationScale;
        var padding = new Thickness(14, 0, AppWindow.TitleBar.RightInset / scale, 0);
        // Setting these unconditionally would invalidate layout from a layout callback and loop.
        if (AppTitleBar.Padding != padding)
            AppTitleBar.Padding = padding;
        var source = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        if (DemoMarker.Visibility != Visibility.Visible || DemoMarker.ActualWidth <= 0)
        {
            if (passthrough is not null)
            {
                passthrough = null;
                source.ClearRegionRects(NonClientRegionKind.Passthrough);
            }
            return;
        }
        var bounds = DemoMarker.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, DemoMarker.ActualWidth, DemoMarker.ActualHeight));
        var physical = new RectInt32((int)(bounds.X * scale), (int)(bounds.Y * scale), (int)Math.Ceiling(bounds.Width * scale), (int)Math.Ceiling(bounds.Height * scale));
        if (passthrough is { } current && current.X == physical.X && current.Y == physical.Y && current.Width == physical.Width && current.Height == physical.Height)
            return;
        passthrough = physical;
        source.SetRegionRects(NonClientRegionKind.Passthrough, [physical]);
    }

    /// <summary>D9: the tray mark reflects the most urgent attention level; the tooltip names cause, account and freshness.</summary>
    private void UpdateTrayGlyph()
    {
        TrayGlyph.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Tray.WorstAttention switch
        {
            AttentionLevel.ReauthRequired or AttentionLevel.Failure or AttentionLevel.Exhausted or AttentionLevel.Critical => Color.FromArgb(255, 0xB3, 0x30, 0x1E),
            AttentionLevel.Warning => Color.FromArgb(255, 0xE8, 0x81, 0x1C),
            AttentionLevel.Stale => Color.FromArgb(255, 0xB2, 0x5A, 0x00),
            _ => Color.FromArgb(255, 0xC8, 0x68, 0x4A),
        });
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (finalClose)
            return;
        args.Cancel = true;
        services.GetRequiredService<AppLifetime>().HideToTray();
    }

    /// <summary>Final close after a confirmed Exit: removes the tray icon so no notification-area ghost remains.</summary>
    public void CloseForExit()
    {
        finalClose = true;
        TrayIcon.Dispose();
        Close();
    }
}
