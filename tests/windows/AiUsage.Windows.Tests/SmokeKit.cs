namespace AiUsage.Windows.Tests;

/// <summary>Smoke helpers: per-launch tray identity.</summary>
internal static class SmokeKit
{
    /// <summary>OD-23: a development build started with this id names its tray icon "AI Usage &lt;id&gt;".</summary>
    internal const string TrayIdVariable = "AIU_SMOKE_TRAY_ID";

    /// <summary>A fresh 8-hex-digit id for one launched app, so its tray icon matches no other AI Usage instance.</summary>
    internal static string NewTrayId() => Guid.NewGuid().ToString("N")[..8];

    internal static string TrayName(string id) => "AI Usage " + id;
}
