using AiUsage.Features.Dashboard;
using Microsoft.UI.Xaml;

namespace AiUsage;

internal sealed partial class MainWindow : Window
{
    internal DashboardShellViewModel ViewModel { get; }

    public MainWindow(DashboardShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Title = App.Resource("AppTitle");
        foreach (var provider in ViewModel.Providers)
            provider.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(DashboardViewModel.ManualEntryVisible) && !ViewModel.Selected.ManualEntryVisible)
                    ManualCode.Password = string.Empty;
            };
    }

    internal void DisableExit()
    {
        ManualCode.Password = string.Empty;
        ExitButton.IsEnabled = false;
    }

    private Visibility VisibleWhen(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private void SubmitCode(object sender, RoutedEventArgs args)
    {
        var code = ManualCode.Password;
        ManualCode.Password = string.Empty;
        ViewModel.Selected.SubmitCode(code);
    }

    private void ProviderChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs args)
    {
        if (ManualCode is not null) ManualCode.Password = string.Empty;
    }

    internal void ShowFailure(string resource)
    {
        FailureMessage.Text = App.Resource(resource);
        FailureMessage.Visibility = Visibility.Visible;
    }
}
