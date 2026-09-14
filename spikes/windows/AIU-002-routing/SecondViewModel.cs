using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;

namespace AiUsage.RoutingSpike;

internal sealed class SecondViewModel
{
    private readonly INavigator navigator;

    public SecondViewModel(INavigator navigator)
    {
        this.navigator = navigator;
        BackCommand = new AsyncRelayCommand(NavigateBackAsync);
    }

    public string MicrosoftUiXamlAssemblyIdentity => RuntimeEvidence.MicrosoftUiXamlAssemblyIdentity;

    public IAsyncRelayCommand BackCommand { get; }

    private async Task NavigateBackAsync() =>
        await navigator.NavigateBackAsync(this);
}
