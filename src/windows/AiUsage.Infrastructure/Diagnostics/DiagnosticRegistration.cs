using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AiUsage.Infrastructure.Diagnostics;

public static class DiagnosticRegistration
{
    /// <summary>The application owns the lifetime; DI disposal cannot disable emergency recording.</summary>
    public static IServiceCollection AddFileDiagnostics(this IServiceCollection services, FileDiagnostics diagnostics)
    {
        services.AddSingleton(diagnostics);
        services.AddSingleton<IDiagnosticSink>(diagnostics);
        services.AddSingleton(new ProviderTransportOptions { Diagnostics = diagnostics });
        services.AddSerilog(new LoggerConfiguration().MinimumLevel.Warning().WriteTo.Sink(new HostSink(diagnostics)).CreateLogger(), dispose: true);
        return services;
    }

    private sealed class HostSink(FileDiagnostics diagnostics) : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            // Framework properties/templates can contain host paths. Keep only severity and safe stack metadata.
            if (logEvent.Exception is { } exception) diagnostics.Failure(DiagnosticEvent.BackgroundFailure, exception);
            else diagnostics.Signal(DiagnosticEvent.BackgroundFailure, logEvent.Level >= LogEventLevel.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning);
        }
    }
}
