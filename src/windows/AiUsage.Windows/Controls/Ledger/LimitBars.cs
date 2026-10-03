using AiUsage.Features.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// The today strip (spec 6.3): one rounded cell per 5h window, or one cell, each split into used, allowed and grey parts.
/// Each cell is a keyboard stop with its tooltip; arrow keys move between cells inside the card.
/// </summary>
internal sealed partial class TodayStrip : Grid
{
    private IReadOnlyList<StripCell> displayedCells = [];
    public static readonly DependencyProperty CellsProperty = DependencyProperty.Register(nameof(Cells), typeof(IReadOnlyList<StripCell>), typeof(TodayStrip), new PropertyMetadata(null, (d, _) => ((TodayStrip)d).Rebuild()));
    public static readonly DependencyProperty CellHeightProperty = DependencyProperty.Register(nameof(CellHeight), typeof(double), typeof(TodayStrip), new PropertyMetadata(14.0, (d, _) => ((TodayStrip)d).Rebuild()));
    public static readonly DependencyProperty FocusableCellsProperty = DependencyProperty.Register(nameof(FocusableCells), typeof(bool), typeof(TodayStrip), new PropertyMetadata(true, (d, _) => ((TodayStrip)d).Rebuild()));

    public IReadOnlyList<StripCell>? Cells { get => (IReadOnlyList<StripCell>?)GetValue(CellsProperty); set => SetValue(CellsProperty, value); }
    public double CellHeight { get => (double)GetValue(CellHeightProperty); set => SetValue(CellHeightProperty, value); }
    public bool FocusableCells { get => (bool)GetValue(FocusableCellsProperty); set => SetValue(FocusableCellsProperty, value); }

    private void Rebuild()
    {
        var oldWidths = displayedCells.Select((cell, i) =>
        {
            var width = i < Children.Count && Children[i] is FrameworkElement element ? element.ActualWidth : 0;
            var total = cell.Parts.Sum(p => p.Weight);
            return cell.Parts.Select(p => total > 0 ? (double?)(width * p.Weight / total) : null).ToArray();
        }).ToArray();
        Children.Clear();
        ColumnDefinitions.Clear();
        var cells = Cells ?? [];
        displayedCells = cells;
        var small = CellHeight <= 10;
        ColumnSpacing = small ? 3 : 4;
        Height = CellHeight;
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.001, cell.Weight), GridUnitType.Star) });
            var element = BuildCell(cell, small, i < oldWidths.Length ? oldWidths[i] : []);
            SetColumn(element, i);
            Children.Add(element);
        }
    }

    private FrameworkElement BuildCell(StripCell cell, bool small, double?[] oldWidths)
    {
        var radius = small ? 4 : 6;
        var parts = new Grid();
        for (var i = 0; i < cell.Parts.Count; i++)
        {
            var part = cell.Parts[i];
            parts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(part.Weight, GridUnitType.Star) });
            var fill = LedgerTheme.Surface(part.Paint);
            SetColumn(fill, i);
            parts.Children.Add(fill);
            LedgerMotion.WidthOnLoad(fill, i < oldWidths.Length ? oldWidths[i] : null);
            if (part.OverEdge)
            {
                var edge = new Rectangle { Width = 2, HorizontalAlignment = HorizontalAlignment.Left, Fill = LedgerTheme.Solid("Ink") };
                SetColumn(edge, i);
                parts.Children.Add(edge);
            }
        }
        var body = new Grid { CornerRadius = new CornerRadius(radius) };
        body.Children.Add(new Border { CornerRadius = new CornerRadius(radius), Child = parts });
        if (cell.Dashed)
            body.Children.Add(new Rectangle
            {
                Stroke = LedgerTheme.Solid("Prev"),
                StrokeThickness = 1,
                StrokeDashArray = [2, 2],
                RadiusX = radius,
                RadiusY = radius,
            });
        if (cell.ShowLabel && !small)
        {
            var left = Cells is { Count: > 0 } all && ReferenceEquals(all[0], cell);
            body.Children.Add(new Border
            {
                Background = LedgerTheme.Solid("CellLabel"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 0, 4, 0),
                Height = 12,
                Margin = new Thickness(2, 1, 2, 1),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                IsHitTestVisible = false,
                Child = new TextBlock { Text = "5h", FontSize = 10, LineHeight = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = LedgerTheme.Solid("Ink"), FontFamily = (FontFamily)LedgerTheme.Find("LedgerSansSemiboldFont")! },
            });
        }
        if (!FocusableCells)
        {
            ToolTipService.SetToolTip(body, LedgerTheme.Tip(cell.Tip));
            return body;
        }
        var stop = new ContentControl
        {
            Content = body,
            IsTabStop = true,
            UseSystemFocusVisuals = true,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            FocusVisualPrimaryBrush = LedgerTheme.Solid("Focus"),
            FocusVisualPrimaryThickness = new Thickness(2),
            FocusVisualMargin = new Thickness(-2),
        };
        LedgerTheme.AttachTip(stop, cell.Tip);
        return stop;
    }
}

/// <summary>
/// The 7d or month bar (spec 6.3): an 8 px track with prior use, today's span, the over part, a ring around today's span,
/// cap and over ticks and 5h dividers. Positions are percent of the width and arrive mirrored for Left mode.
/// </summary>
internal sealed partial class PeriodBar : Canvas
{
    private double[] previousWidths = [];
    public static readonly DependencyProperty VisualProperty = DependencyProperty.Register(nameof(Visual), typeof(CardVisual), typeof(PeriodBar), new PropertyMetadata(null, (d, _) => ((PeriodBar)d).Rebuild()));

    public PeriodBar()
    {
        Height = 20;
        SizeChanged += (_, _) => Rebuild();
    }

    public CardVisual? Visual { get => (CardVisual?)GetValue(VisualProperty); set => SetValue(VisualProperty, value); }

    private void Rebuild()
    {
        Children.Clear();
        var visual = Visual;
        var width = ActualWidth;
        if (visual is null || width <= 0)
            return;
        double X(double percent) => Math.Round(width * percent / 100, 2);
        var track = new Canvas { Width = width, Height = 8, Background = LedgerTheme.Solid("Rail") };
        track.Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, width, 8) };
        var trackBorder = new Border { Width = width, Height = 8, CornerRadius = new CornerRadius(4), Child = track };
        SetTop(trackBorder, 6);
        Children.Add(trackBorder);
        var segments = visual.Segments.Where(s => s.Width > 0.001).ToArray();
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var rect = LedgerTheme.Surface(segment.Paint);
            rect.Width = Math.Max(0, X(segment.Width));
            rect.Height = 8;
            SetLeft(rect, X(segment.Left));
            track.Children.Add(rect);
            LedgerMotion.WidthOnLoad(rect, i < previousWidths.Length ? previousWidths[i] : null);
        }
        previousWidths = [.. segments.Select(s => Math.Max(0, X(s.Width)))];
        foreach (var divider in visual.Dividers)
        {
            var rect = new Rectangle { Width = 2, Height = 8, Fill = LedgerTheme.Solid("Card") };
            SetLeft(rect, X(divider) - 1);
            track.Children.Add(rect);
        }
        if (visual.Ring is { } ring)
        {
            var box = new Border
            {
                Width = Math.Max(8, X(ring.Width) + 8),
                Height = 16,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(7),
                BorderBrush = LedgerTheme.ToneMark(ring.Tone),
                IsHitTestVisible = false,
            };
            SetLeft(box, X(ring.Left) - 4);
            SetTop(box, 2);
            Children.Add(box);
        }
        if (visual.CapTick is { } cap)
            Children.Add(Tick(X(cap), 3, 14, "Ink3"));
        if (visual.OverTick is { } over)
            Children.Add(Tick(X(over), 4, 12, "Ink"));
    }

    private static Rectangle Tick(double x, double top, double height, string key)
    {
        var tick = new Rectangle { Width = 2, Height = height, RadiusX = 1, RadiusY = 1, Fill = LedgerTheme.Solid(key), IsHitTestVisible = false };
        SetLeft(tick, x - 1);
        SetTop(tick, top);
        return tick;
    }
}

/// <summary>Inline history chart (S4): day bars, dashed gaps, reset ticks, a dashed baseline and the focused day.</summary>
internal sealed partial class HistoryChart : Canvas
{
    private const double Plot = 80;
    private const double PlotTop = 20;
    public static readonly DependencyProperty HistoryProperty = DependencyProperty.Register(nameof(History), typeof(LedgerHistoryViewModel), typeof(HistoryChart), new PropertyMetadata(null, (d, _) => ((HistoryChart)d).Attach()));

    public HistoryChart()
    {
        Height = 118;
        SizeChanged += (_, _) => Rebuild();
    }

    public LedgerHistoryViewModel? History { get => (LedgerHistoryViewModel?)GetValue(HistoryProperty); set => SetValue(HistoryProperty, value); }

    private LedgerHistoryViewModel? attached;

    private void Attach()
    {
        if (attached is not null)
            attached.PropertyChanged -= OnChanged;
        attached = History;
        if (attached is not null)
            attached.PropertyChanged += OnChanged;
        Rebuild();
    }

    private void OnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        Children.Clear();
        var history = History;
        var width = ActualWidth;
        if (history is null || width <= 0 || history.DayCount == 0)
            return;
        var column = width / history.DayCount;
        var axis = new Rectangle { Width = width, Height = 1, Fill = LedgerTheme.Solid("LineWindow") };
        SetTop(axis, PlotTop + Plot);
        Children.Add(axis);
        if (history.Baseline is { } baseline)
        {
            var y = PlotTop + Plot - baseline * Plot;
            Children.Add(new Line { X1 = 0, X2 = width, Y1 = y, Y2 = y, Stroke = LedgerTheme.Solid("NeutralP"), StrokeThickness = 1, StrokeDashArray = [3, 3] });
            var label = Text(history.BaselineText, "Ink3");
            label.Padding = new Thickness(0, 0, 6, 0);
            var labelBox = new Border { Background = LedgerTheme.Solid("Hist"), Child = label };
            SetTop(labelBox, y - 14);
            Children.Add(labelBox);
        }
        foreach (var tick in history.ResetTicks)
        {
            var mark = new Rectangle { Width = 1, Height = 12, Fill = LedgerTheme.Solid("Ink3") };
            SetLeft(mark, tick * column);
            SetTop(mark, PlotTop + Plot - 6);
            Children.Add(mark);
        }
        foreach (var bar in history.Bars)
        {
            var height = bar.Height * Plot;
            var focused = bar.Index == history.FocusIndex;
            var rect = new Rectangle
            {
                Width = Math.Max(1, column - 3),
                Height = height,
                RadiusX = 2,
                RadiusY = 2,
                Fill = LedgerTheme.Solid(bar.IsToday ? "OkM" : focused ? "Ink" : "Prev"),
            };
            SetLeft(rect, bar.Index * column + 1.5);
            SetTop(rect, PlotTop + Plot - height);
            Children.Add(rect);
        }
        foreach (var gap in history.Gaps)
        {
            var box = new Border
            {
                Width = Math.Max(1, gap.Count * column - 3),
                Height = 60,
                BorderThickness = new Thickness(1, 1, 1, 0),
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Child = new TextBlock { Text = "no\nreadings", FontSize = 12, LineHeight = 14, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0), Foreground = LedgerTheme.Solid("Ink3") },
            };
            var dashed = new Rectangle { Width = box.Width, Height = 60, Stroke = LedgerTheme.Solid("NeutralP"), StrokeDashArray = [3, 3], StrokeThickness = 1, RadiusX = 4, RadiusY = 4 };
            SetLeft(box, gap.Start * column + 1.5);
            SetTop(box, PlotTop + Plot - 60);
            SetLeft(dashed, gap.Start * column + 1.5);
            SetTop(dashed, PlotTop + Plot - 60);
            Children.Add(dashed);
            Children.Add(box);
        }
        var focus = new Border
        {
            Background = LedgerTheme.Solid("Tip"),
            BorderBrush = LedgerTheme.Solid("LineWindow"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(8, 1, 8, 1),
            Child = Text(history.FocusText, "Ink"),
        };
        focus.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        SetLeft(focus, Math.Clamp(history.FocusIndex * column + column / 2 - focus.DesiredSize.Width / 2, 0, Math.Max(0, width - focus.DesiredSize.Width)));
        SetTop(focus, 0);
        Children.Add(focus);
        foreach (var label in history.Labels)
        {
            var text = Text(label.Text, label.IsToday ? "OkText" : "Ink3");
            if (label.IsToday)
            {
                text.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                text.FontFamily = (FontFamily)LedgerTheme.Find("LedgerSansSemiboldFont")!;
                text.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                SetLeft(text, width - text.DesiredSize.Width);
            }
            else
                SetLeft(text, label.Index * column + 3);
            SetTop(text, PlotTop + Plot + 4);
            Children.Add(text);
        }
        AutomationProperties.SetName(this, history.AccessibleName);
    }

    private static TextBlock Text(string text, string key) => new()
    {
        Text = text,
        FontSize = 12,
        LineHeight = 14,
        Foreground = LedgerTheme.Solid(key),
        FontFamily = (FontFamily)LedgerTheme.Find("LedgerSansFont")!,
    };
}
