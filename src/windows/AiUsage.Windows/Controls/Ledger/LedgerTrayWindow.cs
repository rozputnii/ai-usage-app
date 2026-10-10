using AiUsage.Features.Ledger;
using AiUsage.Platform;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics;
using Windows.System;

using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// The tray flyout as a miniature of the window (R-187, T-055 R-02 to R-08): 260 wide, a title row, then per account a
/// 16 px provider mark in place of its name, its main limit's 14 px today bar and a five-hour ring, padded like a card in the
/// current density. The mark's tooltip names the account. No pills, captions, period bars or buttons. A pointer-only surface
/// (R-204): nothing in it is a tab stop or shows a focus frame, and opening it focuses nothing. A click on a row opens the
/// window at that account, Esc closes when the window receives it, and the flyout closes on deactivation.
/// </summary>
internal sealed partial class LedgerTrayWindow : Window
{
    private const int PopupWidth = 260;
    private const double MarkSize = 16;
    private const double BarHeight = 14, BarRadius = 6;
    private readonly LedgerTrayViewModel tray;
    private readonly Action<string> openAccount;
    private readonly StackPanel rows = new();
    private readonly Border header;
    private readonly Grid root;
    private bool closing;
    /// <summary>T-061 R-04: the rows changed while the miniature was hidden; it rebuilds once before it is shown.</summary>
    private bool stale = true;

    public LedgerTrayWindow(LedgerTrayViewModel tray, Action<string> openAccount)
    {
        this.tray = tray;
        this.openAccount = openAccount;
        Title = "AI Usage";
        var title = new TextBlock
        {
            Text = "AI Usage",
            FontFamily = (FontFamily)LedgerTheme.Find("LedgerSerifFont")!,
            FontSize = 14,
            LineHeight = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = LedgerTheme.Solid("Ink"),
        };
        header = new Border { BorderBrush = LedgerTheme.Solid("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Child = title };
        var stack = new StackPanel();
        stack.Children.Add(header);
        stack.Children.Add(rows);
        root = new Grid
        {
            Background = LedgerTheme.Solid("BgPage"),
            BorderBrush = LedgerTheme.Solid("LineWindow"),
            BorderThickness = new Thickness(1),
            RequestedTheme = ElementTheme.Dark,
        };
        AutomationProperties.SetName(root, "AI Usage tray");
        root.Children.Add(stack);
        root.KeyDown += OnKeyDown;
        Content = root;
        root.Loaded += (_, _) => PositionPopup();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, false);
        }
        AppWindow.IsShownInSwitchers = false;
        LedgerTheme.Apply(root);
        Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
                HidePopup();
        };
        AppWindow.Closing += (_, args) =>
        {
            if (closing)
                return;
            args.Cancel = true;
            HidePopup();
        };
        tray.Rows.CollectionChanged += (_, _) => RebuildIfShown();
        tray.PropertyChanged += (_, _) => RebuildIfShown();
    }

    /// <summary>A shown miniature follows every change; a hidden one only notes that it is out of date.</summary>
    private void RebuildIfShown()
    {
        if (AppWindow.IsVisible)
            Rebuild();
        else
            stale = true;
    }

    /// <summary>R-06: the card's padding in the current density.</summary>
    private Thickness RowPadding => tray.IsCompact ? new Thickness(12, 8, 12, 8) : new Thickness(15, 13, 15, 12);

    private void Rebuild()
    {
        stale = false;
        header.Padding = RowPadding;
        rows.Children.Clear();
        if (tray.IsEmpty)
        {
            rows.Children.Add(new TextBlock { Text = tray.EmptyText, Margin = new Thickness(14, 12, 14, 12), FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = LedgerTheme.Solid("Ink3") });
            return;
        }
        foreach (var row in tray.Rows)
            rows.Children.Add(Row(row));
    }

    private FrameworkElement Row(TrayRow row)
    {
        var grid = new Grid { ColumnSpacing = 10, Padding = RowPadding };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(MarkSize) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        // The ring column is reserved in every row, so the bars line up (R-04); the rush and extra-usage marks follow it.
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(RingGeometry.Size) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });

        // R-07: the provider mark replaces the name and the warning triangle; on an error it takes the critical text colour.
        var providerMark = ProviderMark.Create(row.Provider, LedgerTheme.Solid(row.IsError ? "CritText" : "Ink"), MarkSize);
        providerMark.VerticalAlignment = VerticalAlignment.Center;
        ToolTipService.SetToolTip(providerMark, LedgerTheme.Tip(row.Tip));
        grid.Children.Add(providerMark);

        if (row.Strip is { } strip)
        {
            var body = StripBody(strip);
            body.VerticalAlignment = VerticalAlignment.Center;
            body.Opacity = strip.Opacity;
            ToolTipService.SetToolTip(body, LedgerTheme.Tip(strip.Tip));
            AutomationProperties.SetName(body, strip.AccessibleName);
            Grid.SetColumn(body, 1);
            grid.Children.Add(body);
            if (strip.Mark != TrayMark.None)
            {
                var mark = strip.Mark == TrayMark.Rush
                    ? new Path { Data = Geometry("M6.2,0.6 L1,7 H4.4 L3.5,11.4 L8.8,5 H5.3 Z"), Fill = LedgerTheme.Solid("OkM"), Width = 10, Height = 12 }
                    : new Path { Data = Geometry("M6,0.8 A5.2,5.2 0 1 1 5.99,0.8 Z M7.6,4.2 C7.3,3.7 6.7,3.4 6,3.4 C5.1,3.4 4.4,3.9 4.4,4.6 C4.4,6.2 7.7,5.5 7.7,7.2 C7.7,7.9 7,8.5 6,8.5 C5.2,8.5 4.6,8.2 4.3,7.6 M6,2.4 V3.4 M6,8.5 V9.5"), Stroke = LedgerTheme.Solid("AttText"), StrokeThickness = 1.1, Width = 12, Height = 12 };
                mark.VerticalAlignment = VerticalAlignment.Center;
                ToolTipService.SetToolTip(mark, LedgerTheme.Tip(strip.MarkTip));
                AutomationProperties.SetName(mark, strip.Mark == TrayMark.Rush ? "rush" : "on extra usage");
                Grid.SetColumn(mark, 3);
                grid.Children.Add(mark);
            }
        }
        if (row.Ring is { } five)
        {
            // A stale reading dims the ring with the bar, as the window dims the whole card.
            var ring = new FiveHourRing { Fraction = five.Fraction, ArcBrush = LedgerTheme.ToneMark(five.Tone), VerticalAlignment = VerticalAlignment.Center, Opacity = row.Strip?.Opacity ?? 1 };
            ToolTipService.SetToolTip(ring, LedgerTheme.Tip(five.Tip));
            Grid.SetColumn(ring, 2);
            grid.Children.Add(ring);
        }

        var container = new LedgerClickRow
        {
            Content = grid, Background = LedgerTheme.Solid("Transparent"),
            BorderBrush = LedgerTheme.Solid("Line"), BorderThickness = new Thickness(0, 0, 0, 1),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            IsTabStop = false, UseSystemFocusVisuals = false,
        };
        container.Tapped += (_, _) => Open(row.AccountId);
        ToolTipService.SetToolTip(container, LedgerTheme.Tip(row.Tip));
        AutomationProperties.SetName(container, row.AccessibleName);
        return container;
    }

    private static Geometry Geometry(string data) => (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), data);

    /// <summary>R-05: the window's today strip paints at 14 px with radius 6, and the empty and used-up bodies at the same size.</summary>
    private static FrameworkElement StripBody(TrayStrip strip) => strip.Kind switch
    {
        TrayStripKind.EmptyDashed => new Rectangle { Height = BarHeight, RadiusX = BarRadius, RadiusY = BarRadius, Stroke = LedgerTheme.Solid("NeutralP"), StrokeThickness = 1, StrokeDashArray = [2, 2] },
        TrayStripKind.SolidCritical => new Border { Height = BarHeight, CornerRadius = new CornerRadius(BarRadius), Background = LedgerTheme.Solid("CritM") },
        _ => new TodayStrip { CellHeight = BarHeight, FocusableCells = false, Cells = strip.Cells },
    };

    private void Open(string accountId)
    {
        HidePopup();
        openAccount(accountId);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            HidePopup();
        }
    }

    public void ShowNearTray()
    {
        if (closing)
            return;
        // Rows first, so the popup is sized and shown with the current content.
        if (stale)
            Rebuild();
        AppWindow.Show();
        Activate();
        // XamlRoot's actual monitor scale is available only after the first show.
        DispatcherQueue.TryEnqueue(PositionPopup);
    }

    private void PositionPopup()
    {
        if (root.XamlRoot is null || closing) return;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var scale = root.XamlRoot.RasterizationScale;
        root.Measure(new Windows.Foundation.Size(PopupWidth, double.PositiveInfinity));
        // R-08: the window frame adds invisible resize borders around the client area, so add them to the size that fits the
        // content: the content is then PopupWidth wide. (AppWindow.ResizeClient counts a removed title bar and is not used.)
        var frame = new SizeInt32(AppWindow.Size.Width - AppWindow.ClientSize.Width, AppWindow.Size.Height - AppWindow.ClientSize.Height);
        var width = (int)(PopupWidth * scale) + frame.Width;
        var height = Math.Min(area.Height - 24, (int)Math.Ceiling(Math.Max(120, root.DesiredSize.Height) * scale) + frame.Height);
        AppWindow.MoveAndResize(new RectInt32(area.X + area.Width - width - (int)(12 * scale), area.Y + area.Height - height - (int)(12 * scale), width, height));
    }

    public void HidePopup() => AppWindow.Hide();

    public void CloseForExit()
    {
        closing = true;
        Close();
    }
}
