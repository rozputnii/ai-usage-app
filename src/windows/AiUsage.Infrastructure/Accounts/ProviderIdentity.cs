namespace AiUsage.Infrastructure.Accounts;

/// <summary>Provider-verified binding, private to Infrastructure and protected registry state.</summary>
internal sealed record ProviderIdentity(string Subject, string? Context = null)
{
    public override string ToString() => "ProviderIdentity (redacted)";
}
