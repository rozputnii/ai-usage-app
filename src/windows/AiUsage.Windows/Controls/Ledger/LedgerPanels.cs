using AiUsage.Features.Ledger;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace AiUsage.Controls.Ledger;

/// <summary>Reference footer flow: preserve the figure; move reset/actions to another line when needed.</summary>
internal sealed partial class LedgerFooterPanel : Panel
{
    private const double Gap = 8;
    private const double RowGap = 2;

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children.Where(c => c.Visibility == Visibility.Visible))
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (child.DesiredSize.Width > availableSize.Width)
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        }
        return new Size(availableSize.Width, Layout(availableSize.Width, false));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Layout(finalSize.Width, true);
        return finalSize;
    }

    private double Layout(double width, bool arrange)
    {
        var visible = Children.Where(c => c.Visibility == Visibility.Visible).ToArray();
        double y = 0;
        for (var start = 0; start < visible.Length;)
        {
            var end = start + 1;
            var used = visible[start].DesiredSize.Width;
            var height = visible[start].DesiredSize.Height;
            while (end < visible.Length && used + Gap + visible[end].DesiredSize.Width <= width)
            {
                used += Gap + visible[end].DesiredSize.Width;
                height = Math.Max(height, visible[end++].DesiredSize.Height);
            }
            var x = start == 0 ? 0 : Math.Max(0, width - used);
            for (var i = start; i < end; i++)
            {
                // The reset is pushed to the right in the first line, as in the reference flex footer.
                if (start == 0 && i == 1) x += Math.Max(0, width - used);
                var size = visible[i].DesiredSize;
                if (arrange) visible[i].Arrange(new Rect(x, y + (height - size.Height) / 2, size.Width, size.Height));
                x += size.Width + Gap;
            }
            y += height + (end < visible.Length ? RowGap : 0);
            start = end;
        }
        return y;
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

    // A child shown later (the hidden-limits count) must take part in the next measure pass.
    public ShrinkFirstPanel() => Loaded += (_, _) =>
    {
        foreach (var child in Children)
            if (watched.Add(child))
                child.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => InvalidateMeasure());
    };

    private readonly HashSet<UIElement> watched = [];

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
