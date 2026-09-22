using AiUsage.Core.Diagnostics;

namespace AiUsage.Adapters.Live;

internal static class DiagnosticProjection
{
    // No messages, runtime type names, inner exceptions or Data are inspected or retained.
    public static DiagnosticCategory Category(Exception error) => error switch
    {
        InvalidOperationException => DiagnosticCategory.InvalidOperation,
        System.Text.Json.JsonException or InvalidDataException => DiagnosticCategory.InvalidData,
        UnauthorizedAccessException => DiagnosticCategory.AccessDenied,
        IOException => DiagnosticCategory.Io,
        _ => DiagnosticCategory.Unexpected
    };
}
