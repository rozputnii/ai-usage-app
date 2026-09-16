using AiUsage.Core.Providers.Copilot;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>Only allowlisted classifications cross the provider boundary; payloads never do.</summary>
public sealed class CopilotException(CopilotFailureKind kind, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null)
    : Exception($"GitHub Copilot operation failed: {kind}.")
{
    public CopilotFailureKind Kind { get; } = kind;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
