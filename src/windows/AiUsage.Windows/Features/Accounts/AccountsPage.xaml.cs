using System.ComponentModel;
using AiUsage.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace AiUsage.Features.Accounts;

internal sealed partial class AccountsPage : Page
{
    public AccountsPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AppLayout.Current.PropertyChanged += OnLayoutChanged;
            Arrange();
        };
        Unloaded += (_, _) => AppLayout.Current.PropertyChanged -= OnLayoutChanged;
    }

    public AccountsViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        var first = ViewModel is null;
        ViewModel = (AccountsViewModel)e.Parameter;
        if (first)
            ViewModel.Detail.Rename.PropertyChanged += OnRenameChanged;
        Bindings.Update();
        base.OnNavigatedTo(e);
    }

    private Thickness Gutter(bool compact) => compact ? new Thickness(18, 0, 18, 18) : new Thickness(40, 0, 40, 24);
    private string RenameBorder(bool error) => error ? "CritBrush" : "Stroke2Brush";
    private string ResetKey(bool critical) => critical ? "CritBrush" : "Text2Brush";
    private Style ReconnectStyle(bool primary) => (Style)Application.Current.Resources[primary ? "PrimaryButton" : "SecondaryButton"];

    private void OnRenameChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RenameAccountViewModel.IsEditing) && ViewModel.Detail.Rename.IsEditing)
            DispatcherQueue.TryEnqueue(() =>
            {
                RenameBox.Focus(FocusState.Programmatic);
                RenameBox.SelectAll();
            });
    }

    /// <summary>Enter saves, Esc cancels and restores the prior label.</summary>
    private void OnRenameKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            _ = ViewModel.Detail.Rename.SaveCommand.ExecuteAsync(null);
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            ViewModel.Detail.Rename.CancelCommand.Execute(null);
        }
    }

    private void OnContextClick(object sender, RoutedEventArgs e) =>
        _ = ViewModel.Detail.SelectContextCommand.ExecuteAsync(((FrameworkElement)sender).Tag as ContextOptionViewModel);

    private void OnLayoutChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppLayout.IsCompact))
            Arrange();
    }

    /// <summary>Below 720 px the list stacks above the detail and the hero value and meter wrap to two rows.</summary>
    private void Arrange()
    {
        var compact = AppLayout.Current.IsCompact;
        ListColumn.Width = compact ? new GridLength(1, GridUnitType.Star) : new GridLength(210);
        DetailColumn.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        foreach (var element in new FrameworkElement[] { NoSelection, DetailHost })
        {
            Grid.SetRow(element, compact ? 1 : 0);
            Grid.SetColumn(element, compact ? 0 : 1);
        }
        HeroValueColumn.Width = compact ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        HeroMeterColumn.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(HeroMeterHost, compact ? 1 : 0);
        Grid.SetColumn(HeroMeterHost, compact ? 0 : 1);
        Grid.SetRow(ActionsHost, compact ? 1 : 0);
        Grid.SetColumn(ActionsHost, compact ? 0 : 1);
        ActionsHost.Padding = compact ? new Thickness(0) : new Thickness(0, 18, 0, 0);
    }
}
