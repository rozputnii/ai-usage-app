using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AiUsage.Features.Overview;

internal sealed partial class OverviewPage : Page
{
    public OverviewPage()
    {
        InitializeComponent();
        AppLayout.Current.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppLayout.IsCompact))
                ArrangeSummary();
        };
        Loaded += (_, _) => ArrangeSummary();
    }

    public OverviewViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel = (OverviewViewModel)e.Parameter;
        AccountRowView.Owner = ViewModel;
        Bindings.Update();
        base.OnNavigatedTo(e);
    }

    private Thickness Gutter(bool compact) => compact ? new Thickness(18, 0, 18, 18) : new Thickness(40, 0, 40, 24);

    private void OnProviderClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is Connection.ProviderOptionViewModel option)
            _ = ViewModel.AddAccount.ConnectCommand.ExecuteAsync(option);
    }
    private string CriticalKey(bool critical) => critical ? "CritBrush" : "TextBrush";

    /// <summary>Four headline stats in one row; two per row below 720 px.</summary>
    private void ArrangeSummary()
    {
        var compact = AppLayout.Current.IsCompact;
        SummaryGrid.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        SummaryGrid.ColumnDefinitions[3].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        SummaryGrid.ColumnSpacing = compact ? 20 : 28;
        Place(StatAccounts, 0, 0);
        Place(StatLowest, 0, 1);
        Place(StatReset, compact ? 1 : 0, compact ? 0 : 2);
        Place(StatAttention, compact ? 1 : 0, compact ? 1 : 3);
    }

    private static void Place(FrameworkElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
    }
}
