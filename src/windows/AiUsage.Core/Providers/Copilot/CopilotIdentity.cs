namespace AiUsage.Core.Providers.Copilot;

/// <summary>The GitHub account bound to a grant. The numeric id is stable; the login is renameable routing data.</summary>
public sealed record CopilotIdentity(long AccountId, string Login);
