using AiUsage.Core.Usage;
using System.Net;

namespace AiUsage.Infrastructure.Providers;

/// <summary>Only allowlisted classifications cross the provider boundary; payloads never do.</summary>
internal sealed class ProviderException(ProviderFailureKind kind, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null)
    : Exception($"Provider operation failed: {kind}.")
{
    public ProviderFailureKind Kind { get; } = kind;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
