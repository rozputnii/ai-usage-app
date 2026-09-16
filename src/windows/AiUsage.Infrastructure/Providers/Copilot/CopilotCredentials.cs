using AiUsage.Core.Providers.Copilot;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>Immutable in-memory GitHub user token bound to a verified account.</summary>
public sealed class CopilotCredentials
{
    internal CopilotCredentials(string accessToken, CopilotIdentity identity, string? grantedScope)
    {
        AccessToken = accessToken;
        Identity = identity;
        GrantedScope = grantedScope;
    }

    internal string AccessToken { get; }
    public CopilotIdentity Identity { get; }
    /// <summary>The provider-reported OAuth scope string, which is not a secret.</summary>
    public string? GrantedScope { get; }
    public override string ToString() => "CopilotCredentials (redacted)";
}
