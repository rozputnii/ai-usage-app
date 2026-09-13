using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;

namespace AiUsage.RoutingSpike;

internal sealed class MainViewModel
{
    private readonly INavigator navigator;

    public MainViewModel(INavigator navigator)
    {
        this.navigator = navigator;
        NextCommand = new AsyncRelayCommand(NavigateNextAsync);
    }

    public string MicrosoftUiXamlAssemblyIdentity => RuntimeEvidence.MicrosoftUiXamlAssemblyIdentity;

    public IAsyncRelayCommand NextCommand { get; }

    private async Task NavigateNextAsync() =>
        await navigator.NavigateViewModelAsync<SecondViewModel>(this);
}
