using AiUsage.Features.Dashboard;
using Microsoft.UI.Xaml;

namespace AiUsage;

internal sealed partial class MainWindow : Window
{
    internal DashboardViewModel ViewModel { get; }

    public MainWindow(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Title = App.Resource("AppTitle");
    }

    internal void DisableExit() => ExitButton.IsEnabled = false;

    internal void ShowFailure(string resource)
    {
        FailureMessage.Text = App.Resource(resource);
        FailureMessage.Visibility = Visibility.Visible;
    }
}
