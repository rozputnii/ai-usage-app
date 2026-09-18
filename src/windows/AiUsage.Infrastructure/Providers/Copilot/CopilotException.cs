using AiUsage.Core.Usage;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Copilot;

public sealed class CopilotException(ProviderFailureKind kind, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null)
    : Exception($"Copilot operation failed: {kind}.")
{
    public ProviderFailureKind Kind { get; } = kind;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
