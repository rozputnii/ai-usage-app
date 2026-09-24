using AiUsage.Features.Accounts;
using AiUsage.Features.Connection;
using AiUsage.Features.Demo;
using AiUsage.Features.History;
using AiUsage.Features.Overview;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.Settings;
using AiUsage.Features.Shell;
using AiUsage.Features.Tray;
using AiUsage.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using Windows.UI;

namespace AiUsage;

/// <summary>
/// Native window: title bar, one usage view with a header (Back, Refresh all, Add account menu, Settings), the view
/// frame, the inline sign-in strip, tray icon and close-to-tray. State lives in view models.
/// </summary>
internal sealed partial class MainWindow : Window
{
    private readonly NavigationService navigation;
    private readonly DisplaySimulation display;
    private readonly IUsageSource usage;
    private readonly IServiceProvider services;
    private readonly Dictionary<PageKey, (Type Page, object ViewModel)> pages;
    private IDisposable? densitySubscription;
    private RectInt32? passthrough;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? hoverClose;
    private bool openedByHover;
    private bool pointerOverMenu;
    private bool reopenAfterClose;
    private bool finalClose;

    /// <summary>D-181: the compact usage view needs little room; effective pixels, wide enough to keep settings unstacked.</summary>
    private const int DefaultWidth = 760;
    private const int DefaultHeight = 600;

    public MainWindow(ShellViewModel shell, TrayViewModel tray, RecoveryViewModel recovery, OverviewViewModel overview, AccountsViewModel accounts,
        ProviderHistoryViewModel history, SettingsViewModel settings, AddAccountViewModel addAccount, NavigationService navigation, DisplaySimulation display,
        IUsageSource usage, IServiceProvider services)
    {
        Shell = shell;
        AddAccount = addAccount;
        Tray = tray;
        Recovery = recovery;
        Demo = services.GetService<DemoControlViewModel>();
        this.navigation = navigation;
        this.display = display;
        this.usage = usage;
        this.services = services;
        pages = new()
        {
            [PageKey.Overview] = (typeof(OverviewPage), overview),
            [PageKey.Accounts] = (typeof(AccountsPage), accounts),
            [PageKey.History] = (typeof(HistoryPage), history),
            [PageKey.Settings] = (typeof(SettingsPage), settings),
        };
        InitializeComponent();
        // Button marks pointer input handled, so the hover menu listens for handled events too.
        AddAccountButton.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(OnAddAccountPointerEntered), handledEventsToo: true);
        AddAccountButton.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnMenuPointerExited), handledEventsToo: true);
        ProviderMenu.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(OnMenuPointerEntered), handledEventsToo: true);
        ProviderMenu.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnMenuPointerExited), handledEventsToo: true);
        // A click on the button while the hover menu shows must reach the button instead of only dismissing the menu.
        ProviderFlyout.OverlayInputPassThroughElement = AddAccountButton;

        Title = services.GetRequiredService<ITextResources>().Get("AppTitle");
        AppWindow.SetIcon((string?)null);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        AppWindow.Resize(new SizeInt32(DefaultWidth, DefaultHeight));
        RootGrid.Loaded += ApplyDefaultSizeForScale;
        WindowTheme.Apply(RootGrid, AppWindow);

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
        addAccount.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AddAccountViewModel.IsCode) && addAccount.IsCode)
                DispatcherQueue.TryEnqueue(() => CodeBox.Focus(FocusState.Programmatic));
        };
        UpdateTrayGlyph();
        Closed += (_, _) => densitySubscription?.Dispose();
    }

    public ShellViewModel Shell { get; }
    public AddAccountViewModel AddAccount { get; }
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
    private string SettingsBackground(bool open) => open ? "Card2Brush" : "BtnBrush";
    private string StripBackground(bool note, NoteTone tone) => note && tone == NoteTone.Critical ? "CritBgBrush" : "Card2Brush";
    private string StripBorder(bool note, NoteTone tone) => note && tone == NoteTone.Critical ? "CritStrokeBrush" : "StrokeBrush";
    private string CodeBorder(bool error) => error ? "CritBrush" : "Stroke2Brush";
    private string ProviderIdOf(ProviderOptionViewModel? provider) => provider?.ProviderId ?? string.Empty;

    /// <summary>AppWindow sizes are physical pixels; once the display scale is known, apply the default in effective pixels.</summary>
    private void ApplyDefaultSizeForScale(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= ApplyDefaultSizeForScale;
        var scale = RootGrid.XamlRoot?.RasterizationScale ?? 1;
        if (Math.Abs(scale - 1) > 0.01)
            AppWindow.Resize(new SizeInt32((int)(DefaultWidth * scale), (int)(DefaultHeight * scale)));
    }

    private void ShowPage(NavigationRequest request)
    {
        if (!pages.TryGetValue(request.Page, out var target))
            return;
        if (ContentFrame.Content?.GetType() != target.Page)
        {
            ContentFrame.Navigate(target.Page, target.ViewModel, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
        }
        else if (ContentFrame.Content is UIElement page && request.Tab is null && request.AccountId is null)
            Controls.Motion.Rise(page);
    }

    /// <summary>Hovering Add account opens the provider menu without taking focus.</summary>
    private void OnAddAccountPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        pointerOverMenu = true;
        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch || ProviderFlyout.IsOpen)
            return;
        openedByHover = true;
        ProviderFlyout.ShowAt(AddAccountButton, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
    }

    /// <summary>A hover-opened menu closes shortly after the pointer leaves both the button and the menu.</summary>
    private void OnMenuPointerExited(object sender, PointerRoutedEventArgs e)
    {
        pointerOverMenu = false;
        if (!openedByHover)
            return;
        hoverClose ??= CreateHoverCloseTimer();
        hoverClose.Start();
    }

    private void OnMenuPointerEntered(object sender, PointerRoutedEventArgs e) => pointerOverMenu = true;

    private Microsoft.UI.Dispatching.DispatcherQueueTimer CreateHoverCloseTimer()
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(400);
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            if (openedByHover && !pointerOverMenu)
                ProviderFlyout.Hide();
        };
        return timer;
    }

    /// <summary>Click, tap, Enter and Space open the same menu with keyboard focus inside; it then stays until dismissed.</summary>
    private void OnAddAccountClick(object sender, RoutedEventArgs e)
    {
        openedByHover = false;
        if (ProviderFlyout.IsOpen && ProviderFlyout.ShowMode == FlyoutShowMode.Standard)
        {
            FocusFirstProvider();
            return;
        }
        // A hover-opened menu is transient and light-dismisses on this press; show it again as a standard flyout.
        reopenAfterClose = true;
        if (ProviderFlyout.IsOpen)
            ProviderFlyout.Hide();
        else
            ShowStandardMenu();
    }

    private void ShowStandardMenu() =>
        ProviderFlyout.ShowAt(AddAccountButton, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Standard, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });

    private void OnProviderFlyoutOpened(object? sender, object e)
    {
        if (ProviderFlyout.ShowMode != FlyoutShowMode.Standard)
            return;
        reopenAfterClose = false;
        FocusFirstProvider();
    }

    private void OnProviderFlyoutClosed(object? sender, object e)
    {
        openedByHover = false;
        if (!reopenAfterClose)
            return;
        reopenAfterClose = false;
        DispatcherQueue.TryEnqueue(ShowStandardMenu);
    }

    private void FocusFirstProvider() =>
        DispatcherQueue.TryEnqueue(() =>
        {
            if (FocusManager.FindFirstFocusableElement(ProviderMenu) is Control first)
                first.Focus(FocusState.Keyboard);
        });

    private void OnProviderClick(object sender, RoutedEventArgs e)
    {
        ProviderFlyout.Hide();
        if (((FrameworkElement)sender).Tag is ProviderOptionViewModel option)
            _ = AddAccount.ConnectCommand.ExecuteAsync(option);
    }

    private void OnImportCliClick(object sender, RoutedEventArgs e)
    {
        ProviderFlyout.Hide();
        AddAccount.OpenCliImportCommand.Execute(null);
    }

    private void OnSimulatorClick(object sender, RoutedEventArgs e) =>
        AddAccount.SimulateCommand.Execute(((FrameworkElement)sender).Tag as SimulatorOption);

    private void OnCopyDeviceCode(object sender, RoutedEventArgs e)
    {
        if (!AddAccount.HasDeviceCode)
            return;
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(AddAccount.DeviceUserCode);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        services.GetRequiredService<IAnnouncer>().Announce(services.GetRequiredService<ITextResources>().Get("Announce_CodeCopied"));
    }

    private void OnCodeKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && AddAccount.SubmitCodeCommand.CanExecute(null))
        {
            e.Handled = true;
            _ = AddAccount.SubmitCodeCommand.ExecuteAsync(null);
        }
    }

    /// <summary>Esc returns from settings, history or account detail to the usage view.</summary>
    private void OnEscapeAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (Shell.IsOverview || !Shell.GoBackCommand.CanExecute(null))
            return;
        args.Handled = true;
        Shell.GoBackCommand.Execute(null);
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
        // Narrow windows keep one header row: Refresh all shrinks to its icon (its accessible name stays the label).
        RefreshAllText.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
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
