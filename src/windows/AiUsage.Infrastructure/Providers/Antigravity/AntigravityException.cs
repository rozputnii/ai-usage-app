using AiUsage.Core.Usage;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Antigravity;

public sealed class AntigravityException(ProviderFailureKind kind, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null)
    : Exception($"Antigravity operation failed: {kind}.")
{
    public ProviderFailureKind Kind { get; } = kind;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
