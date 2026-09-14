using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Claude;

internal sealed class ClaudeTokenRequest
{
    [JsonPropertyName("client_id")] public string ClientId { get; init; } = ClaudeHttp.ClientId;
    [JsonPropertyName("grant_type")] public required string GrantType { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("code_verifier")] public string? CodeVerifier { get; init; }
    [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; init; }
    [JsonPropertyName("state")] public string? State { get; init; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
    public override string ToString() => "ClaudeTokenRequest (redacted)";
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ClaudeTokenRequest))]
internal partial class ClaudeAuthJson : JsonSerializerContext;
