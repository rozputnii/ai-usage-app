using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Overview;

internal sealed partial class SkeletonRows : UserControl
{
    public SkeletonRows()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Arrange();
            AppLayout.Current.PropertyChanged += OnLayoutChanged;
        };
        Unloaded += (_, _) => AppLayout.Current.PropertyChanged -= OnLayoutChanged;
    }

    private void OnLayoutChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Arrange();

    private void Arrange()
    {
        var compact = AppLayout.Current.IsCompact;
        foreach (var (grid, lines) in new[] { (GridA, LinesA), (GridB, LinesB) })
        {
            grid.ColumnDefinitions[0].Width = compact ? new GridLength(1, GridUnitType.Star) : new GridLength(180);
            grid.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            Grid.SetRow(lines, compact ? 1 : 0);
            Grid.SetColumn(lines, compact ? 0 : 1);
            if (grid.RowDefinitions.Count == 0)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            foreach (var line in lines.Children.OfType<Grid>())
            {
                line.ColumnDefinitions[0].Width = compact ? new GridLength(1, GridUnitType.Star) : new GridLength(110);
                line.ColumnDefinitions[3].Width = compact ? new GridLength(0) : new GridLength(190);
            }
        }
    }
}
