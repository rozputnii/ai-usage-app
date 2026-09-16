namespace AiUsage.Features.SystemStatusPage;

public enum HealthCheckResult { Ok, Warning, Failed }

/// <summary><paramref name="FindingKey"/> is a resource key; null when the check passed.</summary>
public sealed record HealthCheckStep(int Index, int Total, string NameKey, HealthCheckResult Result, string? FindingKey);

/// <summary>Adapter boundary for diagnostics (D-129/D-130). A health check only reads state and never repairs.</summary>
public interface IDiagnosticsService
{
    IAsyncEnumerable<HealthCheckStep> RunHealthCheckAsync(CancellationToken cancellationToken);

    /// <summary>Sanitized sample text: no credentials, tokens, paths or provider payloads.</summary>
    Task<string> PreviewDiagnosticsAsync(CancellationToken cancellationToken);

    Task<string> PreviewLogsAsync(CancellationToken cancellationToken);
}
