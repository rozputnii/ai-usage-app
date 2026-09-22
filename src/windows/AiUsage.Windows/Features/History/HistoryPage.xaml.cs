using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AiUsage.Features.History;

internal sealed partial class HistoryPage : Page
{
    public HistoryPage() => InitializeComponent();

    public ProviderHistoryViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel = (ProviderHistoryViewModel)e.Parameter;
        Bindings.Update();
        ViewModel.Activate();
        base.OnNavigatedTo(e);
    }

    private Thickness Gutter(bool compact) => compact ? new Thickness(18, 0, 18, 18) : new Thickness(40, 0, 40, 24);

}
