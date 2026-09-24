using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AiUsage.Features.Overview;

internal sealed partial class OverviewPage : Page
{
    public OverviewPage() => InitializeComponent();

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
}
