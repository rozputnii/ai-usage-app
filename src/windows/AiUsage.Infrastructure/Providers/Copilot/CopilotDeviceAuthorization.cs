namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>A pending device authorization. The user code is shown to the owner; the device code never leaves the process.</summary>
public sealed class CopilotDeviceAuthorization
{
    internal CopilotDeviceAuthorization(string deviceCode, string userCode, Uri verificationUri, TimeSpan interval, DateTimeOffset expiresAt)
    {
        DeviceCode = deviceCode;
        UserCode = userCode;
        VerificationUri = verificationUri;
        Interval = interval;
        ExpiresAt = expiresAt;
    }

    internal string DeviceCode { get; }
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal bool Completed { get; set; }
    public string UserCode { get; }
    public Uri VerificationUri { get; }
    public TimeSpan Interval { get; }
    public DateTimeOffset ExpiresAt { get; }
    public override string ToString() => "CopilotDeviceAuthorization (redacted)";
}
