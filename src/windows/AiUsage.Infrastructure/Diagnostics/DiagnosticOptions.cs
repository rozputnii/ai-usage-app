namespace AiUsage.Infrastructure.Diagnostics;

public sealed class DiagnosticOptions
{
    public bool TraceEnabled { get; init; }
    public bool CaptureBodies { get; init; } = true;
    internal int QueueBytes { get; init; } = 16 * 1024 * 1024;
    internal int QueueEntries { get; init; } = 4096;
    internal long ApplicationBytes { get; init; } = 64 * 1024 * 1024;
    internal long TraceBytes { get; init; } = 32 * 1024 * 1024;
    internal long ResponseBytes { get; init; } = 128 * 1024 * 1024;
    internal long CriticalBytes { get; init; } = 32 * 1024 * 1024;

    public static DiagnosticOptions FromEnvironment() => new()
    {
        TraceEnabled = Environment.GetEnvironmentVariable("AIU_LOG_TRACE") == "1",
        CaptureBodies = Environment.GetEnvironmentVariable("AIU_LOG_BODIES") != "0"
    };
}
