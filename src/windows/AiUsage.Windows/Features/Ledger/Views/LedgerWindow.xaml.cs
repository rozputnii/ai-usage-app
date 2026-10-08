using System.Collections.Specialized;
using AiUsage.Controls.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Platform;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.System;

namespace AiUsage.Features.Ledger.Views;

/// <summary>
/// The redesigned main window (AIU-038): title row with Work today, Used/Left, + and Settings; the sign-in strip; the card
/// grid with inline history; the inline settings panel; the undo bar; and the tray icon whose flyout is the miniature.
/// Closing hides to the tray; Exit is in the tray menu.
/// </summary>
internal sealed partial class LedgerWindow : Window
{
    /// <summary>Until the content loads; then the window opens at its content-based minimum width (D-197).</summary>
    private const int InitialWidth = 560;
    private const int DefaultHeight = 600;
    /// <summary>The shortest body without a card (first run, recovery, starting).</summary>
    private const double EmptyBodyMinHeight = 160;
    private readonly Dictionary<LimitCardViewModel, LedgerCardView> views = [];
    private readonly HistoryPanel historyPanel = new();
    private readonly Func<Task> exit;
    private readonly Action showTray;
    private bool finalClose;
    private bool refreshHovered;
    private Storyboard? stripWave;
    private Storyboard? settingsSlide;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? copyReset;

    public LedgerWindow(LedgerViewModel viewModel, Func<Task> exit, Action showTray)
    {
        ViewModel = viewModel;
        this.exit = exit;
        this.showTray = showTray;
        ShowTrayCommand = new RelayCommand(showTray);
        OpenWindowCommand = new RelayCommand(ShowAndActivate);
        ExitCommand = new AsyncRelayCommand(exit);
        InitializeComponent();
        Title = "AI Usage";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleRow);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        AppWindow.Resize(new SizeInt32(InitialWidth, DefaultHeight));
        LedgerTheme.Apply(Root, AppWindow);
        Root.Loaded += OnLoaded;
        Root.SizeChanged += (_, _) => UpdateTitleBarRegions();
        TitleControls.SizeChanged += (_, _) => UpdateTitleBarRegions();
        DayGroup.SizeChanged += (_, _) => UpdateTitleBarRegions();
        CardGrid.SizeChanged += (_, _) => UpdateMinimumSize();
        Body.SizeChanged += (_, e) => SettingsPanel.Height = e.NewSize.Height;
        AppWindow.Closing += OnClosing;

        AddAccelerator(VirtualKey.Escape, VirtualKeyModifiers.None, () => ViewModel.Escape());
        AddAccelerator((VirtualKey)188, VirtualKeyModifiers.Control, () => { ViewModel.ToggleSettings(); return true; });
        AddAccelerator(VirtualKey.N, VirtualKeyModifiers.Control, () =>
        {
            if (ViewModel.CanUseAccounts) AddButton.Flyout?.ShowAt(AddButton);
            else ViewModel.OpenSettings();
            return true;
        });
        AddAccelerator(VirtualKey.Z, VirtualKeyModifiers.Control, () => { var handled = ViewModel.HasUndo; _ = ViewModel.UndoAsync(); return handled; });
        AddAccelerator(VirtualKey.F5, VirtualKeyModifiers.None, () => { _ = ViewModel.RefreshAsync(); return true; });
        AddAccelerator(VirtualKey.Q, VirtualKeyModifiers.Control, () => { _ = exit(); return true; });

        ViewModel.Cards.CollectionChanged += OnCardsChanged;
        ViewModel.PropertyChanged += OnViewModelChanged;
        ViewModel.FocusCardRequested += (_, cardId) => DispatcherQueue.TryEnqueue(() => FocusCard(cardId));
        RebuildGrid();
        if (ViewModel.IsSettingsOpen)
            SlideSettings();
        ApplyPreferences();
        UpdateRefreshOpacity();
        UpdateTrayGlyph();
        UpdateStripWave();
    }

    public LedgerViewModel ViewModel { get; }
    public IRelayCommand ShowTrayCommand { get; }
    public IRelayCommand OpenWindowCommand { get; }
    public IAsyncRelayCommand ExitCommand { get; }

    private void AddAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, Func<bool> action)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += (_, args) =>
        {
            // Text boxes keep Esc and Ctrl+Z for themselves.
            if (key is VirtualKey.Escape or VirtualKey.Z && FocusManager.GetFocusedElement(Root.XamlRoot) is TextBox)
                return;
            args.Handled = action();
        };
        Root.KeyboardAccelerators.Add(accelerator);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // The default width is the minimum one, which this measures from the title row.
        UpdateTitleBarRegions();
        var scale = Root.XamlRoot?.RasterizationScale ?? 1;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var width = (AppWindow.Presenter as OverlappedPresenter)?.PreferredMinimumWidth ?? Math.Min((int)(InitialWidth * scale), area.Width);
        var height = Math.Min((int)(DefaultHeight * scale), area.Height);
        // Keep the entire window reachable at high DPI; the card scroller handles the shorter viewport.
        var x = Math.Clamp(AppWindow.Position.X, area.X, area.X + area.Width - width);
        var y = Math.Clamp(AppWindow.Position.Y, area.Y, area.Y + area.Height - height);
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        UpdateTitleBarRegions();
    }

    /// <summary>The title row drags the window; its controls stay clickable through passthrough regions.</summary>
    private void UpdateTitleBarRegions()
    {
        if (Root.XamlRoot is null)
            return;
        var scale = Root.XamlRoot.RasterizationScale;
        var inset = AppWindow.TitleBar.RightInset / scale;
        var padding = new Thickness(16, 0, 8 + inset, 0);
        if (TitleRow.Padding != padding)
            TitleRow.Padding = padding;
        var rects = new List<RectInt32>();
        foreach (var element in new FrameworkElement[] { RefreshButton, TitleControls, DayGroup })
        {
            if (element.Visibility != Visibility.Visible || element.ActualWidth <= 0)
                continue;
            var bounds = element.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
            rects.Add(new RectInt32((int)(bounds.X * scale), (int)(bounds.Y * scale), (int)Math.Ceiling(bounds.Width * scale), (int)Math.Ceiling(bounds.Height * scale)));
        }
        InputNonClientPointerSource.GetForWindowId(AppWindow.Id).SetRegionRects(NonClientRegionKind.Passthrough, [.. rects]);
        UpdateMinimumSize();
    }

    /// <summary>
    /// The narrowest window keeps the title row's controls clear of the caption buttons; the shortest still shows the first
    /// card's header and primary limit. Both follow what the title row and the first card show now.
    /// </summary>
    private void UpdateMinimumSize()
    {
        if (Root.XamlRoot is null || AppWindow.Presenter is not OverlappedPresenter presenter)
            return;
        var scale = Root.XamlRoot.RasterizationScale;
        // Auto columns measure their content unconstrained, so desired widths stay natural while the window is narrow.
        var width = TitleRow.Padding.Left + TitleRow.Padding.Right + TitleRow.ColumnSpacing * (TitleRow.ColumnDefinitions.Count - 1)
            + TitleRow.Children.Sum(e => e.DesiredSize.Width);
        var first = CardGrid.Children.OfType<LedgerCardView>().FirstOrDefault();
        var body = CardScroller.Visibility == Visibility.Visible && first is { ActualHeight: > 0 }
            ? CardGrid.Padding.Top * 2 + first.SingleLimitHeight : EmptyBodyMinHeight;
        var height = TitleRow.ActualHeight + (SignInStrip.Visibility == Visibility.Visible ? SignInStrip.Height : 0) + body;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var frame = new SizeInt32(AppWindow.Size.Width - AppWindow.ClientSize.Width, AppWindow.Size.Height - AppWindow.ClientSize.Height);
        var minWidth = Math.Min((int)Math.Ceiling(width * scale) + frame.Width, area.Width);
        var minHeight = Math.Min((int)Math.Ceiling(height * scale) + frame.Height, area.Height);
        if (presenter.PreferredMinimumWidth == minWidth && presenter.PreferredMinimumHeight == minHeight)
            return;
        presenter.PreferredMinimumWidth = minWidth;
        presenter.PreferredMinimumHeight = minHeight;
        // A wider title (Work today) or a sign-in strip raises the minimum; grow a window that is already smaller.
        var size = AppWindow.Size;
        if (presenter.State == OverlappedPresenterState.Restored && (size.Width < minWidth || size.Height < minHeight))
            AppWindow.Resize(new SizeInt32(Math.Max(size.Width, minWidth), Math.Max(size.Height, minHeight)));
    }

    private void OnViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LedgerViewModel.History):
                var historyWasOpen = historyPanel.History?.CardId == ViewModel.History?.CardId;
                RebuildGrid();
                if (ViewModel.History is not null && !historyWasOpen)
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        historyPanel.StartBringIntoView();
                        historyPanel.Focus(FocusState.Programmatic);
                        LedgerMotion.FadeIn(historyPanel, 250);
                    });
                break;
            case nameof(LedgerViewModel.SectionLayout):
                RebuildGrid();
                break;
            case nameof(LedgerViewModel.IsSettingsOpen):
                DispatcherQueue.TryEnqueue(SlideSettings);
                break;
            case nameof(LedgerViewModel.HasStrip):
                if (ViewModel.HasStrip) DispatcherQueue.TryEnqueue(() => LedgerMotion.FadeIn(SignInStrip, 150));
                DispatcherQueue.TryEnqueue(UpdateMinimumSize);
                UpdateStripWave();
                break;
            case nameof(LedgerViewModel.RefreshFailed):
                UpdateRefreshOpacity();
                break;
            case nameof(LedgerViewModel.StripBusy):
                UpdateStripWave();
                break;
            case nameof(LedgerViewModel.StripCode):
                ResetCopyIcon();
                break;
            case nameof(LedgerViewModel.HasUndo):
                if (ViewModel.HasUndo) DispatcherQueue.TryEnqueue(() => LedgerMotion.FadeIn(UndoBar, 150));
                break;
            case nameof(LedgerViewModel.IsDayOff):
                DispatcherQueue.TryEnqueue(UpdateTitleBarRegions);
                break;
            case nameof(LedgerViewModel.IsLeft):
            case nameof(LedgerViewModel.IsCompact):
            case nameof(LedgerViewModel.ShowSignedOut):
            case nameof(LedgerViewModel.Preferences):
                ApplyPreferences();
                break;
        }
        UpdateTrayGlyph();
    }

    private void ApplyPreferences()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.IsAlwaysOnTop = ViewModel.Preferences.AlwaysOnTop;
    }

    /// <summary>
    /// The settings sheet drops down over the body and rolls back up, leaving the cards in place (D-197). While it covers
    /// them, the cards behind leave the tab order.
    /// </summary>
    private void SlideSettings()
    {
        var open = ViewModel.IsSettingsOpen;
        var from = SettingsHost.Visibility == Visibility.Visible ? SettingsHost.ActualHeight : 0;
        settingsSlide?.Stop();
        SettingsHost.Visibility = Visibility.Visible;
        if (!open)
            CoverBody(false);
        settingsSlide = LedgerMotion.SlideHeight(SettingsHost, from, open ? Body.ActualHeight : 0, () =>
        {
            if (ViewModel.IsSettingsOpen)
            {
                // Open, the host follows the sheet, which follows the body as the window resizes.
                SettingsHost.ClearValue(FrameworkElement.HeightProperty);
                CoverBody(true);
            }
            else
                SettingsHost.Visibility = Visibility.Collapsed;
        });
    }

    private void CoverBody(bool covered)
    {
        CardScroller.IsEnabled = !covered;
        FirstRunScroller.IsEnabled = !covered;
    }

    private void OnCardsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildGrid();

    /// <summary>Card views follow the view-model order; history goes right after its card.</summary>
    private void RebuildGrid()
    {
        var wanted = new List<UIElement>();
        var openIndex = -1;
        // A retained series may move between the grid and an account section as windows appear/disappear.
        // Detach hosts even on views that are about to leave the grid.
        foreach (var view in views.Values)
            view.Sections = [];
        foreach (var card in ViewModel.Cards)
        {
            if (!views.TryGetValue(card, out var view))
                views[card] = view = new LedgerCardView { ViewModel = card };
        }
        // One account is one card (D-191): its primary limit hosts every shown section; hidden sections are left out.
        foreach (var card in ViewModel.Cards.Where(c => !c.IsAccountSection))
        {
            var view = views[card];
            var sections = ViewModel.Cards.Where(c => c.Account.AccountId == card.Account.AccountId && c.IsAccountSection)
                .OrderBy(c => AccountCard.SectionRank(c.Model)).ToArray();
            foreach (var section in sections)
                CardGrid.Children.Remove(views[section]);
            sections = [.. sections.Where(s => !s.IsHiddenSection)];
            view.Sections = [.. sections.Select(s => views[s])];
            wanted.Add(view);
            if (card.IsHistoryOpen || sections.Any(s => s.IsHistoryOpen))
                openIndex = wanted.Count - 1;
        }
        foreach (var gone in views.Keys.Except(ViewModel.Cards).ToArray())
            views.Remove(gone);
        historyPanel.History = ViewModel.History;
        if (ViewModel.History is not null && openIndex >= 0)
            wanted.Insert(openIndex + 1, historyPanel);
        if (CardGrid.Children.SequenceEqual(wanted))
            return;
        CardGrid.Children.Clear();
        foreach (var element in wanted)
            CardGrid.Children.Add(element);
    }

    private void FocusCard(string cardId)
    {
        var view = views.FirstOrDefault(v => v.Key.CardId == cardId).Value;
        if (view is null)
            return;
        view.StartBringIntoView();
        view.Focus(FocusState.Keyboard);
    }

    /// <summary>True while the window is closed to the tray or not yet shown; AIU-046 installs updates only then.</summary>
    public bool IsHidden { get; private set; } = true;
    public event EventHandler? TrayVisibilityChanged;

    /// <summary>
    /// A `--background` relaunch after an update: the tray icon without the window. The icon only answers clicks once the
    /// window content has loaded (TaskbarIcon.ForceCreate alone left it inert in Sandbox), so activate and hide at once.
    /// </summary>
    public void StartHidden()
    {
        Activate();
        AppWindow.Hide();
    }

    public void ShowAndActivate()
    {
        AppWindow.Show();
        AiUsage.Composition.ApplicationDiagnostics.Current?.WindowVisibility(false);
        SetHidden(false);
        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();
        Activate();
    }

    public void Announce(string message)
    {
        var peer = FrameworkElementAutomationPeer.FromElement(Root) ?? FrameworkElementAutomationPeer.CreatePeerForElement(Root);
        peer?.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.MostRecent, message, "LedgerStatus");
    }

    // ---- Title row ----

    private void OnRefreshPointerEntered(object sender, PointerRoutedEventArgs e) { refreshHovered = true; UpdateRefreshOpacity(); }

    private void OnRefreshPointerExited(object sender, PointerRoutedEventArgs e) { refreshHovered = false; UpdateRefreshOpacity(); }

    /// <summary>Refresh stays quiet like the card icons, and opaque while pointed at or red.</summary>
    private void UpdateRefreshOpacity()
    {
        if (refreshHovered || ViewModel.RefreshFailed)
            RefreshButton.Opacity = 1;
        else
            RefreshButton.ClearValue(UIElement.OpacityProperty);
    }

    private void OnUsedClick(object sender, RoutedEventArgs e) => _ = ViewModel.SetValueModeAsync(ValueMode.Used);

    private void OnLeftClick(object sender, RoutedEventArgs e) => _ = ViewModel.SetValueModeAsync(ValueMode.Left);

    private void OnProviderMenuClick(object sender, RoutedEventArgs e)
    {
        ProviderFlyout.Hide();
        if (((FrameworkElement)sender).Tag is ProviderItem item)
            _ = ViewModel.SignInAsync(item.Provider);
    }

    private void OnShowSignedOutClick(object sender, RoutedEventArgs e) => _ = ViewModel.ToggleShowSignedOutAsync();

    private void OnDemoClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Demo is not { } demo)
            return;
        var menu = new MenuFlyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
        foreach (var scenario in demo.Scenarios)
        {
            var item = new MenuFlyoutItem { Text = scenario.Title };
            var id = scenario.Id;
            item.Click += (_, _) => ViewModel.LoadScenario(id);
            menu.Items.Add(item);
        }
        if (demo.FailSignIn is { } fail)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
            var failure = new MenuFlyoutItem { Text = "Failed sign-in" };
            failure.Click += (_, _) => fail();
            menu.Items.Add(failure);
        }
        var miniature = new MenuFlyoutItem { Text = "Tray miniature" };
        miniature.Click += (_, _) => DispatcherQueue.TryEnqueue(() => showTray());
        menu.Items.Add(miniature);
        menu.ShowAt(DemoButton);
    }

    // ---- Sign-in strip ----

    /// <summary>The waiting wave runs only while a visible sign-in waits on the user.</summary>
    private void UpdateStripWave()
    {
        var run = ViewModel.HasStrip && ViewModel.StripBusy && LedgerTheme.AnimationsEnabled;
        if (run == (stripWave is not null))
            return;
        if (stripWave is { } wave)
        {
            wave.Stop();
            stripWave = null;
            return;
        }
        stripWave = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        foreach (var (target, property, from, to) in new (DependencyObject, string, double, double)[]
        {
            (StripWaveScale, "ScaleX", 1, 3.7), (StripWaveScale, "ScaleY", 1, 3.7), (StripWave, "Opacity", 0.7, 0),
        })
        {
            var animation = new DoubleAnimation
            {
                From = from, To = to, Duration = TimeSpan.FromMilliseconds(1600),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            stripWave.Children.Add(animation);
        }
        Composition.ApplicationDiagnostics.RunAnimation(stripWave.Begin);
    }

    private void OnCopyCodeClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.StripCode.Length == 0)
            return;
        var package = new DataPackage();
        package.SetText(ViewModel.StripCode);
        Clipboard.SetContent(package);
        CopyCodeIcon.Glyph = "\uE73E";
        Announce("Code copied");
        copyReset?.Stop();
        copyReset = DispatcherQueue.CreateTimer();
        copyReset.Interval = TimeSpan.FromSeconds(1.5);
        copyReset.IsRepeating = false;
        copyReset.Tick += (_, _) => ResetCopyIcon();
        copyReset.Start();
    }

    private void ResetCopyIcon()
    {
        copyReset?.Stop();
        copyReset = null;
        CopyCodeIcon.Glyph = "\uE8C8";
    }

    private void OnUndoFocus(object sender, RoutedEventArgs e) => ViewModel.HoldUndo(true);

    private void OnUndoBlur(object sender, RoutedEventArgs e) => ViewModel.HoldUndo(false);

    // ---- Tray ----

    /// <summary>The tray mark takes the most urgent card colour; drawn synchronously as in the current tray (D9).</summary>
    private void UpdateTrayGlyph()
    {
        var tones = ViewModel.Cards.Select(c => c.Visual.Tone).ToArray();
        var key = tones.Contains(Tone.Critical) ? "CritM" : tones.Contains(Tone.Attention) ? "AttM" : "OkM";
        try
        {
            TrayIcon.Icon = TrayGlyph.Create(LedgerTheme.Color(key));
        }
        catch (Exception error) when (error is System.Runtime.InteropServices.ExternalException or InvalidOperationException or ArgumentException or OutOfMemoryException)
        {
            Composition.ApplicationDiagnostics.Current?.TrayFailure(error);
        }
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (finalClose)
            return;
        args.Cancel = true;
        AppWindow.Hide();
        Composition.ApplicationDiagnostics.Current?.WindowVisibility(true);
        SetHidden(true);
    }

    private void SetHidden(bool hidden)
    {
        if (IsHidden == hidden) return;
        IsHidden = hidden;
        TrayVisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CloseForExit()
    {
        finalClose = true;
        TrayIcon.Dispose();
        Close();
    }

    // ---- x:Bind helpers ----

    private Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private Visibility Hide(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
    private Visibility ShowText(string? value) => Show(!string.IsNullOrEmpty(value));
    private Brush StripDot(Tone tone, bool busy) => busy ? LedgerTheme.Solid("WaitM") : LedgerTheme.ToneMark(tone);
    // A rejected code replaces the hint, keeping the strip one line. Without animation the still dot cannot show that the
    // app is waiting, so the line says it.
    private string StripDetail(string text, bool busy, string error) =>
        error.Length > 0 ? error : busy && !LedgerTheme.AnimationsEnabled ? (text.Length > 0 ? text + " · waiting" : "waiting") : text;
    private Brush StripDetailBrush(string error) => error.Length > 0 ? LedgerTheme.ToneText(Tone.Attention) : LedgerTheme.Solid("Ink3");
    private Style StripActionStyle(bool primary) => (Style)LedgerTheme.Find(primary ? "LedgerPrimaryButton" : "LedgerLinkButton")!;
    private Style WorkTodayStyle(bool on) => (Style)LedgerTheme.Find(on ? "LedgerSegmentOnButton" : "LedgerOutlineButton")!;
    private string WorkTodayName(bool on) => on ? "Work today, on until midnight" : "Work today, off";
    private Brush SegmentBackground(bool isLeft, bool forLeft) => LedgerTheme.Solid(isLeft == forLeft ? "ControlOn" : "Transparent");
    private Brush SegmentForeground(bool isLeft, bool forLeft) => LedgerTheme.Solid(isLeft == forLeft ? "Ink" : "Ink3");
    private Brush SettingsBackground(bool open) => LedgerTheme.Solid(open ? "ControlOn" : "Transparent");
    private Brush RefreshBrush(bool failed) => failed ? LedgerTheme.ToneText(Tone.Critical) : LedgerTheme.Solid("Ink");
    private string RefreshName(IReadOnlyList<string> tip) => "Refresh. " + LedgerViews.Spoken(tip);
    private double GridGap(bool compact) => compact ? 8 : 12;
    private Thickness BodyPadding(bool compact, bool undo) => new(compact ? 12 : 14, compact ? 12 : 14, compact ? 12 : 14, undo ? 66 : compact ? 12 : 14);
    private string UndoName(string text) => "Undo: " + text;
}
