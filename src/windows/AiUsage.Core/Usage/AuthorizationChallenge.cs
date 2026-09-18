namespace AiUsage.Core.Usage;

/// <summary>Transient instructions for the active login only; never part of cached account state.</summary>
public sealed record AuthorizationChallenge(Uri VerificationUri, string? UserCode = null, DateTimeOffset? ExpiresAt = null)
{
    public override string ToString() => "AuthorizationChallenge (redacted)";
}
