using System.Text.Json;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>The Claude Code sign-in an app account is bound to: no secret, only who is signed in.</summary>
internal sealed record ClaudeCodeIdentity(string Email, string OrganizationId)
{
    public override string ToString() => "ClaudeCodeIdentity (redacted)";

    internal static bool Safe(string? value) => value is { Length: > 0 and <= 512 } && !value.Any(char.IsControl);
}

internal sealed record ClaudeCodeAccount(ClaudeCodeIdentity Identity, string? Plan)
{
    public override string ToString() => "ClaudeCodeAccount (redacted)";

    /// <summary>Reads `claude auth status --json`; only a claude.ai subscription sign-in qualifies.</summary>
    internal static ClaudeCodeAccount Read(ClaudeCodeRun run)
    {
        try
        {
            using var document = JsonDocument.Parse(run.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("loggedIn", out var loggedIn) ||
                loggedIn.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new ProviderException(ProviderFailureKind.InvalidResponse);
            if (!loggedIn.GetBoolean() || Text(root, "authMethod") != "claude.ai")
                throw new ProviderException(ProviderFailureKind.AuthenticationRequired);
            var email = Text(root, "email");
            var organization = Text(root, "orgId");
            if (!ClaudeCodeIdentity.Safe(email) || !ClaudeCodeIdentity.Safe(organization))
                throw new ProviderException(ProviderFailureKind.InvalidResponse);
            return new(new(email!, organization!), PlanName(Text(root, "subscriptionType")));
        }
        catch (JsonException) { throw new ProviderException(ProviderFailureKind.InvalidResponse); }
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? PlanName(string? subscription) => subscription switch
    {
        "pro" => "Pro", "max" => "Max", "team" => "Team", "enterprise" => "Enterprise", _ => null
    };
}
