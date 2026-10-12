using AiUsage.Features.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// Runs a usage bar's fill (<see cref="BarSweep"/>): it remembers the edges on screen, so the next fill starts from them,
/// or from empty when the bar has not been shown yet or is shown again. Apply draws a frame's edges; Finish restores the
/// bar's own layout once the edges are in place.
/// </summary>
internal sealed class BarFill(Action<double[]> apply, Action finish)
{
    private double[]? shown;
    private double[] target = [];
    private Action? stop;

    /// <summary>The edges on screen; null before the bar was first shown.</summary>
    public double[]? Shown => shown;

    public bool IsRunning => stop is not null;

    public void Show(double[] target)
    {
        stop?.Invoke();
        stop = null;
        this.target = target;
        var sweep = new BarSweep(shown ?? [], target);
        if (!LedgerTheme.AnimationsEnabled || sweep.Duration <= 0)
        {
            Snap(target);
            return;
        }
        // The first frame is drawn now, so the bar never shows its end state before the fill starts.
        Step(sweep.At(0));
        stop = LedgerMotion.Frames(sweep.Duration, elapsed =>
        {
            Step(sweep.At(elapsed));
            if (elapsed < sweep.Duration)
                return;
            stop = null;
            finish();
        });

        void Step(double[] edges)
        {
            shown = edges;
            apply(edges);
        }
    }

    /// <summary>Shows the target at once, stopping a running fill (a resize, or no system animations).</summary>
    public void Snap(double[] target)
    {
        stop?.Invoke();
        stop = null;
        this.target = target;
        shown = target;
        finish();
    }

    /// <summary>Ends a running fill at its target: the bar left the screen or lost its width.</summary>
    public void Stop()
    {
        if (IsRunning)
            Snap(target);
    }

    /// <summary>The next fill starts from these edges, as a bar that replaces another one continues its fill.</summary>
    public void Seed(double[] edges)
    {
        if (!IsRunning)
            shown = edges;
    }

    /// <summary>The next fill starts from empty.</summary>
    public void Forget()
    {
        stop?.Invoke();
        stop = null;
        shown = null;
    }

    /// <summary>Fills every usage bar under the root from empty, as when the window or the tray opens.</summary>
    public static void Replay(DependencyObject root)
    {
        if (root is TodayStrip strip)
            strip.ReplayFill();
        else if (root is PeriodBar bar)
            bar.ReplayFill();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            Replay(VisualTreeHelper.GetChild(root, i));
    }
}

/// <summary>
/// The today strip (spec 6.3): one rounded cell per 5h window, or one cell, each split into used, allowed and grey parts.
/// Each cell is a keyboard stop with its tooltip; arrow keys move between cells inside the card. The parts fill as one
/// strip from the left (<see cref="BarFill"/>): while they move, their star columns become pixel widths.
/// </summary>
internal sealed partial class TodayStrip : Grid
{
    /// <summary>Each part's column with its star weight, its cell and its part of the fill.</summary>
    private readonly List<(ColumnDefinition Column, double Weight, int Cell, int Part)> partColumns = [];
    private readonly BarFill fill;
    /// <summary>The fill's end edges and each cell's first part and bounds, as fractions of the width, for the current size.</summary>
    private double[] target = [];
    private (int First, double Left, double Right)[] cellBounds = [];
    /// <summary>Whether the strip has a width; the first width after none fills it, later ones keep the fill where it ends.</summary>
    private bool laidOut;
    public static readonly DependencyProperty CellsProperty = DependencyProperty.Register(nameof(Cells), typeof(IReadOnlyList<StripCell>), typeof(TodayStrip), new PropertyMetadata(null, (d, _) => ((TodayStrip)d).Rebuild()));
    public static readonly DependencyProperty CellHeightProperty = DependencyProperty.Register(nameof(CellHeight), typeof(double), typeof(TodayStrip), new PropertyMetadata(14.0, (d, _) => ((TodayStrip)d).Rebuild()));
    public static readonly DependencyProperty FocusableCellsProperty = DependencyProperty.Register(nameof(FocusableCells), typeof(bool), typeof(TodayStrip), new PropertyMetadata(true, (d, _) => ((TodayStrip)d).Rebuild()));

    public TodayStrip()
    {
        fill = new BarFill(Apply, Finish);
        SizeChanged += (_, _) =>
        {
            if (ActualWidth <= 0)
            {
                fill.Stop();
                laidOut = false;
                return;
            }
            Measure();
            // A resize during a fill (the tray sizing itself as it opens) lets the fill go on to the new places.
            if (!laidOut || fill.IsRunning) fill.Show(target);
            else fill.Snap(target);
            laidOut = true;
        };
        Unloaded += (_, _) => fill.Stop();
    }

    public IReadOnlyList<StripCell>? Cells { get => (IReadOnlyList<StripCell>?)GetValue(CellsProperty); set => SetValue(CellsProperty, value); }
    public double CellHeight { get => (double)GetValue(CellHeightProperty); set => SetValue(CellHeightProperty, value); }
    public bool FocusableCells { get => (bool)GetValue(FocusableCellsProperty); set => SetValue(FocusableCellsProperty, value); }

    /// <summary>The edges on screen, for a strip that replaces this one (<see cref="ContinueFill"/>).</summary>
    public double[]? ShownEdges => fill.Shown;

    /// <summary>A new strip in place of an old one fills from the old one's edges instead of from empty.</summary>
    public void ContinueFill(double[] edges) => fill.Seed(edges);

    public void ReplayFill()
    {
        fill.Forget();
        Fill();
    }

    private void Fill()
    {
        // Without width there is nothing to fill; a fill still running for the old cells ends before it draws the new ones.
        if (ActualWidth <= 0)
        {
            fill.Stop();
            return;
        }
        Measure();
        fill.Show(target);
    }

    private void Measure()
    {
        var cells = Cells ?? [];
        target = BarSweep.StripEdges([.. cells.Select(c => Math.Max(0.001, c.Weight))], [.. cells.Select(c => c.Parts.Select(p => p.Weight).ToArray())], ActualWidth, ColumnSpacing);
        var gap = ColumnSpacing / ActualWidth;
        cellBounds = new (int, double, double)[cells.Count];
        for (int i = 0, first = 0; i < cells.Count; first += cells[i].Parts.Count + 1, i++)
            cellBounds[i] = (first, first == 0 ? 0 : target[first - 1], i < cells.Count - 1 ? target[first + cells[i].Parts.Count] - gap : 1);
    }

    /// <summary>Each part's column takes its stretch of the moving edges, cut to its cell.</summary>
    private void Apply(double[] edges)
    {
        if (edges.Length != target.Length || cellBounds.Length != (Cells?.Count ?? 0))
            return;
        foreach (var (column, _, cell, part) in partColumns)
        {
            var (first, cellLeft, cellRight) = cellBounds[cell];
            var k = first + part;
            var left = Math.Max(k == 0 ? 0 : edges[k - 1], cellLeft);
            var right = Math.Min(edges[k], cellRight);
            column.Width = new GridLength(Math.Max(0, right - left) * ActualWidth);
        }
    }

    private void Finish()
    {
        foreach (var (column, weight, _, _) in partColumns)
            column.Width = new GridLength(weight, GridUnitType.Star);
    }


    private void Rebuild()
    {
        Children.Clear();
        ColumnDefinitions.Clear();
        partColumns.Clear();
        var cells = Cells ?? [];
        var small = CellHeight <= 10;
        ColumnSpacing = small ? 3 : 4;
        Height = CellHeight;
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.001, cell.Weight), GridUnitType.Star) });
            var element = BuildCell(cell, i, small);
            SetColumn(element, i);
            Children.Add(element);
        }
        // A changed value moves from the edges on screen to the new ones.
        Fill();
    }

    private FrameworkElement BuildCell(StripCell cell, int index, bool small)
    {
        var radius = small ? 4 : 6;
        var parts = new Grid();
        for (var i = 0; i < cell.Parts.Count; i++)
        {
            var part = cell.Parts[i];
            var column = new ColumnDefinition { Width = new GridLength(part.Weight, GridUnitType.Star) };
            parts.ColumnDefinitions.Add(column);
            partColumns.Add((column, part.Weight, index, i));
            var fill = LedgerTheme.Surface(part.Paint);
            SetColumn(fill, i);
            parts.Children.Add(fill);
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
        AutomationProperties.SetAutomationId(stop, "TodayCell");
        LedgerTheme.AttachTip(stop, cell.Tip);
        return stop;
    }
}

/// <summary>
/// The 7d or month bar (spec 6.3): an 8 px track with prior use, today's span, the over part, a ring around today's span,
/// cap and over ticks and 5h dividers. Positions are percent of the width and arrive mirrored for Left mode. The painted
/// parts fill as one strip from the left (<see cref="BarFill"/>); the track, ring, ticks and dividers stay in place.
/// </summary>
internal sealed partial class PeriodBar : Canvas
{
    /// <summary>Each painted part with its final left and width in pixels and its part of the fill.</summary>
    private readonly List<(FrameworkElement Rect, double Left, double Width, int Part)> painted = [];
    private double[] edges = [];
    private readonly BarFill fill;
    /// <summary>Whether the bar has a width; the first width after none fills it, later ones keep the fill where it ends.</summary>
    private bool laidOut;
    public static readonly DependencyProperty VisualProperty = DependencyProperty.Register(nameof(Visual), typeof(CardVisual), typeof(PeriodBar), new PropertyMetadata(null, (d, _) => ((PeriodBar)d).Show(true)));

    public PeriodBar()
    {
        Height = 20;
        fill = new BarFill(Apply, Finish);
        // A resize during a fill lets the fill go on to the new places; otherwise it redraws the bar where the fill ended.
        SizeChanged += (_, _) => Show(!laidOut || fill.IsRunning);
        Unloaded += (_, _) => fill.Stop();
    }

    public CardVisual? Visual { get => (CardVisual?)GetValue(VisualProperty); set => SetValue(VisualProperty, value); }

    public void ReplayFill()
    {
        fill.Forget();
        Show(true);
    }

    private void Show(bool move)
    {
        Rebuild();
        if (ActualWidth <= 0 || Visual is null)
        {
            fill.Stop();
            laidOut = false;
            return;
        }
        laidOut = true;
        if (move)
            fill.Show(edges);
        else
            fill.Snap(edges);
    }

    private void Apply(double[] shown)
    {
        if (shown.Length != edges.Length)
            return;
        foreach (var (rect, _, _, part) in painted)
        {
            var left = part == 0 ? 0 : shown[part - 1];
            SetLeft(rect, left * ActualWidth);
            rect.Width = Math.Max(0, shown[part] - left) * ActualWidth;
        }
    }

    private void Finish()
    {
        foreach (var (rect, left, width, _) in painted)
        {
            SetLeft(rect, left);
            rect.Width = width;
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        painted.Clear();
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
        (edges, var parts) = BarSweep.BarEdges([.. segments.Select(s => (s.Left, s.Width))]);
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var rect = LedgerTheme.Surface(segment.Paint);
            rect.Width = Math.Max(0, X(segment.Width));
            rect.Height = 8;
            SetLeft(rect, X(segment.Left));
            track.Children.Add(rect);
            painted.Add((rect, X(segment.Left), rect.Width, parts[i]));
        }
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
