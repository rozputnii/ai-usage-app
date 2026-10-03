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
/// The tray flyout as a miniature of the window (D-187, S10): 360 wide, a title row, then per account its name and one
/// 8 px today strip per limit. No pills, captions, period bars or buttons. ↑ ↓ move between accounts, ← → between strips,
/// Enter or a click opens the window at that account, Esc closes. Closes on deactivation.
/// </summary>
internal sealed partial class LedgerTrayWindow : Window
{
    private const int PopupWidth = 360;
    private readonly LedgerTrayViewModel tray;
    private readonly Action<string> openAccount;
    private readonly StackPanel rows = new();
    private readonly List<(Control Row, List<Control> Strips)> rowStops = [];
    private readonly Grid root;
    private bool closing;

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
        var header = new Border { Padding = new Thickness(14, 10, 14, 10), BorderBrush = LedgerTheme.Solid("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Child = title };
        var stack = new StackPanel();
        stack.Children.Add(header);
        stack.Children.Add(rows);
        root = new Grid
        {
            Background = LedgerTheme.Solid("BgPage"),
            BorderBrush = LedgerTheme.Solid("LineWindow"),
            BorderThickness = new Thickness(1),
            RequestedTheme = ElementTheme.Dark,
            XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled,
        };
        AutomationProperties.SetName(root, "AI Usage tray");
        root.Children.Add(stack);
        root.KeyDown += OnKeyDown;
        Content = root;
        root.Loaded += (_, _) => { PositionPopup(); FocusFirstRow(); };
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
        tray.Rows.CollectionChanged += (_, _) => Rebuild();
        tray.PropertyChanged += (_, _) => Rebuild();
        Rebuild();
    }

    private void Rebuild()
    {
        rows.Children.Clear();
        rowStops.Clear();
        if (tray.IsEmpty)
        {
            rows.Children.Add(new TextBlock { Text = tray.EmptyText, Margin = new Thickness(14, 12, 14, 12), FontSize = 12, Foreground = LedgerTheme.Solid("Ink3") });
            return;
        }
        foreach (var row in tray.Rows)
            rows.Children.Add(Row(row));
    }

    private FrameworkElement Row(TrayRow row)
    {
        var stripStops = new List<Control>();
        var grid = new Grid { ColumnSpacing = 10, RowSpacing = 5, Padding = new Thickness(14, 9, 14, 9) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        for (var i = 0; i < Math.Max(1, row.Strips.Count); i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var name = new Grid { ColumnSpacing = 5, VerticalAlignment = VerticalAlignment.Center };
        name.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        name.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (row.IsError)
            name.Children.Add(new Path
            {
                Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), "M6,1.2 L11.2,10.4 H0.8 Z M6,4.6 V7.4 M6,8.9 V9"),
                Stroke = LedgerTheme.Solid("CritText"),
                StrokeThickness = 1.3,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = 12,
                Height = 12,
            });
        var nameText = new TextBlock
        {
            Text = row.Name,
            FontFamily = (FontFamily)LedgerTheme.Find("LedgerSerifFont")!,
            FontSize = 14,
            LineHeight = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = LedgerTheme.Solid(row.IsError ? "CritText" : "Ink"),
        };
        Grid.SetColumn(nameText, 1);
        name.Children.Add(nameText);
        ToolTipService.SetToolTip(name, LedgerTheme.Tip(row.NameTip));
        Grid.SetRowSpan(name, Math.Max(1, row.Strips.Count));
        grid.Children.Add(name);

        for (var i = 0; i < row.Strips.Count; i++)
        {
            var strip = row.Strips[i];
            var body = StripBody(strip);
            var stop = new ContentControl
            {
                Content = body,
                IsTabStop = true,
                UseSystemFocusVisuals = true,
                FocusVisualPrimaryBrush = LedgerTheme.Solid("Focus"),
                FocusVisualMargin = new Thickness(-3),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = strip.Opacity,
            };
            ToolTipService.SetToolTip(stop, LedgerTheme.Tip(strip.Tip));
            stripStops.Add(stop);
            AutomationProperties.SetName(stop, strip.AccessibleName);
            var accountId = row.AccountId;
            stop.KeyDown += (_, e) =>
            {
                if (e.Key == VirtualKey.Enter)
                {
                    e.Handled = true;
                    Open(accountId);
                }
            };
            Grid.SetRow(stop, i);
            Grid.SetColumn(stop, 1);
            grid.Children.Add(stop);
            if (strip.Mark != TrayMark.None)
            {
                var mark = strip.Mark == TrayMark.Rush
                    ? new Path { Data = Geometry("M6.2,0.6 L1,7 H4.4 L3.5,11.4 L8.8,5 H5.3 Z"), Fill = LedgerTheme.Solid("OkM"), Width = 10, Height = 12 }
                    : new Path { Data = Geometry("M6,0.8 A5.2,5.2 0 1 1 5.99,0.8 Z M7.6,4.2 C7.3,3.7 6.7,3.4 6,3.4 C5.1,3.4 4.4,3.9 4.4,4.6 C4.4,6.2 7.7,5.5 7.7,7.2 C7.7,7.9 7,8.5 6,8.5 C5.2,8.5 4.6,8.2 4.3,7.6 M6,2.4 V3.4 M6,8.5 V9.5"), Stroke = LedgerTheme.Solid("AttText"), StrokeThickness = 1.1, Width = 12, Height = 12 };
                mark.VerticalAlignment = VerticalAlignment.Center;
                ToolTipService.SetToolTip(mark, LedgerTheme.Tip(strip.MarkTip));
                AutomationProperties.SetName(mark, strip.Mark == TrayMark.Rush ? "rush" : "on extra usage");
                Grid.SetRow(mark, i);
                Grid.SetColumn(mark, 2);
                grid.Children.Add(mark);
            }
        }

        var container = new ContentControl
        {
            Content = grid, Background = LedgerTheme.Solid("Transparent"),
            BorderBrush = LedgerTheme.Solid("Line"), BorderThickness = new Thickness(0, 0, 0, 1),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            IsTabStop = true, UseSystemFocusVisuals = true, FocusVisualPrimaryBrush = LedgerTheme.Solid("Focus"),
            FocusVisualSecondaryThickness = new Thickness(0), FocusVisualPrimaryThickness = new Thickness(2),
        };
        container.Tapped += (_, _) => Open(row.AccountId);
        container.KeyDown += (_, args) =>
        {
            if (args.Key != VirtualKey.Enter) return;
            args.Handled = true;
            Open(row.AccountId);
        };
        ToolTipService.SetToolTip(container, LedgerTheme.Tip(row.NameTip));
        AutomationProperties.SetName(container, row.AccessibleName);
        rowStops.Add((container, stripStops));
        return container;
    }

    private static Geometry Geometry(string data) => (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), data);

    private static FrameworkElement StripBody(TrayStrip strip) => strip.Kind switch
    {
        TrayStripKind.EmptyDashed => new Rectangle { Height = 8, RadiusX = 4, RadiusY = 4, Stroke = LedgerTheme.Solid("NeutralP"), StrokeThickness = 1, StrokeDashArray = [2, 2] },
        TrayStripKind.SolidCritical => new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = LedgerTheme.Solid("CritM") },
        _ => new TodayStrip { CellHeight = 8, FocusableCells = false, Cells = strip.Cells },
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
        else if (e.Key is VirtualKey.Up or VirtualKey.Down or VirtualKey.Left or VirtualKey.Right)
        {
            var focused = FocusManager.GetFocusedElement(root.XamlRoot);
            var index = rowStops.FindIndex(r => ReferenceEquals(r.Row, focused) || r.Strips.Any(s => ReferenceEquals(s, focused)));
            if (index < 0) return;
            e.Handled = true;
            if (e.Key is VirtualKey.Up or VirtualKey.Down)
                rowStops[Math.Clamp(index + (e.Key == VirtualKey.Up ? -1 : 1), 0, rowStops.Count - 1)].Row.Focus(FocusState.Keyboard);
            else
            {
                var stops = rowStops[index].Strips;
                var current = stops.FindIndex(s => ReferenceEquals(s, focused));
                if (stops.Count > 0)
                    stops[Math.Clamp(current + (e.Key == VirtualKey.Left ? -1 : 1), 0, stops.Count - 1)].Focus(FocusState.Keyboard);
            }
        }
    }

    public void ShowNearTray()
    {
        if (closing)
            return;
        AppWindow.Show();
        Activate();
        DispatcherQueue.TryEnqueue(() =>
        {
            // XamlRoot's actual monitor scale is available only after the first show.
            PositionPopup();
            FocusFirstRow();
        });
    }

    private void FocusFirstRow()
    {
        if (rowStops.Count > 0)
            rowStops[0].Row.Focus(FocusState.Keyboard);
    }

    private void PositionPopup()
    {
        if (root.XamlRoot is null || closing) return;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var scale = root.XamlRoot.RasterizationScale;
        root.Measure(new Windows.Foundation.Size(PopupWidth, double.PositiveInfinity));
        var height = Math.Min(area.Height - 24, (int)Math.Ceiling(Math.Max(120, root.DesiredSize.Height + 2) * scale));
        var width = (int)(PopupWidth * scale);
        AppWindow.MoveAndResize(new RectInt32(area.X + area.Width - width - (int)(12 * scale), area.Y + area.Height - height - (int)(12 * scale), width, height));
    }

    public void HidePopup() => AppWindow.Hide();

    public void CloseForExit()
    {
        closing = true;
        Close();
    }
}
