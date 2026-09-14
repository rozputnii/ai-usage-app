using AiUsage.Core.Providers.Codex;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Codex;

public sealed class CodexException : Exception
{
    /// <summary>
    /// Reviewed OAuth/OpenID error codes that may be reported verbatim. Anything else is an
    /// opaque provider value and is reduced to a shape description, never echoed.
    /// </summary>
    private static readonly HashSet<string> KnownErrorCodes = new(StringComparer.Ordinal)
    {
        "invalid_request", "unauthorized_client", "access_denied", "unsupported_response_type",
        "invalid_scope", "server_error", "temporarily_unavailable", "invalid_client", "invalid_grant",
        "unsupported_grant_type", "interaction_required", "login_required", "consent_required",
        "account_selection_required", "request_not_supported", "registration_not_supported",
        "invalid_target", "slow_down", "authorization_pending", "expired_token"
    };

    public CodexException(CodexFailureKind kind, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null,
        string? providerErrorCode = null)
        : base($"Codex operation failed: {kind}.")
    {
        Kind = kind;
        StatusCode = statusCode;
        RetryAfter = retryAfter;
        if (providerErrorCode is { Length: > 0 })
        {
            if (KnownErrorCodes.Contains(providerErrorCode))
                ProviderErrorCode = providerErrorCode;
            else
                UnrecognizedProviderError = Describe(providerErrorCode);
        }
    }

    public CodexFailureKind Kind { get; }
    public HttpStatusCode? StatusCode { get; }
    public TimeSpan? RetryAfter { get; }

    /// <summary>A reviewed standard error code, or null when the provider sent something else.</summary>
    public string? ProviderErrorCode { get; }

    /// <summary>Shape of an unrecognized provider error value; never its content.</summary>
    public string? UnrecognizedProviderError { get; }

    private static string Describe(string value) =>
        $"unrecognized value: {value.Length} characters, classes {(value.Any(char.IsAsciiLetterUpper) ? "U" : "")}{(value.Any(char.IsAsciiLetterLower) ? "l" : "")}{(value.Any(char.IsAsciiDigit) ? "d" : "")}{(value.Any(c => !char.IsAsciiLetterOrDigit(c)) ? "s" : "")}";
}
