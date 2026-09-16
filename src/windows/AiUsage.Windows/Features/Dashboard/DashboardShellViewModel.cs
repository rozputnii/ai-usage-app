using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Dashboard;

/// <summary>Provider selection and desktop lifetime. Each card retains its own operation and state.</summary>
internal sealed partial class DashboardShellViewModel : ObservableObject
{
    internal DashboardShellViewModel(IReadOnlyList<DashboardViewModel> providers, Func<Task> exitAsync, Action showWindow)
    {
        Providers = providers;
        Selected = providers[0];
        ExitCommand = new AsyncRelayCommand(exitAsync);
        ShowCommand = new RelayCommand(showWindow);
    }

    public IReadOnlyList<DashboardViewModel> Providers { get; }
    [ObservableProperty] public partial DashboardViewModel Selected { get; set; }
    public IAsyncRelayCommand ExitCommand { get; }
    public IRelayCommand ShowCommand { get; }
    internal Task LoadAsync(CancellationToken token = default) => Task.WhenAll(Providers.Select(provider => provider.LoadAsync(token)));
    internal Task StopAsync() => Task.WhenAll(Providers.Select(provider => provider.StopAsync()));
}
