using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Codex;

internal sealed class CodexDeviceAuthorization
{
    private static readonly Uri DeviceVerificationUri = new("https://auth.openai.com/codex/device");
    internal CodexDeviceAuthorization(string deviceAuthId, string userCode, TimeSpan interval, DateTimeOffset expiresAt)
    {
        DeviceAuthId = deviceAuthId;
        UserCode = userCode;
        PollInterval = interval;
        ExpiresAt = expiresAt;
    }

    internal string DeviceAuthId { get; }
    internal TimeSpan PollInterval { get; }
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal bool Completed { get; set; }
    public Uri VerificationUri => DeviceVerificationUri;
    [JsonIgnore] public string UserCode { get; }
    public DateTimeOffset ExpiresAt { get; }
    public override string ToString() => "CodexDeviceAuthorization (redacted)";
}
