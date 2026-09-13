namespace AiUsage.RoutingSpike;

internal static class RuntimeEvidence
{
    internal static string MicrosoftUiXamlAssemblyIdentity =>
        typeof(Microsoft.UI.Xaml.Application).Assembly.FullName
        ?? "Microsoft.UI.Xaml assembly identity unavailable";
}
