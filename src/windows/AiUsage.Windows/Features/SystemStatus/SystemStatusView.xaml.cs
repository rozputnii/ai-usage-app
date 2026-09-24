using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.SystemStatusPage;

/// <summary>S09 System status, shown as the last section of the single settings view.</summary>
internal sealed partial class SystemStatusView : UserControl
{
    public SystemStatusView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AppLayout.Current.PropertyChanged += OnLayoutChanged;
            Arrange();
        };
        Unloaded += (_, _) => AppLayout.Current.PropertyChanged -= OnLayoutChanged;
    }

    public SystemStatusViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }

    private string FailureKey(bool failure) => failure ? "CritBrush" : "TextBrush";

    private void OnLayoutChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppLayout.IsCompact))
            Arrange();
    }

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
