using System.Diagnostics;
using System.Reflection;

namespace AiUsage.Infrastructure.Diagnostics;

/// <summary>Only runtime type metadata and symbol locations cross the logging boundary.</summary>
internal sealed record SafeException(string Type, int HResult, string MessageOmitted, string[] Frames, SafeException[] Inner, bool Truncated)
{
    internal static SafeException Project(Exception error, int depth = 0)
    {
        var type = error.GetType();
        var trusted = type.Assembly == typeof(Exception).Assembly ||
            type.Assembly == typeof(HttpRequestException).Assembly ||
            type.Assembly == typeof(System.Text.Json.JsonException).Assembly ||
            type.Assembly == typeof(System.Net.Sockets.SocketException).Assembly ||
            type.Assembly == typeof(System.Security.Authentication.AuthenticationException).Assembly ||
            type.Assembly.GetName().Name?.StartsWith("AiUsage", StringComparison.Ordinal) == true;
        // Do not invoke overridden Message/StackTrace/Data/ToString properties.
        var frames = new StackTrace(error, true).GetFrames() ?? [];
        var inner = error is AggregateException aggregate ? aggregate.InnerExceptions.ToArray() : error.InnerException is { } cause ? [cause] : Array.Empty<Exception>();
        return new(trusted ? type.FullName ?? "Exception" : "ExternalException", error.HResult, "unapproved-message",
            frames.Take(48).Select(Location).ToArray(), depth < 4 ? inner.Take(4).Select(e => Project(e, depth + 1)).ToArray() : [],
            frames.Length > 48 || inner.Length > 4 || (depth >= 4 && inner.Length > 0));
    }

    private static string Location(StackFrame frame)
    {
        var method = frame.GetMethod();
        var name = method?.DeclaringType?.Assembly.GetName().Name;
        if (name is null || !(name.StartsWith("AiUsage", StringComparison.Ordinal) || name.StartsWith("System", StringComparison.Ordinal) || name.StartsWith("Microsoft", StringComparison.Ordinal)))
            return "external-frame";
        var symbol = (method?.DeclaringType?.FullName ?? "") + "." + method?.Name;
        var path = frame.GetFileName()?.Replace('\\', '/');
        var marker = path?.LastIndexOf("/src/", StringComparison.Ordinal) ?? -1;
        if (marker < 0) marker = path?.LastIndexOf("/tests/", StringComparison.Ordinal) ?? -1;
        return marker >= 0 ? $"{symbol} in {path![(marker + 1)..]}:{frame.GetFileLineNumber()}" : symbol;
    }
}
