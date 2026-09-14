namespace AiUsage.Core.Providers.Claude;

/// <summary>The stable account and organization to which the issued grant is bound.</summary>
public sealed record ClaudeIdentity(string AccountId, string OrganizationId);
