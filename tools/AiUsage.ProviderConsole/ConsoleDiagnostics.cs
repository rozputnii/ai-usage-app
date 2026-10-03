using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.ProviderConsole;

internal static class ConsoleDiagnostics
{
    internal static FileDiagnostics? Current { get; private set; }
    internal static FileDiagnostics Start()
    {
        var root = Environment.GetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage", "Development");
        var files = Current = new FileDiagnostics(Path.Combine(root, "Console"), mode: "console");
        AppDomain.CurrentDomain.UnhandledException += (_, args) => files.Fatal(DiagnosticEvent.UnhandledFailure, args.ExceptionObject as Exception, args.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, args) => files.Failure(DiagnosticEvent.BackgroundFailure, args.Exception);
        return files;
    }
    internal static IServiceCollection Services()
    {
        IServiceCollection services = new ServiceCollection();
        return Current is { } files ? services.AddFileDiagnostics(files) : services;
    }
}
