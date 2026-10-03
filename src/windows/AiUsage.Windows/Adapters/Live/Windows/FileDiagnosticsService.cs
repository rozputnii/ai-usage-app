using System.Diagnostics;
using AiUsage.Core.Diagnostics;
using AiUsage.Features.SystemStatusPage;
using AiUsage.Infrastructure.Diagnostics;

namespace AiUsage.Composition;

internal sealed class FileDiagnosticsService(IDiagnosticsService existing, FileDiagnostics files) : IDiagnosticsService
{
    public IAsyncEnumerable<HealthCheckStep> RunHealthCheckAsync(CancellationToken cancellationToken) => existing.RunHealthCheckAsync(cancellationToken);
    public Task<string> PreviewDiagnosticsAsync(CancellationToken cancellationToken) => existing.PreviewDiagnosticsAsync(cancellationToken);
    public Task<string> PreviewLogsAsync(CancellationToken cancellationToken) => files.PreviewAsync();
    public bool CanOpenLogs => true;
    public async Task OpenLogsAsync()
    {
        try
        {
            await files.FlushAsync();
            // No arbitrary user/provider path participates in this action.
            DiagnosticFiles.ValidateDirectory(files.DirectoryPath);
            Process.Start(new ProcessStartInfo(files.DirectoryPath) { UseShellExecute = true });
        }
        catch (Exception exception) { files.Failure(DiagnosticEvent.OperationFailure, exception); }
    }
}
