using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace AiUsage.Windows.Tests;

public sealed partial class ShellSmoke
{
    [Fact(Explicit = true)]
    public void PackagedAutomaticRefreshRetriesOfflineWithoutInput()
    {
        Assert.Equal("WDAGUtilityAccount", Environment.UserName);
        var root = Environment.GetEnvironmentVariable("AIU_RECOVERY_GUEST_ROOT")!;
        var evidence = Environment.GetEnvironmentVariable("AIU_SMOKE_EVIDENCE_DIRECTORY")!;
        Assert.StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "AiUsage.Dev_"), root);
        Directory.CreateDirectory(evidence);
        using var automation = new UIA3Automation();
        using var app = Application.LaunchStoreApp(Environment.GetEnvironmentVariable("AIU_SMOKE_AUMID")!);
        using var process = Process.GetProcessById(app.ProcessId);
        _ = process.Handle;
        Window? window = null;
        try
        {
            Assert.True(WaitUntil(() => (window = FindRecoveryWindow(automation, process.Id)) is not null, TimeSpan.FromSeconds(30)));
            // The first automatic request fails offline; the real desktop timer must retry
            // after the production ten-minute backoff. No clock or product test switch.
            var completed = new List<DateTimeOffset>();
            Assert.True(WaitUntil(() =>
            {
                completed.Clear();
                foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "logs"), "application-*.jsonl"))
                {
                    using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    while (reader.ReadLine() is { } line)
                    {
                        try
                        {
                            using var json = JsonDocument.Parse(line);
                            var record = json.RootElement;
                            if (record.GetProperty("processId").GetInt32() == process.Id &&
                                record.GetProperty("eventId").GetString() == "OperationCompleted" &&
                                record.GetProperty("operation").GetString() == "Refresh")
                            {
                                Assert.Equal("Failed", record.GetProperty("outcome").GetString());
                                completed.Add(record.GetProperty("timestamp").GetDateTimeOffset());
                            }
                        }
                        catch (JsonException) { /* Ignore a final record still being appended. */ }
                    }
                }
                return completed.Count >= 2;
            }, TimeSpan.FromMinutes(12)), "The installed app did not retry the offline synthetic account automatically.");
            Assert.True(completed.Max() - completed.Min() >= TimeSpan.FromMinutes(9));
            Capture(window!, evidence, "automatic-offline-retry");
            // The migrated cache has no period start, so the honest Not ready card
            // keeps the observed amount without inventing a complete budget caption.
            Assert.Contains(window!.FindAllDescendants(), element =>
                (element.Properties.Name.ValueOrDefault ?? "") == "25 used");
            Assert.Contains(window.FindAllDescendants(), element =>
                (element.Properties.Name.ValueOrDefault ?? "").Contains("sync failed", StringComparison.OrdinalIgnoreCase));
            File.WriteAllText(Path.Combine(evidence, "automatic-refresh.json"), JsonSerializer.Serialize(new
            { passed = true, packaged = true, syntheticOffline = true, attempts = completed.Count, elapsedSeconds = (completed.Max() - completed.Min()).TotalSeconds }));
            FocusForKeyboard(window!, window!, evidence, "automatic-exit");
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Q);
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
        }
        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
    }
}
