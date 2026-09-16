using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AiUsage.Features.SystemStatusPage;

internal sealed partial class SystemStatusPage : Page
{
    public SystemStatusPage()
    {
        InitializeComponent();
        AppLayout.Current.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppLayout.IsCompact))
                Arrange();
        };
        Loaded += (_, _) => Arrange();
    }

    public SystemStatusViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel = (SystemStatusViewModel)e.Parameter;
        Bindings.Update();
        base.OnNavigatedTo(e);
    }

    private Thickness Gutter(bool compact) => compact ? new Thickness(18, 0, 18, 18) : new Thickness(40, 0, 40, 24);
    private string FailureKey(bool failure) => failure ? "CritBrush" : "TextBrush";

    private void Arrange()
    {
        var compact = AppLayout.Current.IsCompact;
        var facts = new FrameworkElement[] { RefreshFact, StorageFact, UpdateFact };
        for (var i = 0; i < facts.Length; i++)
        {
            Grid.SetRow(facts[i], compact ? i : 0);
            Grid.SetColumn(facts[i], compact ? 0 : i);
        }
        FactsGrid.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        FactsGrid.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        FactsGrid.ColumnSpacing = compact ? 0 : 32;
    }
}
