using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;

namespace AiUsage.RoutingSpike;

public partial class App : Application
{
    private IHost? host;
    private Window? window;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(hostBuilder => hostBuilder.UseNavigation(RegisterRoutes));

        window = builder.Window;
        host = await builder.NavigateAsync<Shell>();
        window.Activate();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<SecondPage, SecondViewModel>());

        routes.Register(
            new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault: true),
            new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>()));
    }
}
