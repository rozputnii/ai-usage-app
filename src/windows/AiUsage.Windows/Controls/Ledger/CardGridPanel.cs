using AiUsage.Features.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace AiUsage.Controls.Ledger;

/// <summary>
/// The card grid (S1): cards fill rows of one or two equal columns in order; an element marked FullRow (inline history)
/// starts its own row across all columns. Row height is the tallest card of the row.
/// </summary>
internal sealed partial class CardGridPanel : Panel
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(int), typeof(CardGridPanel), new PropertyMetadata(2, Invalidate));
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(nameof(Gap), typeof(double), typeof(CardGridPanel), new PropertyMetadata(8.0, Invalidate));
    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(CardGridPanel), new PropertyMetadata(new Thickness(12), Invalidate));
    public static readonly DependencyProperty FullRowProperty = DependencyProperty.RegisterAttached("FullRow", typeof(bool), typeof(CardGridPanel), new PropertyMetadata(false));

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    public static bool GetFullRow(DependencyObject element) => (bool)element.GetValue(FullRowProperty);
    public static void SetFullRow(DependencyObject element, bool value) => element.SetValue(FullRowProperty, value);

    private static void Invalidate(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((CardGridPanel)d).InvalidateMeasure();

    private IEnumerable<List<UIElement>> Rows()
    {
        var columns = Math.Max(1, Columns);
        var row = new List<UIElement>();
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
                continue;
            if (GetFullRow(child))
            {
                if (row.Count > 0)
                    yield return row;
                yield return [child];
                row = [];
                continue;
            }
            row.Add(child);
            if (row.Count == columns)
            {
                yield return row;
                row = [];
            }
        }
        if (row.Count > 0)
            yield return row;
    }

    private double ColumnWidth(double width) => Math.Max(0, (width - Padding.Left - Padding.Right - Gap * (Math.Max(1, Columns) - 1)) / Math.Max(1, Columns));

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760 : availableSize.Width;
        var column = ColumnWidth(width);
        var full = width - Padding.Left - Padding.Right;
        var height = Padding.Top;
        var first = true;
        foreach (var row in Rows())
        {
            if (!first)
                height += Gap;
            first = false;
            var rowHeight = 0.0;
            foreach (var child in row)
            {
                child.Measure(new Size(GetFullRow(child) ? full : column, double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }
            height += rowHeight;
        }
        return new Size(width, height + Padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var column = ColumnWidth(finalSize.Width);
        var full = finalSize.Width - Padding.Left - Padding.Right;
        var y = Padding.Top;
        var first = true;
        foreach (var row in Rows())
        {
            if (!first)
                y += Gap;
            first = false;
            var rowHeight = row.Max(c => c.DesiredSize.Height);
            var x = Padding.Left;
            foreach (var child in row)
            {
                var w = GetFullRow(child) ? full : column;
                child.Arrange(new Rect(x, y, w, rowHeight));
                x += w + Gap;
            }
            y += rowHeight;
        }
        return finalSize;
    }
}

/// <summary>Inline history under a card (S4): header, chart, legend. ← → move the focused day; Esc closes (window).</summary>
internal sealed partial class HistoryPanel : ContentControl
{
    private readonly HistoryChart chart = new();
    private readonly TextBlock title = Text(string.Empty, "Ink");
    private readonly TextBlock subtitle = Text(string.Empty, "Ink3");
    private readonly TextBlock legend = Text(string.Empty, "Ink3");
    private readonly TextBlock keys = Text(string.Empty, "Ink3");

    public HistoryPanel()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        IsTabStop = true;
        UseSystemFocusVisuals = true;
        FocusVisualPrimaryBrush = LedgerTheme.Solid("Focus");
        FocusVisualPrimaryThickness = new Thickness(2);
        FocusVisualMargin = new Thickness(-4);
        title.FontFamily = (FontFamily)LedgerTheme.Find("LedgerSerifFont")!;
        title.FontSize = 16;
        title.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(title);
        header.Children.Add(subtitle);
        subtitle.VerticalAlignment = VerticalAlignment.Bottom;
        var top = new Grid();
        top.Children.Add(header);
        keys.HorizontalAlignment = HorizontalAlignment.Right;
        top.Children.Add(keys);
        legend.TextWrapping = TextWrapping.Wrap;
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(top);
        chart.Margin = new Thickness(0, 4, 0, 0);
        stack.Children.Add(chart);
        stack.Children.Add(legend);
        var surface = new SquircleSurface { Radius = 20, Fill = LedgerTheme.Solid("Hist"), Stroke = LedgerTheme.Solid("LineCard"), Padding = new Thickness(15, 13, 15, 12) };
        surface.Children.Add(stack);
        Content = surface;
        CardGridPanel.SetFullRow(this, true);
        KeyDown += OnKeyDown;
    }

    public LedgerHistoryViewModel? History
    {
        get;
        set
        {
            field = value;
            chart.History = value;
            title.Text = value?.Title ?? string.Empty;
            subtitle.Text = value?.Subtitle ?? string.Empty;
            legend.Text = value?.Legend ?? string.Empty;
            keys.Text = value?.KeysHint ?? string.Empty;
            UpdateName();
        }
    }

    private void UpdateName() =>
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(this, History?.AccessibleName ?? string.Empty);

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (History is null)
            return;
        if (e.Key is VirtualKey.Left or VirtualKey.Right)
        {
            e.Handled = true;
            History.MoveFocus(e.Key == VirtualKey.Left ? -1 : 1);
            UpdateName();
        }
    }

    private static TextBlock Text(string text, string key) => new()
    {
        Text = text,
        FontSize = 12,
        LineHeight = 16,
        Foreground = LedgerTheme.Solid(key),
        FontFamily = (FontFamily)LedgerTheme.Find("LedgerSansFont")!,
    };
}

/// <summary>
/// A row of children laid out left to right with a gap, where the first child (the account name) gets only the width the
/// others leave, so a long name trims instead of pushing the scope, marks or pill out of the card.
/// </summary>
internal sealed partial class ShrinkFirstPanel : Panel
{
    public double Spacing { get; set; } = 8;

    protected override Size MeasureOverride(Size availableSize)
    {
        var others = 0.0;
        var height = 0.0;
        var visible = Children.Where(c => c.Visibility == Visibility.Visible).ToArray();
        foreach (var child in visible.Skip(1))
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            others += child.DesiredSize.Width + Spacing;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        if (visible.Length > 0)
        {
            var room = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : Math.Max(0, availableSize.Width - others);
            visible[0].Measure(new Size(room, availableSize.Height));
            height = Math.Max(height, visible[0].DesiredSize.Height);
            others += visible[0].DesiredSize.Width;
        }
        return new Size(double.IsInfinity(availableSize.Width) ? others : Math.Min(others, availableSize.Width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        foreach (var child in Children.Where(c => c.Visibility == Visibility.Visible))
        {
            var width = Math.Min(child.DesiredSize.Width, Math.Max(0, finalSize.Width - x));
            child.Arrange(new Rect(x, (finalSize.Height - child.DesiredSize.Height) / 2, width, child.DesiredSize.Height));
            x += width + Spacing;
        }
        return finalSize;
    }
}
