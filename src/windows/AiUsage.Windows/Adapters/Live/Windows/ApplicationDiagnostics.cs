using AiUsage.Adapters.Live;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.Composition;

/// <summary>Desktop fault projection, available before host creation and after host disposal.</summary>
internal sealed class ApplicationDiagnostics : IDiagnosticSink
{
    private IDiagnosticSink? sink;

    public void Initialize(bool demo)
    {
        try { sink = new LocalDiagnosticSink(ApplicationStateDirectory.Get(demo)); }
        catch (Exception) { /* An unavailable state root must not prevent desktop startup or exit. */ }
    }

    public void Register(IServiceCollection services) => services.AddSingleton(this).AddSingleton<IDiagnosticSink>(this);
    public void StartupFailure(Exception error) => Record(DiagnosticEvent.StartupFailure, DiagnosticProjection.Category(error));
    public void ShutdownFailure(Exception error) => Record(DiagnosticEvent.ShutdownFailure, DiagnosticProjection.Category(error));
    public void DisposalFailure(Exception error) => Record(DiagnosticEvent.DisposalFailure, DiagnosticProjection.Category(error));
    public void TrayFailure(Exception error) => Record(DiagnosticEvent.TrayFailure, DiagnosticProjection.Category(error));
    public void UnhandledFailure(Exception? error) => Record(DiagnosticEvent.UnhandledFailure,
        error is null ? DiagnosticCategory.Unexpected : DiagnosticProjection.Category(error));
    public void Record(DiagnosticEvent eventCode, DiagnosticCategory category) => sink?.Record(eventCode, category);
}

internal static class ApplicationStateDirectory
{
    public static string Get(bool demo = false)
    {
        if (!demo)
        {
            try
            {
                _ = Windows.ApplicationModel.Package.Current.Id;
                return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            }
            catch (InvalidOperationException) { }
        }
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage", "Development");
        if (Environment.GetEnvironmentVariable("AIU_DEVELOPMENT_STATE_DIRECTORY") is { Length: > 0 } isolated)
            root = Path.GetFullPath(isolated);
        return demo ? Path.Combine(root, "Demo") : root;
    }
}
