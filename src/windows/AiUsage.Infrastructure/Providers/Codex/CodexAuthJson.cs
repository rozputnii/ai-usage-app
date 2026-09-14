using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Codex;

internal sealed class DeviceCodeRequest
{
    [JsonPropertyName("client_id")] public required string ClientId { get; init; }
}

internal sealed class DevicePollRequest
{
    [JsonPropertyName("device_auth_id")] public required string DeviceAuthId { get; init; }
    [JsonPropertyName("user_code")] public required string UserCode { get; init; }
}

internal sealed class RefreshRequest
{
    [JsonPropertyName("client_id")] public required string ClientId { get; init; }
    [JsonPropertyName("grant_type")] public required string GrantType { get; init; }
    [JsonPropertyName("refresh_token")] public required string RefreshToken { get; init; }
}

[JsonSerializable(typeof(DeviceCodeRequest))]
[JsonSerializable(typeof(DevicePollRequest))]
[JsonSerializable(typeof(RefreshRequest))]
internal partial class CodexAuthJson : JsonSerializerContext;
