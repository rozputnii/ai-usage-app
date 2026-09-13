using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Dashboard;

internal sealed class DashboardViewModel(Func<Task> exitAsync)
{
    public IAsyncRelayCommand ExitCommand { get; } = new AsyncRelayCommand(exitAsync);
}
