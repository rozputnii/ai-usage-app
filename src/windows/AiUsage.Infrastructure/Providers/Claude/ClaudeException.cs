using AiUsage.Core.Providers.Claude;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>Only allowlisted classifications cross the provider boundary; payloads never do.</summary>
public sealed class ClaudeException(ClaudeFailureKind kind, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null)
    : Exception($"Claude operation failed: {kind}.")
{
    public ClaudeFailureKind Kind { get; } = kind;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
